using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Infrastructure.Common.Commands
{
    public class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
    {
        #region 定义属性发生变化时引发的事件及相关操作（没有使用CallerMemberName特性，为了下面使用。）
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public NotifyTaskCompletion(Task<TResult?> cmdTask)
        {
            ThisTask = cmdTask;
            TaskCompletion = WatchTaskAsync(cmdTask);

        }

        #region 方法
        private async Task WatchTaskAsync(Task taskWithoutTresult)
        {
            try
            {
                await taskWithoutTresult;
            }
            catch
            {
            }
            finally
            {
                NotifyPropertiesChanged(taskWithoutTresult);
            }
        }

        private void NotifyPropertiesChanged(Task taskWithoutTresult)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;

            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsNotCompleted));

            if (taskWithoutTresult.IsCanceled)
            {
                OnPropertyChanged(nameof(IsCanceled));
            }
            else if (taskWithoutTresult.IsFaulted)
            {
                OnPropertyChanged(nameof(IsFaulted));
                OnPropertyChanged(nameof(Exception));
                OnPropertyChanged(nameof(InnerException));
                OnPropertyChanged(nameof(ErrorMessage));
            }
            else
            {
                OnPropertyChanged(nameof(IsSuccessfullyCompleted));
                OnPropertyChanged(nameof(Result));
            }
        }
        #endregion

        #region 属性
        public Task<TResult?> ThisTask { get; private set; }
        public Task TaskCompletion { get; private set; }
        public TResult? Result
        {
            get
            {
                return (ThisTask.Status == TaskStatus.RanToCompletion) ?
                    ThisTask.Result : default(TResult);
            }
        }
        public TaskStatus Status { get { return ThisTask.Status; } }
        public bool IsCompleted { get { return ThisTask.IsCompleted; } }
        public bool IsNotCompleted { get { return !ThisTask.IsCompleted; } }
        public bool IsSuccessfullyCompleted
        {
            get
            {
                return ThisTask.Status ==
                    TaskStatus.RanToCompletion;
            }
        }
        public bool IsCanceled { get { return ThisTask.IsCanceled; } }
        public bool IsFaulted { get { return ThisTask.IsFaulted; } }
        public AggregateException? Exception { get { return ThisTask.Exception; } }
        public Exception? InnerException
        {
            get
            {
                return (Exception == null) ?
                    null : Exception.InnerException;
            }
        }
        public string? ErrorMessage
        {
            get
            {
                return (InnerException == null) ?
                    null : InnerException.Message;
            }
        }
        #endregion
    }

    public class NotifyTaskCompletion : INotifyPropertyChanged
    {
        #region 定义属性发生变化时引发的事件及相关操作（没有使用CallerMemberName特性，为了下面使用。）
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public NotifyTaskCompletion(Task cmdTask)
        {
            ThisTask = cmdTask;
            TaskCompletion = WatchTaskAsync(cmdTask);
        }

        #region 方法
        private async Task WatchTaskAsync(Task taskWithoutTresult)
        {
            try
            {
                await taskWithoutTresult;
            }
            catch
            {
            }
            finally
            {
                NotifyPropertiesChanged(taskWithoutTresult);
            }
        }

        private void NotifyPropertiesChanged(Task taskWithoutTresult)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;

            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsNotCompleted));

            if (taskWithoutTresult.IsCanceled)
            {
                OnPropertyChanged(nameof(IsCanceled));
            }
            else if (taskWithoutTresult.IsFaulted)
            {
                OnPropertyChanged(nameof(IsFaulted));
                OnPropertyChanged(nameof(Exception));
                OnPropertyChanged(nameof(InnerException));
                OnPropertyChanged(nameof(ErrorMessage));
            }
            else
            {
                OnPropertyChanged(nameof(IsSuccessfullyCompleted));
                OnPropertyChanged(nameof(Result));
            }
        }
        #endregion

        #region 属性
        public Task ThisTask { get; private set; }
        public Task TaskCompletion { get; private set; }
        public string Result
        {
            get
            {
                return "Task是没有Task<T>.Result属性的。此属性在这里无意义。";
            }
        }
        public TaskStatus Status { get { return ThisTask.Status; } }
        public bool IsCompleted { get { return ThisTask.IsCompleted; } }
        public bool IsNotCompleted { get { return !ThisTask.IsCompleted; } }
        public bool IsSuccessfullyCompleted
        {
            get
            {
                return ThisTask.Status ==
                    TaskStatus.RanToCompletion;
            }
        }
        public bool IsCanceled { get { return ThisTask.IsCanceled; } }
        public bool IsFaulted { get { return ThisTask.IsFaulted; } }
        public AggregateException? Exception { get { return ThisTask.Exception; } }
        public Exception? InnerException
        {
            get
            {
                return (Exception == null) ?
                    null : Exception.InnerException;
            }
        }
        public string? ErrorMessage
        {
            get
            {
                return (InnerException == null) ?
                    null : InnerException.Message;
            }
        }
        #endregion
    }
}
