using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AINovel.Helpers;
using AINovel.Models;
using AINovel.Services;
using HandyControl.Controls;

namespace AINovel.ViewModels;

public partial class GenerateViewModel : ViewModelBase
{
    private readonly SystemConfig _config;

    [ObservableProperty]
    private ObservableCollection<NovelCore> _cores = new();

    [ObservableProperty]
    private ObservableCollection<UserAccount> _accounts = new();

    [ObservableProperty]
    private ObservableCollection<CreativeProject> _projects = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    [NotifyCanExecuteChangedFor(nameof(PublishCommand))]
    private NovelCore? _selectedCore;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    [NotifyCanExecuteChangedFor(nameof(PublishCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCoreCommand))]
    private IList _selectedCores = new ArrayList();

    [ObservableProperty]
    private UserAccount? _filterAccount;

    [ObservableProperty]
    private CreativeProject? _filterCp;

    [ObservableProperty]
    private string _filterStatus = "未发布";

    [ObservableProperty]
    private string _selectedGenerateContent = string.Empty;

    [ObservableProperty]
    private bool _hasSelectedContent;

    [ObservableProperty]
    private string _selectedGenerateHtml = string.Empty;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _contentWordCount;

    public ObservableCollection<string> StatusFilters { get; } = new()
    {
        "全部", "未发布", "待生成", "等待生成", "生成中", "已生成", "生成失败", "已发布"
    };

