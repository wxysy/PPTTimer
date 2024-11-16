using IWshRuntimeLibrary;//������� Com ������ Windows Script Host Object Model
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Infrastructure.Common.AutoStart
{
    /// <summary>
    /// ��C#ʵ�����������������
    /// https://www.cnblogs.com/timefiles/p/17519239.html
    /// ��ԭ��������Ŀ�ݷ�ʽ��������������Զ�����Ŀ¼�£�����Ҫ����ԱȨ�ޣ������ַ�������ͨ�á����Ƹ��١�
    /// <code language="cs" title="ʹ�÷���">
    /// /*��ʹ�÷���-1��*/
    /// //��ݷ�ʽ�����������Ƶ�Ĭ��ֵ�ǵ�ǰ�Ľ�������������Ĭ��Ϊ�������ڣ�һ������²���Ҫ�ֶ�����
    /// //[��ѡ]���ÿ�ݷ�ʽ��������
    /// AutoStart.Instance.QuickDescribe = "�������";
    /// //[��ѡ]���ÿ�ݷ�ʽ������
    /// AutoStart.Instance.QuickName = "�������";
    /// //[��ѡ]�����������Ĵ������ͣ���̨������������������Ϊ��С����
    /// AutoStart.Instance.WindowStyle = WshWindowStyle.WshMinimizedFocus;
    /// //��ݷ�ʽ����trueʱ���оͺ��ԡ�û�оʹ�������������ݷ�ʽֻ�ܴ���һ��
    /// //���ÿ�����������true ��������false ��������
    /// AutoStart.Instance.SetAutoStart(true);
    /// //���������ݷ�ʽ��true ���������ݷ�ʽ���о�������û�оʹ�������false ɾ�������ݷ�ʽ
    /// AutoStart.Instance.SetDesktopQuick(true);
    /// 
    /// /*��ʹ�÷���-2��*/
    /// AutoStart autoStart = new()
    /// {
    ///     //��ݷ�ʽ�����������Ƶ�Ĭ��ֵ�ǵ�ǰ�Ľ�������������Ĭ��Ϊ�������ڣ�һ������²���Ҫ�ֶ�����
    ///     QuickDescribe = "�������2",//[��ѡ]���ÿ�ݷ�ʽ������
    ///     QuickName = "�������2",//[��ѡ]���ÿ�ݷ�ʽ������
    ///     WindowStyle = WshWindowStyle.WshMinimizedFocus,//[��ѡ]�����������Ĵ������ͣ���̨������������������Ϊ��С����
    /// };
    /// //��ݷ�ʽ����trueʱ���оͺ��ԡ�û�оʹ�������������ݷ�ʽֻ�ܴ���һ��
    /// //���ÿ�����������true ��������false ��������
    /// autoStart.SetAutoStart(true);
    /// //���������ݷ�ʽ��true ���������ݷ�ʽ���о�������û�оʹ�������false ɾ�������ݷ�ʽ
    /// autoStart.SetDesktopQuick(true);
    /// MessageBox.Show("�������������ȡ����");
    /// </code>
    /// </summary>
    public class AutoStart
    {
        //�ο���
        //1����C#ʵ�����������������
        //https://www.cnblogs.com/timefiles/p/17519239.html
        //2����C#/WPF/WinForm/����ʵ����������Զ����������ֳ��÷�����
        //https://blog.csdn.net/weixin_42288432/article/details/120059296
        //��ԭ��������Ŀ�ݷ�ʽ��������������Զ�����Ŀ¼�£�����Ҫ����ԱȨ�ޣ������ַ�������ͨ�á����Ƹ��١�

        #region ����
        /// <summary>
        /// Ψһʵ����Ҳ�����Զ���ʵ��
        /// </summary>
        public static AutoStart Instance { get; private set; } = new AutoStart();

        /// <summary>
        /// ��ݷ�ʽ������Ĭ��ֵ�ǵ�ǰ�Ľ�����
        /// </summary>
        public string QuickDescribe { get; set; } = Process.GetCurrentProcess().ProcessName;

        /// <summary>
        /// ��ݷ�ʽ���ƣ�Ĭ��ֵ�ǵ�ǰ�Ľ�����
        /// </summary>
        public string QuickName { get; set; } = Process.GetCurrentProcess().ProcessName;

        /// <summary>
        /// �������������ͣ�Ĭ��ֵ����������
        /// </summary>
        public WshWindowStyle WindowStyle { get; set; } = WshWindowStyle.WshNormalFocus;

        /// <summary>
        /// ���ÿ����Զ�����-ֻ��Ҫ���øķ����Ϳ����˲��������bool�����ǿ��ƿ��������Ŀ��صģ�Ĭ��Ϊ������������
        /// </summary>
        /// <param name="onOff">��������</param>
        public void SetAutoStart(bool onOff = true)
        {
            if (onOff)//��������
            {
                //��ȡ����·��Ӧ�ó����ݷ�ʽ��·������
                List<string> shortcutPaths = GetQuickFromFolder(systemStartPath, appAllPath);
                //����2���Կ�ݷ�ʽ����һ����ݷ�ʽ-�����ظ�����
                if (shortcutPaths.Count >= 2)
                {
                    for (int i = 1; i < shortcutPaths.Count; i++)
                    {
                        DeleteFile(shortcutPaths[i]);
                    }
                }
                else if (shortcutPaths.Count < 1)//�������򴴽���ݷ�ʽ
                {
                    CreateShortcut(systemStartPath, QuickName, appAllPath, QuickDescribe, WindowStyle);
                }
            }
            else//����������
            {
                //��ȡ����·��Ӧ�ó����ݷ�ʽ��·������
                List<string> shortcutPaths = GetQuickFromFolder(systemStartPath, appAllPath);
                //���ڿ�ݷ�ʽ�����ȫ��ɾ��
                if (shortcutPaths.Count > 0)
                {
                    for (int i = 0; i < shortcutPaths.Count; i++)
                    {
                        DeleteFile(shortcutPaths[i]);
                    }
                }
            }
            //���������ݷ�ʽ-�����Ҫ����ȡ��ע��
            //SetDesktopQuick(true)
        }

        /// <summary>
        /// �������ϴ�����ݷ�ʽ-�����Ҫ���Ե���
        /// </summary>
        public void SetDesktopQuick(bool isCreate)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            List<string> shortcutPaths = GetQuickFromFolder(desktopPath, appAllPath);
            if (isCreate)
            {
                //û�оʹ���
                if (shortcutPaths.Count < 1)
                {
                    CreateShortcut(desktopPath, QuickName, appAllPath, QuickDescribe, WshWindowStyle.WshNormalFocus);
                }
            }
            else
            {
                //�о�ɾ��
                for (int i = 0; i < shortcutPaths.Count; i++)
                {
                    DeleteFile(shortcutPaths[i]);
                }
            }
        }

        #endregion ����

        #region ˽��

        /// <summary>
        /// �Զ���ȡϵͳ�Զ�����Ŀ¼
        /// </summary>
        private string systemStartPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

        /// <summary>
        /// �Զ���ȡ��������·��
        /// </summary>
        private string appAllPath = Environment.ProcessPath ?? "";//Process.GetCurrentProcess().MainModule.FileName;

        /// <summary>
        /// �Զ���ȡ����Ŀ¼
        /// </summary>
        private string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        /// <summary>
        /// �����ķ�������Ŀ��·������ָ���ļ��Ŀ�ݷ�ʽ
        /// </summary>
        /// <param name="directory">Ŀ��Ŀ¼</param>
        /// <param name="shortcutName">��ݷ�ʽ����</param>
        /// <param name="targetPath">�ļ���ȫ·��</param>
        /// <param name="description">����</param>
        /// <param name="iconLocation">ͼ���ַ</param>
        /// <returns>�ɹ���ʧ��</returns>
        private bool CreateShortcut(string directory, string shortcutName, string targetPath, string description, WshWindowStyle windowStyle, string? iconLocation = null)
        {
            try
            {
                //Ŀ¼�������򴴽�
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                //�ϳ�·��
                string shortcutPath = Path.Combine(directory, string.Format("{0}.lnk", shortcutName));
                //�����򲻴���
                if (System.IO.File.Exists(shortcutPath)) return true;
                //������� Com ������ Windows Script Host Object Model
                WshShell shell = new();
                //������ݷ�ʽ����
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                //ָ��Ŀ��·��
                shortcut.TargetPath = targetPath;
                //������ʼλ��
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                //�������з�ʽ��Ĭ��Ϊ���洰��
                shortcut.WindowStyle = (int)windowStyle;
                //���ñ�ע
                shortcut.Description = description;
                //����ͼ��·��
                shortcut.IconLocation = string.IsNullOrWhiteSpace(iconLocation) ? targetPath : iconLocation;
                //�����ݷ�ʽ
                shortcut.Save();
                return true;
            }
            catch (Exception ex)
            {
                string temp = ex.Message;
                temp = "";
            }
            return false;
        }

        /// <summary>
        /// ��ȡָ���ļ�����ָ��Ӧ�ó���Ŀ�ݷ�ʽ·������
        /// </summary>
        /// <param name="directory">�ļ���</param>
        /// <param name="targetPath">Ŀ��Ӧ�ó���·��</param>
        /// <returns>Ŀ��Ӧ�ó���Ŀ�ݷ�ʽ</returns>
        private List<string> GetQuickFromFolder(string directory, string targetPath)
        {
            List<string> tempStrs = [];
            tempStrs.Clear();
            string? tempStr = null;
            string[] files = Directory.GetFiles(directory, "*.lnk");
            if (files == null || files.Length < 1)
            {
                return tempStrs;
            }
            for (int i = 0; i < files.Length; i++)
            {
                //files[i] = string.Format("{0}\\{1}", directory, files[i]);
                tempStr = GetAppPathFromQuick(files[i]);
                if (tempStr == targetPath)
                {
                    tempStrs.Add(files[i]);
                }
            }
            return tempStrs;
        }

        /// <summary>
        /// ��ȡ��ݷ�ʽ��Ŀ���ļ�·��-�����ж��Ƿ��Ѿ��������Զ�����
        /// </summary>
        /// <param name="shortcutPath"></param>
        /// <returns></returns>
        private string GetAppPathFromQuick(string shortcutPath)
        {
            //��ݷ�ʽ�ļ���·�� = @"d:\Test.lnk";
            if (System.IO.File.Exists(shortcutPath))
            {
                WshShell shell = new WshShell();
                IWshShortcut shortct = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                //��ݷ�ʽ�ļ�ָ���·��.Text = ��ǰ��ݷ�ʽ�ļ�IWshShortcut��.TargetPath;
                //��ݷ�ʽ�ļ�ָ���Ŀ��Ŀ¼.Text = ��ǰ��ݷ�ʽ�ļ�IWshShortcut��.WorkingDirectory;
                return shortct.TargetPath;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// ����·��ɾ���ļ�-����ȡ������ʱ�Ӽ��������Ŀ¼ɾ������Ŀ�ݷ�ʽ
        /// </summary>
        /// <param name="path">·��</param>
        private void DeleteFile(string path)
        {
            FileAttributes attr = System.IO.File.GetAttributes(path);
            if (attr == FileAttributes.Directory)
            {
                Directory.Delete(path, true);
            }
            else
            {
                System.IO.File.Delete(path);
            }
        }

        #endregion ˽��
    }

}
