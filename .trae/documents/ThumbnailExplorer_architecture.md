# ThumbnailExplorer 架构设计文档

## 1. 项目概述

### 1.1 项目背景

ThumbnailExplorer 是一个文件资源管理器风格的 Windows 桌面应用程序，模仿 TRAE CN 的方式，通过哈希值组织文件属性和缩略图。

### 1.2 核心概念

| 概念 | 说明 |
|------|------|
| **原始文件 (Original File)** | 用户指定的工作目录下的特定类型文件（如 .prt, .stp 等） |
| **属性目录 (Property Directory)** | 基于文件绝对路径+创建日期的哈希值命名的目录，存放缩略图和属性 |
| **哈希值 (Hash)** | SHA256(绝对路径 + 创建日期)，作为属性目录的名称 |
| **工作目录 (Root Directories)** | 配置文件中指定的多个根目录，组成虚拟的文件系统根 |

### 1.3 技术栈

- **.NET 8.0 Windows** (SDK-Style Project)
- **WinForms** (Windows Forms)
- **System.Text.Json** (配置与数据序列化)
- **Microsoft.VisualBasic.FileIO** (回收站操作)

---

## 2. 系统架构

### 2.1 整体架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                           UI 层                                      │
│  ┌─────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │   MainForm      │  │  DirectoryTree    │  │  PropertyPanel   │   │
│  │   (主窗口)       │  │  (目录树视图)      │  │  (属性面板)       │   │
│  └────────┬────────┘  └─────────┬─────────┘  └─────────┬────────┘   │
│           │                    │                      │            │
│           └────────────────────┼──────────────────────┘            │
│                                │                                     │
├────────────────────────────────┼────────────────────────────────────┤
│                         业务逻辑层 (Services)                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────┐ │
│  │ ConfigService│  │ HashService  │  │ PropertyDir  │  │ Scan     │ │
│  │ (配置服务)    │  │ (哈希服务)   │  │ Service      │  │ Service  │ │
│  └──────────────┘  └──────────────┘  │ (属性目录服务)│  │ (扫描服务)│ │
│                                       └──────────────┘  └──────────┘ │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐               │
│  │Thumbnail     │  │ FileSystem   │  │ OpenFile     │               │
│  │Service       │  │ Service      │  │ Service      │               │
│  │(缩略图服务)   │  │ (文件系统服务)│  │ (打开文件服务)│               │
│  └──────────────┘  └──────────────┘  └──────────────┘               │
├─────────────────────────────────────────────────────────────────────┤
│                           数据层                                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐   │
│  │  配置文件         │  │  属性目录         │  │  系统文件系统     │   │
│  │  config.json     │  │  %appdata%\      │  │  (原始文件)       │   │
│  │                  │  │  NQThumbnail\    │  │                  │   │
│  └─────────────────┘  └─────────────────┘  └──────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 目录结构

```
ThumbnailExplorer/
├── Models/
│   ├── OriginalFile.cs         # 原始文件模型
│   ├── PropertyDirectory.cs     # 属性目录模型
│   ├── FileProperty.cs         # 文件属性模型
│   ├── ThumbnailInfo.cs        # 缩略图信息模型
│   └── RootDirectory.cs        # 根目录配置模型
├── Services/
│   ├── ConfigurationService.cs  # 配置文件读写服务
│   ├── HashService.cs           # 哈希计算服务
│   ├── PropertyDirectoryService.cs  # 属性目录 CRUD
│   ├── ThumbnailService.cs      # 缩略图生成/读取
│   ├── FileSystemService.cs     # 文件系统元数据读取
│   ├── OpenFileService.cs       # 系统默认程序打开
│   └── ScanService.cs           # 扫描/清理孤立属性目录
├── UI/
│   ├── MainForm.cs              # 主窗口
│   ├── MainForm.Designer.cs     # 主窗口设计器
│   ├── DirectoryTreeView.cs     # 目录树控件
│   ├── DirectoryTreeNode.cs     # 目录树节点模型
│   ├── PropertyPanel.cs         # 属性面板
│   ├── PropertyEditor.cs        # 属性编辑器
│   └── ThumbnailControl.cs      # 缩略图展示控件
├── Data/
│   ├── AppConfig.cs             # 应用程序配置模型
│   └── FilePropertyData.cs     # 文件属性数据模型
├── Helpers/
│   ├── PathHelper.cs            # 路径辅助工具
│   └── FileHelper.cs            # 文件操作辅助
├── Resources/
│   └── Icons/                   # 图标资源
├── Form1.cs                     # 入口 (将被 MainForm 替代)
├── Program.cs                   # 程序入口
└── ThumbnailExplorer.csproj
```

