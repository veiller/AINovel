using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AINovel.Models;
using AINovel.Services;

namespace AINovel.ViewModels;

public partial class CpViewModel : ViewModelBase
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
    private ObservableCollection<CommonPrompt> _commonPrompts = new();

    [ObservableProperty]
    private ObservableCollection<AccountPrompt> _accountPrompts = new();

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private int _editPromptType; // 0=公共提示词, 1=账号私有提示词

    [ObservableProperty]
    private CommonPrompt? _selectedCommonPrompt;

    [ObservableProperty]
    private AccountPrompt? _selectedAccountPrompt;

    public CpViewModel()
    {
        LoadData();
    }

    private void LoadData()
    {
        var accountList = DbHelper.Db.Queryable<UserAccount>().ToList();
        Accounts = new ObservableCollection<UserAccount>(accountList);

        var commonList = DbHelper.Db.Queryable<CommonPrompt>().ToList();
        CommonPrompts = new ObservableCollection<CommonPrompt>(commonList);

        if (SelectedAccount != null)
        {
            LoadAccountPrompts();
            LoadCps();
        }
        else if (Accounts.Count > 0)
        {
            SelectedAccount = Accounts[0];
        }
    }

    private void LoadAccountPrompts()
    {
        if (SelectedAccount == null)
        {
            AccountPrompts.Clear();
            return;
        }

        var list = DbHelper.Db.Queryable<AccountPrompt>()
            .Where(x => x.AccountId == SelectedAccount.Id)
            .ToList();
        AccountPrompts = new ObservableCollection<AccountPrompt>(list);
    }

    private void LoadCps()
    {
        if (SelectedAccount == null)
        {
            Cps.Clear();
            return;
        }

        var list = DbHelper.Db.Queryable<CreativeProject>()
            .Where(x => x.AccountId == SelectedAccount.Id)
            .OrderByDescending(x => x.CreateTime)
            .ToList();
        Cps = new ObservableCollection<CreativeProject>(list);
    }

    partial void OnSelectedAccountChanged(UserAccount? value)
    {
        LoadAccountPrompts();
        LoadCps();
    }

    partial void OnEditPromptTypeChanged(int value)
    {
        SelectedCommonPrompt = null;
        SelectedAccountPrompt = null;
    }

    [RelayCommand]
    private void AddCp()
    {
        if (SelectedAccount == null)
        {
            StatusMessage = "请先选择账号";
            return;
        }
        SelectedCp = null;
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditPromptType = 0;
        SelectedCommonPrompt = null;
        SelectedAccountPrompt = null;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditCp()
    {
        if (SelectedCp == null) return;
        EditName = SelectedCp.Name;
        EditDescription = SelectedCp.Description ?? string.Empty;
        EditPromptType = 0;
        SelectedCommonPrompt = null;
        SelectedAccountPrompt = null;

        if (SelectedCp.PromptId != null)
        {
            var commonPrompt = CommonPrompts.FirstOrDefault(x => x.Id == SelectedCp.PromptId);
            if (commonPrompt != null)
            {
                EditPromptType = 0;
                SelectedCommonPrompt = commonPrompt;
            }
            else
            {
                var accountPrompt = AccountPrompts.FirstOrDefault(x => x.Id == SelectedCp.PromptId);
                if (accountPrompt != null)
                {
                    EditPromptType = 1;
                    SelectedAccountPrompt = accountPrompt;
                }
            }
        }

        IsEditing = true;
    }

    [RelayCommand]
    private void SaveCp()
    {
        if (SelectedAccount == null)
        {
            StatusMessage = "请先选择账号";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditName))
        {
            StatusMessage = "CP名称不能为空";
            return;
        }

        int? promptId = null;
        if (EditPromptType == 0 && SelectedCommonPrompt != null)
        {
            promptId = SelectedCommonPrompt.Id;
        }
        else if (EditPromptType == 1 && SelectedAccountPrompt != null)
        {
            promptId = SelectedAccountPrompt.Id;
        }

        if (SelectedCp == null)
        {
            var cp = new CreativeProject
            {
                AccountId = SelectedAccount.Id,
                Name = EditName,
                Description = EditDescription,
                PromptId = promptId,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            DbHelper.Db.Insertable(cp).ExecuteCommand();
            StatusMessage = "CP添加成功";
        }
        else
        {
            SelectedCp.Name = EditName;
            SelectedCp.Description = EditDescription;
            SelectedCp.PromptId = promptId;
            SelectedCp.UpdateTime = DateTime.Now;
            DbHelper.Db.Updateable(SelectedCp).ExecuteCommand();
            StatusMessage = "CP更新成功";
        }

        IsEditing = false;
        LoadCps();
    }

    [RelayCommand]
    private void DeleteCp()
    {
        if (SelectedCp == null) return;

        var result = MessageBox.Show(
            $"确定要删除CP「{SelectedCp.Name}」吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        DbHelper.Db.Deleteable<CreativeProject>()
            .Where(x => x.Id == SelectedCp.Id)
            .ExecuteCommand();
        StatusMessage = "CP删除成功";
        LoadCps();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }
}
