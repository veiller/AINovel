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
    private bool _isEditing;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private string _editPromptContent = string.Empty;

    public CpViewModel()
    {
        LoadData();
    }

    private void LoadData()
    {
        var accountList = DbHelper.Db.Queryable<UserAccount>().ToList();
        Accounts = new ObservableCollection<UserAccount>(accountList);

        if (SelectedAccount != null)
        {
            LoadCps();
        }
        else if (Accounts.Count > 0)
        {
            SelectedAccount = Accounts[0];
        }
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
        LoadCps();

        if (Cps.Count > 0)
        {
            SelectedCp = Cps[0];
            EditCp();
        }
        else
        {
            IsEditing = false;
        }
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
        EditPromptContent = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditCp()
    {
        if (SelectedCp == null) return;
        EditName = SelectedCp.Name;
        EditDescription = SelectedCp.Description ?? string.Empty;

        // 加载关联的私有提示词内容
        if (SelectedCp.PromptId != null)
        {
            var promptId = SelectedCp.PromptId.Value;
            var prompt = DbHelper.Db.Queryable<AccountPrompt>().InSingle(promptId);
            EditPromptContent = prompt?.Content ?? string.Empty;
        }
        else
        {
            EditPromptContent = string.Empty;
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

        // 自动创建或更新私有提示词
        int? promptId = null;
        if (!string.IsNullOrWhiteSpace(EditPromptContent))
        {
            var promptTitle = $"{SelectedAccount.AccountName}-{EditName}";

            if (SelectedCp?.PromptId != null)
            {
                // 更新已有提示词
                var existingPromptId = SelectedCp.PromptId.Value;
                var existingPrompt = DbHelper.Db.Queryable<AccountPrompt>().InSingle(existingPromptId);
                if (existingPrompt != null)
                {
                    existingPrompt.Title = promptTitle;
                    existingPrompt.Content = EditPromptContent;
                    existingPrompt.UpdateTime = DateTime.Now;
                    DbHelper.Db.Updateable(existingPrompt).ExecuteCommand();
                    promptId = existingPrompt.Id;
                }
            }

            if (promptId == null)
            {
                // 新建私有提示词（Insertable 会自动回填自增 Id）
                var newPrompt = new AccountPrompt
                {
                    AccountId = SelectedAccount.Id,
                    Title = promptTitle,
                    Content = EditPromptContent,
                    IsCopyFromCommon = false,
                    IsDefault = false,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                DbHelper.Db.Insertable(newPrompt).ExecuteCommand();
                promptId = newPrompt.Id;
            }
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
