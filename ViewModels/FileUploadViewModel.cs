using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AINovel.Models;
using AINovel.Services;

namespace AINovel.ViewModels;

public partial class FileUploadViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<UserAccount> _accounts = new();

    [ObservableProperty]
    private UserAccount? _selectedAccount;

    [ObservableProperty]
    private ObservableCollection<CreativeProject> _cps = new();

    [ObservableProperty]
    private CreativeProject? _selectedCp;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CorePreviewItem> _previewCores = new();

    [ObservableProperty]
    private CorePreviewItem? _selectedPreviewCore;

    [ObservableProperty]
    private bool _hasPreview;

    [ObservableProperty]
    private bool _hasSelectedPreview;

    [ObservableProperty]
    private string _editSerialNumber = string.Empty;

    [ObservableProperty]
    private string _editContent = string.Empty;

    [ObservableProperty]
    private bool _isAddingManual;

    public FileUploadViewModel()
    {
        LoadAccounts();
    }

    private void LoadAccounts()
    {
        var list = DbHelper.Db.Queryable<UserAccount>().Where(x => x.IsEnable).ToList();
        Accounts = new ObservableCollection<UserAccount>(list);

        if (Accounts.Count > 0)
        {
            SelectedAccount = Accounts[0];
        }
    }

    private void LoadCps()
    {
        Cps.Clear();
        SelectedCp = null;

        if (SelectedAccount == null) return;

        var list = DbHelper.Db.Queryable<CreativeProject>()
            .Where(x => x.AccountId == SelectedAccount.Id)
            .ToList();
        Cps = new ObservableCollection<CreativeProject>(list);
    }

    partial void OnSelectedAccountChanged(UserAccount? value)
    {
        LoadCps();
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "支持的文件格式|*.md;*.docx|Markdown文件|*.md|Word文档|*.docx",
            Title = "选择文件"
        };

        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FileName;
            ParseFile();
        }
    }

    private async void ParseFile()
    {
        if (string.IsNullOrEmpty(FilePath) || SelectedAccount == null) return;

        var filePath = FilePath;
        StatusMessage = "正在解析文件...";

        try
        {
            var cores = await Task.Run(() => FileParser.ParseFile(filePath));

            PreviewCores = new ObservableCollection<CorePreviewItem>(
                cores.Select(c => new CorePreviewItem { SerialNumber = c.SerialNumber, Content = c.Content }));
            HasPreview = cores.Count > 0;
            StatusMessage = HasPreview
                ? $"成功解析 {cores.Count} 个核心梗"
                : "文件中未找到核心梗，请检查格式（【数字】）";
        }
        catch (Exception ex)
        {
            StatusMessage = $"解析失败: {ex.Message}";
            HasPreview = false;
        }
    }

    [RelayCommand]
    private void SaveToAccount()
    {
        if (SelectedAccount == null)
        {
            StatusMessage = "请先选择账号";
            return;
        }

        if (PreviewCores.Count == 0)
        {
            StatusMessage = "没有可保存的核心梗";
            return;
        }

        var accountId = SelectedAccount.Id;
        var cpId = SelectedCp?.Id;

        // 查询已有序号，避免重复
        var existingSerials = DbHelper.Db.Queryable<NovelCore>()
            .Where(x => x.AccountId == accountId)
            .Select(x => x.SerialNumber)
            .ToList();

        var coresToInsert = new List<NovelCore>();
        var duplicateCount = 0;

        foreach (var item in PreviewCores)
        {
            if (existingSerials.Contains(item.SerialNumber))
            {
                duplicateCount++;
                continue;
            }

            coresToInsert.Add(new NovelCore
            {
                AccountId = accountId,
                SerialNumber = item.SerialNumber,
                Content = item.Content,
                GenerateStatus = 0,
                CreateTime = DateTime.Now,
                GenerateProgress = 0,
                CpId = cpId
            });
        }

        if (coresToInsert.Count == 0)
        {
            StatusMessage = "所有核心梗均已存在，无新数据保存";
            return;
        }

        // 批量插入
        DbHelper.Db.Insertable(coresToInsert).ExecuteCommand();

        var msg = $"已保存 {coresToInsert.Count} 个核心梗到账号 {SelectedAccount.AccountName}";
        if (duplicateCount > 0)
        {
            msg += $"，{duplicateCount} 个重复序号已跳过";
        }

        StatusMessage = msg;
        PreviewCores.Clear();
        HasPreview = false;
        FilePath = string.Empty;
    }

    [RelayCommand]
    private void AddManualCore()
    {
        if (SelectedAccount == null)
        {
            StatusMessage = "请先选择账号";
            return;
        }

        EditSerialNumber = string.Empty;
        EditContent = string.Empty;
        IsAddingManual = true;
    }

    [RelayCommand]
    private void SaveManualCore()
    {
        if (SelectedAccount == null)
        {
            StatusMessage = "请先选择账号";
            return;
        }

        if (!FileParser.ValidateSerialNumber(EditSerialNumber))
        {
            StatusMessage = "序号格式不正确，应为【数字】格式，如【001】";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditContent))
        {
            StatusMessage = "核心梗内容不能为空";
            return;
        }

        // 检查重复序号
        var exists = DbHelper.Db.Queryable<NovelCore>()
            .Any(x => x.AccountId == SelectedAccount.Id && x.SerialNumber == EditSerialNumber);

        if (exists)
        {
            StatusMessage = $"序号 {EditSerialNumber} 已存在，请使用其他序号";
            return;
        }

        var core = new NovelCore
        {
            AccountId = SelectedAccount.Id,
            SerialNumber = EditSerialNumber,
            Content = EditContent,
            GenerateStatus = 0,
            CreateTime = DateTime.Now,
            GenerateProgress = 0,
            CpId = SelectedCp?.Id
        };
        DbHelper.Db.Insertable(core).ExecuteCommand();

        StatusMessage = "核心梗添加成功";
        IsAddingManual = false;
    }

    [RelayCommand]
    private void CancelManualAdd()
    {
        IsAddingManual = false;
    }

    partial void OnSelectedPreviewCoreChanged(CorePreviewItem? value)
    {
        HasSelectedPreview = value != null;
    }

    [RelayCommand]
    private void ShowPreviewDetail()
    {
        var item = SelectedPreviewCore;
        if (item == null) return;

        var info = $"序号: {item.SerialNumber}\n\n完整内容:\n{item.Content}";
        System.Windows.MessageBox.Show(info, "核心梗详情", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
