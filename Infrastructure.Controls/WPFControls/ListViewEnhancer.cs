using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Infrastructure.Controls.WPFControls
{
    /*【用法示例】- 前端
        <Window.Resources>
            <localViews:ListViewIndexConverter x:Key="MyIndexConverter"/>
        </Window.Resources>
        ......
        <ListView x:Name="listView" ItemsSource="{Binding ListViewItems}" GridViewColumnHeader.Click="ListViewSort_Click" d:ItemsSource="{d:SampleData ItemCount=7}" ScrollViewer.VerticalScrollBarVisibility="Visible" VerticalAlignment="Center" Height="307">
            <ListView.View>
                <GridView>
                    <GridViewColumn>
                        <!--WPF ListView Header添加CheckBox-->
                        <!--《WPF ListView Header Checkbox and MVVM Command》-->
                        <!--https://stackoverflow.com/questions/28546582/wpf-listview-header-checkbox-and-mvvm-command-->
                        <GridViewColumn.HeaderTemplate>
                            <DataTemplate>
                                <!--②-1、添加CheckBox控件到ListViewHeader中，并处理CheckBox点击-->
                                <!--【方式1】（推荐）注意Click事件，配套ListViewEnhancer.SortByColumnHeaderClick方法-->
                                <CheckBox x:Name="cbALL" Click="CheckBoxSelectAll_Click" HorizontalContentAlignment="Center"/>
                                <!--【方式2】（不推荐）用Command绑定，尤其注意这个Command是怎么绑定的，如果直接绑定属性SelectAllCMD会提示找不到，因为模型类ChangeInfo中并没有命令SelectAllCMD-->
                                <!--<CheckBox x:Name="cbALL" Command="{Binding DataContext.SelectAllCMD, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type local:MainWindow}}}" CommandParameter="{Binding IsChecked, RelativeSource={RelativeSource Self}}" HorizontalContentAlignment="Center"/>-->
                                <!--为什么不推荐用Command？解耦【前台的事前端处理，后台的事后端解决】-->
                            </DataTemplate>
                        </GridViewColumn.HeaderTemplate>
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <CheckBox IsChecked="{Binding IsCBChecked}" IsEnabled="{Binding IsLoaded}"/>
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                    <GridViewColumn Header="序号" DisplayMemberBinding="{Binding Converter={StaticResource MyIndexConverter}, Mode=OneWay, RelativeSource={RelativeSource AncestorType={x:Type ListViewItem}, Mode=FindAncestor}}"/>
                    <GridViewColumn Header="展示名称" DisplayMemberBinding="{Binding ListViewData.PluginMD.Title}"/>
                    ......
                </GridView>
            </ListView.View>
        </ListView>
     */

    /*【用法示例】- 后端
        public LicViewWindow()
        {
            InitializeComponent();
            viewModel = new LicViewModel();
            this.DataContext = viewModel;

            lvEnhancer = new(lv_List);
        }
        ......

        #region ListView增强模块（CheckBox、标题排序、全选）    
        ListViewEnhancer lvEnhancer;
        private void ListViewSort_Click(object sender, RoutedEventArgs e)
        {
            //ListView排序
            lvEnhancer.SortByColumnHeaderClick(sender, e);
        }
        private void CheckBoxSelectAll_Click(object sender, RoutedEventArgs e)
        {
            //ListView全选
            if (e.Source is CheckBox cb)//sender is CheckBox cb 也可以
                lvEnhancer.SelectAll<ListViewItemModel>(cb.IsChecked, nameof(ListViewItemModel.IsCBChecked));//用反射方式更新IsSelected属性
        }
        #endregion
     */

    /// <summary>
    /// ListView控件增强类【点击Checkbox全选，点击标题排序】
    /// </summary>
    /// <param name="listView"></param>
    public class ListViewEnhancer(ListView listView) : Window//必须要继承Window，排序方法要用。
    {
        #region 点击ListView控件各列头部实现排序
        //【***要和前端GridViewColumnHeader.Click配合使用***】
        //《如何：在单击标题时对 GridView 列进行排序》
        //https://learn.microsoft.com/zh-cn/dotnet/desktop/wpf/controls/how-to-sort-a-gridview-column-when-a-header-is-clicked?view=netframeworkdesktop-4.8
        //WPF中ListView控件没有ListViewItemSorter属性。
        //【不能用】《使用 Visual C# 使用列对 ListView 控件进行排序》
        //【不能用】https://learn.microsoft.com/zh-cn/troubleshoot/developer/visualstudio/csharp/language-compilers/sort-listview-by-column

        GridViewColumnHeader? _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;
        public void SortByColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            ListSortDirection direction;
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    var sortRes = Sort(sortBy, direction);

                    if (sortRes)
                    {
                        if (direction == ListSortDirection.Ascending)
                        {
                            headerClicked.Column.HeaderTemplate =
                                Resources["HeaderTemplateArrowUp"] as DataTemplate;//必须要继承Window，不然报错。
                        }
                        else
                        {
                            headerClicked.Column.HeaderTemplate =
                                Resources["HeaderTemplateArrowDown"] as DataTemplate;//必须要继承Window，不然报错。
                        }

                        // Remove arrow from previously sorted header
                        if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                        {
                            _lastHeaderClicked.Column.HeaderTemplate = null;
                        }

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                    else
                    { }
                }
            }
        }

        private bool Sort(string? sortBy, ListSortDirection direction)
        {
            //【有些列可以排序，有些列不能排序（比如 序号 列）】
            try//没报错就是可以排序的列
            {
                ICollectionView dataView =
                    CollectionViewSource.GetDefaultView(listView.ItemsSource);
                dataView.SortDescriptions.Clear();
                SortDescription sd = new(sortBy, direction);
                dataView.SortDescriptions.Add(sd);
                dataView.Refresh();
                return true;
            }
            catch//报错就是不能排序的列
            {
                return false;
            }
        }
        #endregion

        #region 全选列表项
        public void SelectAll<T>(bool? isChecked, string propertyName)
        {
            if (listView.ItemsSource is Collection<T> source)
            {
                foreach (var item in source)
                {
                    var propertyInfo = typeof(T).GetProperty(propertyName);
                    propertyInfo?.SetValue(item, isChecked, null);//反射-设定值
                    //propertyInfo?.GetValue(item, null);//反射-读取值
                }
            }
            else
            { }
        }
        #endregion
    }
}
