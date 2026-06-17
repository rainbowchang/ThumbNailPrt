# 修复 "无显示部件" 错误 - 详细分析与方案

## 问题分析

错误位置：`ThumbnailGenerator.cs` 第97行 `theSession.Parts.SetDisplay(workPart, true, true, out displayLoadStatus)`

**根本原因**：当 NX 中已经有打开的部件时，调用 `SetDisplay` 会失败；或者文件通过 `OpenBaseDisplay` 打开后，图形窗口没有正确刷新导致 `ImageExportBuilder.Commit()` 检测不到显示部件。

## 当前代码的问题

```csharp
// 问题1：只检查 workPart，不检查 displayPart
// 如果当前 workPart 就是目标文件，但 displayPart 不同，不会进入设置逻辑
if (workPart == null || !workPart.FullPath.Equals(partPath, StringComparison.OrdinalIgnoreCase))
{
    workPart = FindOpenedPart(partPath);
    if (workPart == null)
    {
        BasePart basePart = theSession.Parts.OpenBaseDisplay(partPath, out partLoadStatus);
        workPart = (Part)basePart;
    }
    theSession.Parts.SetWork(workPart);
    theSession.Parts.SetDisplay(workPart, true, true, out displayLoadStatus);
}

// 问题2：当 workPart == partPath 时，跳过整个 if 块
// 但此时 theSession.Parts.Display 可能 != workPart，导致 ImageExportBuilder 失败
```

## 修复方案

### 方案：统一处理逻辑，确保 workPart 和 displayPart 都指向目标文件

修改思路：
1. 不检查 `workPart` 是否等于目标路径，而是直接查找/打开目标文件
2. 显式地将目标文件设置为 **workPart** 和 **displayPart**
3. 添加状态检查，确保 `theSession.Parts.Display` 不为 null
4. 使用 `theUI.CreateImageExportBuilder()` 替代 `workPart.Views.CreateImageExportBuilder()`（更稳定）

## 具体修改内容

### 1. 修改 `GenerateThumbnail` 方法（第80-101行）

```csharp
public static void GenerateThumbnail(string partPath, string thumbnailPath)
{
    Part workPart = theSession.Parts.Work;
    Part displayPart = theSession.Parts.Display;

    // 先尝试查找已打开的部件
    workPart = FindOpenedPart(partPath);

    if (workPart == null)
    {
        // 文件未打开，使用 OpenBaseDisplay 打开并显示
        PartLoadStatus partLoadStatus;
        BasePart basePart = theSession.Parts.OpenBaseDisplay(partPath, out partLoadStatus);
        workPart = (Part)basePart;
    }

    // 确保目标文件既是 workPart 也是 displayPart
    theSession.Parts.SetWork(workPart);

    // 检查 displayPart 是否正确
    if (displayPart == null || displayPart.FullPath != workPart.FullPath)
    {
        PartLoadStatus displayLoadStatus;
        theSession.Parts.SetDisplay(workPart, false, true, out displayLoadStatus);
    }

    ExportImage(thumbnailPath);
}
```

### 2. 修改 `ExportImage` 方法（第103-116行）

```csharp
public static void ExportImage(string imageFileFullPath)
{
    NXOpen.Gateway.ImageExportBuilder imageExportBuilder = theUI.CreateImageExportBuilder();
    imageExportBuilder.RegionMode = false;
    imageExportBuilder.FileFormat = NXOpen.Gateway.ImageExportBuilder.FileFormats.Png;
    imageExportBuilder.FileName = imageFileFullPath;
    imageExportBuilder.BackgroundOption = NXOpen.Gateway.ImageExportBuilder.BackgroundOptions.Transparent;
    imageExportBuilder.EnhanceEdges = false;
    
    imageExportBuilder.Commit();
    imageExportBuilder.Destroy();
}
```

## 技术说明

1. **`theUI.CreateImageExportBuilder()` vs `workPart.Views.CreateImageExportBuilder()`**
   - 前者使用 NX 全局 UI 对象创建，更稳定，不受具体部件视图状态影响
   - 后者依赖部件的视图集合，当视图状态异常时可能失败

2. **`SetDisplay` 参数说明**
   - 第二个参数 `allowReplace`：`false` 表示如果已有显示部件则不替换
   - 第三个参数 `setEntirePart`：`true` 表示设置整个部件为显示部件
   - 当 `displayPart` 已是目标文件时，跳过 `SetDisplay` 调用

3. **`OpenBaseDisplay` vs `Open`**
   - `OpenBaseDisplay`：打开文件并直接在图形窗口中显示
   - `Open`：仅加载到内存，不显示
