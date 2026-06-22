using System.Windows.Forms;
using ThumbnailExplorer.Models;
using ThumbnailExplorer.Services;

namespace ThumbnailExplorer
{
    public partial class Form1 : Form
    {
        private ConfigurationService _configService;
        private HashService _hashService;
        private PropertyDirectoryService _propertyDirService;
        private FileSystemService _fileSystemService;
        private ScanService _scanService;
        private OpenFileService _openFileService;

        private TreeView _treeView;
        private Panel _propertyPanel;
        private Label _lblFileName;
        private Label _lblPath;
        private Label _lblCreationDate;
        private Label _lblFileSize;
        private Label _lblLastWriteTime;
        private PictureBox _picThumbnail;
        private TextBox _txtCustomName;
        private TextBox _txtDescription;
        private TextBox _txtTags;
        private TextBox _txtCategory;
        private TextBox _txtCreatedBy;
        private Button _btnSave;
        private Button _btnOpenFile;
        private Button _btnRefresh;
        private Button _btnScan;
        private Button _btnAddRootDir;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _lblStatus;
        private ToolStripStatusLabel _lblFileCount;
        private ToolStripStatusLabel _lblPropDirCount;

        private string? _selectedFilePath;

        public Form1()
        {
            InitializeServices();
            InitializeComponent();
            LoadTreeView();
            UpdateStatusBar();

            // 窗口加载完成后再最大化，确保布局计算正确
            this.Load += (s, e) =>
            {
                this.WindowState = FormWindowState.Maximized;
            };
        }

        private void InitializeServices()
        {
            _configService = new ConfigurationService();
            _configService.LoadConfig();

            _hashService = new HashService(_configService);
            _propertyDirService = new PropertyDirectoryService(_configService, _hashService);
            _fileSystemService = new FileSystemService();
            _scanService = new ScanService(_configService, _fileSystemService, _propertyDirService);
            _openFileService = new OpenFileService();
        }

        private void InitializeComponent()
        {
            this.Text = "ThumbnailExplorer";
            this.Size = new System.Drawing.Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 菜单和工具栏先不设置最大化，等 OnLoad 时再设置
            // 这样可以确保布局计算基于正确的尺寸

            // Menu Strip
            var menuStrip = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("文件");
            fileMenu.DropDownItems.Add("添加根目录", null, (s, e) => AddRootDirectory());
            fileMenu.DropDownItems.Add("刷新", null, (s, e) => RefreshTreeView());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("退出", null, (s, e) => Close());
            menuStrip.Items.Add(fileMenu);

            var scanMenu = new ToolStripMenuItem("扫描");
            scanMenu.DropDownItems.Add("扫描所有目录", null, (s, e) => ScanDirectories());
            scanMenu.DropDownItems.Add("清理孤立属性目录", null, (s, e) => CleanOrphanedDirectories());
            menuStrip.Items.Add(scanMenu);

            var helpMenu = new ToolStripMenuItem("帮助");
            helpMenu.DropDownItems.Add("关于", null, (s, e) => ShowAbout());
            menuStrip.Items.Add(helpMenu);

            // Tool Bar
            var toolStrip = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.System
            };
            _btnRefresh = new Button { Text = "刷新", Width = 70 };
            _btnRefresh.Click += (s, e) => RefreshTreeView();
            toolStrip.Items.Add(new ToolStripControlHost(_btnRefresh));

            _btnScan = new Button { Text = "扫描", Width = 70 };
            _btnScan.Click += (s, e) => ScanDirectories();
            toolStrip.Items.Add(new ToolStripControlHost(_btnScan));

            _btnAddRootDir = new Button { Text = "添加目录", Width = 80 };
            _btnAddRootDir.Click += (s, e) => AddRootDirectory();
            toolStrip.Items.Add(new ToolStripControlHost(_btnAddRootDir));

            // Split Container - 检查 SplitterFixedPanel 是否影响了分隔条
            var splitContainer = new SplitContainer
            {
                SplitterDistance = 300,
                IsSplitterFixed = false  // 确保分隔条可以移动
            };

            // Tree View Panel (Left)
            var leftPanel = new Panel { Dock = DockStyle.Fill };
            _treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                ImageList = CreateFileIconList()
            };
            _treeView.AfterSelect += TreeView_AfterSelect;
            _treeView.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;
            leftPanel.Controls.Add(_treeView);

            // Property Panel (Right)
            _propertyPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            InitializePropertyPanel();

            splitContainer.Panel1.Controls.Add(leftPanel);
            splitContainer.Panel2.Controls.Add(_propertyPanel);

