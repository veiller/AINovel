using System.Windows;
using System.Windows.Controls;
using AINovel.ViewModels;

namespace AINovel.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void NavigateToAccount(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            mainVm.NavigateCommand.Execute("账号管理");
    }

    private void NavigateToPrompt(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            mainVm.NavigateCommand.Execute("提示词管理");
    }

    private void NavigateToUpload(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            mainVm.NavigateCommand.Execute("文件上传");
    }

    private void NavigateToGenerate(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            mainVm.NavigateCommand.Execute("生成管理");
    }

    private void NavigateToConfig(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            mainVm.NavigateCommand.Execute("系统配置");
    }
}