using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AINovel.ViewModels;

namespace AINovel;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 设置窗口图标
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
        {
            Icon = new BitmapImage(new Uri(iconPath));
        }
    }

    private void NavListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is string selectedModule)
        {
            var vm = DataContext as MainViewModel;
            vm?.NavigateCommand.Execute(selectedModule);
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        var result = MessageBox.Show(
            "是否退出程序？\n\n点击「是」关闭程序，点击「否」最小化到系统托盘，点击「取消」继续编辑。",
            "确认退出",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        switch (result)
        {
            case MessageBoxResult.Yes:
                e.Cancel = false;
                _ = Dispatcher.InvokeAsync(() => Application.Current.Shutdown());
                break;
            case MessageBoxResult.No:
                e.Cancel = true;
                WindowState = WindowState.Minimized;
                Hide();
                break;
            case MessageBoxResult.Cancel:
            default:
                e.Cancel = true;
                break;
        }
    }
}
