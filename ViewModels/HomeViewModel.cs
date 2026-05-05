using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AINovel.Models;
using AINovel.Services;

namespace AINovel.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly SystemConfig _config;

    [ObservableProperty]
    private int _totalAccounts;

    [ObservableProperty]
    private int _totalCores;

    [ObservableProperty]
    private int _totalGenerated;

    [ObservableProperty]
    private int _totalPending;

    [ObservableProperty]
    private string _threadStatus = "停止";

    [ObservableProperty]
    private string _apiStatus = "未配置";

    public HomeViewModel(SystemConfig config)
    {
        _config = config;
        RefreshStatistics();
    }

    [RelayCommand]
    private void RefreshStatistics()
    {
        TotalAccounts = DbHelper.Db.Queryable<UserAccount>().Count();
        TotalCores = DbHelper.Db.Queryable<NovelCore>().Count();
        TotalGenerated = DbHelper.Db.Queryable<NovelCore>()
            .Where(x => x.GenerateStatus == 2 || x.GenerateStatus == 4)
            .Count();
        TotalPending = DbHelper.Db.Queryable<NovelCore>()
            .Where(x => x.GenerateStatus == 0)
            .Count();

        // 实时读取配置状态（配置可能在其他页面被修改）
        var currentConfig = DbHelper.Db.Queryable<SystemConfig>().First();
        ApiStatus = !string.IsNullOrEmpty(currentConfig?.GptApiKey) ? "已配置" : "未配置";
    }
}
