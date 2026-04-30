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
    private bool _hasPreview;

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

    private void ParseFile()
    {
        if (string.IsNullOrEmpty(FilePath) || SelectedAccount == null) return;

        try
        {
            var cores = FileParser.ParseFile(FilePath);
            PreviewCores = new ObservableCollection<CorePreviewItem>(
                cores.Select(c => new CorePreviewItem { SerialNumber = c.SerialNumber, Content = c.Content }));
            HasPreview = cores.Count > 0;
            StatusMessage = $"成功解析 {cores.Count} 个核心梗";
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

        foreach (var item in PreviewCores)
        {
            var core = new NovelCore
            {
                AccountId = SelectedAccount.Id,
                SerialNumber = item.SerialNumber,
                Content = item.Content,
                GenerateContent = string.Empty,
                GenerateStatus = 0,
                CreateTime = DateTime.Now,
                GenerateTime = DateTime.Now,
                GenerateProgress = 0,
                CpId = SelectedCp?.Id
            };
            DbHelper.Db.Insertable(core).ExecuteCommand();
        }

        StatusMessage = $"已保存 {PreviewCores.Count} 个核心梗到账号 {SelectedAccount.AccountName}";
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

        var core = new NovelCore
        {
            AccountId = SelectedAccount.Id,
            SerialNumber = EditSerialNumber,
            Content = EditContent,
            GenerateContent = string.Empty,
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
}