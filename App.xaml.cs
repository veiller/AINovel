using System.IO;
using System.Windows;
using AINovel.Helpers;
using AINovel.Services;
using WinForms = System.Windows.Forms;

namespace AINovel;

public partial class App : Application
{
    private WinForms.NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 注册全局异常处理
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show(
                $"程序发生未处理的异常：{ex?.Message}\n\n堆栈：{ex?.StackTrace}",
                "程序错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"UI 线程发生未处理异常：{args.Exception.Message}",
                "程序错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);

        // 生成应用图标
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var iconPath = Path.Combine(appDir, "app.ico");
        IconHelper.EnsureIcon(iconPath);

        // 初始化数据库
        var dbPath = Path.Combine(appDir, "ainovel.db");
        var connectionString = $"Data Source={dbPath}";

        DbHelper.Initialize(connectionString);

        // 恢复异常状态的核心梗
        DbHelper.ResetGeneratingToWait();

        // 设置系统托盘
        SetupTrayIcon(iconPath);
    }

    private void SetupTrayIcon(string iconPath)
    {
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = new System.Drawing.Icon(iconPath),
            Text = "AI小说生成程序",
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        _notifyIcon.ContextMenuStrip = new WinForms.ContextMenuStrip();
        _notifyIcon.ContextMenuStrip.Items.Add("显示窗口", null, (_, _) => ShowMainWindow());
        _notifyIcon.ContextMenuStrip.Items.Add("退出", null, (_, _) => ExitApp());
    }

    private void ShowMainWindow()
    {
        if (MainWindow == null) return;

        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    private void ExitApp()
    {
        var result = MessageBox.Show(
            "确定要退出程序吗？",
            "确认退出",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        _notifyIcon?.Dispose();
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}
