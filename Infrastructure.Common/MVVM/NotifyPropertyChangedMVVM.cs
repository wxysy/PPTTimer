using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Infrastructure.Common.MVVM
{
    public abstract class NotifyPropertyChangedMVVM : INotifyPropertyChanged
    {
        #region 定义属性发生变化时引发的事件及相关操作（里面的内容是固定的，直接用。）
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T field, T fValue, [CallerMemberName] string propertyName = "")
        {
            /* 1、【这个方法还是有问题】引用类型的变化有时无法捕捉到
             * 还是直接用 NotifyPropertyChanged(); 方法，没有此问题。
             * 2、很长时间一直搞不懂这个方法怎么用，看了微软的教程终于明白了。
             * 培训 --《在 Windows 应用程序中实现数据绑定》-- 轻松实现 INotifyPropertyChanged
             * https://learn.microsoft.com/zh-cn/training/modules/implement-data-binding-in-windows-10-app/4-implementing-inotifypropertychanged-easy-way?pivots=wpf
             */
            if (EqualityComparer<T>.Default.Equals(field, fValue))//Equals(field, value)
            {
                return false;
            }
            else
            {
                field = fValue;
                NotifyPropertyChanged(propertyName);
                return true;
            }
        }
        #endregion            
    }

    /*【用法示例】*/
    public class DemoViewModel : NotifyPropertyChangedMVVM
    {
        //定义一个其他事件，用于展示属性变化时的其他操作。
        public event EventHandler<string>? OtherEventDemo;
        protected void OnOtherEventDemo([CallerMemberName] string propertyName = "")
        {
            OtherEventDemo?.Invoke(this, propertyName);
        }

        //NotifyPropertyChanged用法
        private string _sample = "示例属性1";
        public string SampleProperty
        {
            set //总是返回void类型
            {
                _sample = value;
                NotifyPropertyChanged();
            }
            get //总是返回属性类型的值
            {
                return _sample;
            }
        }

        //SetProperty用法        
        private string _sample2 = "此控件绑定示例属性2";
        public string SampleProperty2
        {
            set //总是返回void类型
            {
                // SetProperty返回什么值对set访问器都不影响，只影响内部逻辑判断。
                if (SetProperty(ref _sample2, value))
                {
                    //属性变化时的其他操作
                    OnOtherEventDemo();
                }
            }
            get => _sample2; //总是返回属性类型的值
        }
    }
}