---

## 3. 数据模型

### 3.1 配置文件 (config.json)

```json
{
  "rootDirectories": [
    "C:\\Projects\\CADFiles",
    "D:\\Work\\NXParts"
  ],
  "fileExtensions": [
    ".prt",
    ".stp",
    ".igs",
    ".step",
    ".iges"
  ],
  "propertyDirectoryRoot": "%appdata%\\NQThumbnail",
  "maxThumbnailsPerFile": 4,
  "thumbnailSize": {
    "width": 256,
    "height": 256
  }
}
```

### 3.2 属性目录结构

```
%appdata%\NQThumbnail\
└── {hash}\
    ├── thumbnails\
    │   ├── thumbnail_001.png
    │   ├── thumbnail_002.png
    │   ├── thumbnail_003.png
    │   └── thumbnail_004.png   (可选，最多4个)
    ├── properties.json          # 用户自定义属性
    └── path.txt                # 原始文件绝对路径
```

### 3.3 哈希计算规则

```
Hash = SHA256(AbsolutePath + CreationDate.ToString("yyyyMMddHHmmss"))
```

示例：
- 文件路径：`C:\Projects\Part.prt`
- 创建日期：`2024-01-15 10:30:45`
- 哈希输入：`C:\Projects\Part.prt20240115103045`
- 哈希值：`a1b2c3d4e5f6...` (64字符十六进制)

### 3.4 数据模型类图

```
┌─────────────────────────┐
│     RootDirectory       │
├─────────────────────────┤
│ + Path: string          │
│ + IsEnabled: bool       │
│ + LastScanTime: DateTime│
└───────────┬─────────────┘

┌─────────────────────────┐       ┌─────────────────────────┐
│     OriginalFile        │       │   PropertyDirectory    │
├─────────────────────────┤       ├─────────────────────────┤
│ + FullPath: string       │       │ + Hash: string          │
│ + FileName: string      │       │ + RootPath: string      │
│ + Extension: string    │       │ + DirectoryPath: string │
│ + CreationTime: DateTime│───────│ + Thumbnails: List     │
│ + LastWriteTime: DateTime│      │ + Properties: FileProp │
│ + Length: long          │       │ + OriginalPath: string │
│ + Hash: string (计算属性) │      └───────────┬─────────────┘
└─────────────────────────┘                  │
                                              │
┌─────────────────────────┐       ┌──────────▼─────────────┐
│     FileProperty        │       │    ThumbnailInfo       │
├─────────────────────────┤       ├─────────────────────────┤
│ + FileHash: string      │       │ + Index: int            │
│ + CustomName: string     │       │ + FilePath: string      │
│ + Description: string    │       │ + CreatedTime: DateTime │
│ + Tags: List<string>    │       │ + Width: int           │
│ + Category: string       │       │ + Height: int          │
│ + CreatedBy: string      │       └─────────────────────────┘
│ + CreatedTime: DateTime  │
│ + ModifiedTime: DateTime │
└─────────────────────────┘
```

---

## 4. 服务层设计

### 4.1 ConfigurationService

**职责**：管理应用程序配置

| 方法 | 说明 |
|------|------|
| `LoadConfig(): AppConfig` | 从 config.json 加载配置 |
| `SaveConfig(AppConfig)` | 保存配置到 config.json |
| `GetRootDirectories(): List<RootDirectory>` | 获取根目录列表 |
| `GetFileExtensions(): List<string>` | 获取支持的文件扩展名 |
| `GetPropertyDirectoryRoot(): string` | 获取属性目录根路径（展开 %appdata%） |

### 4.2 HashService

**职责**：计算和管理文件哈希值