            // 调试：打印 SplitContainer 尺寸信息
            splitContainer.Resize += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"SplitContainer: Width={splitContainer.Width}, SplitterDistance={splitContainer.SplitterDistance}");
                System.Diagnostics.Debug.WriteLine($"Panel1: Width={splitContainer.Panel1.Width}, Panel2: Width={splitContainer.Panel2.Width}");
            };

            // Status Strip
            _statusStrip = new StatusStrip();
            _lblStatus = new ToolStripStatusLabel("就绪");
            _lblFileCount = new ToolStripStatusLabel("文件数: 0");
            _lblPropDirCount = new ToolStripStatusLabel("属性目录数: 0");
            _statusStrip.Items.Add(_lblStatus);
            _statusStrip.Items.Add(_lblFileCount);
            _statusStrip.Items.Add(_lblPropDirCount);

            // 设置 Form 级控件的 Dock
            menuStrip.Dock = DockStyle.Top;
            toolStrip.Dock = DockStyle.Top;
            splitContainer.Dock = DockStyle.Fill;
            _statusStrip.Dock = DockStyle.Bottom;

            // 将 MenuStrip 设置为主菜单
            this.MainMenuStrip = menuStrip;

            // 关键：WinForms 中 Dock=Top 的控件按 z-order 反向排列
            // 后加入的 Dock=Top 控件出现在更靠上的位置
            // 所以先加 splitContainer (Fill) 占满剩余空间
            // 再加 toolStrip (Top) 占顶部
            // 最后加 menuStrip (Top) 占最顶部（在 toolStrip 之上）
            // 状态栏 (Bottom) 加在中间位置即可
            this.Controls.Add(splitContainer);
            this.Controls.Add(_statusStrip);
            this.Controls.Add(toolStrip);
            this.Controls.Add(menuStrip);
        }

        private void InitializePropertyPanel()
        {
            int y = 20;
            int labelWidth = 100;
            int textBoxWidth = 400;
            int spacing = 35;

            // Thumbnail Section - 放在最上面
            var sectionTitle2 = new Label
            {
                Text = "缩略图",
                Font = new System.Drawing.Font("Microsoft Sans Serif", 10, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(20, y),
                AutoSize = true
            };
            _propertyPanel.Controls.Add(sectionTitle2);
            y += 30;

            _picThumbnail = new PictureBox
            {
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(256, 256),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.White
            };
            _propertyPanel.Controls.Add(_picThumbnail);
            y += 270;

            // File Info Section - 放在缩略图下面，标签改成"详细信息"
            y += 10;
            var sectionTitle1 = new Label
            {
                Text = "详细信息",
                Font = new System.Drawing.Font("Microsoft Sans Serif", 10, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(20, y),
                AutoSize = true
            };
            _propertyPanel.Controls.Add(sectionTitle1);
            y += 30;

            // File Name
            _propertyPanel.Controls.Add(new Label { Text = "文件名:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _lblFileName = new Label { Location = new System.Drawing.Point(130, y), Width = textBoxWidth, Text = "-" };
            _propertyPanel.Controls.Add(_lblFileName);
            y += spacing;

            // Path
            _propertyPanel.Controls.Add(new Label { Text = "路径:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _lblPath = new Label { Location = new System.Drawing.Point(130, y), Width = textBoxWidth, Text = "-" };
            _lblPath.DoubleClick += (s, e) => { if (_selectedFilePath != null) _openFileService.OpenInExplorer(_selectedFilePath); };
            _propertyPanel.Controls.Add(_lblPath);
            y += spacing;

            // Creation Date
            _propertyPanel.Controls.Add(new Label { Text = "创建日期:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _lblCreationDate = new Label { Location = new System.Drawing.Point(130, y), Width = textBoxWidth, Text = "-" };
            _propertyPanel.Controls.Add(_lblCreationDate);
            y += spacing;

            // File Size
            _propertyPanel.Controls.Add(new Label { Text = "大小:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _lblFileSize = new Label { Location = new System.Drawing.Point(130, y), Width = textBoxWidth, Text = "-" };
            _propertyPanel.Controls.Add(_lblFileSize);
            y += spacing;

            // Last Write Time
            _propertyPanel.Controls.Add(new Label { Text = "修改日期:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _lblLastWriteTime = new Label { Location = new System.Drawing.Point(130, y), Width = textBoxWidth, Text = "-" };
            _propertyPanel.Controls.Add(_lblLastWriteTime);
            y += spacing;

            // Custom Properties - 删除"自定义属性"标签，直接显示字段
            y += 10;

            // Custom Name
            _propertyPanel.Controls.Add(new Label { Text = "名称:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _txtCustomName = new TextBox { Location = new System.Drawing.Point(130, y), Width = textBoxWidth };
            _propertyPanel.Controls.Add(_txtCustomName);
            y += spacing;

            // Description
            _propertyPanel.Controls.Add(new Label { Text = "描述:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _txtDescription = new TextBox { Location = new System.Drawing.Point(130, y), Width = textBoxWidth, Multiline = true, Height = 60 };
            _propertyPanel.Controls.Add(_txtDescription);
            y += 65;

            // Tags
            _propertyPanel.Controls.Add(new Label { Text = "标签:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _txtTags = new TextBox { Location = new System.Drawing.Point(130, y), Width = textBoxWidth };
            _propertyPanel.Controls.Add(_txtTags);
            y += spacing;

            // Category
            _propertyPanel.Controls.Add(new Label { Text = "类别:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _txtCategory = new TextBox { Location = new System.Drawing.Point(130, y), Width = textBoxWidth };
            _propertyPanel.Controls.Add(_txtCategory);
            y += spacing;

            // Created By
            _propertyPanel.Controls.Add(new Label { Text = "创建人:", Location = new System.Drawing.Point(20, y), Width = labelWidth });
            _txtCreatedBy = new TextBox { Location = new System.Drawing.Point(130, y), Width = textBoxWidth };
            _propertyPanel.Controls.Add(_txtCreatedBy);
            y += spacing + 10;

            // Buttons
            _btnSave = new Button { Text = "保存属性", Location = new System.Drawing.Point(130, y), Width = 100 };
            _btnSave.Click += BtnSave_Click;
            _propertyPanel.Controls.Add(_btnSave);

            _btnOpenFile = new Button { Text = "打开文件", Location = new System.Drawing.Point(240, y), Width = 100 };
            _btnOpenFile.Click += (s, e) => { if (_selectedFilePath != null) _openFileService.OpenWithDefaultProgram(_selectedFilePath); };
            _propertyPanel.Controls.Add(_btnOpenFile);
        }

        private ImageList CreateFileIconList()
        {
            var imageList = new ImageList();

            // Use system icons
            Icon folderIcon = SystemIcons.Application;
            Icon fileIcon = SystemIcons.WinLogo;

            imageList.Images.Add("folder", folderIcon);
            imageList.Images.Add("file", fileIcon);

            return imageList;
        }

        private void LoadTreeView()
        {
            _treeView.Nodes.Clear();

            var rootDirs = _configService.GetRootDirectories();
            var extensions = _configService.GetFileExtensions();
            var ignoredDirs = _configService.GetIgnoredDirectories();

            foreach (var rootDir in rootDirs)
            {
                if (!rootDir.IsEnabled || !Directory.Exists(rootDir.Path))
                    continue;

                var rootNode = new TreeNode
                {
                    Text = rootDir.Path,
                    Tag = new TreeNodeTag { NodeType = NodeType.Root, Path = rootDir.Path },
                    ImageKey = "folder",
                    SelectedImageKey = "folder"
                };

                LoadChildNodes(rootNode, rootDir.Path, extensions, ignoredDirs);
                _treeView.Nodes.Add(rootNode);
            }

            _treeView.ExpandAll();
        }

        private void LoadChildNodes(TreeNode parentNode, string path, List<string> extensions, List<string> ignoredDirs)
        {
            try
            {
                // Add subdirectories
                foreach (var dir in Directory.GetDirectories(path))
                {
                    string dirName = Path.GetFileName(dir);
                    if (ignoredDirs.Contains(dirName))
                        continue;

                    var dirNode = new TreeNode
                    {
                        Text = dirName,
                        Tag = new TreeNodeTag { NodeType = NodeType.Directory, Path = dir },
                        ImageKey = "folder",
                        SelectedImageKey = "folder"
                    };

                    LoadChildNodes(dirNode, dir, extensions, ignoredDirs);

                    // Only add directory node if it has children (subdirs or files)
                    if (dirNode.Nodes.Count > 0 || HasMatchingFiles(dir, extensions))
                    {
                        parentNode.Nodes.Add(dirNode);
                    }
                }

                // Add matching files
                foreach (var file in Directory.GetFiles(path))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (!extensions.Contains(ext))
                        continue;

                    var fileNode = new TreeNode
                    {
                        Text = Path.GetFileName(file),
                        Tag = new TreeNodeTag { NodeType = NodeType.File, Path = file },
                        ImageKey = "file",
                        SelectedImageKey = "file"
                    };
                    parentNode.Nodes.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
            }
        }

        private bool HasMatchingFiles(string dir, List<string> extensions)
        {
            try
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (extensions.Contains(ext))
                        return true;
                }
            }
            catch
            {
                // Ignore
            }
            return false;
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var tag = e.Node?.Tag as TreeNodeTag;
            if (tag == null || tag.NodeType != NodeType.File)
            {
                ClearPropertyPanel();
                return;
            }

            _selectedFilePath = tag.Path;
            LoadFileProperties(tag.Path);
        }

        private void TreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var tag = e.Node?.Tag as TreeNodeTag;
            if (tag != null && tag.NodeType == NodeType.File && !string.IsNullOrEmpty(tag.Path))
            {
                try
                {
                    _openFileService.OpenWithDefaultProgram(tag.Path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开文件: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadFileProperties(string filePath)
        {
            try
            {
                var fileInfo = _fileSystemService.GetFileInfo(filePath);
                var propDir = _propertyDirService.GetOrCreate(filePath, fileInfo.CreationTime);
                var properties = _propertyDirService.LoadProperties(propDir);
                var thumbnails = _propertyDirService.GetThumbnails(propDir);

                // Display file info
                _lblFileName.Text = fileInfo.FileName;
                _lblPath.Text = fileInfo.FullPath;
                _lblCreationDate.Text = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                _lblFileSize.Text = FormatFileSize(fileInfo.Length);
                _lblLastWriteTime.Text = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

                // Display thumbnail
                if (thumbnails.Count > 0)
                {
                    try
                    {
                        _picThumbnail.Image = Image.FromFile(thumbnails[0].FilePath);
                    }
                    catch
                    {
                        _picThumbnail.Image = null;
                    }
                }
                else
                {
                    _picThumbnail.Image = null;
                }

                // Display custom properties
                _txtCustomName.Text = properties.CustomName;
                _txtDescription.Text = properties.Description;
                _txtTags.Text = string.Join(", ", properties.Tags);
                _txtCategory.Text = properties.Category;
                _txtCreatedBy.Text = properties.CreatedBy;

                // Store property directory reference
                _propertyPanel.Tag = propDir;

                _lblStatus.Text = "已加载";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件属性失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearPropertyPanel()
        {
            _lblFileName.Text = "-";
            _lblPath.Text = "-";
            _lblCreationDate.Text = "-";
            _lblFileSize.Text = "-";
            _lblLastWriteTime.Text = "-";
            _picThumbnail.Image = null;
            _txtCustomName.Text = "";
            _txtDescription.Text = "";
            _txtTags.Text = "";
            _txtCategory.Text = "";
            _txtCreatedBy.Text = "";
            _propertyPanel.Tag = null;
            _selectedFilePath = null;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (_propertyPanel.Tag is not PropertyDirectory propDir)
            {
                MessageBox.Show("请先选择一个文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var properties = propDir.Properties;
                properties.CustomName = _txtCustomName.Text;
                properties.Description = _txtDescription.Text;
                properties.Tags = _txtTags.Text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
                properties.Category = _txtCategory.Text;
                properties.CreatedBy = _txtCreatedBy.Text;

                _propertyDirService.SaveProperties(propDir, properties);

                MessageBox.Show("属性已保存", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _lblStatus.Text = "已保存";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存属性失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTreeView()
        {
            LoadTreeView();
            UpdateStatusBar();
            _lblStatus.Text = "已刷新";
        }

        private void AddRootDirectory()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择根目录"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var rootDirs = _configService.GetRootDirectories();

                if (!rootDirs.Any(r => r.Path.Equals(dialog.SelectedPath, StringComparison.OrdinalIgnoreCase)))
                {
                    rootDirs.Add(new RootDirectory
                    {
                        Path = dialog.SelectedPath,
                        IsEnabled = true
                    });

                    _configService.SaveConfig(_configService.GetConfig());
                    LoadTreeView();
                    UpdateStatusBar();
                    _lblStatus.Text = "已添加根目录";
                }
            }
        }

        private void ScanDirectories()
        {
            var result = _scanService.ScanAll();

            string message = $"扫描完成:\n\n" +
                $"文件总数: {result.TotalFiles}\n" +
                $"属性目录总数: {result.TotalPropertyDirs}\n" +
                $"孤立目录数: {result.OrphanedDirs}";

            MessageBox.Show(message, "扫描结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            UpdateStatusBar();
        }

        private void CleanOrphanedDirectories()
        {
            var orphanedCount = _scanService.GetOrphanedCount();

            if (orphanedCount == 0)
            {
                MessageBox.Show("没有孤立的属性目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"发现 {orphanedCount} 个孤立属性目录。\n\n是否将它们移到回收站?",
                "确认清理",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _scanService.CleanOrphanedDirectories();
                MessageBox.Show("清理完成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatusBar();
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "ThumbnailExplorer v1.0\n\n" +
                "基于哈希值的文件属性和缩略图管理器\n\n" +
                "模仿 TRAE CN 的方式管理文件属性",
                "关于",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateStatusBar()
        {
            var result = _scanService.ScanAll();
            _lblFileCount.Text = $"文件数: {result.TotalFiles}";
            _lblPropDirCount.Text = $"属性目录数: {result.TotalPropertyDirs}";
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }

    public enum NodeType
    {
        Root,
        Directory,
        File
    }

    public class TreeNodeTag
    {
        public NodeType NodeType { get; set; }
        public string Path { get; set; } = string.Empty;
    }
}
