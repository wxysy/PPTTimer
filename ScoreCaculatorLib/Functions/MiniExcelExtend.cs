using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MiniExcelLibs.OpenXml;
using MiniExcelLibs;

namespace ScoreCaculatorLib.Functions
{
    /// <summary>
    /// 此类本质上是封装了Nuget包MiniExcel，参考：https://gitee.com/dotnetchina/MiniExcel。
    /// 按目前的理解，还是喜欢MiniExcel强类型（即定义了记录类型的）下的导出与导入，感觉不容易出错。
    /// </summary>
    public class MiniExcelExtend
    {
        //--csv和xlsx互转--
        //MiniExcel.ConvertCsvToXlsx();
        //MiniExcel.ConvertXlsxToCsv();

        //--读取时清除空行(Dictionary类型)--
        public static IEnumerable<IDictionary<string, object>> QueryWithoutEmptyRow(string path, bool useHeaderRow, string sheetName)
        {
            var config = new OpenXmlConfiguration()
            {
                FillMergedCells = true,// 处理合并的单元格填充(如果此种情况没有则完全不需要)
            };
            var rows = MiniExcel.Query(path, useHeaderRow, sheetName, configuration: config);
            foreach (IDictionary<string, object> row in rows.Select(v => (IDictionary<string, object>)v))
            {
                if (row.Keys.Any(key => row[key] != null))
                    yield return row;
            }
        }

        //--读取时清除空行(强类型)--
        public static IEnumerable<T> QueryWithoutEmptyRow<T>(string path, string sheetName)
            where T : class, new()
        {
            var config = new OpenXmlConfiguration()
            {
                FillMergedCells = true,// 处理合并的单元格填充(如果此种情况没有则完全不需要)
            };
            var rows = MiniExcel.Query<T>(path, sheetName, configuration: config);
            foreach (var row in rows)
            {
                //--方法1：最初设想，但是不起作用--
                //if (row != new T() && row != default(T) && row.ID==5)
                //{
                //    yield return row;
                //}

                //--方法2：改进型想法，验证通过--
                var rType = row.GetType();
                PropertyInfo[] propertyInfos = rType.GetProperties();
                if (propertyInfos.Any(
                    p =>
                    {
                        var pType = p.PropertyType;
                        var pDefaultValue = p.PropertyType.IsValueType ? Activator.CreateInstance(p.PropertyType) : null;//获取变量的默认值
                        var pValue = p.GetValue(row);
                        var res = !(pValue?.Equals(pDefaultValue)) ?? false;//【这一句很关键，之前问题就卡在这！！！】
                        return res;
                    }))
                {
                    yield return row;
                }
            }
            //《C#获取类型的默认值》
            //https://blog.csdn.net/jumtre/article/details/83386501


            //--方法3：部分有效，但float不行，double无法转换为float，会报错--
            //var rows = QueryWithoutEmptyRow(path, true, sheetName);
            //List<T> result = new List<T>();
            //foreach (var row in rows)
            //{
            //    Type type = typeof(T);
            //    var ins = Activator.CreateInstance(type);
            //    foreach (var item in row)
            //    {
            //        PropertyInfo? propertyInfo = type.GetProperty(item.Key);
            //        propertyInfo?.SetValue(ins, item.Value);
            //    }
            //    if (ins is T res)
            //    {
            //        result.Add(res);
            //    }
            //}
            //return result;
        }

