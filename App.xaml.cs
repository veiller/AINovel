using System.IO;
using System.Windows;
using AINovel.Services;

namespace AINovel;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
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