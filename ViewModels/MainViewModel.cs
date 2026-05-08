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

    /// <summary>
    /// 缓存包含表单状态的 ViewModel（数据编辑类），动态数据类不缓存
    /// </summary>
    private readonly Dictionary<string, ViewModelBase> _viewModelCache = new();

    private static readonly HashSet<string> _cachedModules = new()
    {
        "账号管理", "系统配置", "数据库备份"
    };

    public ObservableCollection<string> NavigationItems { get; } = new()
    {
        "首页",
        "账号管理",
        "CP管理",
        "文件上传",
        "生成管理",
        "系统配置",
        "数据库备份"
    };

    public MainViewModel()
    {
        LoadConfig();
    }

    public void LoadConfig()
    {
        SystemConfig = DbHelper.Db.Queryable<SystemConfig>().First() ?? new SystemConfig();
    }

    [RelayCommand]
    private void Navigate(string module)
    {
        CurrentModule = module;

        // 切出旧页面时清理 GenerateViewModel 的 Messenger
        if (CurrentViewModel is GenerateViewModel oldGenVm)
        {
            oldGenVm.Cleanup();
        }

        // 缓存命中且属于缓存模块则复用
        if (_cachedModules.Contains(module) && _viewModelCache.TryGetValue(module, out var cached))
        {
            CurrentViewModel = cached;
            return;
        }

        ViewModelBase vm = module switch
        {
            "首页" => new HomeViewModel(SystemConfig),
            "账号管理" => new AccountViewModel(),
            "CP管理" => new CpViewModel(),
            "文件上传" => new FileUploadViewModel(),
            "生成管理" => new GenerateViewModel(SystemConfig),
            "系统配置" => new ConfigViewModel(SystemConfig),
            "数据库备份" => new BackupViewModel(SystemConfig),
            _ => new HomeViewModel(SystemConfig)
        };

        if (_cachedModules.Contains(module))
        {
            _viewModelCache[module] = vm;
        }

        CurrentViewModel = vm;
    }
}
