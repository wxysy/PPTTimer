using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Handler.Models
{
    public class RuleModel<T>
    {
        public required bool IsActive { get; set; } //规则是否激活
        public string RuleName { get; set; } = string.Empty; //规则名称
        public required RuleType RuleType { get; set; } //规则类型
        public Func<T[], (bool Res, T[] DataWashed, T? ErrorItem)>? WashingRule { get; set; } //清洗方法
        public Func<List<T>, (bool Res, T? ErrorItem)>? CheckingRule { get; set; } //检测方法
    }
    public enum RuleType
    {
        Washing,
        Checking,
    }
}
