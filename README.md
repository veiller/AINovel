# AINovel - AI 在线小说生成程序

基于 .NET 8 WPF 的 AI 辅助小说生成桌面应用程序。管理小说项目、解析源文档（Markdown/Word）、调用 GPT API 生成章节内容，数据存储在本地 SQLite 数据库中。

## 功能

- **多账号管理** — 支持多个 API 账号，按账号隔离提示词和作品
- **CP 管理** — 创建作品项目（CP），在 CP 编辑面板中直接管理关联的私有提示词
- **文件上传与核心梗拆分** — 上传 Markdown / Word 文档，自动按 `【数字】` 格式拆分核心梗
- **AI 生成** — 调用 OpenAI 兼容 API 生成小说内容，支持流式输出与进度跟踪
- **批量处理** — 批量生成、批量重试、批量发布、多选删除
- **Channel 并发控制** — 基于 `System.Threading.Channels` 的 Worker 池，并发数可通过 UI 配置
- **自动生成** — 自动检查待生成核心梗并触发生成
- **备份恢复** — 数据库备份与定期清理

## 技术栈

| 组件 | 版本 |
|------|------|
| .NET | 8.0-windows |
| WPF | 原生 |
| HandyControl | 3.5.1 |
| CommunityToolkit.Mvvm | 8.4.2 |
| SqlSugarCore | 5.1.4.214 |
| SQLite | 内置 |

## 项目结构

```
AINovel/
├── Models/          # 实体类
├── ViewModels/      # MVVM ViewModel
├── Views/           # XAML 视图
├── Services/        # 业务服务
│   ├── DbHelper.cs          # 数据库初始化
│   ├── GenerationService.cs # 生成编排（Channel Worker 池）
│   ├── GptService.cs        # GPT API 调用（流式）
│   └── FileParser.cs        # Markdown/Word 核心梗解析
├── Converters/      # 值转换器
├── docs/            # 设计方案文档
└── CLAUDE.md        # Claude Code 项目指导
```

## 构建与运行

```bash
dotnet build
dotnet run
```

首次运行自动创建 SQLite 数据库文件 `ainovel.db`。

## 配置

在"系统配置"页面设置：
- API 接口地址
- API Key
- 模型名称（如 `gpt-3.5-turbo`、`gpt-4o`）
- 最大并发线程数
