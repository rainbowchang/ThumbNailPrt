using System;
using System.IO;
using System.Windows.Forms;
using NXOpen;
using NXOpen.UF;

namespace ThumbnailPrt02
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 连接到 NX 会话 - 逐步尝试，定位问题
                Session theSession = null;
                UI theUI = null;

                try
                {
                    theSession = Session.GetSession();
                    MessageBox.Show("Session.GetSession() 成功", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Session.GetSession() 失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    // UFSession 在外部模式下可能无法获取，跳过它（代码中并未使用）
                    // UFSession theUFSession = UFSession.GetUFSession();
                    // MessageBox.Show("UFSession.GetUFSession() 成功", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("UFSession.GetUFSession() 失败: " + ex.Message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                try
                {
                    theUI = UI.GetUI();
                    MessageBox.Show("UI.GetUI() 成功", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("UI.GetUI() 失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 选择 PRT 文件
                string partPath = SelectPartFile();
                if (string.IsNullOrEmpty(partPath))
                {
                    return;
                }

                // 生成缩略图路径：与 PRT 文件同目录下的 thumbnail.png
                string thumbnailPath = Path.GetDirectoryName(partPath) + "\\thumbnail.png";

                // 打开文件并生成缩略图
                GenerateThumbnail(theSession, theUI, partPath);

                MessageBox.Show("缩略图已生成！\n保存路径：" + thumbnailPath, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (NXException ex)
            {
                MessageBox.Show("NX 异常：" + ex.Message + "\n\n堆栈：" + ex.StackTrace, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (System.Runtime.InteropServices.SEHException ex)
            {
                MessageBox.Show("SEH 异常（外部组件）：" + ex.Message + "\n\n堆栈：" + ex.StackTrace, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("错误：" + ex.Message + "\n\n堆栈：" + ex.StackTrace, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string SelectPartFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "选择 PRT 文件";
                openFileDialog.Filter = "NX Part Files (*.prt)|*.prt|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
            }
            return string.Empty;
        }

        private void GenerateThumbnail(Session theSession, UI theUI, string partPath)
        {
            MessageBox.Show("开始 GenerateThumbnail", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Part workPart = theSession.Parts.Work;
            Part displayPart = theSession.Parts.Display;

            MessageBox.Show($"WorkPart: {workPart?.FullPath ?? "null"}, DisplayPart: {displayPart?.FullPath ?? "null"}", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 查找已打开的部件
            workPart = FindOpenedPart(theSession, partPath);
            MessageBox.Show($"FindOpenedPart 结果: {workPart?.FullPath ?? "null"}", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (workPart == null)
            {
                // 文件未打开，使用 OpenBaseDisplay 打开并显示
                PartLoadStatus partLoadStatus;
                MessageBox.Show("准备调用 OpenBaseDisplay", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
                BasePart basePart = theSession.Parts.OpenBaseDisplay(partPath, out partLoadStatus);
                workPart = (Part)basePart;
                MessageBox.Show($"OpenBaseDisplay 成功: {workPart.FullPath}", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            theSession.Parts.SetWork(workPart);
            MessageBox.Show("SetWork 成功", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 只有当 displayPart 不是目标文件时才调用 SetDisplay
            if (displayPart == null || displayPart.FullPath != workPart.FullPath)
            {
                PartLoadStatus displayLoadStatus;
                MessageBox.Show("准备调用 SetDisplay", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
                theSession.Parts.SetDisplay(workPart, false, true, out displayLoadStatus);
                MessageBox.Show("SetDisplay 成功", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // 生成缩略图
            MessageBox.Show("准备调用 ExportImage", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ExportImage(theSession, theUI, workPart);
            MessageBox.Show("ExportImage 成功", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Part FindOpenedPart(Session theSession, string partPath)
        {
            foreach (Part part in theSession.Parts.ToArray())
            {
                if (part.FullPath.Equals(partPath, StringComparison.OrdinalIgnoreCase))
                {
                    return part;
                }
            }
            return null;
        }

        private void ExportImage(Session theSession, UI theUI, Part workPart)
        {
            // 获取缩略图保存路径
            string thumbnailPath = Path.GetDirectoryName(workPart.FullPath) + "\\thumbnail.png";

            MessageBox.Show($"准备创建 ImageExportBuilder，路径: {thumbnailPath}", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 使用 ImageExportBuilder 导出图像
            try
            {
                NXOpen.Gateway.ImageExportBuilder imageExportBuilder = theUI.CreateImageExportBuilder();
                MessageBox.Show("CreateImageExportBuilder 成功", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);

                imageExportBuilder.RegionMode = false;
                imageExportBuilder.FileFormat = NXOpen.Gateway.ImageExportBuilder.FileFormats.Png;
                imageExportBuilder.FileName = thumbnailPath;
                imageExportBuilder.BackgroundOption = NXOpen.Gateway.ImageExportBuilder.BackgroundOptions.Transparent;
                imageExportBuilder.EnhanceEdges = false;

                MessageBox.Show("准备调用 Commit", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
                imageExportBuilder.Commit();
                MessageBox.Show("Commit 成功", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);

                imageExportBuilder.Destroy();
                MessageBox.Show("Destroy 成功", "调试", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ExportImage 内部异常: " + ex.Message + "\n\n堆栈: " + ex.StackTrace, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
    }
}
