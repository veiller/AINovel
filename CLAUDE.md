# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

**AINovel** 是一个基于 .NET 8 WPF 的桌面应用程序，用于 AI 辅助小说生成。管理小说项目、解析源文档（Markdown、Word）、调用 GPT API 生成章节内容，数据存储在本地 SQLite 数据库中。

- **解决方案：** `AINovel.sln`（单项目：`AINovel.csproj`）
- **目标框架：** `net8.0-windows`
- **UI 框架：** WPF + HandyControl 3.5.1 + CommunityToolkit.Mvvm 8.4.2
- **ORM：** SqlSugarCore 5.1.4.214 + SQLite
- **架构：** MVVM（CommunityToolkit.Mvvm），ViewModel 在 `ViewModels/`，视图在 `Views/`，服务在 `Services/`

## 构建 / 运行

```bash
dotnet build        # Debug 构建
dotnet build -c Release
dotnet run          # 从解决方案根目录运行
```

无测试项目、无 linter、无自定义构建脚本。

## 架构要点

- **App.xaml.cs** — 启动时调用 `DbHelper.Initialize()`（自动创建所有表），然后调用 `DbHelper.ResetGeneratingToWait()` 从崩溃的生成会话中恢复（将 `GenerateStatus=1` 的行重置为 `GenerateStatus=0`，`FailReason="程序异常中断"`）。
- **DbHelper** — 单例，持有 `SqlSugar` 连接。连接字符串：`Data Source={AppDomain.CurrentDomain.BaseDirectory}/ainovel.db`。**无加密** — `GptApiKey` 以明文存储在 `SystemConfig` 表中。
- **GenerationService** — 懒加载单例，使用 `SemaphoreSlim`（默认 2 个槽位）。`UpdateThreadCount()` 是空实现。
- **GptService** — 在 `GenerateAsync` 和 `TestConnectionAsync` 中均硬编码 `"gpt-3.5-turbo"`。模型无法通过 UI 配置。
- **视图基于 DataTemplate** — `MainWindow.xaml` 使用 `ContentControl` + `DataTemplate` 按 ViewModel 类型映射视图，不是 `UserControl` 导航或 Prism 区域。
- **核心梗格式** — 源文件解析使用正则 `【数字】`（如 `【001】`），在 `FileParser` 中强制校验。
- **无全局异常处理器** — `App.xaml.cs` 未注册 `Application.DispatcherUnhandledException` 或 `AppDomain.CurrentDomain.UnhandledException`。
- **BackupPath 是相对路径** — 默认为 `"Backup"`（相对于工作目录，非 `AppDomain.CurrentDomain.BaseDirectory`）。

## 关键文件

- `Services/DbHelper.cs` — 数据库初始化、ORM、崩溃恢复
- `Services/GenerationService.cs` — 核心生成编排（单例，信号量控制）
- `Services/GptService.cs` — GPT API 调用（硬编码模型）
- `Services/FileParser.cs` — Markdown/Word 核心梗提取
- `Models/Entities.cs` — 所有 SqlSugar 实体类
- `ViewModels/` — 11 个 MVVM ViewModel（CommunityToolkit.Mvvm）
- `Views/` — 14 个 XAML 视图及代码后台
- `docs/WPF多账号AI在线小说生成程序设计方案.md` — 完整设计文档（中文）

## 分支 / PR 规范

- 分支名称使用 **PascalCase**（如 `Feature/NewFeature`、`Fix/BugDescription`）
