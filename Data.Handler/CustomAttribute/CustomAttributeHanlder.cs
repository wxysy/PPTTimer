using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Data.Handler.CustomAttribute
{
    public static class CustomAttributeHanlder
    {
        /// <summary>
        /// 获取目标对象自定义特性的属性值_V1.3(.Net8版本)
        /// </summary>
        /// <typeparam name="TAttribute">自定义特性类</typeparam>
        /// <typeparam name="TAttributeProperty">自定义特性类中属性的类型</typeparam>
        /// <param name="targetResideClassType">目标对象所在的那个类的类型</param>
        /// <param name="targetType">目标对象的类型(现支持种类：类、属性、方法、方法返回值、接口)</param>
        /// <param name="targetName">目标对象的名称</param>
        /// <param name="attributePropertyName">需获取的特性属性的名称</param>
        /// <returns>目标对象的特性的属性值(不支持的种类会直接返回null)。</returns>
        public static TAttributeProperty? GetPropertyInfo<TAttribute, TAttributeProperty>(Type targetResideClassType, AttributeTargets targetType, string targetName, string attributePropertyName)
        {
            //Type type = typeof(TTargetInClass);
            //TypeInfo typeInfo = typeof(TCheckClass).GetTypeInfo();//也能用typeinfo替代type用。
            Type type = targetResideClassType;

            object[]? attributes = default;
            //object[]? attributes = System.Attribute.GetCustomAttributes(type);//也能用这种写法获取特性

            switch (targetType)
            {
                case AttributeTargets.Assembly:
                    break;
                case AttributeTargets.Module:
                    break;
                case AttributeTargets.Class:// 这个类是个广泛含义的类，单独定义的接口类，也是类，也用它。
                    var tName = type.Name;
                    if (tName == targetName || string.IsNullOrWhiteSpace(targetName))
                    {
                        attributes = type.GetCustomAttributes(false);//获取类的特性描述
                    }
                    else
                    {
                        MemberInfo[] members = type.GetMember(targetName, BindingFlags.Public | BindingFlags.NonPublic);
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
                    attributes = type.GetMethod(targetName)?.GetCustomAttributes(false);//获取类中指定方法的特性描述
                    break;
                case AttributeTargets.Property:
                    attributes = type.GetProperty(targetName)?.GetCustomAttributes(false);//获取类中指定属性的特性描述
                    break;
                case AttributeTargets.Field:
                    break;
                case AttributeTargets.Event:
                    break;
                case AttributeTargets.Interface:
                    /****人家指的是由当前 Type 实现或继承的特定接口。****/
                    //不是单独的接口类，接口类，还是类，按类处理。
                    attributes = type.GetInterface(targetName)?.GetCustomAttributes(false);
                    //获取继承类中的元素(方法、属性等)的特性如下。
                    //attributes = type.GetInterface(checkTargetName)?.GetMethod("MyProperty")?.GetCustomAttributes(false);
                    break;
                case AttributeTargets.Parameter:
                    break;
                case AttributeTargets.Delegate:
                    break;
                case AttributeTargets.ReturnValue:
                    // 返回值种类很多，最常用的就是方法的返回值。
                    attributes = type.GetMethod(targetName)?.ReturnTypeCustomAttributes.GetCustomAttributes(false);//获取类中指定方法的返回值的特性描述
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
                TAttributeProperty? resAttributePropertyValue = default;
                foreach (var item in attributes)
                {
                    if (item is TAttribute myAttr && myAttr != null)
                    {
                        // 读取属性值
                        Type typeP = typeof(TAttribute);
                        PropertyInfo? property = typeP.GetProperty(attributePropertyName);
                        resAttributePropertyValue = property?.GetValue(myAttr) is TAttributeProperty tRes ? tRes : default;
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

        /// <summary>
        /// [扩展方法]获取目标对象自定义特性的属性值_V1.3(.Net8版本)
        /// </summary>
        /// <typeparam name="TTargetResideClass">目标对象所在的那个类</typeparam>
        /// <typeparam name="TAttribute">自定义特性类</typeparam>
        /// <typeparam name="TAttributeProperty">自定义特性类中属性的类型</typeparam>
        /// <param name="attributeClassName">自定义特性类的全称(例如，特性“MyAttr”全称：“MyAttrAttribute”)</param>
        /// <param name="targetType">目标对象的类型(现支持种类：类、属性、方法、方法返回值、接口)</param>
        /// <param name="targetName">目标对象的名称</param>
        /// <param name="attributePropertyName">需获取的特性属性的名称</param>
        /// <returns>目标对象的特性的属性值(不支持的种类会直接返回null)。</returns>
        public static TAttributeProperty? GetAttributePropertyInfoExtend<TTargetResideClass, TAttribute, TAttributeProperty>(this TTargetResideClass targetResideClass, AttributeTargets targetType, string targetName, string attributePropertyName)
            where TTargetResideClass : class
        {
            //****改成扩展方法，方法内部就改了这一句。****
            Type targetResideClassType = targetResideClass.GetType();

            return GetPropertyInfo<TAttribute, TAttributeProperty>(targetResideClassType, targetType, targetName, attributePropertyName);
        }
    }
}
