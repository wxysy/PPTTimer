using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Data.Common.Serialize
{
    public class MySerialize
    {
        #region 微软官方DataContract序列化
        /* 【DataContractSerializer是微软官方推荐的BinaryFormatter替代类型】
         * 参考文献
         * 1、《使用 BinaryFormatter 和相关类型时的反序列化风险》
         * https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/binaryformatter-security-guide
         * 2、《XmlSerializer不能序列化Dictionary对象。。。》
         * https://www.cnblogs.com/fuhongwei041/archive/2009/08/28/1555692.html
         * System.NotSupportedException : Cannot serialize member corp.Azure.DictProperties of type System.Collections.Hashtable, because it implements IDictionary.
         * 原来是因为Dictionary类型只实现的ISerializable接口，而没有实现IXmlSerializable接口，所以在.net 3.0以前用XmlSerializer是不能序列化的。
         * 3、《DataContractSerializer 类》
         * https://learn.microsoft.com/zh-cn/dotnet/api/system.runtime.serialization.datacontractserializer?view=net-8.0
         * 4、《XmlObjectSerializer.WriteObject 方法》
         * https://learn.microsoft.com/zh-cn/dotnet/api/system.runtime.serialization.xmlobjectserializer.writeobject?view=net-8.0
         * 5、《XmlObjectSerializer.ReadObject 方法》
         * https://learn.microsoft.com/zh-cn/dotnet/api/system.runtime.serialization.xmlobjectserializer.readobject?view=net-7.0
         * 6、《SerializableAttribute 类》
         * https://learn.microsoft.com/zh-cn/dotnet/api/system.serializableattribute?view=net-7.0
         * 7、《NonSerializedAttribute 类》
         * https://learn.microsoft.com/zh-cn/dotnet/api/system.nonserializedattribute?view=net-7.0&redirectedfrom=MSDN
         * 8、《类型“System.ComponentModel.PropertyChangedEventManager”未标记为可序列化》
         * https://blog.csdn.net/cocoasprite/article/details/44888061
         */
        /* 一、关于下面2个方法的说明：
         * 1.1 下面2个方法是参照上述文献，【是根据自己需求改进的】，不是原样照搬。
         * 1.2 经过长时间的试验使用，达到预期效果。
         * 二、关于要序列化类的说明：
         * 2.1 对于将要序列化的类，定义时可以加特性，也可以不加。
         * 2.2 但根据实际编程的经验，一般都要加。
         * 2.3 加特性方法1（Serializable 和 NonSerialized）
         * 即在“类名”上加特性[Serializable]，在不想序列化的“成员”上加特性[NonSerialized]。
         * 事件不能序列化，要加 [field:NonSerializedAttribute()]。
         * 2.4 加特性方法2（DataContract 和 DataMember）
         * 即在“类名”上加特性[DataContract]，在“成员”上加特性[DataMember]。
         * 但如果加的话，两个特性必须一起使用，在一个类定义中，不能只有[DataContract]或只有[DataMember]。
         */

        /// <summary>
        /// DataContract序列化（微软官方推荐BinaryFormatter替代类型，本质上是xml序列化）        
        /// </summary>
        /// <remarks>
        /// 1、可以序列化类中几乎一切元素，可以序列化Dictionary对象。
        /// 2、在待序列化的类定义时，“类名”上加特性[Serializable]，在不想序列化的“成员”上加特性[NonSerialized]。
        /// 3、事件不能序列化，要加 [field:NonSerializedAttribute()]。
        /// </remarks>
        /// <typeparam name="T">待序列化的类型</typeparam>
        /// <param name="obj">类型实例</param>
        /// <returns>[字节数组（已序列化的数据）]</returns>
        public static byte[] DataContractSerializeToBytes<T>(T obj)
        {
            using MemoryStream ms = new();
            var type = typeof(T);
            DataContractSerializer contractSerializer = new(type);
            contractSerializer.WriteObject(ms, obj);
            byte[] res = ms.ToArray();
            return res;
        }

        /// <summary>
        /// DataContract反序列化（微软官方推荐的BinaryFormatter替代类型，本质上是xml序列化）
        /// </summary>
        /// <remarks>
        /// 1、可以反序列化类中几乎一切元素，可以反序列化Dictionary对象。
        /// 2、返回为 null 或 默认值，很可能就是反序列化失败。
        /// </remarks>
        /// <typeparam name="T">已序列化数据的原类型</typeparam>
        /// <param name="buffer">字节数组（已序列化的数据）</param>
        /// <returns>[原类型实例] 或 null</returns>
        public static T? DataContractDeserializeFromBytes<T>(byte[] buffer)
        {
            using MemoryStream ms = new(buffer);
            var type = typeof(T);
            ms.Seek(0, SeekOrigin.Begin);
            DataContractSerializer contractSerializer = new(type);
            object? obj = contractSerializer.ReadObject(ms);
            //在微软的例子中是有XmlDictionaryReader的，但是用了它之后反倒报错。不用它反而怪好。

            if (obj is T t)//return (T?)obj;
                return t;
            else
                return default;
        }
        #endregion

        #region 微软官方System.Text.Json序列化
        /*【参考】
         * 1、《如何将 .NET 对象编写为 JSON（序列化）》-- 序列化为 UTF-8
         * https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/system-text-json/how-to#serialize-to-utf-8
         * 2、《如何将 JSON 读取为 .NET 对象（反序列化）》-- 从 UTF-8 进行反序列化
         * https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/system-text-json/deserialization#deserialize-from-utf-8
         */

        /// <summary>
        /// Json序列化（微软官方System.Text.Json库）
        /// </summary>
        /// <typeparam name="T">待序列化的类型</typeparam>
        /// <param name="obj">类型实例</param>
        /// <returns>[字节数组（已序列化的数据）]</returns>
        public static byte[] JsonSerializeToBytes<T>(T obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj);
        }

        /// <summary>
        /// Json反序列化（微软官方System.Text.Json库）
        /// </summary>
        /// <typeparam name="T">已序列化数据的原类型</typeparam>
        /// <param name="buffer">字节数组（已序列化的数据）</param>
        /// <returns>[原类型实例] 或 null</returns>
        public static T? JsonDeserializeFromBytes<T>(byte[] buffer)
        {
            return JsonSerializer.Deserialize<T>(buffer);
        }
        #endregion
    }
}
