
using Data.Common.Serialize;
using Infrastructure.Common.Commands;
using ScoreCaculatorLib.Functions;
using ScoreCaculatorLib.Models;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ScoreCaculatorLib
{
    public class ScoreCaculator: INotifyPropertyChanged
    {
        #region 定义属性发生变化时引发的事件及相关操作（里面的内容是固定的，直接用。）
        /*--------监听事件处理程序------------------------------------------------------------------------*/
        /// <summary>
        /// 属性发生变化时引发的事件
        /// </summary>
        //[field: NonSerializedAttribute()]//保证事件PropertyChanged不被序列化的必要设定。事件不能序列化！
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 提醒侦听者们（listeners）属性已经变化
        /// </summary>
        /// <param name="propertyName">变化的属性名称。
        /// 这是可选参数，能够被CallerMemberName自动提供。
        /// 当然你也可以手动输入</param>
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /* 上面原型
         protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
         */

        // 下面这个方法目前不知道干啥的
        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(storage, value))
            {
                return false;
            }
            else
            {
                storage = value;
                NotifyPropertyChanged(propertyName);

                return true;
            }
        }
        /*------------------------------------------------------------------------------------------------*/
        #endregion

        #region 属性与字段
        IProgress<string> progress;
        private string mes = string.Empty;
        public string Mes
        {
            get { return mes; }
            set { mes = value; NotifyPropertyChanged(); }
        }

        public ScoreCaculator()
        {
            progress = new Progress<string>(p =>
            {
                Mes += $"{p}\n";
            });
            LoadingCMDs();
        }
        #endregion

        #region 命令与实现
        public ICommand? StartCMD { get; set; }
        List<OutputRecordModel> scoreList = [];
        List<(string Department, double Score)> scoreFinal = [];

        public void LoadingCMDs()
        {
            StartCMD = new MyCommand(async cmdPara =>
            {
                Mes = string.Empty; //清空显示
                scoreList.Clear();
                scoreFinal.Clear();

                //获取起算时间
                var timeStr = cmdPara as string;
                var timeRes = DateTime.TryParseExact(timeStr, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timeOut);
                if (timeRes == false)
                {
                    progress.Report("开始时间未正确设定！");
                    return;
                }
                else
                { }

                var readRes = await MiniExcelHandler.SingleToSingleExcelFileHandler7(timeOut, progress, t =>
                {
                    scoreList = (t as List<OutputRecordModel>)!;
                });

                if (readRes == false)
                {
                    progress.Report("未读取到评分信息");
                }
                else
                {                   
                    //【获取科室名称】去除重复录入的，以最后录入的为准。（先按名称分组，再组内排序）
                    //《linq分组再实现组内排序》https://blog.csdn.net/qq_39585172/article/details/107201634
                    //《Linq分组后，再对分组后的每组进行内部排序，获取每组中的第一条记录》https://blog.csdn.net/zzhzhonghua/article/details/121206103
                    var departments = (from r in scoreList
                                       group r by r.Department into p  //先分组
                                       select p.OrderByDescending(x => x.SubmissionTime).First()  //后组内降序排序
                                      ).ToList();

                    progress.Report($"\n----科室计算结果----"); //不加 \n 容易看花眼
                    foreach (var dp in departments)
                    {
                        var res = ScoreHandler.ScoreCalculatorV2(scoreList, dp.Department);
                        progress.Report(res.ScoreInfo);
                        scoreFinal.Add((dp.Department, res.Score));
                    }
                    var showList = scoreFinal.OrderByDescending(d => d.Score).ToList();

                    progress.Report($"\n----科室评分排序----");
                    progress.Report($"评分日期：{timeOut}");
                    int i = 1;
                    foreach (var item in showList)
                    {
                        progress.Report($"{i++}、{item.Department} -- {item.Score:f3}");
                    }
                }                
            }, cmdPara => true);
        }
        #endregion           
    }

}
