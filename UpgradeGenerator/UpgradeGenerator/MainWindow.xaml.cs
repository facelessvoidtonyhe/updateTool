using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UpgradeGenerator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        List<UpgradeFile> fileList = new List<UpgradeFile>();
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        System.Windows.Forms.FolderBrowserDialog openFileDialog2 = new System.Windows.Forms.FolderBrowserDialog();
        System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void grid_Drop(object sender, DragEventArgs e)
        {
            string fileName = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            //获得文件名后的操作...
            addFile(fileName);
            lb_filelist.ItemsSource = null;
            lb_filelist.ItemsSource = fileList;
        }

        private void grid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void addFile(string fileName)
        {
            FileInfo file = new FileInfo(fileName);
            switch (file.Extension)
            {
                case ".class":
                    handleClass(fileName);
                    break;
                case ".json":
                    handleJson(fileName);
                    break;
                case ".gz":
                    handleGz(fileName);
                    break;
                default: break;
            }
        }

        private void handleClass(string fileName)
        {

            FileInfo file = new FileInfo(fileName);
            UpgradeFile upgrade = new UpgradeFile();
            var currentList = fileList.Find(m => m.FilePath == fileName);
            if (currentList != null)
            {
                MessageBox.Show("这个文件已经添加过，请勿重复添加!");
                return;
            }
            if (!fileName.Contains("iot-one") && !fileName.Contains("iot-business-dcom"))
            {
                MessageBox.Show("添加的Class文件无效!");
                return;
            }
            int index = fileName.IndexOf("target");
            string relativePath = fileName.Substring(index, fileName.Length - index);
            upgrade.FileName = file.Name;
            upgrade.FileType = fileName.Contains("iot-one") ? "iot-one" : "iot-business-dcom";
            upgrade.FilePath = file.FullName.Replace(file.Name, "");
            upgrade.FileTarget = relativePath.Replace(file.Name, "");
            fileList.Add(upgrade);
        }

        private void handleJson(string fileName)
        {
        }

        private void handleGz(string fileName)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            fileList.Clear();
            lb_filelist.ItemsSource = fileList;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Image a = (Image)sender;
            var targetFile = fileList.Find(m => m.FilePath == a.Tag.ToString());
            if (targetFile != null)
            {
                fileList.Remove(targetFile);
                lb_filelist.ItemsSource = null;
                lb_filelist.ItemsSource = fileList;
            }
        }

        /// <summary>
        /// 生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(tb_version.Text))
            {
                MessageBox.Show("版本号不能为空!");
                return;
            }
            if (String.IsNullOrEmpty(tb_description.Text))
            {
                MessageBox.Show("描述不能为空!");
                return;
            }
            if (fileList.Count == 0)
            {
                MessageBox.Show("至少选择一个文件!");
                return;
            }
            string errorMsg = "";
            string templatePath = System.Environment.CurrentDirectory + "//templates//upgrade.tar.gz";
            // 将templates先删除
            ZipHelper.DeleteDirectory(System.Environment.CurrentDirectory + "//update");
            // 清空完了创建
            Directory.CreateDirectory(System.Environment.CurrentDirectory + "//update");
            // 解压文件
            ZipHelper.UnzipTgz(templatePath, System.Environment.CurrentDirectory + "//update");
            // 遍历文件列表
            foreach (var file in fileList)
            {
                string targetPath = System.Environment.CurrentDirectory + "/update";
                switch (file.FileType)
                {
                    case "iot-one":
                        targetPath += "/classes/iot-one/BOOT-INF";
                        break;
                    case "iot-business-dcom":
                        targetPath += "/classes/iot-business-dcom/BOOT-INF";
                        break;
                }
                ZipHelper.CopyFile(file.FilePath, targetPath + "/" + file.FileTarget, file.FileName);
            }
            // 写入版本信息
            // 写入说明文件
            // 打包
            ZipHelper.CreatTarGzArchive(System.Environment.CurrentDirectory, "update");
            if (File.Exists(System.Environment.CurrentDirectory + "/update.tar.gz"))
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string foldPath = dialog.SelectedPath;
                    DirectoryInfo theFolder = new DirectoryInfo(foldPath);
                    File.Copy(System.Environment.CurrentDirectory + "/update.tar.gz", foldPath + "/update" + DateTime.Now.ToString("yyyyMMdd") + ".tar.gz", true);
                    System.Windows.MessageBox.Show("导出成功！");
                }
            }
        }
    }

    public class UpgradeFile
    {
        public string FileName { set; get; }
        public string FileType { set; get; }
        public string FilePath { set; get; }
        public string FileTarget { set; get; }
    }
}
