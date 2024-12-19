using System;
using System.Reflection;

namespace Data.Handler.CustomAttribute
{
    public static class ExtendGetCustomAttributeInfo
    {
        /// <summary>
        /// 获取指定对象的自定义特性值_V1.3
        /// </summary>
        /// <typeparam name="TClassAppliedAttributes">应用了特性的类</typeparam>
        /// <typeparam name="TAttributeClass">自定义特性类</typeparam>
        /// <typeparam name="TRAttributeProperty">特性属性类型</typeparam>
        /// <param name="attributeTargets">对象类型(现支持种类：类、属性、方法、方法返回值、接口)</param>
        /// <param name="checkTargetName">对象名称</param>
        /// <param name="attributePropertyName">特性属性名称</param>
        /// <returns>指定对象的特性的属性值(不支持的种类会直接返回null)。</returns>
        private static TRAttributeProperty? GetAttributeValueExtend<TClassAppliedAttributes, TAttributeClass, TRAttributeProperty>(this TClassAppliedAttributes appliedAttributesClass, AttributeTargets attributeTargets, string checkTargetName, string attributePropertyName)
            where TClassAppliedAttributes : class
            where TAttributeClass : Attribute
        {
            //Type type = typeof(TClassAppliedAttributes);
            //TypeInfo typeInfo = typeof(TClassAppliedAttributes).GetTypeInfo();//也能用typeinfo替代type用。

            //****改成扩展方法，方法内部就改了这一句。****
            Type type = appliedAttributesClass.GetType();

            //----------------------------------------------------------------------------------------
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

        /// <summary>
        /// 获取指定对象的自定义特性值_V1.4(泛型扩展方法进一步改进)
        /// </summary>
        /// <typeparam name="TClassAppliedAttributes">应用了特性的类</typeparam>
        /// <typeparam name="TR">待获取的特性值的类型</typeparam>
        /// <param name="attributeTargets">特性应用到的对象类型(现支持种类：类、属性、方法、方法返回值、接口)</param>
        /// <param name="checkTargetName">对象名称</param>
        /// <param name="attributeClassFullName">特性类型的全称(例如，特性“ServiceCustomAttr”全称：“ServiceCustomAttrAttribute”。)</param>
        /// <param name="attributePropertyName">特性属性名称</param>
        /// <returns>返回TR?类型实例。(不支持的种类会直接返回null)。</returns>
        public static TR? GetAttributeValueExtend<TClassAppliedAttributes, TR>(this TClassAppliedAttributes appliedAttributesClass, AttributeTargets attributeTargets, string checkTargetName, string attributeClassFullName, string attributePropertyName)
            where TClassAppliedAttributes : class
        {
            //Type type = typeof(TClassAppliedAttributes);
            //TypeInfo typeInfo = typeof(TClassAppliedAttributes).GetTypeInfo();//也能用typeinfo替代type用。

            //****改成扩展方法，方法内部就改了这一句。****
            Type type = appliedAttributesClass.GetType();

            //----------------------------------------------------------------------------------------
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
                TR? resAttributePropertyValue = default;
                foreach (var item in attributes)
                {
                    Type typeP = item.GetType();
                    if (typeP.Name == attributeClassFullName)
                    {
                        // 读取属性值
                        PropertyInfo? property = typeP.GetProperty(attributePropertyName);
                        var propertyValue = property?.GetValue(item);
                        if (propertyValue is TR tr)
                            resAttributePropertyValue = tr;
                        else
                            resAttributePropertyValue = default;
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

    /* 二、使用扩展方法获取特性信息[通过服务(TService)]。
     * 【V1.3版】
     * //**本方法返回为服务实现(TImplementation)实例。[只不过自动隐式引用转换为服务(TService)实例罢了]**
     * var p1 = App.GetServiceImplementationInstance<IMessageService>();
     * //--获取服务(TService)中的特性信息（仅AttributeTargets变化）--
     * var r1 = p1?.GetAttributeValueExtend<IMessageService, ServiceCustomAttrAttribute, string>(AttributeTargets.Interface, "IMessageService", "ServiceVersion");//--> 1.0.0.0
     * //--获取服务实现(TImplementation)中的特性信息（仅AttributeTargets变化）--
     * var r2 = p1?.GetAttributeValueExtend<IMessageService, ServiceCustomAttrAttribute, string>(AttributeTargets.Class, "", "ServiceVersion");//--> 2.0.0.0        
     * var r3 = p1?.GetAttributeValueExtend<IMessageService, ServiceCustomAttrAttribute, string>(AttributeTargets.Class, "IShow", "ServiceVersion");//--> 3.0.0.0           
     * tb_Show.Text += $"使用扩展方法V1.3获取特性信息[通过服务(TService)]。\n服务类-IMessageService版本：{r1}\n服务实现类-MessageServiceImplement版本：{r2}\n服务实现类内部成员-IShow接口(广义子类)版本：{r3}\n\n";
     * 
     * 【V1.4版】
     * var p21 = App.GetServiceImplementationInstance<IMessageService>();
     * var r21 = p21?.GetAttributeValueExtend(AttributeTargets.Interface, "IMessageService", "ServiceCustomAttrAttribute", "ServiceVersion");//--> 1.0.0.0
     * var r22 = p21?.GetAttributeValueExtend(AttributeTargets.Class, "", "ServiceCustomAttrAttribute", "ServiceVersion");//--> 2.0.0.0        
     * var r23 = p21?.GetAttributeValueExtend(AttributeTargets.Class, "IShow", "ServiceCustomAttrAttribute", "ServiceVersion");//--> 3.0.0.0           
     * tb_Show.Text += $"使用扩展方法V1.4获取特性信息[通过服务(TService)]。\n服务类-IMessageService版本：{r21}\n服务实现类-MessageServiceImplement版本：{r22}\n服务实现类内部成员-IShow接口(广义子类)版本：{r23}\n\n";
     */
}
