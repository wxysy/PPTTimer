using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OFFICECORE = Microsoft.Office.Core; //COM引用，Microsoft Office 16.0 Object Library。
using POWERPOINT = Microsoft.Office.Interop.PowerPoint;//COM引用，Microsoft Powerpoint 16.0 Object Library。

namespace PPTOperateLib.Play
{
    public class PPTPlay
    {
        //《（转）C#操作PPT》
        //https://www.cnblogs.com/hhhh2010/p/4630738.html
        //《如何在wpf窗口中播放PPT。》
        //https://www.cnblogs.com/tony-god/p/7650747.html
        //《C# 用Microsoft.Office.Interop.PowerPoint类库操作PPT》
        //https://blog.csdn.net/S1lenceAAA/article/details/109511859
        //《关于Microsoft支持和恢复助手(Support and Recovery Assistant)卸载后无法再次安装的问题》
        //https://blog.csdn.net/marvapc/article/details/120877453
        //《三行代码教你WPF播放mp3音乐》
        //https://blog.csdn.net/abraham_ly/article/details/106250084
        //《WPF PopUp的简单使用》
        //https://blog.csdn.net/qq_43024228/article/details/110353099

        #region 属性和变量
        System.Timers.Timer timer;
        POWERPOINT.Application? objApp;
        POWERPOINT.Presentation? objPresSet;
        POWERPOINT.SlideShowWindows? objSSWs;
        POWERPOINT.SlideShowTransition? objSST;
        POWERPOINT.SlideShowSettings? objSSS;
        POWERPOINT.SlideRange? objSldRng;

        //bool isAssistantOn = false;
        //double pixperPoint = 0;
        //double offsetx = 0;
        //double offsety = 0;

        public bool IsPPTOpened { get; private set; } = false;
        public bool IsInSlideShowMode { get; private set; } = false;//是否在放映模式
        public int PPTSlideNow { get; private set; } = 0;
        public int PPTTotalSlides { get; private set; } = 0;
        #endregion

        public PPTPlay()
        {
            timer = new() { Interval = 100 };
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (objPresSet == null)
                {
                    IsPPTOpened = false;
                    IsInSlideShowMode = false;
                    return;
                }

                if (objApp != null && objPresSet != null)
                    IsInSlideShowMode = objApp?.SlideShowWindows.Count > 0;
                else
                    IsInSlideShowMode = false;
            }
            catch (Exception)
            {
                IsPPTOpened = false;
                IsInSlideShowMode = false;
            }
            
        }

        #region 方法和命令
        /// <summary>
        /// 打开PPT文档并播放显示。
        /// </summary>
        /// <param name="filePath">PPT文件路径</param>
        public void PPTOpen(string filePath)
        {
            //防止连续打开多个PPT程序.
            if (objApp != null)
            {
                return;
            }
            try
            {
                objApp = new POWERPOINT.Application();
                //以非只读方式打开,方便操作结束后保存.
                objPresSet = objApp.Presentations.Open(filePath, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse);
                //isAssistantOn = objApp.Assistant.On;//Prevent Office Assistant from displaying alert messages:
                //objApp.Assistant.On = false;

                objSSS = objPresSet.SlideShowSettings;
                objSSS.Run();

                IsPPTOpened = true;
                IsInSlideShowMode = true;
                PPTTotalSlides = objPresSet.Slides.Count;
                PPTSlideNow = 1;
            }
            catch (Exception)
            {
                objApp?.Quit();
            }
        }

        /// <summary>
        /// 自动播放PPT文档.
        /// 【方法还是有问题，PPT退出黑屏时点击报错】
        /// </summary>
        /// <param name="filePath">PPT文件路径.</param>
        /// <param name="playTime">翻页的时间间隔.【以秒为单位】</param>
        public void PPTAuto(string filePath, int playTime)
        {
            //防止连续打开多个PPT程序.
            if (objApp != null)
            { return; }

            objApp = new POWERPOINT.Application();
            objPresSet = objApp.Presentations.Open(filePath, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse);

            // 自动播放的代码（开始）
            int Slides = objPresSet.Slides.Count;
            int[] SlideIdx = new int[Slides];
            for (int i = 0; i < Slides; i++)
            { SlideIdx[i] = i + 1; };
            objSldRng = objPresSet.Slides.Range(SlideIdx);
            objSST = objSldRng.SlideShowTransition;
            //设置翻页的时间.
            objSST.AdvanceOnTime = OFFICECORE.MsoTriState.msoCTrue;
            objSST.AdvanceTime = playTime;
            //翻页时的特效!
            objSST.EntryEffect = POWERPOINT.PpEntryEffect.ppEffectCircleOut;

            //Prevent Office Assistant from displaying alert messages:
            //isAssistantOn = objApp.Assistant.On;
            //objApp.Assistant.On = false;

            //Run the Slide show from slides 1 thru 3.
            objSSS = objPresSet.SlideShowSettings;
            objSSS.StartingSlide = 1;
            objSSS.EndingSlide = Slides;
            objSSS.Run();

            //Wait for the slide show to end.
            objSSWs = objApp.SlideShowWindows;
            while (objSSWs.Count >= 1)
                Thread.Sleep(playTime * 100);

            objPresSet.Close();
            objApp.Quit();
        }

        /// <summary>
        /// PPT下一页。
        /// </summary>
        public void NextSlide()
        {
            if (objApp != null)
            {
                objPresSet?.SlideShowWindow.View.Next();
                PPTSlideNow++;
            }
        }

        /// <summary>
        /// PPT上一页。
        /// </summary>
        public void PreviousSlide()
        {
            if (objApp != null)
            {
                objPresSet?.SlideShowWindow.View.Previous();
                PPTSlideNow--;
            }
        }

        /// <summary>
        /// 跳到指定页
        /// </summary>
        /// <param name="num">PPT第几页（从1开始）</param>
        /// <returns></returns>
        public void GoToSlide(int num)
        {
            objPresSet?.SlideShowWindow.View.GotoSlide(num);
            PPTSlideNow = num;
        }

        /// <summary>
        /// 关闭PPT文档。
        /// </summary>
        public void PPTClose()
        {
            try
            {
                //装备PPT程序。
                if (objApp != null && objPresSet != null)
                {
                    //判断是否退出程序,可以不使用。
                    objSSWs = objApp.SlideShowWindows;
                    if (objSSWs.Count >= 1)
                    {
                        //if (MessageBox.Show("是否保存修改的笔迹!", "提示", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        //{
                        //    this.objPresSet.Save();
                        //}
                        objPresSet?.Close();
                    }
                }
                objApp?.Quit();               
            }
            catch (Exception)
            { }

            IsPPTOpened = false;
            IsInSlideShowMode = false;
        }
        #endregion
    }
}
