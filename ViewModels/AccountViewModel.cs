using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AINovel.Models;
using AINovel.Services;

namespace AINovel.ViewModels;

public partial class AccountViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<UserAccount> _accounts = new();

    [ObservableProperty]
    private UserAccount? _selectedAccount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editAccountName = string.Empty;

    [ObservableProperty]
    private string _editRemark = string.Empty;

    [ObservableProperty]
    private bool _editIsEnable = true;

    public AccountViewModel()
    {
        LoadAccounts();
    }

    private void LoadAccounts()
    {
        var list = DbHelper.Db.Queryable<UserAccount>().ToList();
        Accounts = new ObservableCollection<UserAccount>(list);
    }

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            LoadAccounts();
        }
        else
        {
            var list = DbHelper.Db.Queryable<UserAccount>()
                .Where(x => x.AccountName.Contains(SearchText))
                .ToList();
            Accounts = new ObservableCollection<UserAccount>(list);
        }
    }

    [RelayCommand]
    private void AddNew()
    {
        SelectedAccount = null;
        EditAccountName = string.Empty;
        EditRemark = string.Empty;
        EditIsEnable = true;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedAccount == null) return;
        EditAccountName = SelectedAccount.AccountName;
        EditRemark = SelectedAccount.Remark ?? string.Empty;
        EditIsEnable = SelectedAccount.IsEnable;
        IsEditing = true;
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(EditAccountName))
        {
            StatusMessage = "账号名称不能为空";
            return;
        }

        if (SelectedAccount == null)
        {
            var newAccount = new UserAccount
            {
                AccountName = EditAccountName,
                Remark = EditRemark,
                IsEnable = EditIsEnable,
                CreateTime = DateTime.Now
            };
            DbHelper.Db.Insertable(newAccount).ExecuteCommand();
            StatusMessage = "账号添加成功";
        }
        else
        {
            DbHelper.Db.Updateable<UserAccount>()
                .SetColumns(x => x.AccountName == EditAccountName)
                .SetColumns(x => x.Remark == EditRemark)
                .SetColumns(x => x.IsEnable == EditIsEnable)
                .Where(x => x.Id == SelectedAccount.Id)
                .ExecuteCommand();
            StatusMessage = "账号更新成功";
        }

        IsEditing = false;
        LoadAccounts();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedAccount == null) return;

        var result = MessageBox.Show(
            $"确定要删除账号「{SelectedAccount.AccountName}」吗？\n关联的私有提示词、核心梗和CP将一并删除。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        var accountId = SelectedAccount.Id;

        DbHelper.Db.Deleteable<AccountPrompt>().Where(x => x.AccountId == accountId).ExecuteCommand();
        DbHelper.Db.Deleteable<NovelCore>().Where(x => x.AccountId == accountId).ExecuteCommand();
        DbHelper.Db.Deleteable<CreativeProject>().Where(x => x.AccountId == accountId).ExecuteCommand();

        DbHelper.Db.Deleteable<UserAccount>()
            .Where(x => x.Id == accountId)
            .ExecuteCommand();

        StatusMessage = "账号及关联数据删除成功";
        LoadAccounts();
    }

    [RelayCommand]
    private void ToggleEnable()
    {
        if (SelectedAccount == null) return;

        DbHelper.Db.Updateable<UserAccount>()
            .SetColumns(x => x.IsEnable == !x.IsEnable)
            .Where(x => x.Id == SelectedAccount.Id)
            .ExecuteCommand();

        LoadAccounts();
    }
}
