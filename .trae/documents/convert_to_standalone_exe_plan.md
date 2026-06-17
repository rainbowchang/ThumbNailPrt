# 项目改造：从 NX 插件 (DLL) 改为独立可执行程序 (.exe)

## 当前状态

- 项目类型：`Library` (DLL) - 只能在 NX 内部作为插件加载
- 入口：`ThumbnailGenerator.Main(string[] args)` - NX 插件入口
- 程序功能：选择 PRT 文件 → 生成缩略图 PNG

## 目标状态

- 项目类型：`WinExe` (独立可执行文件) - 双击即可运行
- 入口：`Program.Main()` → 启动 WinForms 主窗口
- 程序功能：当 NX 已打开时，通过 `Session.GetSession()` 连接到 NX 会话，然后执行缩略图生成
- 部署：将 .exe 与 NXOpen DLL 放在同一目录，或配置环境变量

## 方案选择

**方案 A：改造现有项目为 WinExe（推荐）**
- 将 `OutputType` 从 `Library` 改为 `WinExe`
- 添加 `Program.cs` 作为 WinForms 入口（包含 `[STAThread] Main`）
- 添加 `MainForm.cs` 作为主窗口（包含"选择文件"按钮 + "生成缩略图"按钮）
- 保留 `ThumbnailGenerator.cs` 中的核心业务逻辑

**方案 B：新建独立 .exe 项目，引用核心逻辑**
- 保留原 DLL 项目不变
- 新建一个 WinForms 项目，引用 ThumbnailGenerator 的逻辑
- 两个项目独立编译

**推荐：方案 A** - 更简单，一个项目搞定

## 具体修改步骤

### 1. 修改 .csproj

```xml
<!-- 从 -->
<OutputType>Library</OutputType>

<!-- 改为 -->
<OutputType>WinExe</OutputType>
```

### 2. 新增 Program.cs

```csharp
using System;
using System.Windows.Forms;

namespace ThumbNailPrt01
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
```

### 3. 新增 MainForm.cs

```csharp
using System;
using System.IO;
using System.Windows.Forms;
using NXOpen;
using NXOpen.UF;

namespace ThumbNailPrt01
{
    public partial class MainForm : Form
    {
        private Session theSession;
        private UFSession theUFSession;
        private UI theUI;

        private Button btnSelectFile;
        private Button btnGenerate;
        private TextBox txtFilePath;
        private Label lblStatus;

        public MainForm()
        {
            this.Text = "NX PRT 缩略图生成器";
            this.Width = 500;
            this.Height = 200;
            this.StartPosition = FormStartPosition.CenterScreen;

            // 初始化控件
            btnSelectFile = new Button { Text = "选择 PRT 文件", Left = 20, Top = 20, Width = 120 };
            txtFilePath = new TextBox { Left = 150, Top = 22, Width = 300, ReadOnly = true };
            btnGenerate = new Button { Text = "生成缩略图", Left = 20, Top = 60, Width = 430, Enabled = false };
            lblStatus = new Label { Left = 20, Top = 110, Width = 430, Text = "请先启动 NX 并打开一个部件文件" };

            btnSelectFile.Click += BtnSelectFile_Click;
            btnGenerate.Click += BtnGenerate_Click;

            this.Controls.Add(btnSelectFile);
            this.Controls.Add(txtFilePath);
            this.Controls.Add(btnGenerate);
            this.Controls.Add(lblStatus);

            // 尝试连接 NX
            try
            {
                theSession = Session.GetSession();
                theUFSession = UFSession.GetUFSession();
                theUI = UI.GetUI();
                lblStatus.Text = "已连接到 NX 会话。请选择 PRT 文件。";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "无法连接到 NX: " + ex.Message;
                btnSelectFile.Enabled = false;
            }
        }

        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "选择 PRT 文件";
                openFileDialog.Filter = "NX Part Files (*.prt)|*.prt|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;
                    btnGenerate.Enabled = true;
                }
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                string partPath = txtFilePath.Text;
                string thumbnailPath = Path.GetDirectoryName(partPath) + "\\thumbnail.png";

                // 调用核心逻辑
                ThumbnailGenerator.GenerateThumbnail(theSession, theUFSession, theUI, partPath, thumbnailPath);

                lblStatus.Text = "缩略图已生成: " + thumbnailPath;
                MessageBox.Show("缩略图生成成功！\n保存路径：" + thumbnailPath, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "错误: " + ex.Message;
                MessageBox.Show("生成失败：\n" + ex.Message + "\n\nStackTrace:\n" + ex.StackTrace, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
```

### 4. 修改 ThumbnailGenerator.cs

将静态字段改为方法参数，使其可以被外部调用：

```csharp
// 修改 GenerateThumbnail 签名
public static void GenerateThumbnail(Session session, UFSession ufSession, UI ui, string partPath, string thumbnailPath)
{
    // 使用传入的 session/ufSession/ui 代替静态字段
    Part workPart = session.Parts.Work;
    Part displayPart = session.Parts.Display;
    // ... 其余逻辑不变
}

// 保留原 Main 方法用于插件模式（可选）
// 或者删除不再需要的 Main / SelectPartFile / FindOpenedPart 等
```

### 5. 新增项目项（在 .csproj 中）

```xml
<Compile Include="Program.cs" />
<Compile Include="MainForm.cs" />
```

## 部署与运行

### 方式 1：从 NX BIN 目录运行

将生成的 `ThumbNailPrt01.exe` 复制到 NX 安装目录的 NXBIN 文件夹（例如 `C:\Program Files\Siemens\NX 2212\NXBIN`），然后在 NX 已经打开的情况下双击运行。

### 方式 2：设置环境变量

在项目的 `bin\Debug` 或 `bin\Release` 目录下创建批处理文件 `run.bat`：

```batch
@echo off
set PATH=%PATH%;C:\Program Files\Siemens\NX 2212\NXBIN
ThumbNailPrt01.exe
```

**前提**：NX 必须已经启动并运行。

## 注意事项

1. **NX 必须已启动**：独立 .exe 模式下，`Session.GetSession()` 会尝试连接到已运行的 NX 实例。如果 NX 未启动，连接会失败。
2. **无需启动独立的 NX 会话**：不需要调用 `Session.StartSession()` 或 `UFSession.Initialize()` —— 当 NX 已运行时，直接连接即可。
3. **图像导出仍然需要显示部件**：`ImageExportBuilder` 需要在 NX 中有图形窗口显示部件。因此操作流程是：先在 NX 中打开 PRT → 运行 .exe → 选择文件 → 生成缩略图。
4. **不建议尝试"无显示部件"模式**：因为缩略图本质上是对 NX 图形窗口的渲染导出，无论内部/外部模式都需要显示部件。

## 方案 A 修改清单

| 文件 | 操作 |
|------|------|
| ThumbNailPrt01.csproj | OutputType Library → WinExe |
| Program.cs | 新建（WinForms 入口） |
| MainForm.cs | 新建（主窗口 UI） |
| ThumbnailGenerator.cs | 修改方法签名，接受 session 参数 |
