using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AINovel.Models;
using AINovel.Services;

namespace AINovel.ViewModels;

public partial class PromptViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<CommonPrompt> _commonPrompts = new();

    [ObservableProperty]
    private ObservableCollection<AccountPrompt> _accountPrompts = new();

    [ObservableProperty]
    private ObservableCollection<UserAccount> _accounts = new();

    [ObservableProperty]
    private CommonPrompt? _selectedCommonPrompt;

    [ObservableProperty]
    private AccountPrompt? _selectedAccountPrompt;

    [ObservableProperty]
    private UserAccount? _selectedAccount;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private string _editContent = string.Empty;

    [ObservableProperty]
    private bool _editIsDefault;

    public PromptViewModel()
    {
        LoadData();
    }

    private void LoadData()
    {
        var commonList = DbHelper.Db.Queryable<CommonPrompt>().OrderByDescending(x => x.CreateTime).ToList();
        CommonPrompts = new ObservableCollection<CommonPrompt>(commonList);

        var accountList = DbHelper.Db.Queryable<UserAccount>().ToList();
        Accounts = new ObservableCollection<UserAccount>(accountList);

        if (SelectedAccount != null)
        {
            LoadAccountPrompts();
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
            .OrderByDescending(x => x.CreateTime)
            .ToList();
        AccountPrompts = new ObservableCollection<AccountPrompt>(list);
    }

    partial void OnSelectedAccountChanged(UserAccount? value)
    {
        LoadAccountPrompts();
    }

    [RelayCommand]
    private void AddCommonPrompt()
    {
        SelectedCommonPrompt = null;
        EditTitle = string.Empty;
        EditContent = string.Empty;
        EditIsDefault = false;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditCommonPrompt()
    {
        if (SelectedCommonPrompt == null) return;
        EditTitle = SelectedCommonPrompt.Title;
        EditContent = SelectedCommonPrompt.Content;
        EditIsDefault = false;
        IsEditing = true;
    }

    [RelayCommand]
    private void SavePrompt()
    {
        if (string.IsNullOrWhiteSpace(EditTitle) || string.IsNullOrWhiteSpace(EditContent))
        {
            StatusMessage = "标题和内容不能为空";
            return;
        }

        if (SelectedTabIndex == 0)
        {
            SaveCommonPrompt();
        }
        else
        {
            SaveAccountPrompt();
        }
    }

    private void SaveCommonPrompt()
    {
        if (SelectedCommonPrompt == null)
        {
            var prompt = new CommonPrompt
            {
                Title = EditTitle,
                Content = EditContent,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            DbHelper.Db.Insertable(prompt).ExecuteCommand();
            StatusMessage = "公共提示词添加成功";
        }
        else
        {
            DbHelper.Db.Updateable<CommonPrompt>()
                .SetColumns(x => x.Title == EditTitle)
                .SetColumns(x => x.Content == EditContent)
                .SetColumns(x => x.UpdateTime == DateTime.Now)
                .Where(x => x.Id == SelectedCommonPrompt.Id)
                .ExecuteCommand();
            StatusMessage = "公共提示词更新成功";
        }

        IsEditing = false;
        var commonList = DbHelper.Db.Queryable<CommonPrompt>().OrderByDescending(x => x.CreateTime).ToList();
        CommonPrompts = new ObservableCollection<CommonPrompt>(commonList);
    }

    private void SaveAccountPrompt()
    {
        if (SelectedAccount == null)
        {
            StatusMessage = "请先选择账号";
            return;
        }

        if (SelectedAccountPrompt == null)
        {
            var prompt = new AccountPrompt
            {
                AccountId = SelectedAccount.Id,
                Title = EditTitle,
                Content = EditContent,
                IsDefault = EditIsDefault,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            DbHelper.Db.Insertable(prompt).ExecuteCommand();
            StatusMessage = "私有提示词添加成功";
        }
        else
        {
            DbHelper.Db.Updateable<AccountPrompt>()
                .SetColumns(x => x.Title == EditTitle)
                .SetColumns(x => x.Content == EditContent)
                .SetColumns(x => x.IsDefault == EditIsDefault)
                .SetColumns(x => x.UpdateTime == DateTime.Now)
                .Where(x => x.Id == SelectedAccountPrompt.Id)
                .ExecuteCommand();
            StatusMessage = "私有提示词更新成功";
        }

        IsEditing = false;
        LoadAccountPrompts();
    }

    [RelayCommand]
    private void DeleteCommonPrompt()
    {
        if (SelectedCommonPrompt == null) return;
        DbHelper.Db.Deleteable<CommonPrompt>()
            .Where(x => x.Id == SelectedCommonPrompt.Id)
            .ExecuteCommand();
        StatusMessage = "公共提示词删除成功";
        LoadData();
    }

    [RelayCommand]
    private void CopyToAccount()
    {
        if (SelectedCommonPrompt == null || SelectedAccount == null)
        {
            StatusMessage = "请选择公共提示词和目标账号";
            return;
        }

        var prompt = new AccountPrompt
        {
            AccountId = SelectedAccount.Id,
            Title = SelectedCommonPrompt.Title,
            Content = SelectedCommonPrompt.Content,
            IsCopyFromCommon = true,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };
        DbHelper.Db.Insertable(prompt).ExecuteCommand();
        StatusMessage = "已复制到账号私有提示词";
        LoadAccountPrompts();
    }

    [RelayCommand]
    private void AddAccountPrompt()
    {
        if (SelectedAccount == null)
        {
            StatusMessage = "请先选择账号";
            return;
        }
        SelectedAccountPrompt = null;
        EditTitle = string.Empty;
        EditContent = string.Empty;
        EditIsDefault = false;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditAccountPrompt()
    {
        if (SelectedAccountPrompt == null) return;
        EditTitle = SelectedAccountPrompt.Title;
        EditContent = SelectedAccountPrompt.Content;
        EditIsDefault = SelectedAccountPrompt.IsDefault;
        IsEditing = true;
    }

    [RelayCommand]
    private void DeleteAccountPrompt()
    {
        if (SelectedAccountPrompt == null) return;
        DbHelper.Db.Deleteable<AccountPrompt>()
            .Where(x => x.Id == SelectedAccountPrompt.Id)
            .ExecuteCommand();
        StatusMessage = "私有提示词删除成功";
        LoadAccountPrompts();
    }

    [RelayCommand]
    private void SetAsDefault()
    {
        if (SelectedAccountPrompt == null) return;

        DbHelper.Db.Updateable<AccountPrompt>()
            .SetColumns(x => x.IsDefault == false)
            .Where(x => x.AccountId == SelectedAccountPrompt.AccountId)
            .ExecuteCommand();

        DbHelper.Db.Updateable<AccountPrompt>()
            .SetColumns(x => x.IsDefault == true)
            .Where(x => x.Id == SelectedAccountPrompt.Id)
            .ExecuteCommand();

        StatusMessage = "已设为默认提示词";
        LoadAccountPrompts();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }
}