    public GenerateViewModel(SystemConfig config)
    {
        _config = config;
        LoadData();

        WeakReferenceMessenger.Default.Register<GenerationStartedMessage>(this, (r, m) =>
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var (accountId, coreId) = m.Value;
                var core = Cores.FirstOrDefault(x => x.Id == coreId);
                if (core != null)
                {
                    core.GenerateStatus = 1;
                    RefreshCoreInGrid(core);
                }
                StatusMessage = $"核心梗【{core?.SerialNumber}】开始生成";
            });
        });

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
                    core.GenerateTime = DateTime.Now;
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

        WeakReferenceMessenger.Default.Register<QueueCompletedMessage>(this, (r, m) =>
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var (succeeded, failed) = m.Value;
                var total = succeeded + failed;
                var msg = $"生成完成：共 {total} 个，成功 {succeeded} 个";
                if (failed > 0) msg += $"，失败 {failed} 个";
                Growl.InfoGlobal(msg);
                StatusMessage = msg;
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
        if (accountList.Count > 0)
        {
            FilterAccount = accountList[0];
        }
        else
        {
            _ = RefreshCoresAsync();
        }
    }

    [RelayCommand]
    public async Task RefreshCoresAsync()
    {
        var query = DbHelper.Db.Queryable<NovelCore>();

        if (FilterAccount != null)
        {
            query = query.Where(x => x.AccountId == FilterAccount.Id);
        }

        if (FilterStatus == "未发布")
        {
            query = query.Where(x => x.GenerateStatus != 4);
        }
        else if (FilterStatus != "全部")
        {
            var status = FilterStatus switch
            {
                "待生成" => 0,
                "生成中" => 1,
                "已生成" => 2,
                "生成失败" => 3,
                "已发布" => 4,
                "等待生成" => 5,
                _ => -1
            };
            if (status >= 0)
            {
                query = query.Where(x => x.GenerateStatus == status);
            }
        }

        var list = await query.OrderByDescending(x => x.CreateTime).ToListAsync();

        // 应用 CP 筛选（内存筛选，因为可能已有其他 SQL 条件）
        if (FilterCp != null)
        {
            list = list.Where(x => x.CpId == FilterCp.Id).ToList();
        }

        Cores = new ObservableCollection<NovelCore>(list);
        SelectedCount = Cores.Count;
    }

    partial void OnFilterAccountChanged(UserAccount? value)
    {
        _ = OnFilterAccountChangedAsync(value);
    }

    private async Task OnFilterAccountChangedAsync(UserAccount? value)
    {
        Projects.Clear();
        if (value != null)
        {
            var cpList = await DbHelper.Db.Queryable<CreativeProject>()
                .Where(x => x.AccountId == value.Id)
                .ToListAsync();
            Projects = new ObservableCollection<CreativeProject>(cpList);
            FilterCp = cpList.FirstOrDefault();
        }
        else
        {
            FilterCp = null;
            await RefreshCoresAsync();
        }
    }

    partial void OnFilterCpChanged(CreativeProject? value)
    {
        _ = RefreshCoresAsync();
    }

    partial void OnFilterStatusChanged(string value)
    {
        _ = RefreshCoresAsync();
    }

    // 选中行时自动显示内容
    partial void OnSelectedCoreChanged(NovelCore? value)
    {
        if (value != null)
        {
            SelectedGenerateContent = value.GenerateContent ?? string.Empty;
            SelectedGenerateHtml = MarkdownHelper.ToHtml(SelectedGenerateContent);
            HasSelectedContent = !string.IsNullOrEmpty(SelectedGenerateContent);
            ContentWordCount = GetWordCount(SelectedGenerateContent);
        }
        else
        {
            SelectedGenerateContent = string.Empty;
            SelectedGenerateHtml = string.Empty;
            HasSelectedContent = false;
            ContentWordCount = 0;
        }
    }

    [RelayCommand]
    private void ViewContent()
    {
        if (SelectedCore == null) return;
        SelectedGenerateContent = SelectedCore.GenerateContent ?? string.Empty;
        SelectedGenerateHtml = MarkdownHelper.ToHtml(SelectedGenerateContent);
        HasSelectedContent = !string.IsNullOrEmpty(SelectedGenerateContent);
        ContentWordCount = GetWordCount(SelectedGenerateContent);
    }

    private static int GetWordCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Count(c => !char.IsWhiteSpace(c));
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        var cores = SelectedCores?.Cast<NovelCore>()
            .Where(x => x.GenerateStatus != 4 && x.GenerateStatus != 1
                        && !GenerationService.Instance.IsInQueue(x.Id))
            .ToList();

        if (cores == null || cores.Count == 0)
        {
            if (SelectedCore != null)
                StatusMessage = "选中的核心梗均无法生成（已发布、正在生成或在队列中）";
            return;
        }

        // 检查是否需要覆盖确认
        var hasExisting = cores.Any(x => x.GenerateStatus == 2);
        if (hasExisting)
        {
            var result = System.Windows.MessageBox.Show(
                "部分核心梗已生成内容，是否覆盖原有内容？",
                "确认覆盖",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        await GenerationService.Instance.EnqueueBatchAsync(cores, 1);
        foreach (var core in cores)
        {
            core.GenerateStatus = 5;
            core.GenerateTime = null;
            core.FailReason = "";
            RefreshCoreInGrid(core);
        }
        StatusMessage = $"已加入 {cores.Count} 个生成任务";
    }

    private bool CanGenerate() => SelectedCore != null || (SelectedCores?.Count > 0);

    [RelayCommand]
    private void ShowCoreDetail()
    {
        var core = SelectedCore;
        if (core == null) return;

        var window = new Views.CoreDetailWindow(core, this);
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }

    [RelayCommand(CanExecute = nameof(CanPublish))]
    private async Task PublishAsync()
    {
        var cores = SelectedCores?.Cast<NovelCore>()
            .Where(x => x.GenerateStatus == 2)
            .ToList();

        if (cores == null || cores.Count == 0)
        {
            StatusMessage = "选中的核心梗中没有可发布的（需要已生成状态）";
            return;
        }

        foreach (var core in cores)
        {
            await DbHelper.Db.Updateable<NovelCore>()
                .SetColumns(x => x.GenerateStatus == 4)
                .SetColumns(x => x.PublishTime == DateTime.Now)
                .SetColumns(x => x.Operator == "系统用户")
                .Where(x => x.Id == core.Id)
                .ExecuteCommandAsync();
        }

        StatusMessage = $"已发布 {cores.Count} 个核心梗";
        await RefreshCoresAsync();
    }

    private bool CanPublish() => SelectedCore != null || (SelectedCores?.Count > 0);

    [RelayCommand]
    private async Task DeleteCoreAsync()
    {
        var cores = SelectedCores?.Cast<NovelCore>().ToList();
        if (cores == null || cores.Count == 0) return;

        var msg = cores.Count == 1
            ? $"确定要删除核心梗【{cores[0].SerialNumber}】吗？"
            : $"确定要删除选中的 {cores.Count} 个核心梗吗？";

        var result = System.Windows.MessageBox.Show(
            msg, "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var ids = cores.Select(x => x.Id).ToList();
            await DbHelper.Db.Deleteable<NovelCore>()
                .Where(x => ids.Contains(x.Id))
                .ExecuteCommandAsync();

            StatusMessage = $"已删除 {cores.Count} 个核心梗";
            await RefreshCoresAsync();
        }
    }

    // ========== 复制操作 ==========

    private string[] GetContentLines()
    {
        return (SelectedCore?.GenerateContent ?? "")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }

    private static int FindLineIndex(string[] lines, string marker)
    {
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Trim().Contains(marker))
                return i;
        return -1;
    }

    [RelayCommand]
    private void CopyTitle()
    {
        if (SelectedCore == null) return;
        var lines = GetContentLines();
        var titleIdx = FindLineIndex(lines, "【标题】");
        if (titleIdx >= 0 && titleIdx + 1 < lines.Length)
        {
            var title = lines[titleIdx + 1].Trim();
            if (!string.IsNullOrEmpty(title))
            {
                SetClipboardFormatted(title);
                StatusMessage = "标题已复制到剪贴板";
                return;
            }
        }
        StatusMessage = "未找到标题内容";
    }

    [RelayCommand]
    private void CopyFreeContent()
    {
        if (SelectedCore == null) return;
        var lines = GetContentLines();
        var contentIdx = FindLineIndex(lines, "【正文】");
        var paidIdx = FindLineIndex(lines, "【付费卡点】");

        if (contentIdx < 0)
        {
            StatusMessage = "未找到【正文】标记";
            return;
        }

        var start = contentIdx + 1;
        var end = paidIdx >= 0 ? paidIdx : lines.Length;

        if (start >= end)
        {
            StatusMessage = "免费文内容为空";
            return;
        }

        var content = string.Join("\r\n", lines.Skip(start).Take(end - start)).Trim();
        if (!string.IsNullOrEmpty(content))
        {
            SetClipboardFormatted(content);
            StatusMessage = "免费文已复制到剪贴板";
        }
        else
        {
            StatusMessage = "免费文内容为空";
        }
    }

    [RelayCommand]
    private void CopyPaidContent()
    {
        if (SelectedCore == null) return;
        var lines = GetContentLines();
        var paidIdx = FindLineIndex(lines, "【付费卡点】");

        if (paidIdx < 0 || paidIdx + 1 >= lines.Length)
        {
            StatusMessage = "未找到付费内容";
            return;
        }

        var content = string.Join("\r\n", lines.Skip(paidIdx + 1)).Trim();
        if (!string.IsNullOrEmpty(content))
        {
            SetClipboardFormatted(content);
            StatusMessage = "付费文已复制到剪贴板";
        }
        else
        {
            StatusMessage = "付费内容为空";
        }
    }

    /// <summary>
    /// 同时写入 HTML 格式和纯文本格式到剪贴板，确保粘贴到富文本编辑器时保留样式
    /// </summary>
    private static void SetClipboardFormatted(string markdown)
    {
        var html = MarkdownHelper.ToClipboardHtml(markdown);
        var data = new DataObject();
        data.SetData(DataFormats.Html, html);
        data.SetText(markdown);
        Clipboard.SetDataObject(data, true);
    }

    public void Cleanup()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}