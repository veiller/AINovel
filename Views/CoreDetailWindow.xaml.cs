using System.Windows;
using AINovel.Models;
using AINovel.Services;
using AINovel.ViewModels;

namespace AINovel.Views;

public partial class CoreDetailWindow : Window
{
    private readonly NovelCore _core;
    private readonly GenerateViewModel _parentViewModel;

    public CoreDetailWindow(NovelCore core, GenerateViewModel parentViewModel)
    {
        InitializeComponent();

        _core = core;
        _parentViewModel = parentViewModel;

        DataContext = new CoreDetailViewModel(core);
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // 将 UI 中的修改写回实体
        var vm = (CoreDetailViewModel)DataContext;
        _core.Content = vm.Content;

        // 保存到数据库
        await DbHelper.Db.Updateable<NovelCore>()
            .SetColumns(x => x.Content == _core.Content)
            .Where(x => x.Id == _core.Id)
            .ExecuteCommandAsync();

        // 刷新父 ViewModel 的列表
        await _parentViewModel.RefreshCoresAsync();

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// 窗口内部 ViewModel，用于展示核心梗详情
/// </summary>
public class CoreDetailViewModel
{
    public string SerialNumber { get; }
    public string StatusText { get; }
    public string CreateTime { get; }
    public string GenerateTime { get; }
    public string Content { get; set; }

    public CoreDetailViewModel(NovelCore core)
    {
        SerialNumber = core.SerialNumber;
        StatusText = core.GenerateStatus switch
        {
            0 => "待生成",
            1 => "生成中",
            2 => "已生成",
            3 => "生成失败",
            4 => "已发布",
            5 => "等待生成",
            _ => "未知"
        };
        CreateTime = core.CreateTime.ToString("yyyy-MM-dd HH:mm");
        GenerateTime = core.GenerateTime?.ToString("yyyy-MM-dd HH:mm") ?? "-";
        Content = core.Content;
    }
}
