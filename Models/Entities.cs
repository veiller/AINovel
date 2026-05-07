using SqlSugar;

namespace AINovel.Models;

/// <summary>
/// 账号表 - 存储多账号基础信息
/// </summary>
public class UserAccount
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public bool IsEnable { get; set; } = true;
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public string? Remark { get; set; }
}

/// <summary>
/// 公共提示词表 - 存储全局公共提示词
/// </summary>
public class CommonPrompt
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 账号私有提示词表
/// </summary>
public class AccountPrompt
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCopyFromCommon { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 核心梗表 - 存储核心梗与生成状态
/// </summary>
public class NovelCore
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// 生成状态: 0=待生成, 1=生成中, 2=已生成, 3=生成失败, 4=已发布
    /// </summary>
    public int GenerateStatus { get; set; } = 0;
    public string? GenerateContent { get; set; } = string.Empty;
    [SugarColumn(IsNullable = true)]
    public DateTime? GenerateTime { get; set; }
    public string? FailReason { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; } = DateTime.Now;
    [SugarColumn(IsNullable = true)]
    public DateTime? PublishTime { get; set; }
    public string? Operator { get; set; } = string.Empty;
    /// <summary>
    /// 生成方式: 0=自动生成, 1=手工生成
    /// </summary>
    public int GenerateType { get; set; } = 0;
    [SugarColumn(IsNullable = true)]
    public int? PromptId { get; set; }
    /// <summary>
    /// 生成进度 0-100
    /// </summary>
    public int GenerateProgress { get; set; } = 0;
    [SugarColumn(IsNullable = true)]
    public int? CpId { get; set; }
}

/// <summary>
/// CP（作品）表 - 关联账号，可包含多个核心梗和提示词
/// </summary>
public class CreativeProject
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [SugarColumn(IsNullable = true)]
    public int? PromptId { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 系统配置表 - 存储全局系统参数
/// </summary>
public class SystemConfig
{
    [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
    public int Id { get; set; }
    public string GptApiUrl { get; set; } = string.Empty;
    public string GptApiKey { get; set; } = string.Empty;
    public int MaxThreadCount { get; set; } = 2;
    public int MinWaitGenerateCount { get; set; } = 3;
    public int ApiTimeout { get; set; } = 180;
    public int BackupFrequency { get; set; } = 1;
    public string BackupPath { get; set; } = "Backup";
    public int BackupRetentionDays { get; set; } = 7;
    public string GptModel { get; set; } = "gpt-3.5-turbo";
    public double GptTemperature { get; set; } = 0.7;
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}
