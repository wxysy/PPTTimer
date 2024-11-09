using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace Infrastructure.Controls.WPFControls
{
    /*【用法示例】- 前端
    <Window.Resources>
        <HierarchicalDataTemplate x:Key="tvDT" ItemsSource="{Binding Path=Elements}">
            <TextBlock Text="{Binding Path=Attribute[Title].Value}"/>
        </HierarchicalDataTemplate>
    </Window.Resources>
    ........
        <TreeView x:Name="treeView" Margin="10,38,0,0"
                  VirtualizingStackPanel.IsVirtualizing="True" 
                  ItemsSource="{Binding Path=Root.Elements}" 
                  ItemTemplate="{StaticResource ResourceKey=tvDT}">
        </TreeView>*/

    /*【用法示例】- 后端
     TreeViewEnhancer tvEnhancer = new();
     ........
     private void Btn_Search_Click(object sender, RoutedEventArgs e)
     {
         tvEnhancer.SelectTreeViewItemByXElement(treeView, x => x.Attribute("Title")?.Value == tb_Search.Text);
     }
    */

    /// <summary>
    /// TreeView控件增强类【根据筛选条件跳转到指定节点】
    /// </summary>
    public class TreeViewEnhancer
    {
        #region 选择TreeView控件指定项
        int searchIndex = -1;
        List<XElement> selectedItems = [];
        public void SelectTreeViewItemByXElement(TreeView treeView, Predicate<XElement> predicate, bool isMessageBoxShowed = true)
        {
            if (searchIndex == -1)
            {
                selectedItems.Clear();
                var tvItems = treeView.Items;
                foreach (var item in tvItems)
                {
                    if (item is XElement xe)
                    {
                        var col = xe.SearchXElements(predicate);
                        if (col.Any())
                            selectedItems.AddRange(col);
                    }
                    else
                    { }
                }
                if (selectedItems.Count > 0)
                { searchIndex = 0; }
                else
                {
                    if (isMessageBoxShowed)
                        MessageBox.Show("未搜索到匹配项");
                }
            }

            if (selectedItems.Count > 0)
            {
                var findItem = FindTreeViewItem(treeView, selectedItems[searchIndex++]);
                if (findItem != null)
                    findItem.IsSelected = true;
                if (searchIndex == selectedItems.Count)
                {
                    searchIndex = -1;
                    if (isMessageBoxShowed)
                        MessageBox.Show("已搜寻完毕");
                }
            }
        }

        // 通过数据内容跳转到指定的控件节点上        
        private static TreeViewItem? FindTreeViewItem(ItemsControl item, object data)
        {
            //《WPF TreeView控件根据数据内容跳转到指定节点》
            //https://www.cnblogs.com/dongweian/p/17265801.html
            // 不管 HierarchicalDataTemplate 中控件是什么类型，该方法均适用。

            TreeViewItem? findItem = null;
            for (int i = 0; i < item.Items.Count; i++)
            {
                if (item.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem tvItem)
                {
                    if (tvItem == null)
                    {
                        continue;
                    }

                    if (tvItem.DataContext == data)
                    {
                        findItem = tvItem;
                        break;
                    }
                    else if (tvItem.Items.Count > 0)
                    {
                        if (!tvItem.IsExpanded)
                        {
                            // 打开未选中节点
                            tvItem.SetValue(TreeViewItem.IsExpandedProperty, true);
                            tvItem.UpdateLayout();
                        }
                        findItem = FindTreeViewItem(tvItem, data);
                        if (findItem != null)
                        {
                            break;
                        }
                        else
                        {
                            // 关闭未选中节点
                            tvItem.SetValue(TreeViewItem.IsExpandedProperty, false);
                            tvItem.UpdateLayout();
                        }
                    }
                }
            }
            return findItem;
        }
        #endregion
    }

    public static class XElementCRUDExtend
    {
        public static IEnumerable<XElement> SearchXElements(this XElement xe, Predicate<XElement> predicate)
        {
            IEnumerable<XElement> selects =
                from xel in xe.DescendantsAndSelf()
                where predicate(xel)
                select xel;
            return selects;
        }
    }
}
