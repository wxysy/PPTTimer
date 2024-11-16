using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Data.Common.Serialize
{
    public class MyXmlSerialize
    {
        /* 参考文献
         * 1、《XmlSerializer 类》
         * https://learn.microsoft.com/zh-cn/dotnet/api/system.xml.serialization.xmlserializer?view=net-8.0
         * 2、《XML 序列化》
         * https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/introducing-xml-serialization
         * XML 序列化只将对象的公共字段和属性值序列化为 XML 流。 
         * XML 序列化不能转换方法、索引器、私有字段或只读属性（只读集合除外）。 
         * XML 序列化不包括类型信息。
         * 若要序列化对象的所有公共和私有字段和属性，请使用 DataContractSerializer 而不要使用 XML 序列化。
         * 3、XML 不能序列化 Dictionary 类型（微软定的）
         * 【可以通过将 Dictionary 转换为 Array，或者 List 来解决】
         * 原因："......cannot be serialized because it does not have a parameterless constructor."
         * 《Creating instance of type without default constructor in C# using reflection》
         * https://stackoverflow.com/questions/390578/creating-instance-of-type-without-default-constructor-in-c-sharp-using-reflectio
         * 《RuntimeHelpers.GetUninitializedObject(Type) 方法》
         * https://learn.microsoft.com/zh-cn/dotnet/api/system.runtime.compilerservices.runtimehelpers.getuninitializedobject?view=net-8.0
         * FormatterServices类已过时，FormatterServices.GetUninitializedObject(Type) 方法同样过时。
         */

        #region Xml序列化标准模块（也供标准版用）
        /// <summary>
        /// Xml序列化
        /// </summary>
        /// <remarks>
        /// 1、XML序列化只能将对象的公共字段和属性值序列化为XML流。
        /// 2、XML 序列化不能转换方法、索引器、私有字段或只读属性（只读集合除外），{private set; get;}也算只读属性，{init; get;}不算。 
        /// 3、XML序列化不包括类型信息。
        /// 4、若要序列化对象的所有公共和私有字段和属性，请使用 DataContractSerializer 而不要使用XML序列化。
        /// 5、XML 不能序列化 Dictionary 类型（微软定的）
        /// </remarks>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">对象实例</param>
        /// <returns>[字节数组（已序列化的数据）]</returns>
        public static byte[] XmlSerializeToBytes<T>(T obj)
        {
            using MemoryStream ms = new();
            var serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(ms, obj);
            byte[] res = ms.ToArray();
            return res;
        }

        /// <summary>
        /// Xml反序列化      
        /// </summary>
        /// <remarks>
        /// 1、只能反序列化对象的公共字段和属性值，且无法反序列化Dictionary对象。
        /// 2、返回为 null 或 默认值，很可能就是反序列化失败。
        /// 3、XML 不能反序列化 Dictionary 类型（微软定的）
        /// </remarks>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="buffer">字节数组（已序列化的数据）</param>
        /// <returns>[原类型实例] 或 null</returns>
        public static T? XmlDeserializeFromBytes<T>(byte[] buffer)
        {
            using MemoryStream ms = new(buffer);
            ms.Seek(0, SeekOrigin.Begin);
            var serializer = new XmlSerializer(typeof(T));
            /* 除了XAttribute节点，其他类型节点都派生自XNode类。
             * 因此要单独写2种处理方法。*/
            serializer.UnknownNode += Serializer_UnknownNode;//意外节点处理
            serializer.UnknownAttribute += Serializer_UnknownAttribute;//意外特性处理
            object? obj = serializer.Deserialize(ms);
            if (obj is T r)
                return r;
            else
                return default;
        }
        #endregion

        #region Xml通用模块（异常处理）
        private static void Serializer_UnknownAttribute(object? sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            throw new Exception($"Unknown attribute:{attr.Name}\t{attr.Value}");
        }
        private static void Serializer_UnknownNode(object? sender, XmlNodeEventArgs e)
        {
            throw new Exception($"Unknown Node:{e.Name}\t{e.Text}");
        }
        #endregion

        #region Xml文件生成与读取（标准版）
        /// <summary>
        /// 【标准版】将对象存储为xml文件
        /// （XML不能序列化Dictionary类型，微软定的）
        /// </summary>
        /// <remarks>
        /// Dictionary类型 -- 不行，报错。
        /// 普通属性 -- 可以；
        /// init属性 -- 可以；
        /// 只读属性 -- 不行，不报错；
        /// private set属性 -- 不行，报错。
        /// </remarks>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="input">对象实例</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>([是否成功])</returns>
        public static bool XmlFileCreater<T>(T input, string filePath, IProgress<string>? progress = null)
        {
            try
            {
                if (Path.GetExtension(filePath) != ".xml")
                    return false;
                if (File.Exists(filePath))
                    File.Delete(filePath);

                var bufferTotal = XmlSerializeToBytes(input);
                using var fs = new FileStream(filePath, FileMode.OpenOrCreate);
                fs.Write(bufferTotal, 0, bufferTotal.Length);
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 【标准版】将对象从xml文件读取
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <param name="bufferLength">读取缓存大小（默认256byte）</param>
        /// <returns>([对象类型实例] 或 null)</returns>
        public static T? XmlFileReader<T>(string filePath, int bufferLength = 256, IProgress<string>? progress = null)
        {
            try
            {
                if (File.Exists(filePath) != true || Path.GetExtension(filePath) != ".xml")
                    throw new ArgumentException("文件不存在或者后缀名不为xml");

                using var fs = new FileStream(filePath, FileMode.Open);
                int readLen = 0;
                List<byte> bufferList = [];
                do
                {
                    var buffer = new byte[bufferLength];
                    readLen = fs.Read(buffer, 0, buffer.Length);
                    if (readLen == buffer.Length)
                    {
                        bufferList.AddRange(buffer);
                    }
                    else if (0 < readLen && readLen < buffer.Length)
                    {
                        var temp = new byte[readLen];
                        Array.Copy(buffer, temp, readLen);
                        bufferList.AddRange(temp);
                    }
                    else
                    { }
                } while (readLen > 0);
                var bufferTotal = bufferList.ToArray();
                //《合并byte数组》
                //https://www.cnblogs.com/yuwuji/p/8081897.html

                var obj = XmlDeserializeFromBytes<T>(bufferTotal);
                return obj;
            }
            catch (Exception ex)
            {
                progress?.Report(ex.Message);
                return default;
            }
        }
        #endregion

        #region Xml文件生成与读取（直接版）
        /// <summary>
        /// 【直接版】将对象存储为xml文件
        /// （XML不能序列化Dictionary类型，微软定的）
        /// </summary>
        /// <remarks>
        /// Dictionary类型 -- 不行，报错。
        /// 普通属性 -- 可以；
        /// init属性 -- 可以；
        /// 只读属性 -- 不行，不报错；
        /// private set属性 -- 不行，报错。
        /// </remarks>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">对象实例</param>
        /// <param name="filePath">文件路径（后缀名必须是【.xml】）</param>
        /// <returns>([是否成功])</returns>
        public static bool XmlSerializeToFile<T>(T obj, string filePath, IProgress<string>? progress = null)
        {
            try
            {
                if (Path.GetExtension(filePath) != ".xml")
                    return false;
                if (File.Exists(filePath))
                    File.Delete(filePath);

                using var fs = new FileStream(filePath, FileMode.OpenOrCreate);
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(fs, obj);
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 【直接版】将对象从xml文件读取
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="filePath">文件路径（后缀名必须是【.xml】）</param>
        /// <returns>([对象类型实例] 或 null)</returns>
        public static T? XmlDeserializeFromFile<T>(string filePath, IProgress<string>? progress = null)
        {
            try
            {
                if (File.Exists(filePath) != true || Path.GetExtension(filePath) != ".xml")
                    throw new ArgumentException("文件不存在或者后缀名不为xml");

                using var fs = new FileStream(filePath, FileMode.Open);
                var serializer = new XmlSerializer(typeof(T));
                /* 除了XAttribute节点，其他类型节点都派生自XNode类。
                 * 因此要单独写2种处理方法。*/
                serializer.UnknownNode += Serializer_UnknownNode;//意外节点处理
                serializer.UnknownAttribute += Serializer_UnknownAttribute;//意外特性处理
                object? obj = serializer.Deserialize(fs);
                if (obj is T r)
                    return r;
                else
                    return default;
            }
            catch (Exception ex)
            {
                progress?.Report(ex.Message);
                return default;
            }
        }
        #endregion

        #region Dictionary与Array、List的相互转化
        /// <summary>
        /// Dictionary转化为Array（Dictionary不允许被xml序列化）
        /// </summary>
        /// <typeparam name="TKey">Dict-Key</typeparam>
        /// <typeparam name="TValue">Dict-Value</typeparam>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static (TKey Key, TValue Value)[] DictionaryToArray<TKey, TValue>(Dictionary<TKey, TValue> dict)
            where TKey : notnull
        {
            (TKey, TValue)[] array = new (TKey, TValue)[dict.Count];
            int i = 0;
            foreach (var item in dict)
            {
                array[i] = (item.Key, item.Value);
                i++;
            }
            return array;
        }

        /// <summary>
        /// Array转化为Dictionary（Dictionary不允许被xml序列化）
        /// </summary>
        /// <typeparam name="TKey">Dict-Key</typeparam>
        /// <typeparam name="TValue">Dict-Value</typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> ArrayToDictionary<TKey, TValue>((TKey Key, TValue Value)[] array)
            where TKey : notnull
        {
            Dictionary<TKey, TValue> dict = [];
            foreach (var item in array)
            {
                dict.Add(item.Key, item.Value);
            }
            return dict;
        }

        /// <summary>
        /// Dictionary转化为List（Dictionary不允许被xml序列化）
        /// </summary>
        /// <typeparam name="TKey">Dict-Key</typeparam>
        /// <typeparam name="TValue">Dict-Value</typeparam>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static List<(TKey Key, TValue Value)> DictionaryToList<TKey, TValue>(Dictionary<TKey, TValue> dict)
            where TKey : notnull
        {
            List<(TKey, TValue)> list = [];
            foreach (var item in dict)
            {
                list.Add((item.Key, item.Value));
            }
            return list;
        }

        /// <summary>
        /// List转化为Dictionary（Dictionary不允许被xml序列化）
        /// </summary>
        /// <typeparam name="TKey">Dict-Key</typeparam>
        /// <typeparam name="TValue">Dict-Value</typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> ListToDictionary<TKey, TValue>(List<(TKey Key, TValue Value)> list)
            where TKey : notnull
        {
            Dictionary<TKey, TValue> dict = [];
            foreach (var item in list)
            {
                dict.Add(item.Key, item.Value);
            }
            return dict;
        }
        #endregion
    }
}
