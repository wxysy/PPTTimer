using System;
using System.Reflection;

namespace Data.Handler.CustomAttribute
{
    public class GetCustomAttributeInfo
    {
        /// <summary>
        /// 获取指定对象的自定义特性值_V1.2
        /// </summary>
        /// <typeparam name="TClassAppliedAttributes">指定对象所在的类</typeparam>
        /// <typeparam name="TAttributeClass">自定义特性类</typeparam>
        /// <typeparam name="TRAttributeProperty">特性属性类型</typeparam>
        /// <param name="attributeTargets">对象类型(现支持种类：类、属性、方法、方法返回值、接口)</param>
        /// <param name="checkTargetName">对象名称</param>
        /// <param name="attributePropertyName">特性属性名称</param>
        /// <returns>指定对象的特性的属性值(不支持的种类会直接返回null)。</returns>
        public static TRAttributeProperty? GetCustomAttributePropertyValue<TClassAppliedAttributes, TAttributeClass, TRAttributeProperty>(AttributeTargets attributeTargets, string checkTargetName, string attributePropertyName)
        {
            Type type = typeof(TClassAppliedAttributes);
            //TypeInfo typeInfo = typeof(TClassAppliedAttributes).GetTypeInfo();//也能用typeinfo替代type用。
            object[]? attributes = default;
            //object[]? attributes = System.Attribute.GetCustomAttributes(type);//也能用这种写法获取特性

            switch (attributeTargets)
            {
                case AttributeTargets.Assembly:
                    break;
                case AttributeTargets.Module:
                    break;
                case AttributeTargets.Class:// 这个类是个广泛含义的类，单独定义的接口类，也是类，也用它。
                    var tName = type.Name;
                    if (tName == checkTargetName || string.IsNullOrWhiteSpace(checkTargetName))
                    {
                        attributes = type.GetCustomAttributes(false);//获取类的特性描述
                    }
                    else
                    {
                        MemberInfo[] members = type.GetMember(checkTargetName, BindingFlags.Public | BindingFlags.NonPublic);
                        if (members.Length == 1)
                        {
                            attributes = members[0].GetCustomAttributes(false);//获取类的子类(广义：类中子类、类中接口都算)特性描述
                        }
                        else
                        { }
                    }
                    break;
                case AttributeTargets.Struct:
                    break;
                case AttributeTargets.Enum:
                    break;
                case AttributeTargets.Constructor:
                    break;
                case AttributeTargets.Method:
                    attributes = type.GetMethod(checkTargetName)?.GetCustomAttributes(false);//获取类中指定方法的特性描述
                    break;
                case AttributeTargets.Property:
                    attributes = type.GetProperty(checkTargetName)?.GetCustomAttributes(false);//获取类中指定属性的特性描述
                    break;
                case AttributeTargets.Field:
                    break;
                case AttributeTargets.Event:
                    break;
                case AttributeTargets.Interface:
                    /****人家指的是由当前 Type 实现或继承的特定接口。****/
                    //不是单独的接口类，接口类，还是类，按类处理。
                    attributes = type.GetInterface(checkTargetName)?.GetCustomAttributes(false);
                    //获取继承类中的元素(方法、属性等)的特性如下。
                    //attributes = type.GetInterface(checkTargetName)?.GetMethod("MyProperty")?.GetCustomAttributes(false);
                    break;
                case AttributeTargets.Parameter:
                    break;
                case AttributeTargets.Delegate:
                    break;
                case AttributeTargets.ReturnValue:
                    // 返回值种类很多，最常用的就是方法的返回值。
                    attributes = type.GetMethod(checkTargetName)?.ReturnTypeCustomAttributes.GetCustomAttributes(false);//获取类中指定方法的返回值的特性描述
                    break;
                case AttributeTargets.GenericParameter:
                    break;
                case AttributeTargets.All:
                    break;
                default:
                    break;
            }

            if (attributes?.Length > 0)
            {
                TRAttributeProperty? resAttributePropertyValue = default;
                foreach (var item in attributes)
                {
                    if (item is TAttributeClass myAttr && myAttr != null)
                    {
                        // 读取属性值
                        Type typeP = typeof(TAttributeClass);
                        PropertyInfo? property = typeP.GetProperty(attributePropertyName);
                        resAttributePropertyValue = property?.GetValue(myAttr) is TRAttributeProperty tRes ? tRes : default;
                        break;
                    }
                    else
                    { }
                }
                return resAttributePropertyValue;
            }
            else
            {
                return default;
            }
        }
    }

    /* 一、使用普通方法获取特性信息。
     * //--获取服务(TService)中的特性信息--
     * var res_Ver1 = GetCustomAttributeInfo.GetCustomAttributePropertyValue<IMessageService, ServiceCustomAttrAttribute, string>(AttributeTargets.Class, "IMessageService", "ServiceVersion");
     * //--获取服务实现(TImplementation)中的特性信息--
     * var res_Ver2 = GetCustomAttributeInfo.GetCustomAttributePropertyValue<MessageServiceImplement, ServiceCustomAttrAttribute, string>(AttributeTargets.Class, "", "ServiceVersion");
     * var res_Ver3 = GetCustomAttributeInfo.GetCustomAttributePropertyValue<MessageServiceImplement, ServiceCustomAttrAttribute, string>(AttributeTargets.Class, "IShow", "ServiceVersion");
     * tb_Show.Text += $"使用普通方法获取特性信息。\n服务类-IMessageService版本：{res_Ver1}\n服务实现类-MessageServiceImplement版本：{res_Ver2}\n服务实现类内部成员-IShow接口(广义子类)版本：{res_Ver3}\n\n";
     */

    // 如何定义自定义特性
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]//AllowMultiple、Inherited 一般可以不写。
    public sealed class MyAttributeAttribute : Attribute
    {
        /*【特性只是一种特殊的类】
         * 1、必须派生自：System.Attribute
         * 2、公共成员只能是：构造函数 + （字段 或 属性 或 两者都有）。
         * 3、类名必须以Attribute结尾。
         * 怎么给属性和字段赋值，见类CheckClass。
         * 怎么获取特性值，见类GetCustomAttributeMethod。
         */

        public string Version { get; private set; }//用位置参数处理
        public string Description { get; set; } = string.Empty;//用命名参数处理
        public MyAttributeAttribute(string ver)
        {
            Version = ver;
        }
    }

    // 如何使用自定义特性到类、方法、方法返回值、属性上。
    // 特性中的属性和字段：Version用位置参数赋值，Description用命名参数赋值。
    [MyAttribute("1.0.0.0", Description = "特性的版本号说明-类")]
    internal class CheckClass : ICheckClass
    {
        // 特性中的属性和字段：Version用位置参数赋值，Description用命名参数赋值。
        [MyAttribute("2.0.0.0", Description = "特性的版本号说明-方法")]
        [return: MyAttribute("3.0.0.0", Description = "特性的版本号说明-方法返回值")]
        public string CheckMethod()
        {
            return "112233";
        }

        [MyAttribute("4.0.0.0", Description = "特性的版本号说明-属性")]
        public string CheckProperty { get; set; } = string.Empty;

        [MyAttribute("5.0.0.0", Description = "特性的版本号说明-(广义)子类")]
        class CheckSubClass
        { }

        //实现接口中方法
        [MyAttribute("7.0.0.0", Description = "特性的版本号说明-实现接口的方法")]
        public void Check(string name, string value)
        {

        }
    }

    [MyAttribute("6.0.0.0", Description = "特性的版本号说明-(类继承的)接口")]
    interface ICheckClass
    {
        void Check(string name, string value);
    }
}
