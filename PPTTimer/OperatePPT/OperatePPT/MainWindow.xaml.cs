﻿
using PPTOperateLib.CountDown;
using PPTOperateLib.Play;
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
using System.Windows.Threading;

namespace OperatePPT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //《C# 使MessageBox.Show弹出框保持最前》
            //https://blog.csdn.net/qq_41184334/article/details/138279986
            //《WPF 窗口 最前端 Topmost Owner》
            //https://www.cnblogs.com/CSSZBB/p/12016152.html
            //《WPF 让窗口激活作为前台最上层窗口的方法》
            //https://blog.csdn.net/lindexi_gd/article/details/105684558

            PPTPlay pptPlay = new();
            CountDownWindow timerWindow = new(int.Parse(tb_CountDownSeconds.Text), int.Parse(tb_WarningSeconds.Text));
            timerWindow.CountDownToZeroEvent += (sender, e) =>
            {
                pptPlay.PPTClose();//关闭PPT
                this.Dispatcher.Invoke(() => e.Close());//计时器关闭            
            };

            bool isInSlideShow = false;//PPT是否在放映模式
            timerWindow.AutoStartStopEvent += (sender, e) =>
            {
                var isPPTRunning = pptPlay.IsPPTOpened;
                if (isPPTRunning) //反正没有这个判断就失去焦点
                {
                    
                    var isInSlideShowNow = pptPlay.IsInSlideShowMode;
                    if (isInSlideShow != isInSlideShowNow)
                    {
                        isInSlideShow = isInSlideShowNow;
                        e.StartAndStop();
                        if (isInSlideShowNow == false) //true --> false, 说明PPT放完了
                        {
                            e.RaiseCountDownToZeroEvent();
                        }
                    }
                    else
                    { }
                }
                else
                {
                    //e.Close();//反正只要加了e.Close()这一句就失去焦点
                }
            };
            timerWindow.Show();

            var filePath = tb_FilePath.Text;
            pptPlay.PPTOpen(filePath);
        }
    }
}