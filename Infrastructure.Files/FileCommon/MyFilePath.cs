using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Infrastructure.Files.FileCommon
{
    public class MyFilePath
    {
        #region 读取与设定文件路径、文件夹路径、获取文件夹下所有文件路径
        //读取文件路径
        //在RegisterViewModel.cs文件-->RegisterCheckMethodAsync方法中，每读取一次用完后会清除。
        public static string ReadFilePath(IProgress<string>? progress, string p_Filter = "注册文件|*.reg;*.regi|许可文件|*.lic|", string dialogTitle = "读取文件")
        {
            OpenFileDialog ofd = new()
            {
                Multiselect = false,
                Filter = $"{p_Filter}所有文件|*.*",
                //示例：Filter = "图片文件 (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|自定义文件(*.wxy)|*.wxy|All files (*.*)|*.*",

                InitialDirectory = Directory.GetCurrentDirectory(),//(可选)，初始目录设定。也常用Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Title = dialogTitle,//(可选)，标题
                //FileName = "默认文件名",//(可选)，默认文件名
                //DefaultExt = ".jpg"//(可选)，默认文件扩展名
            };
            var res = ofd.ShowDialog();
            if (res == true)
            {
                progress?.Report($"文件路径已读取-{ofd.FileName}");
                return ofd.FileName;
            }
            else
            {
                progress?.Report("文件路径未读取。");
                return string.Empty;//string.Empty 等同于 "";
            }
        }

        //设定许可文件路径
        //在RegisterViewModel.cs文件-->RegisterCheckMethodAsync方法中，每读取一次用完后会清除。
        public static string SetFilePath(IProgress<string>? progress, string p_Filter = "注册文件|*.reg;*.regi|许可文件|*.lic|", string dialogTitle = "保存文件")
        {
            SaveFileDialog sfd = new()
            {
                Filter = $"{p_Filter}所有文件|*.*",
                //示例：Filter = "图片文件 (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|自定义文件(*.wxy)|*.wxy|All files (*.*)|*.*",

                InitialDirectory = Directory.GetCurrentDirectory(),//(可选)，初始目录设定。也常用Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Title = dialogTitle,//(可选)，标题
                //FileName = "默认文件名",//(可选)，默认文件名
                //DefaultExt = ".jpg"//(可选)，默认文件扩展名
            };
            var res = sfd.ShowDialog();
            if (res == true)
            {
                progress?.Report($"文件路径已设定-{sfd.FileName}");
                return sfd.FileName;
            }
            else
            {
                progress?.Report("文件路径未设定。");
                return string.Empty;//string.Empty 等同于 "";
            }
        }

        //【作废】选择文件夹路径(需要using System.Windows.Forms;)
        //常见用法是和FileInfo类配合使用，获取该目录下的所有文件。
        //public static string SelectFolderPathOld(IProgress<string>? progress)
        //{
        //    /* 在WPF中使用FolderBrowserDialog类(Windows.Forms控件)条件：
        //     * 1、在Data.File.csproj中添加<UseWindowsForms>true</UseWindowsForms>语句。
        //     * csproj文件进入方法：项目右键--“编辑项目文件” ，或者 直接在项目上左键单击 也能进入。
        //     * 2、添加using System.Windows.Forms;引用
        //     * 3、OpenFileDialog和SaveFileDialog在添加Windows.Forms引用后会有二义性，因此要在using中指明。
        //     */
        //    FolderBrowserDialog folderBrowser = new()
        //    {
        //        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),//(可选)，初始目录设定
        //        ShowNewFolderButton = true,//是否在对话框中设置”新建文件夹“按钮
        //        Description = "选择文件夹路径",//描述                
        //    };
        //    var res = folderBrowser.ShowDialog();
        //    if (res == DialogResult.OK)
        //    {
        //        var resPath = folderBrowser.SelectedPath;
        //        progress?.Report("文件夹路径已选择。");
        //        return resPath;
        //    }
        //    else
        //    {
        //        progress?.Report("文件夹路径未选择。");
        //        return string.Empty;//string.Empty 等同于 "";
        //    }
        //}


        /// <summary>
        /// 选择文件夹路径(.Net8 版本，OpenFolderDialog代替Windows.Forms控件的FolderBrowserDialog)
        /// 常见用法是和FileInfo类配合使用，获取该目录下的所有文件。
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static string SelectFolderPath(IProgress<string>? progress, string dialogTitle = "选择文件夹")
        {
            /* .Net8中，有了OpenFolderDialog，可替代FolderBrowserDialog类(Windows.Forms控件)
             * 《.NET 8 中的 WPF File Dialog改进》
             * https://devblogs.microsoft.com/dotnet-ch/net-8-%E4%B8%AD%E7%9A%84-wpf-file-dialog%E6%94%B9%E8%BF%9B/
             */
            OpenFolderDialog folderDialog = new()
            {
                Title = dialogTitle,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),//(可选)，初始目录设定
                Multiselect = false,//对话框是否允许选择多个文件夹。                          
            };
            var res = folderDialog.ShowDialog();
            if (res == true)
            {
                var resPath = folderDialog.FolderName;
                progress?.Report("文件夹路径已选择。");
                return resPath;
            }
            else
            {
                progress?.Report("文件夹路径未选择。");
                return string.Empty;//string.Empty 等同于 "";
            }
        }

        /// <summary>
        /// 获取选定文件夹下的所有文件文件路径。
        /// 该方法内有【关于搜索模式 searchPattern】的说明。
        /// 该方法的结果一般使用System.IO.Path方法进行后续处理。
        /// </summary>
        /// <param name="progress">进度指示</param>
        /// <param name="p_FolderPath">文件夹路径(不设定会弹出文件夹选择框)</param>
        /// <param name="p_SearchPattern">搜索模式设定(默认搜索全部文件)</param>
        /// <param name="p_SearchOption">是否搜索子文件夹(默认不搜索子文件夹)</param>
        /// <returns>文件路径列表(一般使用System.IO.Path对结果进行后续处理)</returns>
        public static List<string> GetFilePathInSelectedFolder(IProgress<string>? progress, string p_FolderPath = "", string p_SearchPattern = "*.*", SearchOption p_SearchOption = SearchOption.TopDirectoryOnly)
        {
            string folderPathChecked;

            #region 前期准备
            // 检测文件夹路径
            string tempPath;
            if (string.IsNullOrEmpty(p_FolderPath))
            {
                tempPath = SelectFolderPath(progress);
                if (tempPath == string.Empty)
                {
                    progress?.Report($"文件路径为空。");
                    return new List<string>();
                }
                else
                { }
            }
            else
            { tempPath = p_FolderPath; }

            DirectoryInfo directoryCheck = new(tempPath);
            if (directoryCheck.Exists)
            {
                progress?.Report($"该文件夹存在。");
                folderPathChecked = tempPath;
            }
            else
            {
                progress?.Report($"该文件夹不存在。");
                return new List<string>();
            }

            // 反馈是否搜索子文件夹
            string showSearchOption;
            if (p_SearchOption == SearchOption.TopDirectoryOnly)
            { showSearchOption = "不搜索子文件夹"; }
            else
            { showSearchOption = "搜索子文件夹"; }
            progress?.Report($"搜索模式：{p_SearchPattern}\n{showSearchOption}。");
            #endregion

            /* 
             * 【关于搜索模式 searchPattern】的说明
             * 1、允许使用通配符 * 和 ？，支持字符串内插 $，支持原义识别符 @；但不支持正则表达式。
             * 2、一次只能匹配一个模式，希望像OpenFileDialog对话框一样同时匹配两个模式"*.txt|*.xlsx"是不行的，只能是"*.txt"或"*.xlsx"。
             * 
             * 可以使用占位符 * 和 ?，如果想搜索全部文件，直接 searchPattern = "*"或者"*.*"。
             * 一般都会是搜索特定名称，如 searchPattern = "PluginMEF.*";
             * 代表搜寻 PluginMEF.AppOneMEF.dll 这样的文件，后缀名不限。
             * 也可以结合内插字符串用，如 searchPattern = $"PluginMEF.{guid}.*";
             * string searchPattern = $"*";
             * 
             * string searchPattern = $"PluginMEF.*";
             * 这句话就限定了，创建MEF插件项目时的项目名称格式。
             * 必须是“PluginMEF.AppOne”这样的，否则就不会搜索到。
             */
            string searchPattern = p_SearchPattern;//搜索模式设定
            SearchOption searchOption = p_SearchOption;//是否搜索子文件夹

            DirectoryInfo directory = new(folderPathChecked);
            FileInfo[] files = directory.GetFiles(searchPattern, searchOption);
            List<string> resList = new();
            foreach (var file in files)
            {
                resList.Add(file.FullName);
            }
            progress?.Report($"搜索完毕，文件数量-{resList.Count}。");

            return resList;
            //一般使用System.IO.Path对后续结果进一步处理
            //System.IO.Path.GetFullPath(openFileDialog1.FileName); //绝对路径
            //System.IO.Path.GetExtension(openFileDialog1.FileName); //文件扩展名
            //System.IO.Path.GetFileNameWithoutExtension(openFileDialog1.FileName); //文件名没有扩展名
            //System.IO.Path.GetFileName(openFileDialog1.FileName); //文件名
            //System.IO.Path.GetDirectoryName(openFileDialog1.FileName); //得到文件所在文件夹路径
        }
        #endregion

        #region 绝对路径与相对路径的转化
        /*《C# 文件绝对路径与相对路径的转换》
         * https://www.cnblogs.com/MaFeng0213/p/8919919.html
         *《C#相对路径转绝对路径，绝对路径转相对路径》
         * https://www.cnblogs.com/hont/p/5412340.html
         */
        /// <summary>
        /// 文件(或文件夹)的 绝对路径 --> 相对路径
        /// </summary>
        /// <param name="absolutePath">绝对路径</param>
        /// <param name="basePath">基路径（相对路径的参考基准），可用Directory.SetCurrentDirectory()改变当前比较路径。</param>
        /// <returns>相对路径</returns>
        /// <exception cref="ArgumentNullException">输入参数为空报错</exception>
        public static string AbsoluteToRelative(string absolutePath, string basePath)
        {
            /* 这个方法目前完全不懂。就是Program.MakeRelative()方法。*/
            ArgumentNullException.ThrowIfNull(basePath);

            ArgumentNullException.ThrowIfNull(absolutePath);

            bool isRooted = Path.IsPathRooted(basePath) && Path.IsPathRooted(absolutePath);

            if (isRooted)
            {
                bool isDifferentRoot = string.Compare(Path.GetPathRoot(basePath), Path.GetPathRoot(absolutePath), true) != 0;

                if (isDifferentRoot)
                    return absolutePath;
            }

            List<string> relativePath = new();
            string[] fromDirectories = basePath.Split(Path.DirectorySeparatorChar);

            string[] toDirectories = absolutePath.Split(Path.DirectorySeparatorChar);

            int length = Math.Min(fromDirectories.Length, toDirectories.Length);

            int lastCommonRoot = -1;

            // find common root
            for (int x = 0; x < length; x++)
            {
                if (string.Compare(fromDirectories[x], toDirectories[x], true) != 0)
                    break;

                lastCommonRoot = x;
            }

            if (lastCommonRoot == -1)
                return absolutePath;

            // add relative folders in from path
            for (int x = lastCommonRoot + 1; x < fromDirectories.Length; x++)
            {
                if (fromDirectories[x].Length > 0)
                    relativePath.Add("..");
            }

            // add to folders to path
            for (int x = lastCommonRoot + 1; x < toDirectories.Length; x++)
            {
                relativePath.Add(toDirectories[x]);
            }

            // create relative path
            string[] relativeParts = new string[relativePath.Count];
            relativePath.CopyTo(relativeParts, 0);

            string newPath = string.Join(Path.DirectorySeparatorChar.ToString(), relativeParts);

            return newPath;
        }

        /// <summary>
        /// 文件(或文件夹)的 相对路径 --> 绝对路径
        /// 默认basePath = Directory.GetCurrentDirectory();
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <param name="basePath">基路径（相对路径的参考基准），可用Directory.SetCurrentDirectory()改变当前比较路径。</param>
        /// <returns>绝对路径</returns>
        public static string RelativeToAbsolute(string relativePath, string basePath = "")
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Directory.GetCurrentDirectory();
                var res = Path.GetFullPath(relativePath, basePath);
                return res;
            }
            else
            {
                var res = Path.GetFullPath(relativePath, basePath);
                return res;
            }

            //Directory.SetCurrentDirectory(basePath);//用SetCurrentDirectory改变当前比较路径         
        }
        #endregion

        #region 检测文件名是否重名
        /// <summary>
        /// 该方法用于检测某个文件名(如：abc.txt)是否与集合中的文件名重复，如果重复的话生成新名字(如：abc_3.txt)。
        /// </summary>
        /// <param name="filePath">待检测的文件名（或文件路径）</param>
        /// <param name="fileNameList">对比用文件名的集合</param>
        /// <returns>(是否存在相同文件名，推荐的新文件名)</returns>
        public static (bool HasSameFileName, string RecommendedFilePath) FileNameDuplicateChecker(string filePath, string[] fileNameList)
        {
            if (fileNameList.Length > 0)
            {
                List<string> fileNameWEList = [];
                foreach (string fp in fileNameList)
                {
                    var itemNameWE = Path.GetFileNameWithoutExtension(fp);
                    fileNameWEList.Add(itemNameWE);
                }

                bool isNameContains = false;
                int i = 1;
                string fileNameExtension = Path.GetExtension(filePath);
                string fileNameWE = Path.GetFileNameWithoutExtension(filePath);
                string fileNameWE_Temp = fileNameWE;
                string fileNameWE_New = fileNameWE;
                while (fileNameWEList.Contains(fileNameWE_New) == true)
                {
                    isNameContains = true;
                    fileNameWE_New = $"{fileNameWE_Temp}_{i}";//加密后的文件名带编号来区分
                    i++;
                }
                var newFileName = fileNameWE_New + fileNameExtension;
                var directoryFullName = Path.GetDirectoryName(filePath);
                var newFilePath = directoryFullName != null ? Path.Combine(directoryFullName, newFileName) : newFileName;
                return (isNameContains, newFilePath);
            }
            else
            {
                return (false, filePath);
            }
        }
        #endregion
    }
}
