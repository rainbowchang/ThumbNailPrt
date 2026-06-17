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
                // 连接到 NX 会话
                Session theSession = Session.GetSession();
                UFSession theUFSession = UFSession.GetUFSession();
                UI theUI = UI.GetUI();

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
                MessageBox.Show("NX 异常：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("错误：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            Part workPart = theSession.Parts.Work;
            Part displayPart = theSession.Parts.Display;

            // 查找已打开的部件
            workPart = FindOpenedPart(theSession, partPath);

            if (workPart == null)
            {
                // 文件未打开，使用 OpenBaseDisplay 打开并显示
                PartLoadStatus partLoadStatus;
                BasePart basePart = theSession.Parts.OpenBaseDisplay(partPath, out partLoadStatus);
                workPart = (Part)basePart;
            }

            theSession.Parts.SetWork(workPart);

            // 只有当 displayPart 不是目标文件时才调用 SetDisplay
            if (displayPart == null || displayPart.FullPath != workPart.FullPath)
            {
                PartLoadStatus displayLoadStatus;
                theSession.Parts.SetDisplay(workPart, false, true, out displayLoadStatus);
            }

            // 生成缩略图
            ExportImage(theSession, theUI, workPart);
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

            // 使用 ImageExportBuilder 导出图像
            NXOpen.Gateway.ImageExportBuilder imageExportBuilder = theUI.CreateImageExportBuilder();
            imageExportBuilder.RegionMode = false;
            imageExportBuilder.FileFormat = NXOpen.Gateway.ImageExportBuilder.FileFormats.Png;
            imageExportBuilder.FileName = thumbnailPath;
            imageExportBuilder.BackgroundOption = NXOpen.Gateway.ImageExportBuilder.BackgroundOptions.Transparent;
            imageExportBuilder.EnhanceEdges = false;

            imageExportBuilder.Commit();
            imageExportBuilder.Destroy();
        }
    }
}
