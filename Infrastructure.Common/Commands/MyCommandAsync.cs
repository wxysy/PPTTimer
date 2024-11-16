using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Infrastructure.Common.Commands
{
    public class MyCommandAsync<Tprogress> : AsyncCommandBase, INotifyPropertyChanged
    {
        #region 定义属性发生变化时引发的事件及相关操作
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region 构造函数
        public MyCommandAsync(Func<object?, IProgress<Tprogress>?, CancellationToken, Task> cmdHandler, Predicate<object?>? cmdCanExecute, IProgress<Tprogress>? cmdProgress)
        {
            commandHandler = cmdHandler;
            commandProgress = cmdProgress;
            commandCanExecute = cmdCanExecute;
            cancelThisAsyncCommand = new CancelAsyncCommand();
            cancelThisAsyncCommand.CancelAsyncCommandExecuteEvent += CancelThisAsyncCommand_CancelAsyncCommandExecuteEvent;
            CancelToken = cancelThisAsyncCommand.Token;
        }

        private void CancelThisAsyncCommand_CancelAsyncCommandExecuteEvent(object? sender, CancellationToken e)
        {
            OnCommandCancelled(sender, e);
        }

        #endregion

        #region 字段与属性
        private Func<object?, IProgress<Tprogress>?, CancellationToken, Task> commandHandler;
        private IProgress<Tprogress>? commandProgress;
        private Predicate<object?>? commandCanExecute;

        private NotifyTaskCompletion? execution;
        public NotifyTaskCompletion? Execution
        {
            get { return execution; }
            set { execution = value; NotifyPropertyChanged(); }
        }

        private CancelAsyncCommand cancelThisAsyncCommand;
        public ICommand CancelThisAsyncCommand => cancelThisAsyncCommand;//只读属性

        public CancellationToken CancelToken { get; }
        #endregion

        #region 委托与事件
        public event EventHandler<CancellationToken>? CommandCancelledEvent;
        private void OnCommandCancelled(object? sender, CancellationToken token)
        {
            CommandCancelledEvent?.Invoke(sender, token);
        }
        #endregion

        #region 实现抽象类AsyncCommandBase
        public override bool CanExecute(object? parameter)
        {
            bool condition1 = Execution == null || Execution.IsCompleted;
            bool condition2;
            if (commandCanExecute != null)
            {
                condition2 = commandCanExecute(parameter);
            }
            else
            {
                condition2 = true;
            }
            bool res = condition1 && condition2;
            return res;
        }

        public override async Task ExecuteAsync(object? parameter)
        {
            cancelThisAsyncCommand.NotifyCommandStarting();
            CancellationToken token = cancelThisAsyncCommand.Token;
            var taskT = commandHandler.Invoke(parameter, commandProgress, token);
            Execution = new NotifyTaskCompletion(taskT);
            RaiseCanExecuteChanged();
            await Execution.TaskCompletion;
            cancelThisAsyncCommand.NotifyCommandFinished();
            RaiseCanExecuteChanged();

        }
        #endregion
    }

    public class MyCommandAsync<Tprogress, TResult> : AsyncCommandBase, INotifyPropertyChanged
    {
        #region 定义属性发生变化时引发的事件及相关操作
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region 构造函数
        public MyCommandAsync(Func<object?, IProgress<Tprogress>?, CancellationToken, Task<TResult?>> cmdHandler, Predicate<object?>? cmdCanExecute, IProgress<Tprogress>? cmdProgress)
        {
            commandHandler = cmdHandler;
            commandProgress = cmdProgress;
            commandCanExecute = cmdCanExecute;
            cancelThisAsyncCommand = new CancelAsyncCommand();
            cancelThisAsyncCommand.CancelAsyncCommandExecuteEvent += CancelThisAsyncCommand_CancelAsyncCommandExecuteEvent;
            CancelToken = cancelThisAsyncCommand.Token;
        }

        private void CancelThisAsyncCommand_CancelAsyncCommandExecuteEvent(object? sender, CancellationToken e)
        {
            OnCommandCancelled(sender, e);
        }
        #endregion

        #region 字段与属性
        private Func<object?, IProgress<Tprogress>?, CancellationToken, Task<TResult?>> commandHandler;
        private IProgress<Tprogress>? commandProgress;
        private Predicate<object?>? commandCanExecute;

        private NotifyTaskCompletion<TResult?>? execution;
        public NotifyTaskCompletion<TResult?>? Execution
        {
            get { return execution; }
            set { execution = value; NotifyPropertyChanged(); }
        }

        private CancelAsyncCommand cancelThisAsyncCommand;
        public ICommand CancelThisAsyncCommand => cancelThisAsyncCommand;

        public CancellationToken CancelToken { get; }
        #endregion

        #region 委托与事件
        public event EventHandler<CancellationToken>? CommandCancelledEvent;
        private void OnCommandCancelled(object? sender, CancellationToken token)
        {
            CommandCancelledEvent?.Invoke(sender, token);
        }
        #endregion

        #region 实现抽象类AsyncCommandBase
        public override bool CanExecute(object? parameter)
        {
            bool condition1 = Execution == null || Execution.IsCompleted;
            bool condition2;
            if (commandCanExecute != null)
            {
                condition2 = commandCanExecute(parameter);
            }
            else
            {
                condition2 = true;
            }
            bool res = condition1 && condition2;
            return res;
        }

        public override async Task ExecuteAsync(object? parameter)
        {
            cancelThisAsyncCommand.NotifyCommandStarting();
            CancellationToken token = cancelThisAsyncCommand.Token;
            var taskT = commandHandler.Invoke(parameter, commandProgress, token);
            Execution = new NotifyTaskCompletion<TResult?>(taskT);
            RaiseCanExecuteChanged();
            await Execution.TaskCompletion;
            cancelThisAsyncCommand.NotifyCommandFinished();
            RaiseCanExecuteChanged();
        }
        #endregion     
    }

    sealed class CancelAsyncCommand : ICommand
    {
        private CancellationTokenSource _cts = new();
        private bool _commandExecuting;
        public CancellationToken Token { get { return _cts.Token; } }

        public event EventHandler<CancellationToken>? CancelAsyncCommandExecuteEvent;
        private void OnCancelAsyncCommandExecuteEvent(object? sender, CancellationToken token)
        {
            CancelAsyncCommandExecuteEvent?.Invoke(sender, token);
        }

        public void NotifyCommandStarting()
        {
            _commandExecuting = true;
            if (!_cts.IsCancellationRequested)
                return;
            _cts = new CancellationTokenSource();
            RaiseCanExecuteChanged();
        }

        public void NotifyCommandFinished()
        {
            _commandExecuting = false;
            RaiseCanExecuteChanged();
        }

        bool ICommand.CanExecute(object? parameter)
        {
            return _commandExecuting && !_cts.IsCancellationRequested;
        }

        void ICommand.Execute(object? parameter)
        {
            _cts.Cancel();
            RaiseCanExecuteChanged();
            OnCancelAsyncCommandExecuteEvent(this, Token);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        private void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public abstract class AsyncCommandBase : IAsyncCommand
    {
        #region 实现IAsyncCommand接口
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public abstract bool CanExecute(object? parameter);

        public async void Execute(object? parameter)
        {
            await ExecuteAsync(parameter);
        }

        public abstract Task ExecuteAsync(object? parameter);
        #endregion

        protected void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
