
using Data.Common.Serialize;
using Infrastructure.Common.Commands;
using ScoreCaculatorLib.Functions;
using System.ComponentModel;
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
        List<(string Department, string ScoreType, double Score)> scoreList = [];
        List<(string Department, double Score)> scoreFinal = [];

        public void LoadingCMDs()
        {
            StartCMD = new MyCommand(async cmdPara =>
            {
                Mes = string.Empty; //清空显示
                scoreList.Clear();
                scoreFinal.Clear();

                var readRes = await MiniExcelHandler.SingleToSingleExcelFileHandler6(progress, t =>
                {
                    scoreList = (t as List<(string Department, string ScoreType, double Score)>)!;
                });

                if (readRes == false)
                {
                    progress.Report("未读取到评分信息");
                }
                else
                {
                    //string[] departments =
                    //["大数据中心", "办公室", "政策和规划科", "数据资源科", "数字经济科", "数字科技和基础设施建设科", "数字政务与应用科",
                    //"政务改革协调科","安全管理科", "投资服务科", "项目管理科", "审批服务一科", "审批服务二科", "政务服务科", "政务监督科",
                    //"机关党委人事科", "机关纪委",];// 机关党委（人事科） 命名特别

                    //string[] departments = 
                    //["办公室", "数字科技和基础设施建设科", "机关纪委",];

                    //string departmentNamePath = $@".\Settings\DepartmentNames.xml";
                    string departmentNamePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $@".\Settings\DepartmentNames.xml";
                    string[] departments = MyXmlSerialize.XmlDeserializeFromFile<string[]>(departmentNamePath) ?? [];

                    progress.Report($"----有效票数统计----");
                    foreach (var dp in departments)
                    {
                        var res = ScoreHandler.ScoreCalculatorV2(scoreList, dp);
                        progress.Report(res.ScoreInfo);
                        scoreFinal.Add((dp, res.Score));
                    }
                    var showList = scoreFinal.OrderByDescending(d => d.Score).ToList();

                    progress.Report($"----科室评分排序----");
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
