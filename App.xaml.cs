using System.IO;
using System.Windows;
using AINovel.Services;

namespace AINovel;

public partial class App : Application
{
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

        // 初始化数据库
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var dbPath = Path.Combine(appDir, "ainovel.db");
        var connectionString = $"Data Source={dbPath}";

        DbHelper.Initialize(connectionString);

        // 恢复异常状态的核心梗
        DbHelper.ResetGeneratingToWait();
    }
}
