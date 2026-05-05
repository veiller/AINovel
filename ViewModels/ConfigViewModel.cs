using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AINovel.Models;
using AINovel.Services;

namespace AINovel.ViewModels;

public partial class ConfigViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _gptApiUrl = string.Empty;

    [ObservableProperty]
    private string _gptApiKey = string.Empty;

    [ObservableProperty]
    private string _gptModel = "gpt-3.5-turbo";

    [ObservableProperty]
    private double _gptTemperature = 0.7;

    [ObservableProperty]
    private int _maxThreadCount = 2;

    [ObservableProperty]
    private int _minWaitGenerateCount = 3;

    [ObservableProperty]
    private int _apiTimeout = 30;

    [ObservableProperty]
    private string _connectionTestResult = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    private readonly SystemConfig _config;

    public ConfigViewModel(SystemConfig config)
    {
        _config = config;
        LoadConfig(config);
    }

    private void LoadConfig(SystemConfig config)
    {
        GptApiUrl = config.GptApiUrl;
        GptApiKey = config.GptApiKey;
        GptModel = config.GptModel;
        GptTemperature = config.GptTemperature;
        MaxThreadCount = config.MaxThreadCount;
        MinWaitGenerateCount = config.MinWaitGenerateCount;
        ApiTimeout = config.ApiTimeout;
    }

    [RelayCommand]
    private void Save()
    {
        _config.GptApiUrl = GptApiUrl;
        _config.GptApiKey = GptApiKey;
        _config.GptModel = GptModel;
        _config.GptTemperature = GptTemperature;
        _config.MaxThreadCount = MaxThreadCount;
        _config.MinWaitGenerateCount = MinWaitGenerateCount;
        _config.ApiTimeout = ApiTimeout;
        _config.UpdateTime = DateTime.Now;

        if (_config.Id == 0)
        {
            DbHelper.Db.Insertable(_config).ExecuteCommand();
        }
        else
        {
            DbHelper.Db.Updateable(_config).ExecuteCommand();
        }

        GenerationService.Instance.UpdateThreadCount(MaxThreadCount);

        StatusMessage = "配置保存成功";
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(GptApiUrl) || string.IsNullOrWhiteSpace(GptApiKey))
        {
            ConnectionTestResult = "请填写接口地址和API Key";
            return;
        }

        IsTesting = true;
        ConnectionTestResult = "测试中...";

        var gptService = new GptService();
        var success = await gptService.TestConnectionAsync(GptApiUrl, GptApiKey, GptModel, 10);

        IsTesting = false;
        ConnectionTestResult = success ? "连接成功" : "连接失败，请检查接口地址和API Key";
    }
}
