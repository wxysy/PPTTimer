using System;
using System.Windows.Controls;
using System.Windows.Data;

namespace Infrastructure.Controls.WPFControls
{
    /*【用法示例】-- 仅需在前端设置
        <Window.Resources>
            <ResourceDictionary>
                <!--[第2步]加载DataGridIndexConverter为资源（第1步是定义DataGridIndexConverter）-->
                <local:DataGridIndexConverter x:Key="rowToIndexConverter"/>
            </ResourceDictionary>
        </Window.Resources>
        <Grid>
            <DataGrid x:Name="TestDataGrid" IsReadOnly="True" AutoGenerateColumns="False" Margin="0,167,0,0">
                <DataGrid.Columns>
                    <!--[第3步]在序号列使用资源-->
                    <DataGridTextColumn Header="序"
                                        Binding="{Binding RelativeSource={RelativeSource AncestorType=DataGridRow}, Converter={StaticResource rowToIndexConverter}}" />
                    <!--【DataGridColumn绑定List<Dictionary>>中的Dictionary元素】-->
                    <!--Dictionary中的key当作属性绑定，绑定时加方括号[key]-->
                    <DataGridTextColumn Header="姓名" 
                                        Binding="{Binding [Name]}" Width="60" />
                    ......
                    <!--【DataGridTextColum 显示时间格式化】-->
                    <!--《WPF DataGridTextColum 显示时间格式化》https://www.cnblogs.com/dotnetHui/p/8464834.html-->
                    <DataGridTextColumn Header="时间"
                                        Binding="{Binding [Time], StringFormat='yyyy/MM/dd'}" Width="60" />
                </DataGrid.Columns>
            </DataGrid>
            ......
        </Grid>
     */

    /// <summary>
    /// Datagrid自动生成行号(DataGrid自带排序功能)
    /// </summary>
    public class DataGridIndexConverter : IValueConverter
    {
        //《WPF DataGrid自动生成序号》
        //https://www.cnblogs.com/y-yp/p/7691248.html

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DataGridRow row)
                return row.GetIndex() + 1;
            else
                return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
