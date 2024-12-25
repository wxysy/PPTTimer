using Data.Handler.CustomAttribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Handler.Models
{
    public class RuleMD
    {
        /// <summary>
        /// 获取Rule方法的元数据（自定义特性）
        /// </summary>
        /// <typeparam name="TRule">定义Rule方法（Washing or Checking Method）的类（例如：MiniExcelRules）</typeparam>
        /// <param name="ruleMethodName">Rule方法的名称（例如：nameof(MiniExcelRules.WashingRecordsRule)）</param>
        /// <returns>（是否激活，Rule类型，Rule标题，Rule描述）</returns>
        public static (bool IsActive, RuleType RuleType, string RuleTitle, string RuleDescription) GetRuleMetadata<TRule>(string ruleMethodName)
            where TRule : class
        {
            var isActive = GetCustomAttributeInfo.GetCustomAttributePropertyValue<TRule, RuleAttribute, bool>(AttributeTargets.Method, ruleMethodName, nameof(RuleAttribute.IsActive))!;
            var ruleType = GetCustomAttributeInfo.GetCustomAttributePropertyValue<TRule, RuleAttribute, RuleType>(AttributeTargets.Method, ruleMethodName, nameof(RuleAttribute.RuleType))!;
            var ruleName = GetCustomAttributeInfo.GetCustomAttributePropertyValue<TRule, RuleAttribute, string>(AttributeTargets.Method, ruleMethodName, nameof(RuleAttribute.RuleTitle))!;
            var ruleDescription = GetCustomAttributeInfo.GetCustomAttributePropertyValue<TRule, RuleAttribute, string>(AttributeTargets.Method, ruleMethodName, nameof(RuleAttribute.RuleDescription))!;
            return (isActive, ruleType, ruleName, ruleDescription);
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]//AllowMultiple、Inherited 一般可以不写。
    public sealed class RuleAttribute : System.Attribute
    {
        /*【特性只是一种特殊的类】
         * 1、必须派生自：System.Attribute
         * 2、公共成员只能是：构造函数 + （字段 或 属性 或 两者都有）。
         * 3、类名必须以Attribute结尾。
         * 怎么给属性和字段赋值，见类CheckClass。
         * 怎么获取特性值，见类GetCustomAttributeMethod。
         */

        public required bool IsActive { get; set; } //规则是否激活
        public required string RuleTitle { get; set; } //规则标题
        public required RuleType RuleType { get; set; } //规则类型
        public string RuleDescription { get; set; } = ""; //规则描述

        public RuleAttribute() 
        {
        
        }
    }

    public enum RuleType
    {
        Washing,
        Checking,
    }  
}
