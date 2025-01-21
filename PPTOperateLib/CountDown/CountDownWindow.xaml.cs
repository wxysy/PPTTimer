using PPTOperateLib.Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using OFFICECORE = Microsoft.Office.Core; //COM引用，Microsoft Office 16.0 Object Library。
using POWERPOINT = Microsoft.Office.Interop.PowerPoint;//COM引用，Microsoft Powerpoint 16.0 Object Library。

namespace PPTOperateLib.CountDown
{
    /// <summary>
    /// CountDownWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CountDownWindow : Window
    {
        #region 窗口操作
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var windowMode = this.ResizeMode;
                if (this.ResizeMode != ResizeMode.NoResize)
                {
                    this.ResizeMode = ResizeMode.NoResize;
                }
                this.UpdateLayout();

                DragMove();
                if (this.ResizeMode != windowMode)
                {
                    this.ResizeMode = windowMode;
                }
                this.UpdateLayout();
            }
        }//实现窗口拖动，禁止放大与缩小
        #endregion

        #region 通用属性和字段
        double defaulttime = 600;//默认倒计时时间(600s,10分钟)
        double defaultwarntime = 60;//默认提醒时间(60s，1分钟)
        #endregion

        #region 方法和事件
        bool countDownToZeroEventHandled = false;//时刻为0时引发的事件是否被处理
        public event EventHandler<CountDownWindow>? CountDownToZeroEvent;//时刻为0时引发的事件
        void OnCountDownToZero() =>
            CountDownToZeroEvent?.Invoke(this, this);
        public void RaiseCountDownToZeroEvent() =>
            Task.Run(() => OnCountDownToZero());
        //OnCountDownToZero();

        public event EventHandler<CountDownWindow>? AutoStartStopEvent;//计时器自动启停事件c
        //public event EventHandler? AutoStartStopEvent;//计时器自动启停事件
        void OnAutoStartStop() =>
            AutoStartStopEvent?.Invoke(this, this);//AutoStartStopEvent?.Invoke(this, this);
        #endregion

        public CountDownWindow(int countDownSeconds, int warningSeconds)
        {
            InitializeComponent();

            //《C# 使MessageBox.Show弹出框保持最前》
            //https://blog.csdn.net/qq_41184334/article/details/138279986
            //《WPF 窗口 最前端 Topmost Owner》
            //https://www.cnblogs.com/CSSZBB/p/12016152.html
            //《WPF 让窗口激活作为前台最上层窗口的方法》
            //https://blog.csdn.net/lindexi_gd/article/details/105684558

            Topmost = true;//窗口保持前置
            ResizeMode = ResizeMode.NoResize;//禁止缩放

            LoadingParas(countDownSeconds, warningSeconds);
            InitializeTimer();//初始化Timer                    
        }
        private void LoadingParas(int countDownSeconds, int warningSeconds)
        {
            defaulttime = countDownSeconds;
            defaultwarntime = warningSeconds;
        }

        #region 初始化计时器
        public DispatcherTimer? mainTimer; //主计时器
        public DispatcherTimer? autoCheckTimer; //启停检测计时器
        private void InitializeTimer()
        {
            mainTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) }; //主计时器1s循环
            mainTimer.Tick += Timer_Tick;

            autoCheckTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100) }; //ppt检测计时器100毫秒循环
            autoCheckTimer.Tick += AutoChenkTimer_Tick;
            autoCheckTimer!.Start();
        }      
        #endregion

        #region 主计时器-控制计时
        public string starttime = "", nowtime = "";
        public bool IsCounting { get; private set; } = false;//记录是否处于开始计时状态
        public double change = 0;
        private double times = 0;

        public void Reset(int countDownSeconds, int warningSeconds)
        {
            LoadingParas(countDownSeconds, warningSeconds);
            countDownToZeroEventHandled = false;
            if (IsCounting)
                mainTimer?.Stop();
            times = 0;
            change = 0;
            TimeDisplay(string.Empty, defaulttime);
            time.Foreground = new SolidColorBrush(Colors.Black);
        }
        public void StartAndStop()
        {
            if (!IsCounting)
            {
                starttime = DateTime.Now.ToLongTimeString();
                mainTimer?.Start();
                time.Opacity = 1.0;
                IsCounting = true;
            }
            else
            {
                starttime = nowtime;
                times = change;
                mainTimer?.Stop();

                time.Opacity = 0.6;
                TimeDisplay(string.Empty, defaulttime - change);
                IsCounting = false;
            }
        }

        void Timer_Tick(object? sender, EventArgs e)
        {
            nowtime = DateTime.Now.ToLongTimeString();
            Dopass();
        }
        void Dopass()
        {
            if (starttime.Length == 7)
            {
                starttime = "0" + starttime;
            }
            if (nowtime.Length == 7)
            {
                nowtime = "0" + nowtime;
            }

            char[] starts = starttime.ToCharArray();
            char[] nows = nowtime.ToCharArray();
            int shour, sminute, ssecond;
            int nhour, nminute, nsecond;

            shour = (starts[0] - 48) * 10 + starts[1] - 48;
            sminute = (starts[3] - 48) * 10 + starts[4] - 48;
            ssecond = (starts[6] - 48) * 10 + starts[7] - 48;

            nhour = (nows[0] - 48) * 10 + nows[1] - 48;
            nminute = (nows[3] - 48) * 10 + nows[4] - 48;
            nsecond = (nows[6] - 48) * 10 + nows[7] - 48;
            int changehour, changeminute, changesecond;

            changehour = nhour - shour;
            changeminute = nminute - sminute;
            changesecond = nsecond - ssecond;
            if (changehour >= 0)
            {
                change = changehour * 60 * 60 + changeminute * 60 + changesecond + times;
            }
            else
            {
                change = (changehour + 24) * 60 * 60 + changeminute * 60 + changesecond + times;
            }
            var timeLeft = defaulttime - change;

            if (timeLeft <= 0 && !countDownToZeroEventHandled)
            {               
                RaiseCountDownToZeroEvent();//异步执行事件，避免计时器停止。
                countDownToZeroEventHandled = true;
            }
            else
            { }

            TimeDisplay(string.Empty, timeLeft);
        }  //时间显示刷新
        #endregion

        #region 主计时器-规范化时间显示
        private void TimeDisplay(string state, double timeshow)//时间显示
        {
            string judge = "";
            if (timeshow <= defaultwarntime && timeshow >= 0)//若小于警告时间则字体变蓝
            {
                time.Foreground = new SolidColorBrush(Colors.Blue);
            }
            else if (timeshow < 0)
            {                   
                time.Foreground = new SolidColorBrush(Colors.Red);
                timeshow = Math.Abs(timeshow);
                judge = "-";
            }


            time.Text = state + judge + TimeFormat(Math.Floor(timeshow / 60).ToString())
                + ":" + TimeFormat(Math.Floor(timeshow % 60).ToString());
        }
       
        private static string TimeFormat(string s) //规范化时间
        {
            if (s.Length == 1)
            {
                return "0" + s;
            }
            else
            {
                return s;
            }
        }

        private void Icon2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var result = MessageBox.Show("退出计时器？", "退出", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
                this.Close();
        }
        #endregion

        #region 启停计时器
        private void AutoChenkTimer_Tick(object? sender, EventArgs e)
        {
            OnAutoStartStop();
        }
        #endregion

        //#region PPT检测计时器
        //PPTPlay pptPlay;
        //bool isInSlideShow = false;//PPT是否在放映模式
        //private void PptChenkTimer_Tick(object? sender, EventArgs e)
        //{
        //    var isInSlideShowNow = pptPlay.IsInSlideShowMode;
        //    if (isInSlideShow != isInSlideShowNow)
        //    {
        //        isInSlideShow = isInSlideShowNow;
        //        StartAndStop();
        //        if (isInSlideShowNow == false) //true --> false, 说明PPT放完了
        //        {            
        //            OnCountDownToZero(pptPlay.IsPPTOpened);
        //        }
        //    }
        //    else
        //    { }
        //}
        //#endregion
    }
}
