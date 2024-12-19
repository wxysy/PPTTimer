using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Handler.Models
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]//AllowMultiple、Inherited 一般可以不写。
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
        public required string RuleName { get; set; } //规则名称
        public required RuleType RuleType { get; set; } //规则类型

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
