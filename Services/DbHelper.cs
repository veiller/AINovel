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

        // 初始化默认系统配置
        InitDefaultConfig();
    }

    private static void InitDefaultConfig()
    {
        var config = _db.Queryable<SystemConfig>().First();
        if (config == null)
        {
            _db.Insertable(new SystemConfig
            {
                GptApiUrl = "https://api.openai.com/v1/chat/completions",
                GptApiKey = "",
                MaxThreadCount = 2,
                MinWaitGenerateCount = 3,
                ApiTimeout = 30,
                BackupFrequency = 1,
                BackupPath = "Backup",
                BackupRetentionDays = 7,
                UpdateTime = DateTime.Now
            }).ExecuteCommand();
        }
    }

    public static void ResetGeneratingToWait()
    {
        var list = _db.Queryable<NovelCore>()
            .Where(x => x.GenerateStatus == 1)
            .ToList();

        foreach (var item in list)
        {
            _db.Updateable<NovelCore>()
                .SetColumns(x => x.GenerateStatus == 0)
                .SetColumns(x => x.FailReason == "程序异常中断")
                .Where(x => x.Id == item.Id)
                .ExecuteCommand();
        }
    }
}