using System;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Infrastructure.Common.Commands
{
    public class MyCommand : ICommand, INotifyPropertyChanged
    {
        #region 定义属性发生变化时引发的事件及相关操作（里面的内容是固定的，直接用。）
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
        #endregion

        public MyCommand(Action<object?> cmdExecuteHandler, Predicate<object?>? cmdCanExecute)
        {
            commandHandler = cmdExecuteHandler;
            canExecute = cmdCanExecute;
        }

        #region 字段
        private Action<object?> commandHandler;
        private Predicate<object?>? canExecute;
        #endregion

        #region 实现ICommand接口
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            if (commandHandler != null)
                commandHandler.Invoke(parameter);
        }
        #endregion

    }

    public class MyCommand<TResult> : ICommand, INotifyPropertyChanged
    {
        #region 定义属性发生变化时引发的事件及相关操作（里面的内容是固定的，直接用。）
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
        #endregion

        public MyCommand(Func<object?, TResult?> cmdExecuteHandler, Predicate<object?>? cmdCanExecute)
        {
            commandHandler = cmdExecuteHandler;
            canExecute = cmdCanExecute;
        }

        #region 字段和属性
        private Func<object?, TResult?> commandHandler;
        private Predicate<object?>? canExecute;

        private TResult? result;

        public TResult? Result
        {
            get { return result; }
            set { result = value; NotifyPropertyChanged(); }
        }

        #endregion

        #region 实现ICommand接口
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            if (commandHandler != null)
                Result = commandHandler.Invoke(parameter);
        }
        #endregion

    }
}
