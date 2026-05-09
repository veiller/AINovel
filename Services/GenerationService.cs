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
    private Channel<GenerationRequest> _channel;
    private CancellationTokenSource _autoLoopCts;
    private readonly List<Task> _workers;
    private volatile int _targetWorkerCount;
    private volatile int _currentWorkerCount;
    private readonly object _workerLock = new();
    private bool _isRunning;
    private int _pendingCount;
    private int _totalSucceeded;
    private int _totalFailed;
    private readonly ConcurrentDictionary<int, byte> _pendingSet = new();

    public event Action<int, int, string>? GenerationCompleted;
    public event Action<int, int, string>? GenerationFailed;

    /// <summary>检查指定核心梗是否正在队列中等待（尚未开始处理）</summary>
    public bool IsInQueue(int coreId) => _pendingSet.ContainsKey(coreId);

    private GenerationService()
    {
        _gptService = new GptService();
        _autoLoopCts = new CancellationTokenSource();
        _workers = new List<Task>();
        _targetWorkerCount = 2;
        _channel = CreateChannel();
    }

    private static Channel<GenerationRequest> CreateChannel()
    {
        return Channel.CreateUnbounded<GenerationRequest>(new UnboundedChannelOptions
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

    public async Task EnqueueRequestAsync(NovelCore core, int generateType)
    {
        EnsureWorkers();

        // 更新状态为"等待生成"并清空生成时间和错误信息
        await DbHelper.Db.Updateable<NovelCore>()
            .SetColumns(x => x.GenerateStatus == 5)
            .SetColumns(x => x.GenerateTime == null)
            .SetColumns(x => x.FailReason == "")
            .Where(x => x.Id == core.Id)
            .ExecuteCommandAsync();

        Interlocked.Increment(ref _pendingCount);
        _pendingSet.TryAdd(core.Id, 0);
        _channel.Writer.TryWrite(new GenerationRequest(core, generateType));
    }

    public async Task EnqueueBatchAsync(IEnumerable<NovelCore> cores, int generateType)
    {
        EnsureWorkers();
        var count = 0;
        foreach (var core in cores)
        {
            // 更新状态为"等待生成"并清空生成时间和错误信息
            await DbHelper.Db.Updateable<NovelCore>()
                .SetColumns(x => x.GenerateStatus == 5)
                .SetColumns(x => x.GenerateTime == (DateTime?)null)
                .SetColumns(x => x.FailReason == "")
                .Where(x => x.Id == core.Id)
                .ExecuteCommandAsync();

            _pendingSet.TryAdd(core.Id, 0);
            _channel.Writer.TryWrite(new GenerationRequest(core, generateType));
            count++;
        }
        Interlocked.Add(ref _pendingCount, count);
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

        if (_channel.Reader.Completion.IsCompleted)
        {
            _channel = CreateChannel();
        }

        Task.Run(() => AutoGenerationLoopAsync());
    }

    public void Stop()
    {
        _isRunning = false;
        _autoLoopCts.Cancel();

        _channel.Writer.TryComplete();

        lock (_workerLock)
        {
            _currentWorkerCount = 0;
        }
    }

    // ========== Worker 管理 ==========

    private void EnsureWorkers()
    {
        if (_currentWorkerCount == 0)
        {
            lock (_workerLock)
            {
                if (_currentWorkerCount == 0)
                {
                    if (_channel.Reader.Completion.IsCompleted)
                    {
                        _channel = CreateChannel();
                    }
                    AdjustWorkers();
                }
            }
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
            catch (OperationCanceledException)
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
            // 从待处理集合中移除（开始处理）
            _pendingSet.TryRemove(core.Id, out _);

            await DbHelper.Db.Updateable<NovelCore>()
                .SetColumns(x => x.GenerateStatus == 1)
                .SetColumns(x => x.GenerateProgress == 0)
                .Where(x => x.Id == core.Id)
                .ExecuteCommandAsync();

            WeakReferenceMessenger.Default.Send(
                new GenerationStartedMessage((core.AccountId, core.Id)));

            var prompt = GetPrompt(core);

            var content = await _gptService.GenerateAsync(
                config.GptApiUrl,
                config.GptApiKey,
                prompt,
                config.GptModel,
                config.ApiTimeout);

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
            Interlocked.Increment(ref _totalSucceeded);
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
            Interlocked.Increment(ref _totalFailed);
        }

        if (Interlocked.Decrement(ref _pendingCount) == 0)
        {
            var succeeded = Interlocked.Exchange(ref _totalSucceeded, 0);
            var failed = Interlocked.Exchange(ref _totalFailed, 0);
            WeakReferenceMessenger.Default.Send(new QueueCompletedMessage((succeeded, failed)));
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

                    var accountTotalEnqueued = 0;
                    var cps = DbHelper.Db.Queryable<CreativeProject>()
                        .Where(x => x.AccountId == account.Id)
                        .ToList();

                    // 按 CP 分组处理
                    foreach (var cp in cps)
                    {
                        if (!_isRunning || _autoLoopCts.IsCancellationRequested) break;

                        var pipelineCount = DbHelper.Db.Queryable<NovelCore>()
                            .Where(x => x.AccountId == account.Id && x.CpId == cp.Id
                                        && x.GenerateStatus != 0 && x.GenerateStatus != 4)
                            .Count();

                        if (pipelineCount < config.MinWaitGenerateCount)
                        {
                            var takeCount = config.MinWaitGenerateCount - pipelineCount;
                            var cores = DbHelper.Db.Queryable<NovelCore>()
                                .Where(x => x.AccountId == account.Id && x.CpId == cp.Id && x.GenerateStatus == 0)
                                .OrderBy(x => x.CreateTime)
                                .Take(takeCount)
                                .ToList();

                            foreach (var core in cores)
                            {
                                if (!_isRunning || _autoLoopCts.IsCancellationRequested) break;
                                await EnqueueRequestAsync(core, 0);
                                accountTotalEnqueued++;
                            }
                        }
                    }

                    // 处理没有关联 CP 的核心梗
                    if (!_isRunning || _autoLoopCts.IsCancellationRequested) continue;

                    var noCpPipelineCount = DbHelper.Db.Queryable<NovelCore>()
                        .Where(x => x.AccountId == account.Id && x.CpId == null
                                    && x.GenerateStatus != 0 && x.GenerateStatus != 4)
                        .Count();

                    if (noCpPipelineCount < config.MinWaitGenerateCount)
                    {
                        var takeCount = config.MinWaitGenerateCount - noCpPipelineCount;
                        var cores = DbHelper.Db.Queryable<NovelCore>()
                            .Where(x => x.AccountId == account.Id && x.CpId == null && x.GenerateStatus == 0)
                            .OrderBy(x => x.CreateTime)
                            .Take(takeCount)
                            .ToList();

                        foreach (var core in cores)
                        {
                            if (!_isRunning || _autoLoopCts.IsCancellationRequested) break;
                            await EnqueueRequestAsync(core, 0);
                            accountTotalEnqueued++;
                        }
                    }

                    if (accountTotalEnqueued > 0)
                    {
                        var accountName = account.AccountName;
                        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            HandyControl.Controls.Growl.InfoGlobal(
                                $"自动生成：账号【{accountName}】已加入 {accountTotalEnqueued} 个生成任务"));
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

        return $"请根据以下核心梗生成小说:\n\n{core.Content}";
    }
}
