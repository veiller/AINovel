using System.Collections.ObjectModel;
using System.Windows;
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

    private async Task LoadAccountPromptsAsync()
    {
        if (SelectedAccount == null)
        {
            AccountPrompts.Clear();
            return;
        }

        var list = await DbHelper.Db.Queryable<AccountPrompt>()
            .Where(x => x.AccountId == SelectedAccount.Id)
            .OrderByDescending(x => x.CreateTime)
            .ToListAsync();
        AccountPrompts = new ObservableCollection<AccountPrompt>(list);
    }

    partial void OnSelectedAccountChanged(UserAccount? value)
    {
        _ = LoadAccountPromptsAsync();
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
    private async Task SavePromptAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTitle) || string.IsNullOrWhiteSpace(EditContent))
        {
            StatusMessage = "标题和内容不能为空";
            return;
        }

        if (SelectedTabIndex == 0)
        {
            await SaveCommonPromptAsync();
        }
        else
        {
            await SaveAccountPromptAsync();
        }
    }

    private async Task SaveCommonPromptAsync()
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
            await DbHelper.Db.Insertable(prompt).ExecuteCommandAsync();
            StatusMessage = "公共提示词添加成功";
        }
        else
        {
            await DbHelper.Db.Updateable<CommonPrompt>()
                .SetColumns(x => x.Title == EditTitle)
                .SetColumns(x => x.Content == EditContent)
                .SetColumns(x => x.UpdateTime == DateTime.Now)
                .Where(x => x.Id == SelectedCommonPrompt.Id)
                .ExecuteCommandAsync();
            StatusMessage = "公共提示词更新成功";
        }

        IsEditing = false;
        var commonList = await DbHelper.Db.Queryable<CommonPrompt>().OrderByDescending(x => x.CreateTime).ToListAsync();
        CommonPrompts = new ObservableCollection<CommonPrompt>(commonList);
    }

    private async Task SaveAccountPromptAsync()
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
            await DbHelper.Db.Insertable(prompt).ExecuteCommandAsync();
            StatusMessage = "私有提示词添加成功";
        }
        else
        {
            await DbHelper.Db.Updateable<AccountPrompt>()
                .SetColumns(x => x.Title == EditTitle)
                .SetColumns(x => x.Content == EditContent)
                .SetColumns(x => x.IsDefault == EditIsDefault)
                .SetColumns(x => x.UpdateTime == DateTime.Now)
                .Where(x => x.Id == SelectedAccountPrompt.Id)
                .ExecuteCommandAsync();
            StatusMessage = "私有提示词更新成功";
        }

        IsEditing = false;
        await LoadAccountPromptsAsync();
    }

    [RelayCommand]
    private async Task DeleteCommonPromptAsync()
    {
        if (SelectedCommonPrompt == null) return;

        var result = MessageBox.Show(
            $"确定要删除公共提示词「{SelectedCommonPrompt.Title}」吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        await DbHelper.Db.Deleteable<CommonPrompt>()
            .Where(x => x.Id == SelectedCommonPrompt.Id)
            .ExecuteCommandAsync();
        StatusMessage = "公共提示词删除成功";
        LoadData();
    }

    [RelayCommand]
    private async Task CopyToAccountAsync()
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
        await DbHelper.Db.Insertable(prompt).ExecuteCommandAsync();
        StatusMessage = "已复制到账号私有提示词";
        await LoadAccountPromptsAsync();
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
    private async Task DeleteAccountPromptAsync()
    {
        if (SelectedAccountPrompt == null) return;

        var result = MessageBox.Show(
            $"确定要删除私有提示词「{SelectedAccountPrompt.Title}」吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        await DbHelper.Db.Deleteable<AccountPrompt>()
            .Where(x => x.Id == SelectedAccountPrompt.Id)
            .ExecuteCommandAsync();
        StatusMessage = "私有提示词删除成功";
        await LoadAccountPromptsAsync();
    }

    [RelayCommand]
    private async Task SetAsDefaultAsync()
    {
        if (SelectedAccountPrompt == null) return;

        await DbHelper.Db.Updateable<AccountPrompt>()
            .SetColumns(x => x.IsDefault == false)
            .Where(x => x.AccountId == SelectedAccountPrompt.AccountId)
            .ExecuteCommandAsync();

        await DbHelper.Db.Updateable<AccountPrompt>()
            .SetColumns(x => x.IsDefault == true)
            .Where(x => x.Id == SelectedAccountPrompt.Id)
            .ExecuteCommandAsync();

        StatusMessage = "已设为默认提示词";
        await LoadAccountPromptsAsync();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }
}