        /// <summary>
        /// 读取excel文件(强类型)通用方法
        /// </summary>
        /// <typeparam name="Trecord">记录模型类（行：记录，列：字段）</typeparam>
        /// <param name="filepath">文件路径</param>
        /// <param name="sheetName">页面名称(读取所有页面名称：var sheetNames = MiniExcel.GetSheetNames(filepath);)</param>
        /// <param name="excelRecordHandler">读取之后对每行记录干什么</param>
        /// <returns>是否读取成功</returns>
        public static bool GeneralReadOnStrongType<Trecord>(string filepath, string sheetName, Action<Trecord> excelRecordHandler)
            where Trecord : class, new()
        {
            try
            {
                #region MiniExcel强类型通用导入流程(顺便清理空行)
                // 1、获取Excel文件中所有页面名称
                var sheetNames = MiniExcel.GetSheetNames(filepath);
                // 2、查找待导入页面名
                string sheetNameSelected = sheetName;
                // 3、导入页面中的所有记录
                bool sheetNameContains = sheetNames.Contains(sheetNameSelected);
                if (sheetNameContains == true)
                {
                    var sheetRecords = QueryWithoutEmptyRow<Trecord>(filepath, sheetNameSelected);

                    //除非必要请不要使用 ToList 等方法读取全部数据到内存(MiniExcel.Query(exportFilepath).ToList();)，严重影响性能。
                    //强类型导入无需定义待导入Excel文件中的所有字段，且可以定义待导入Excel文件中不包含的字段。
                    //导入方法只导入两者相匹配的数据，其他不导入，不报错。

                    // 3-1.2、Dictionary类型导入（感觉虽然方便，但是很容易出错，设定第一行数据为key）
                    //var sheetRecords = MiniExcel.Query(exportFilepath, sheetName: searchValue_SheetName, useHeaderRow: true);

                    // 4、使用记录数据(设定第一行数据为key后，强类型和Dictionary类型的使用一致)
                    foreach (var excelRecord in sheetRecords)
                    {
                        excelRecordHandler(excelRecord);
                    }
                    return true;
                }
                else
                { return false; }
                #endregion             
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 写入excel文件(强类型)通用方法
        /// </summary>
        /// <typeparam name="Trecord">记录模型类（行：记录，列：字段）</typeparam>
        /// <param name="filepath">文件路径</param>
        /// <param name="excelSheets">excel页面集合(Key:页面名称，Value:页面记录集合)</param>
        /// <returns>是否写入成功</returns>
        public static bool GeneralWriteOnStrongType<Trecord>(string filepath, Dictionary<string, List<Trecord>> excelSheets)
            where Trecord : class, new()
        {
            /* 生成Excel的难点不在于怎么生成文件，而在于怎么生成结构化的数据(也就是第1-3步)。
             * 一、Excel文件的数据结构
             * book ← new Dictionary<string, object>()
             *   --sheet1
             *   --sheet2
             *   (...)
             *   --sheetN ← new List<T>() 或 new List<Dictionary<string, object>>()
             *       --record1
             *       --record2
             *       (...)
             *       --recordN ← new T() 或 new Dictionary<string, object>()
             *       
             * 二、文件内容编程顺序：record → sheet → book
             * 
             * 三、生成结构化的数据
            // 1、生成记录(如果是循环，除非必要请不要使用 ToList 等方法读取全部数据到内存，严重影响性能)
            // 1-1、Dictionary模式（通用方法，灵活，易对应出错。）
            var record_1 = new Dictionary<string, object>()
            {
                ["Column1"] = "MiniExcel",//为字段赋值，[字段名(列标题)] = 字段值
                ["Column2"] = 1,
                ["Column5"] = "extraMiniExcel",//**强类型导出时可以包含强类型未定义的字段**
            };
            // 1-2、Dictionary模式（【推荐用法】灵活且不容易对应出错。）
            var record_2 = new Dictionary<string, object>()
            {
                [nameof(ExcelSelectedRecordModel.Column1)] = "Github",//**强类型属性名 = 字段名**
                [nameof(ExcelSelectedRecordModel.Column2)] = 2,
                ["Column5"] = "extraGithub",
            };
            // 1-3、强类型模式（方便但不灵活了）
            var record_3 = new ExcelSelectedRecordModel()
            {
                Column1 = "EPPlus",//为字段赋值，强类型属性名 → 字段名，[字段名(列标题)] = 字段值
                Column2 = 1,
                Column3 = "infoEPPlus",
                //这种写法就无法包含强类型未定义的字段"Column5"。
            };

            // 2、生成页面
            // 2-1、Dictionary类型模式
            var sheet_1 = new List<Dictionary<string, object>>()
            {
                record_1,//添加一条记录   
                record_2,//生成一条记录
            };
            // 2-2、强类型模式
            var sheet_2 = new List<ExcelSelectedRecordModel>()
            {
                record_3,//添加一条记录   
            };

            // 3、生成工作簿（也就是Excel文件的内容），只能用Dictionary类型模式。
            var book = new Dictionary<string, object>()
            {
                ["sheet_D"] = sheet_1,
                ["sheet_T"] = sheet_2,
            };
             */

            #region MiniExcel强类型通用导出流程
            try
            {
                // 1、定义工作簿
                var excelBook = new Dictionary<string, object>();
                foreach (var excelSheet in excelSheets)
                {
                    var (sheetName, sheetInfo) = (excelSheet.Key, excelSheet.Value);
                    excelBook.Add(sheetName, sheetInfo);
                }

                // 2、3、都在excelSheets里面了。
                // excelSheets页面集合(Key:页面名称string，Value:页面记录集合List<Trecord>)

                // 4、生成Excel文件
                // 生成Excel的难点不在于怎么生成文件，而在于怎么生成结构化的数据(也就是第1-3步)。
                var config = new OpenXmlConfiguration()
                {
                    TableStyles = TableStyles.Default,// 表格样式选择(不加也行，就是默认样式)
                };
                MiniExcel.SaveAs(filepath, excelBook, configuration: config, overwriteFile: true);
                return true;
            }
            catch
            { return false; }
            #endregion
        }

        //--生成Excel文件(弱类型)（这个没法通用化）--
        public static bool GeneralWrite(string filepath, Dictionary<string, object> excelBook)
        {
            /* 生成Excel的难点不在于怎么生成文件，而在于怎么生成结构化的数据(也就是第1-3步)。
             * 一、Excel文件的数据结构
             * book ← new Dictionary<string, object>()
             *   --sheet1
             *   --sheet2
             *   (...)
             *   --sheetN ← new List<T>() 或 new List<Dictionary<string, object>>()
             *       --record1
             *       --record2
             *       (...)
             *       --recordN ← new T() 或 new Dictionary<string, object>()
             *       
             * 二、文件内容编程顺序：record → sheet → book
             * 
             * 三、生成结构化的数据
            // 1、生成记录(如果是循环，除非必要请不要使用 ToList 等方法读取全部数据到内存，严重影响性能)
            // 1-1、Dictionary模式（通用方法，灵活，易对应出错。）
            var record_1 = new Dictionary<string, object>()
            {
                ["Column1"] = "MiniExcel",//为字段赋值，[字段名(列标题)] = 字段值
                ["Column2"] = 1,
                ["Column5"] = "extraMiniExcel",//**强类型导出时可以包含强类型未定义的字段**
            };
            // 1-2、Dictionary模式（【推荐用法】灵活且不容易对应出错。）
            var record_2 = new Dictionary<string, object>()
            {
                [nameof(ExcelSelectedRecordModel.Column1)] = "Github",//**强类型属性名 = 字段名**
                [nameof(ExcelSelectedRecordModel.Column2)] = 2,
                ["Column5"] = "extraGithub",
            };
            // 1-3、强类型模式（方便但不灵活了）
            var record_3 = new ExcelSelectedRecordModel()
            {
                Column1 = "EPPlus",//为字段赋值，强类型属性名 → 字段名，[字段名(列标题)] = 字段值
                Column2 = 1,
                Column3 = "infoEPPlus",
                //这种写法就无法包含强类型未定义的字段"Column5"。
            };

            // 2、生成页面
            // 2-1、Dictionary类型模式
            var sheet_1 = new List<Dictionary<string, object>>()
            {
                record_1,//添加一条记录   
                record_2,//生成一条记录
            };
            // 2-2、强类型模式
            var sheet_2 = new List<ExcelSelectedRecordModel>()
            {
                record_3,//添加一条记录   
            };

            // 3、生成工作簿（也就是Excel文件的内容），只能用Dictionary类型模式。
            var book = new Dictionary<string, object>()
            {
                ["sheet_D"] = sheet_1,
                ["sheet_T"] = sheet_2,
            };
             */

            #region MiniExcel强类型通用导出流程
            try
            {
                // 4、生成Excel文件
                // 生成Excel的难点不在于怎么生成文件，而在于怎么生成结构化的数据(也就是第1-3步)。
                var config = new OpenXmlConfiguration()
                {
                    TableStyles = TableStyles.Default,// 表格样式选择(不加也行，就是默认样式)
                };
                MiniExcel.SaveAs(filepath, excelBook, configuration: config, overwriteFile: true);
                return true;
            }
            catch
            { return false; }
            #endregion
        }
    }
}
