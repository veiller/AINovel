using System.Collections.Concurrent;
using System.Threading.Channels;
using AINovel.Models;
using AINovel.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace AINovel.Services;

public class GenerationService
{
    private static readonly Lazy<GenerationService> _instance = new(() => new GenerationService());
    public static GenerationService Instance => _instance.Value;

    private readonly GptService _gptService;
    private readonly Channel<GenerationRequest> _channel;
    private CancellationTokenSource _autoLoopCts;
    private readonly List<Task> _workers;
    private volatile int _targetWorkerCount;
    private volatile int _currentWorkerCount;
    private readonly object _workerLock = new();
    private bool _isRunning;

    public event Action<int, int, int>? ProgressChanged;
    public event Action<int, int, string>? GenerationCompleted;
    public event Action<int, int, string>? GenerationFailed;

    private GenerationService()
    {
        _gptService = new GptService();
        _autoLoopCts = new CancellationTokenSource();
        _workers = new List<Task>();
        _targetWorkerCount = 2;

        _channel = Channel.CreateUnbounded<GenerationRequest>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    private static SystemConfig GetConfig()
    {
        return DbHelper.Db.Queryable<SystemConfig>().First() ?? new SystemConfig();
    }

    // ========== Public API ==========

    public void EnqueueRequest(NovelCore core, int generateType)
    {
        EnsureWorkers();
        _channel.Writer.TryWrite(new GenerationRequest(core, generateType));
    }

    public void EnqueueBatch(IEnumerable<NovelCore> cores, int generateType)
    {
        EnsureWorkers();
        foreach (var core in cores)
        {
            _channel.Writer.TryWrite(new GenerationRequest(core, generateType));
        }
    }

    public void UpdateThreadCount(int count)
    {
        if (count < 1) count = 1;
        _targetWorkerCount = count;
        AdjustWorkers();
    }

    public void StartAutoGeneration()
    {
        if (_isRunning) return;
        _isRunning = true;

        _autoLoopCts.Cancel();
        _autoLoopCts = new CancellationTokenSource();

        Task.Run(() => AutoGenerationLoopAsync());
    }

    public void Stop()
    {
        _isRunning = false;
        _autoLoopCts.Cancel();

        // 清空通道中等待的请求（不取消正在执行的 worker）
        while (_channel.Reader.TryRead(out _)) { }
    }

    // ========== Worker 管理 ==========

    private void EnsureWorkers()
    {
        if (_currentWorkerCount == 0)
        {
            AdjustWorkers();
        }
    }

    private void AdjustWorkers()
    {
        lock (_workerLock)
        {
            while (_currentWorkerCount < _targetWorkerCount)
            {
                var index = _currentWorkerCount;
                _currentWorkerCount++;
                var task = Task.Run(() => WorkerLoopAsync(index));
                _workers.Add(task);
            }
        }
    }

    private async Task WorkerLoopAsync(int workerIndex)
    {
        while (workerIndex < _targetWorkerCount)
        {
            try
            {
                var request = await _channel.Reader.ReadAsync();
                await ProcessRequestAsync(request);
            }
            catch (ChannelClosedException)
            {
                break;
            }
        }

        lock (_workerLock)
        {
            _currentWorkerCount--;
        }
    }

    // ========== 核心处理 ==========

    private async Task ProcessRequestAsync(GenerationRequest request)
    {
        var core = request.Core;
        var generateType = request.GenerateType;
        var config = GetConfig();

        try
        {
            // 更新状态为生成中
            await DbHelper.Db.Updateable<NovelCore>()
                .SetColumns(x => x.GenerateStatus == 1)
                .SetColumns(x => x.GenerateProgress == 0)
                .Where(x => x.Id == core.Id)
                .ExecuteCommandAsync();

            // 获取提示词
            var prompt = GetPrompt(core);

            // 调用GPT生成
            var content = await _gptService.GenerateAsync(
                config.GptApiUrl,
                config.GptApiKey,
                prompt,
                config.GptModel,
                config.ApiTimeout);

            // 更新生成完成
            DbHelper.Db.Updateable<NovelCore>()
                .SetColumns(x => x.GenerateStatus == 2)
                .SetColumns(x => x.GenerateContent == content)
                .SetColumns(x => x.GenerateTime == DateTime.Now)
                .SetColumns(x => x.GenerateProgress == 100)
                .SetColumns(x => x.GenerateType == generateType)
                .Where(x => x.Id == core.Id)
                .ExecuteCommand();

            GenerationCompleted?.Invoke(core.AccountId, core.Id, content);
            WeakReferenceMessenger.Default.Send(
                new GenerationCompletedMessage((core.AccountId, core.Id, content)));
        }
        catch (Exception ex)
        {
            DbHelper.Db.Updateable<NovelCore>()
                .SetColumns(x => x.GenerateStatus == 3)
                .SetColumns(x => x.FailReason == ex.Message)
                .Where(x => x.Id == core.Id)
                .ExecuteCommand();

            GenerationFailed?.Invoke(core.AccountId, core.Id, ex.Message);
            WeakReferenceMessenger.Default.Send(
                new GenerationFailedMessage((core.AccountId, core.Id, ex.Message)));
        }
    }

    // ========== 自动生成循环 ==========

    private async Task AutoGenerationLoopAsync()
    {
        while (_isRunning && !_autoLoopCts.IsCancellationRequested)
        {
            try
            {
                var config = GetConfig();
                var accounts = DbHelper.Db.Queryable<UserAccount>().Where(x => x.IsEnable).ToList();

                foreach (var account in accounts)
                {
                    if (!_isRunning || _autoLoopCts.IsCancellationRequested) break;

                    var waitCount = DbHelper.Db.Queryable<NovelCore>()
                        .Where(x => x.AccountId == account.Id && x.GenerateStatus == 0)
                        .Count();

                    if (waitCount < config.MinWaitGenerateCount && waitCount > 0)
                    {
                        var cores = DbHelper.Db.Queryable<NovelCore>()
                            .Where(x => x.AccountId == account.Id && x.GenerateStatus == 0)
                            .OrderBy(x => x.CreateTime)
                            .Take(1)
                            .ToList();

                        foreach (var core in cores)
                        {
                            if (!_isRunning || _autoLoopCts.IsCancellationRequested) break;
                            EnqueueRequest(core, 0);
                        }
                    }
                }

                await Task.Delay(5000, _autoLoopCts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    // ========== 提示词解析 ==========

    private static string GetPrompt(NovelCore core)
    {
        // 通过 CP 获取提示词
        if (core.CpId.HasValue)
        {
            var cp = DbHelper.Db.Queryable<CreativeProject>()
                .Where(x => x.Id == core.CpId.Value)
                .First();

            if (cp?.PromptId != null)
            {
                var accountPrompt = DbHelper.Db.Queryable<AccountPrompt>()
                    .Where(x => x.Id == cp.PromptId.Value)
                    .First();
                if (accountPrompt != null)
                    return accountPrompt.Content.Replace("<在此填入用户提供的核心梗内容>", core.Content);

                var commonPrompt = DbHelper.Db.Queryable<CommonPrompt>()
                    .Where(x => x.Id == cp.PromptId.Value)
                    .First();
                if (commonPrompt != null)
                    return commonPrompt.Content.Replace("<在此填入用户提供的核心梗内容>", core.Content);
            }
        }

        // 回退
        return $"请根据以下核心梗生成小说:\n\n{core.Content}";
    }
}