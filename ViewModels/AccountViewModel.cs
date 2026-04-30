using System.Collections.ObjectModel;
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
            // 新增
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
            // 更新
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

        DbHelper.Db.Deleteable<UserAccount>()
            .Where(x => x.Id == SelectedAccount.Id)
            .ExecuteCommand();

        StatusMessage = "账号删除成功";
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