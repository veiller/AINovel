using SqlSugar;
using AINovel.Models;

namespace AINovel.Services;

public class DbHelper
{
    private static SqlSugarScope? _db;
    public static SqlSugarScope Db => _db ?? throw new InvalidOperationException("数据库未初始化");

    public static void Initialize(string connectionString)
    {
        _db = new SqlSugarScope(new List<ConnectionConfig>
        {
            new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true
            }
        });

        // 自动建表
        _db.CodeFirst.InitTables<UserAccount>();
        _db.CodeFirst.InitTables<CommonPrompt>();
        _db.CodeFirst.InitTables<AccountPrompt>();
        _db.CodeFirst.InitTables<NovelCore>();
        _db.CodeFirst.InitTables<CreativeProject>();
        _db.CodeFirst.InitTables<SystemConfig>();

        // 启用 WAL 模式提升多线程并发性能
        _db.Ado.ExecuteCommand("PRAGMA journal_mode=WAL;");

        // 初始化默认系统配置
        InitDefaultConfig();
    }

    private static void InitDefaultConfig()
    {
        if (!Db.Queryable<SystemConfig>().Any())
        {
            Db.Insertable(new SystemConfig
            {
                GptApiUrl = "https://api.openai.com/v1/chat/completions",
                GptApiKey = "",
                GptModel = "gpt-3.5-turbo",
                GptTemperature = 0.7,
                MaxThreadCount = 2,
                MinWaitGenerateCount = 3,
                ApiTimeout = 180,
                BackupFrequency = 1,
                BackupPath = "Backup",
                BackupRetentionDays = 7,
                UpdateTime = DateTime.Now
            }).ExecuteCommand();
        }
    }

    /// <summary>
    /// 关闭并重置数据库连接（用于备份恢复前释放文件锁）
    /// </summary>
    public static void CloseConnection()
    {
        _db?.Dispose();
        _db = null;
    }

    public static void ResetGeneratingToWait()
    {
        // 将崩溃前正在生成的恢复为待生成
        _db!.Updateable<NovelCore>()
            .SetColumns(x => x.GenerateStatus == 0)
            .SetColumns(x => x.FailReason == "程序异常中断")
            .Where(x => x.GenerateStatus == 1)
            .ExecuteCommand();

        // 将崩溃前在队列中等待的也恢复为待生成
        _db!.Updateable<NovelCore>()
            .SetColumns(x => x.GenerateStatus == 0)
            .SetColumns(x => x.FailReason == "程序异常中断")
            .Where(x => x.GenerateStatus == 5)
            .ExecuteCommand();
    }
}