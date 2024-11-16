using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Common.ParallelHandler
{
    //【演示】并行运行没有循环关系的任意同步/异步方法
    public class ParallelInvokeDemo
    {
        private static readonly object balanceLock = new();
        //《lock 语句 - 确保对共享资源的独占访问权限。》
        //https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/statements/lock

        public ObservableCollection<ListViewItemModel> LVItemList { get; set; } = [];

        public void ActiveParallelInvokeAsync(IProgress<string> progress)
        {
            //【并行计算--并行运行没有循环关系的任意同步/异步方法】
            int j = 0;//指示第几个循环
            CancellationTokenSource cts = new();
            ParallelOptions options = new()
            {
                CancellationToken = cts.Token,//同步方法同样可用cts.Cancel();取消
                MaxDegreeOfParallelism = Environment.ProcessorCount,//最大并行度设定
            };

            Parallel.Invoke(options,
                () => //【同步】
                {
                    int i;
                    lock (balanceLock)
                    {
                        i = ++j;
                        //为什么要单独放一个变量？因为在代码运行的过程中，由于并行计算，j就已经改变了。
                    }

                    var item = new ListViewItemModel()
                    {
                        SentenceID = i,
                        Sentence = "示例5-1：【同步】并行运行没有循环关系的同步方法",
                        SentenceScore = 111,
                    };

                    //【异步用】《不支持从调度程序线程以外的线程对其 SourceCollection 进行的更改》
                    //https://blog.csdn.net/Until_youyf/article/details/102720112
                    System.Windows.Application.Current.Dispatcher.Invoke(() => // 多数情况下也可用：This.Dispatcher.Invoke(()=>{})
                    {
                        //--真正业务内容--
                        LVItemList.Add(item);
                        //Progress放在哪里都不受影响
                        progress.Report($"示例5-1：第{i}个项目添加完毕。");
                        //--真正业务内容End--
                    });

                    ////【同步用】《该类型的 CollectionView 不支持从调度程序线程以外的线程对其 SourceCollection 进行的更改。》
                    ////https://www.cnblogs.com/jiangyan219/articles/9248947.html
                    //ThreadPool.QueueUserWorkItem(r =>
                    //{
                    //    var dispather = System.Windows.Application.Current.Dispatcher;
                    //    var dispatherContext = new DispatcherSynchronizationContext(dispather);
                    //    SynchronizationContext.SetSynchronizationContext(dispatherContext);
                    //    SynchronizationContext.Current?.Send(p =>//感觉Send比Post效果好，也卡，但是卡死程度减轻。
                    //    {
                    //        //--真正业务内容--
                    //        LVItemList.Add(item);
                    //        Progress.Report($"示例5-1：第{i}个项目添加完毕。");//Progress在哪里都不受影响
                    //        //--真正业务内容End--
                    //    }, null);
                    //});
                    progress.Report($"示例5-1：已运行第{i}个循环。");
                },
                async () => //【异步】
                {
                    int i;
                    lock (balanceLock)
                    {
                        i = ++j;
                        //为什么要单独放一个变量？因为在代码运行的过程中，由于并行计算，j就已经改变了。
                    }

                    var item = new ListViewItemModel()
                    {
                        SentenceID = i,
                        Sentence = "示例5-2：【异步】并行运行没有循环关系的异步方法",
                        SentenceScore = 222,
                    };

                    await Task.Delay(1000);

                    //【异步用】《不支持从调度程序线程以外的线程对其 SourceCollection 进行的更改》
                    //https://blog.csdn.net/Until_youyf/article/details/102720112
                    System.Windows.Application.Current.Dispatcher.Invoke(() => // 多数情况下也可用：This.Dispatcher.Invoke(()=>{})
                    {
                        //--真正业务内容--
                        LVItemList.Add(item);
                        //Progress放在哪里都不受影响
                        progress.Report($"示例5-2：第{i}个项目添加完毕。");
                        //--真正业务内容End--
                    });

                    ////【同步用】《该类型的 CollectionView 不支持从调度程序线程以外的线程对其 SourceCollection 进行的更改。》
                    ////https://www.cnblogs.com/jiangyan219/articles/9248947.html
                    //ThreadPool.QueueUserWorkItem(r =>
                    //{
                    //    var dispather = System.Windows.Application.Current.Dispatcher;
                    //    var dispatherContext = new DispatcherSynchronizationContext(dispather);
                    //    SynchronizationContext.SetSynchronizationContext(dispatherContext);
                    //    SynchronizationContext.Current?.Send(p =>//感觉Send比Post效果好，也卡，但是卡死程度减轻。
                    //    {
                    //        //--真正业务内容--
                    //        LVItemList.Add(item);
                    //        Progress.Report($"示例5-2：第{i}个项目添加完毕。");//Progress在哪里都不受影响
                    //        //--真正业务内容End--
                    //    }, null);
                    //});
                    progress.Report($"示例5-2：已运行第{i}个循环。");
                });
        }
    }

    public class ListViewItemModel : INotifyPropertyChanged
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

        private int sentenceID;
        public int SentenceID
        {
            get { return sentenceID; }
            set { sentenceID = value; NotifyPropertyChanged(); }
        }

        private string sentence = string.Empty;
        public string Sentence
        {
            get { return sentence; }
            set { sentence = value; NotifyPropertyChanged(); }
        }

        private double sentenceScore;
        public double SentenceScore
        {
            get { return sentenceScore; }
            set { sentenceScore = value; NotifyPropertyChanged(); }
        }

    }
}