| 方法 | 说明 |
|------|------|
| `ComputeHash(string path, DateTime creationTime): string` | 计算 SHA256 哈希 |
| `GetPropertyDirectoryPath(string hash): string` | 获取哈希对应的属性目录路径 |
| `IsHashValid(string hash): bool` | 验证哈希格式 |

### 4.3 PropertyDirectoryService

**职责**：属性目录的 CRUD 操作

| 方法 | 说明 |
|------|------|
| `GetOrCreate(string originalPath, DateTime creationTime): PropertyDirectory` | 获取或创建属性目录 |
| `LoadProperties(PropertyDirectory): FileProperty` | 从属性目录加载属性 |
| `SaveProperties(PropertyDirectory, FileProperty)` | 保存属性到属性目录 |
| `GetThumbnails(PropertyDirectory): List<ThumbnailInfo>` | 获取缩略图列表 |
| `AddThumbnail(PropertyDirectory, byte[] imageData)` | 添加缩略图 |
| `Delete(PropertyDirectory)` | 删除属性目录到回收站 |

### 4.4 ThumbnailService

**职责**：缩略图生成和管理

| 方法 | 说明 |
|------|------|
| `GenerateThumbnail(string filePath, int width, int height): byte[]` | 生成缩略图 |
| `SaveThumbnail(PropertyDirectory, byte[], int index)` | 保存缩略图 |
| `LoadThumbnail(string thumbnailPath): Image` | 加载缩略图 |
| `GetThumbnailCount(PropertyDirectory): int` | 获取缩略图数量 |

### 4.5 FileSystemService

**职责**：读取文件系统元数据

| 方法 | 说明 |
|------|------|
| `GetFileInfo(string path): OriginalFile` | 获取文件信息（不包括编辑日期和大小，这两个不存储）|
| `FileExists(string path, DateTime creationTime): bool` | 按路径+创建时间验证文件是否存在 |
| `GetFileSize(string path): long` | 获取文件大小 |
| `GetLastWriteTime(string path): DateTime` | 获取文件修改时间 |

### 4.6 ScanService

**职责**：扫描和清理孤立属性目录

| 方法 | 说明 |
|------|------|
| `ScanAllRootDirectories(): ScanResult` | 扫描所有根目录，返回扫描结果 |
| `FindOrphanedDirectories(): List<PropertyDirectory>` | 查找孤立的属性目录（原始文件已不存在） |
| `CleanOrphanedDirectories()` | 清理所有孤立目录到回收站 |
| `ValidatePropertyDirectory(PropertyDirectory): bool` | 验证属性目录对应的原始文件是否还存在 |

### 4.7 OpenFileService

**职责**：系统文件关联打开

| 方法 | 说明 |
|------|------|
| `OpenWithDefaultProgram(string filePath)` | 用系统默认程序打开文件 |
| `OpenInExplorer(string path)` | 在资源管理器中显示 |

---

## 5. UI 层设计

### 5.1 主窗口布局 (MainForm)

```
┌────────────────────────────────────────────────────────────────────────┐
│ 菜单栏: 文件 | 编辑 | 视图 | 扫描 | 帮助                              │
├────────────────────────────────────────────────────────────────────────┤
│ 工具栏: [刷新] [扫描] [配置]                                            │
├─────────────────────────────┬──────────────────────────────────────────┤
│                             │                                          │
│   目录树 (DirectoryTree)    │         属性面板 (PropertyPanel)          │
│                             │                                          │
│   📁 工作目录1               │   ┌────────────────────────────────┐    │
│   ├── 📁 子目录1             │   │  文件名: example.prt            │    │
│   │   ├── 📄 文件1.prt      │   │  路径: C:\...\example.prt      │    │
│   │   └── 📄 文件2.prt      │   │  创建日期: 2024-01-15          │    │
│   └── 📁 子目录2             │   │  大小: 1.23 MB                 │    │
│   📁 工作目录2               │   │  修改日期: 2024-06-10          │    │
│   └── 📁 子目录3             │   ├────────────────────────────────┤    │
│       └── 📄 文件3.stp      │   │  缩略图:                        │    │
│                             │   │  ┌─────┐ ┌─────┐               │    │
│                             │   │  │ 缩略 │ │ 缩略 │               │    │
│                             │   │  │  1  │ │  2  │               │    │
│                             │   │  └─────┘ └─────┘               │    │
│                             │   ├────────────────────────────────┤    │
│                             │   │  自定义属性:                     │    │
│                             │   │  名称: [____________]           │    │
│                             │   │  描述: [____________]           │    │
│                             │   │  标签: [____________]           │    │
│                             │   │  类别: [____________]           │    │
│                             │   │  创建人: [____________]         │    │
│                             │   │                    [保存]     │    │
│                             │   └────────────────────────────────┘    │
├─────────────────────────────┴──────────────────────────────────────────┤
│ 状态栏: 就绪 | 文件数: 123 | 属性目录数: 456                            │
└────────────────────────────────────────────────────────────────────────┘
```

