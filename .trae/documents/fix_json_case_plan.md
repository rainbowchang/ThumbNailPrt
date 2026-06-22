# JSON 属性大小写问题修复计划

## 问题分析

当前 `ConfigurationService` 在序列化配置文件时，没有启用大小写不敏感的反序列化选项。这导致：
- 代码生成的 JSON 使用 PascalCase（`RootDirectories`, `Path`）
- 用户手动编辑时如果写成 camelCase（`rootDirectories`, `path`），反序列化会失败
- 最终结果是根目录列表为空，左侧树状结构不显示

## 修复方案

在 `ConfigurationService.cs` 的 `SaveConfig` 和 `LoadConfig` 方法中，修改 `JsonSerializerOptions`：

**修改前**：
```csharp
var options = new JsonSerializerOptions { WriteIndented = true };
```

**修改后**：
```csharp
var options = new JsonSerializerOptions 
{ 
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
};
```

## 修改文件

- `Services/ConfigurationService.cs`

## 影响范围

- 配置文件读写功能
- 不影响其他业务逻辑

## 验证方式

1. 修改 `%appdata%\NQThumbnail\config.json`，使用 camelCase 写属性名
2. 重启应用程序
3. 验证目录树是否正确加载

## 风险评估

- 低风险：仅修改序列化选项，不影响数据结构
