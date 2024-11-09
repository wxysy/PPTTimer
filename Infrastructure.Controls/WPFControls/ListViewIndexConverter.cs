using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Infrastructure.Controls.WPFControls
{
    /// <summary>
    /// 自动列表序号(配合ListViewEnhancer使用)
    /// </summary>
    public class ListViewIndexConverter : IValueConverter
    {
        //《WPF中ListView序号自动生成》
        //https://zhuanlan.zhihu.com/p/37087886

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ListViewItem item = (ListViewItem)value;
            ListView listView = (ItemsControl.ItemsControlFromItemContainer(item) as ListView)!;
            return listView!.ItemContainerGenerator.IndexFromContainer(item) + 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
