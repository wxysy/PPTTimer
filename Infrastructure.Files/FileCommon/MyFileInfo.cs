using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Infrastructure.Files.FileCommon
{
    public class MyFileInfo
    {
        public static string GetFileVersion(string filePath)
        {
            /* 1、《C# 获取dll版本号》
             * https://blog.csdn.net/m0_37447148/article/details/82109643
             * 2、《C#编程：文件版本号的读取和设置》
             * https://blog.csdn.net/weixin_41964246/article/details/124566708
             */
            if (File.Exists(filePath))
            {
                FileVersionInfo fInfo = FileVersionInfo.GetVersionInfo(filePath);
                var fVer = fInfo.FileVersion ?? string.Empty;
                return fVer;
            }
            else
            { return string.Empty; }
        }

        public static Dictionary<string, string?> GetVersionInfoFromFile(string dllFilepath)
        {
            if (!File.Exists(dllFilepath))
                return [];

            System.Diagnostics.FileVersionInfo ss = System.Diagnostics.FileVersionInfo.GetVersionInfo(dllFilepath);
            Dictionary<string, string?> versionDic = new()
            {
                ["FilePath"] = dllFilepath,
                ["FileName"] = System.IO.Path.GetFileName(dllFilepath),
                [nameof(ss.FileVersion)] = ss.FileVersion,
                [nameof(ss.ProductVersion)] = ss.ProductVersion,
            };
            return versionDic;
        }
    }
}
