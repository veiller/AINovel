---
name: verify-changes
description: 在提交更改前运行 dotnet build 验证编译是否成功
---

# verify-changes

在每次代码修改后，运行 `dotnet build` 验证项目能够成功编译。如果构建失败，分析错误并尝试修复。

## 使用场景

当 Claude 完成了代码修改后，应主动运行此 skill 验证编译通过，再向用户报告结果。

## 执行步骤

1. 运行 `dotnet build e:/repos/AINovel/AINovel.sln`
2. 如果构建成功：报告"构建成功"
3. 如果构建失败：
   - 读取错误信息
   - 尝试定位问题（常见：缺少 using、语法错误、类型不匹配）
   - 修复问题
   - 重新运行 `dotnet build`
   - 如果无法自行修复，报告错误并说明原因