### 5.2 主要 UI 组件

| 组件 | 说明 |
|------|------|
| **DirectoryTreeView** | 显示目录树，基于配置的根目录结构，仅显示指定类型的原始文件 |
| **PropertyPanel** | 右侧面板，显示选中文件的信息、缩略图和可编辑的自定义属性 |
| **ThumbnailControl** | 缩略图展示控件，支持多图切换 |
| **PropertyEditor** | 属性编辑对话框，用于编辑自定义属性 |

### 5.3 事件流

```
用户点击文件
    │
    ▼
DirectoryTreeView.OnFileSelected(filePath)
    │
    ▼
FileSystemService.GetFileInfo(filePath)  ──获取文件基本信息
    │
    ▼
HashService.ComputeHash(path, creationTime)  ──计算哈希
    │
    ▼
PropertyDirectoryService.GetOrCreate(hash)  ──获取或创建属性目录
    │
    ▼
PropertyDirectoryService.LoadProperties()  ──加载自定义属性
    │
    ▼
PropertyDirectoryService.GetThumbnails()  ──加载缩略图
    │
    ▼
PropertyPanel.Display(fileInfo, properties, thumbnails)  ──更新UI
```

---

## 6. 核心流程

### 6.1 文件选择流程

1. 用户点击目录树中的文件节点
2. 触发 `OnFileSelected` 事件
3. 调用 `FileSystemService.GetFileInfo()` 获取文件基本信息
4. 调用 `HashService.ComputeHash()` 计算哈希
5. 调用 `PropertyDirectoryService.GetOrCreate()` 获取/创建属性目录
6. 调用 `PropertyDirectoryService.LoadProperties()` 加载自定义属性
7. 调用 `ThumbnailService.GetThumbnails()` 获取缩略图列表
8. 更新 `PropertyPanel` 显示所有信息

### 6.2 属性编辑流程

1. 用户在属性面板中双击编辑框
2. 弹出 `PropertyEditor` 对话框
3. 用户修改属性后点击保存
4. 调用 `PropertyDirectoryService.SaveProperties()` 保存到 `properties.json`
5. 关闭对话框，更新面板显示

### 6.3 文件打开流程

1. 用户在属性面板中双击文件节点或点击"打开"按钮
2. 调用 `OpenFileService.OpenWithDefaultProgram(filePath)`
3. 系统使用默认程序打开文件

### 6.4 扫描清理流程

