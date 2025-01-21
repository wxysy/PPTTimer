
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
        #region �������Է����仯ʱ�������¼�����ز���������������ǹ̶��ģ�ֱ���á���
        /*--------�����¼��������------------------------------------------------------------------------*/
        /// <summary>
        /// ���Է����仯ʱ�������¼�
        /// </summary>
        //[field: NonSerializedAttribute()]//��֤�¼�PropertyChanged�������л��ı�Ҫ�趨���¼��������л���
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// �����������ǣ�listeners�������Ѿ��仯
        /// </summary>
        /// <param name="propertyName">�仯���������ơ�
        /// ���ǿ�ѡ�������ܹ���CallerMemberName�Զ��ṩ��
        /// ��Ȼ��Ҳ�����ֶ�����</param>
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /* ����ԭ��
         protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
         */

        // �����������Ŀǰ��֪����ɶ��
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

        #region �������ֶ�
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

        #region ������ʵ��
        public ICommand? StartCMD { get; set; }
        List<OutputRecordModel> scoreList = [];
        List<(string Department, double Score)> scoreFinal = [];

        public void LoadingCMDs()
        {
            StartCMD = new MyCommand(async cmdPara =>
            {
                Mes = string.Empty; //�����ʾ
                scoreList.Clear();
                scoreFinal.Clear();

                //��ȡ����ʱ��
                var timeStr = cmdPara as string;
                var timeRes = DateTime.TryParseExact(timeStr, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timeOut);
                if (timeRes == false)
                {
                    progress.Report("��ʼʱ��δ��ȷ�趨��");
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
                    progress.Report("δ��ȡ��������Ϣ");
                }
                else
                {                   
                    //����ȡ�������ơ�ȥ���ظ�¼��ģ������¼���Ϊ׼�����Ȱ����Ʒ��飬����������
                    //��linq������ʵ����������https://blog.csdn.net/qq_39585172/article/details/107201634
                    //��Linq������ٶԷ�����ÿ������ڲ����򣬻�ȡÿ���еĵ�һ����¼��https://blog.csdn.net/zzhzhonghua/article/details/121206103
                    var departments = (from r in scoreList
                                       group r by r.Department into p  //�ȷ���
                                       select p.OrderByDescending(x => x.SubmissionTime).First()  //�����ڽ�������
                                      ).ToList();

                    progress.Report($"\n----���Ҽ�����----"); //���� \n ���׿�����
                    foreach (var dp in departments)
                    {
                        var res = ScoreHandler.ScoreCalculatorV2(scoreList, dp.Department);
                        progress.Report(res.ScoreInfo);
                        scoreFinal.Add((dp.Department, res.Score));
                    }
                    var showList = scoreFinal.OrderByDescending(d => d.Score).ToList();

                    progress.Report($"\n----������������----");
                    progress.Report($"�������ڣ�{timeOut}");
                    int i = 1;
                    foreach (var item in showList)
                    {
                        progress.Report($"{i++}��{item.Department} -- {item.Score:f3}");
                    }
                }                
            }, cmdPara => true);
        }
        #endregion           
    }

}
