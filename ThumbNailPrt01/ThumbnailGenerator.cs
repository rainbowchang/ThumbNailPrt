using System;
using NXOpen;
using NXOpen.UF;
using System.Windows.Forms;

namespace ThumbNailPrt01
{
    public class ThumbnailGenerator
    {
        public static Session theSession;
        public static UFSession theUFSession;
        public static UI theUI;

        public static int Main(string[] args)
        {
            int retValue = 0;
            try
            {
                theSession = Session.GetSession();
                theUFSession = UFSession.GetUFSession();
                theUI = UI.GetUI();

                string partPath = SelectPartFile();
                if (string.IsNullOrEmpty(partPath))
                {
                    theUI.NXMessageBox.Show("Thumbnail Generator", NXMessageBox.DialogType.Information, "未选择文件，操作取消");
                    return 0;
                }

                string thumbnailPath = System.IO.Path.GetDirectoryName(partPath) + "\\thumbnail.png";

                GenerateThumbnail(partPath, thumbnailPath);

                theUI.NXMessageBox.Show("Thumbnail Generator", NXMessageBox.DialogType.Information, "缩略图生成成功！\n保存路径：" + thumbnailPath);
            }
            catch (NXException ex)
            {
                theUI.NXMessageBox.Show("Thumbnail Generator", NXMessageBox.DialogType.Error, "NX异常 ：" + ex.ToString());
                theUI.NXMessageBox.Show("Thumbnail Generator", NXMessageBox.DialogType.Error, "NX异常 ：" + ex.Message);
                theUI.NXMessageBox.Show("Thumbnail Generator", NXMessageBox.DialogType.Error, "NX异常 ：" + ex.StackTrace);
                retValue = 1;
            }
            catch (Exception ex)
            {
                theUI.NXMessageBox.Show("Thumbnail Generator", NXMessageBox.DialogType.Error, "错误：" + ex.Message);
                retValue = 1;
            }
            return retValue;
        }

        public static string SelectPartFile()
        {
            string selectedFile = string.Empty;
            
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "选择PRT文件";
                    openFileDialog.InitialDirectory = @"C:\";
                    openFileDialog.Filter = "NX Part Files (*.prt)|*.prt|All Files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;
                    openFileDialog.Multiselect = false;
                    
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        selectedFile = openFileDialog.FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                theUI.NXMessageBox.Show("Thumbnail Generator", NXMessageBox.DialogType.Error, "文件选择失败：" + ex.Message);
            }
            
            return selectedFile;
        }

        public static void GenerateThumbnail(string partPath, string thumbnailPath)
        {
            Part workPart = theSession.Parts.Work;
            Part displayPart = theSession.Parts.Display;
            bool needToClose = false;  // 标记是否需要关闭这个部件

            workPart = FindOpenedPart(partPath);

            if (workPart == null)
            {
                // 文件未打开，需要新打开，完成后需要关闭
                PartLoadStatus partLoadStatus;
                BasePart basePart = theSession.Parts.OpenBaseDisplay(partPath, out partLoadStatus);
                workPart = (Part)basePart;
                needToClose = true;
            }

            theSession.Parts.SetWork(workPart);

            if (displayPart == null || displayPart.FullPath != workPart.FullPath)
            {
                PartLoadStatus displayLoadStatus;
                theSession.Parts.SetDisplay(workPart, false, true, out displayLoadStatus);
            }

            ExportImage(workPart, thumbnailPath, needToClose);
        }

        public static void ExportImage(Part workPart, string imageFileFullPath, bool needToClose)
        {
            NXOpen.Gateway.ImageExportBuilder imageExportBuilder = theUI.CreateImageExportBuilder();
            imageExportBuilder.RegionMode = false;
            imageExportBuilder.FileFormat = NXOpen.Gateway.ImageExportBuilder.FileFormats.Png;
            imageExportBuilder.FileName = imageFileFullPath;
            imageExportBuilder.BackgroundOption = NXOpen.Gateway.ImageExportBuilder.BackgroundOptions.Transparent;
            imageExportBuilder.EnhanceEdges = false;

            imageExportBuilder.Commit();
            imageExportBuilder.Destroy();

            // 如果是新打开的部件，生成缩略图后关闭它
            if (needToClose && workPart != null)
            {
                int closeWholeTree = 0;
                workPart.Close((BasePart.CloseWholeTree)closeWholeTree, BasePart.CloseModified.CloseModified, null);
            }
        }

        public static Part FindOpenedPart(string partPath)
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

        public static int GetUnloadOption()
        {
            return 1;
        }
    }
}