1. 用户点击菜单"扫描" → "清理孤立目录"
2. 调用 `ScanService.ScanAllRootDirectories()`
3. 遍历所有根目录，收集所有原始文件的哈希
4. 遍历 `%appdata%\NQThumbnail\` 下的所有属性目录
5. 对于每个属性目录，检查对应的原始文件是否存在
6. 如果不存在（路径+创建日期匹配不上），将属性目录移动到回收站
7. 显示扫描结果报告

### 6.5 哈希目录创建流程

当用户第一次点击一个原始文件时：

1. 计算哈希：`hash = SHA256(path + creationTime)`
2. 获取属性目录路径：`propDir = %appdata%\NQThumbnail\{hash}`
3. 如果目录不存在，创建：
   - `propDir/thumbnails/`
   - `propDir/path.txt` (写入原始文件路径)
   - `propDir/properties.json` (写入空属性或默认属性)
4. 如果目录已存在，跳过创建，直接使用

---

## 7. 配置文件详解

### 7.1 config.json 结构

```json
{
  "version": "1.0",
  "rootDirectories": [
    {
      "path": "C:\\Projects\\CADFiles",
      "isEnabled": true,
      "lastScanTime": null
    },
    {
      "path": "D:\\Work\\NXParts",
      "isEnabled": true,
      "lastScanTime": null
    }
  ],
  "fileExtensions": [".prt", ".stp", ".igs", ".step", ".iges"],
  "propertyDirectoryRoot": "%appdata%\\NQThumbnail",
  "maxThumbnailsPerFile": 4,
  "thumbnailSize": {
    "width": 256,
    "height": 256
  },
  "ignoredDirectories": ["node_modules", ".git", "backup"]
}
```

### 7.2 properties.json 结构

```json
{
  "fileHash": "a1b2c3d4e5f6...",
  "customName": "叶轮部件",
  "description": "这是一级叶轮，用于某型号泵组",
  "tags": ["叶轮", "泵", "关键件"],
  "category": "旋转部件",
  "createdBy": "张三",
  "createdTime": "2024-01-15T10:30:45",
  "modifiedTime": "2024-06-10T14:20:30"
}
```

### 7.3 path.txt 结构

```
C:\Projects\CADFiles\Impeller\TA0152.01.01-01.prt
```

---

## 8. 关键技术点

### 8.1 目录树虚拟化

由于根目录是配置指定的多个目录，而不是真正的文件系统根，需要实现一个虚拟的目录树：
- 根节点是配置的根目录列表
- 每个根目录下按照实际目录结构展开
- 只显示匹配扩展名的文件

### 8.2 哈希冲突处理

理论上 SHA256 碰撞概率极低，但如果发生：
- 检查 `path.txt` 中的路径是否与当前文件路径完全匹配
- 如果哈希相同但路径不同，说明碰撞，需要用路径+创建时间+索引等方式重新计算

### 8.3 缩略图生成

对于 CAD 文件 (.prt, .stp, .igs)：
- 如果 NX 已安装，可以使用 NXOpen API 生成缩略图
- 否则可以使用系统图标或默认图标

### 8.4 回收站操作

使用 `Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory()` 并指定 `UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin`

---

## 9. 模块依赖关系

```
Program.cs
    └── MainForm
            ├── ConfigurationService
            ├── DirectoryTreeView
            │       └── FileSystemService
            └── PropertyPanel
                    ├── FileSystemService
                    ├── HashService
                    ├── PropertyDirectoryService
                    │       ├── HashService
                    │       └── ThumbnailService
                    └── ThumbnailService

ScanService (独立模块，可从菜单调用)
    ├── ConfigurationService
    ├── FileSystemService
    ├── HashService
    └── PropertyDirectoryService
            └── HashService
```

---

## 10. 开发优先级

### Phase 1: 基础框架
1. 项目结构搭建
2. 数据模型定义
3. ConfigurationService 实现
4. HashService 实现

### Phase 2: 核心功能
5. PropertyDirectoryService 实现
6. DirectoryTreeView 实现
7. FileSystemService 实现

### Phase 3: UI 功能
8. MainForm 主窗口布局
9. PropertyPanel 属性面板
10. 文件选择和显示流程

### Phase 4: 增强功能
11. ThumbnailService 实现
12. 属性编辑功能
13. 文件打开功能

### Phase 5: 维护功能
14. ScanService 实现
15. 孤立目录清理

---

## 11. 附录

### 11.1 文件命名规范

| 类型 | 规范 | 示例 |
|------|------|------|
| 类名 | PascalCase | `PropertyDirectoryService` |
| 方法名 | PascalCase | `GetOrCreate` |
| 私有字段 | _camelCase | `_configService` |
| 接口名 | I + PascalCase | `IHashService` |
| 目录名 | PascalCase | `PropertyDirectory` |
| 配置文件 | camelCase | `config.json` |

### 11.2 异常处理策略

| 异常类型 | 处理方式 |
|----------|----------|
| 文件不存在 | 记录日志，从目录树移除 |
| 哈希目录损坏 | 删除并重建 |
| 缩略图生成失败 | 使用默认图标 |
| 配置损坏 | 使用默认配置 |
