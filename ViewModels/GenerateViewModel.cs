using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AINovel.Models;
using AINovel.Services;

namespace AINovel.ViewModels;

public partial class GenerateViewModel : ViewModelBase
{
    private readonly SystemConfig _config;

    [ObservableProperty]
    private ObservableCollection<NovelCore> _cores = new();

    [ObservableProperty]
    private ObservableCollection<UserAccount> _accounts = new();

    [ObservableProperty]
    private NovelCore? _selectedCore;

    [ObservableProperty]
    private UserAccount? _filterAccount;

    [ObservableProperty]
    private string _filterStatus = "全部";

    [ObservableProperty]
    private string _selectedGenerateContent = string.Empty;

    [ObservableProperty]
    private bool _hasSelectedContent;

    [ObservableProperty]
    private int _selectedCount;

    public ObservableCollection<string> StatusFilters { get; } = new()
    {
        "全部", "待生成", "生成中", "已生成", "生成失败", "已发布"
    };

    public GenerateViewModel(SystemConfig config)
    {
        _config = config;
        LoadData();

        WeakReferenceMessenger.Default.Register<GenerationCompletedMessage>(this, (r, m) =>
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var (accountId, coreId, content) = m.Value;
                var core = Cores.FirstOrDefault(x => x.Id == coreId);
                if (core != null)
                {
                    core.GenerateStatus = 2;
                    core.GenerateContent = content;
                    RefreshCoreInGrid(core);
                }
                StatusMessage = $"核心梗【{core?.SerialNumber}】生成完成";
            });
        });

        WeakReferenceMessenger.Default.Register<GenerationFailedMessage>(this, (r, m) =>
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var (accountId, coreId, reason) = m.Value;
                var core = Cores.FirstOrDefault(x => x.Id == coreId);
                if (core != null)
                {
                    core.GenerateStatus = 3;
                    core.FailReason = reason;
                    RefreshCoreInGrid(core);
                }
                StatusMessage = $"核心梗【{core?.SerialNumber}】生成失败: {reason}";
            });
        });
    }

    private void RefreshCoreInGrid(NovelCore core)
    {
        var idx = Cores.IndexOf(core);
        if (idx >= 0)
        {
            Cores.RemoveAt(idx);
            Cores.Insert(idx, core);
        }
    }

    private void LoadData()
    {
        var accountList = DbHelper.Db.Queryable<UserAccount>().ToList();
        Accounts = new ObservableCollection<UserAccount>(accountList);
        RefreshCores();
    }

    [RelayCommand]
    private void RefreshCores()
    {
        var query = DbHelper.Db.Queryable<NovelCore>();

        if (FilterAccount != null)
        {
            query = query.Where(x => x.AccountId == FilterAccount.Id);
        }

        if (FilterStatus != "全部")
        {
            var status = FilterStatus switch
            {
                "待生成" => 0,
                "生成中" => 1,
                "已生成" => 2,
                "生成失败" => 3,
                "已发布" => 4,
                _ => -1
            };
            if (status >= 0)
            {
                query = query.Where(x => x.GenerateStatus == status);
            }
        }

        var list = query.OrderByDescending(x => x.CreateTime).ToList();
        Cores = new ObservableCollection<NovelCore>(list);
        SelectedCount = Cores.Count;
    }

    partial void OnFilterAccountChanged(UserAccount? value) => RefreshCores();
    partial void OnFilterStatusChanged(string value) => RefreshCores();

    // 选中行时自动显示内容
    partial void OnSelectedCoreChanged(NovelCore? value)
    {
        if (value != null)
        {
            SelectedGenerateContent = value.GenerateContent ?? string.Empty;
            HasSelectedContent = !string.IsNullOrEmpty(SelectedGenerateContent);
        }
    }

    [RelayCommand]
    private void ViewContent()
    {
        if (SelectedCore == null) return;
        SelectedGenerateContent = SelectedCore.GenerateContent ?? string.Empty;
        HasSelectedContent = !string.IsNullOrEmpty(SelectedGenerateContent);
    }

    [RelayCommand]
    private void Generate()
    {
        if (SelectedCore == null) return;

        if (SelectedCore.GenerateStatus == 4)
        {
            StatusMessage = "已发布状态的核心梗不能直接生成，请先修改状态";
            return;
        }

        if (SelectedCore.GenerateStatus == 1)
        {
            StatusMessage = "核心梗正在生成中，请勿重复触发";
            return;
        }

        if (SelectedCore.GenerateStatus == 2)
        {
            var result = MessageBox.Show(
                "该核心梗已生成内容，是否覆盖原有内容？",
                "确认覆盖",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        GenerationService.Instance.EnqueueRequest(SelectedCore, 1);
        StatusMessage = $"核心梗【{SelectedCore.SerialNumber}】生成任务已加入队列";
    }

    [RelayCommand]
    private void RetryGenerate()
    {
        if (SelectedCore == null || SelectedCore.GenerateStatus != 3) return;
        GenerationService.Instance.EnqueueRequest(SelectedCore, 1);
        StatusMessage = $"核心梗【{SelectedCore.SerialNumber}】重试任务已加入队列";
    }

    [RelayCommand]
    private void Publish()
    {
        if (SelectedCore == null) return;

        DbHelper.Db.Updateable<NovelCore>()
            .SetColumns(x => x.GenerateStatus == 4)
            .SetColumns(x => x.PublishTime == DateTime.Now)
            .SetColumns(x => x.Operator == "系统用户")
            .Where(x => x.Id == SelectedCore.Id)
            .ExecuteCommand();

        StatusMessage = $"核心梗【{SelectedCore.SerialNumber}】已发布";
        RefreshCores();
    }

    [RelayCommand]
    private void BatchGenerate()
    {
        var pendingCores = Cores.Where(x => x.GenerateStatus == 0 || x.GenerateStatus == 2).ToList();
        if (!pendingCores.Any())
        {
            StatusMessage = "当前列表中没有可生成的核心梗（待生成或已生成状态）";
            return;
        }

        GenerationService.Instance.EnqueueBatch(pendingCores, 1);
        StatusMessage = $"已加入 {pendingCores.Count} 个生成任务";
    }

    [RelayCommand]
    private void BatchPublish()
    {
        var generatedCores = Cores.Where(x => x.GenerateStatus == 2).ToList();
        if (!generatedCores.Any())
        {
            StatusMessage = "当前列表中没有已生成的核心梗";
            return;
        }

        var count = 0;
        foreach (var core in generatedCores)
        {
            DbHelper.Db.Updateable<NovelCore>()
                .SetColumns(x => x.GenerateStatus == 4)
                .SetColumns(x => x.PublishTime == DateTime.Now)
                .Where(x => x.Id == core.Id)
                .ExecuteCommand();
            count++;
        }

        StatusMessage = $"已发布 {count} 个核心梗";
        RefreshCores();
    }

    [RelayCommand]
    private void BatchRetry()
    {
        var failedCores = Cores.Where(x => x.GenerateStatus == 3).ToList();
        if (!failedCores.Any())
        {
            StatusMessage = "当前列表中没有生成失败的核心梗";
            return;
        }

        GenerationService.Instance.EnqueueBatch(failedCores, 1);
        StatusMessage = $"已加入 {failedCores.Count} 个重试任务";
    }

    [RelayCommand]
    private void DeleteCore()
    {
        if (SelectedCore == null) return;

        var result = MessageBox.Show(
            $"确定要删除核心梗【{SelectedCore.SerialNumber}】吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            DbHelper.Db.Deleteable<NovelCore>()
                .Where(x => x.Id == SelectedCore.Id)
                .ExecuteCommand();

            StatusMessage = "核心梗已删除";
            RefreshCores();
        }
    }

    public void Cleanup()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}