using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AINovel.Models;
using AINovel.Services;

namespace AINovel.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private string _currentModule = "首页";

    [ObservableProperty]
    private SystemConfig _systemConfig = new();

    public ObservableCollection<string> NavigationItems { get; } = new()
    {
        "首页",
        "账号管理",
        "提示词管理",
        "CP管理",
        "文件上传",
        "生成管理",
        "系统配置",
        "数据库备份"
    };

    public MainViewModel()
    {
        // 加载系统配置
        LoadConfig();
    }

    private void LoadConfig()
    {
        SystemConfig = DbHelper.Db.Queryable<SystemConfig>().First() ?? new SystemConfig();
    }

    [RelayCommand]
    private void Navigate(string module)
    {
        CurrentModule = module;
        CurrentViewModel = module switch
        {
            "首页" => new HomeViewModel(SystemConfig),
            "账号管理" => new AccountViewModel(),
            "提示词管理" => new PromptViewModel(),
            "CP管理" => new CpViewModel(),
            "文件上传" => new FileUploadViewModel(),
            "生成管理" => new GenerateViewModel(SystemConfig),
            "系统配置" => new ConfigViewModel(SystemConfig),
            "数据库备份" => new BackupViewModel(SystemConfig),
            _ => new HomeViewModel(SystemConfig)
        };
    }

    public void RefreshConfig()
    {
        LoadConfig();
    }
}