using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Handler.Models
{
    public class RuleModel<TItem>
    {
        public required bool IsActive { get; set; } //规则是否激活
        public required string RuleName { get; set; } //规则名称
        public required RuleType RuleType { get; set; } //规则类型
        public Func<List<TItem>, (bool Res, List<TItem> DataWashed, TItem? ErrorItem)>? WashingRule { get; set; } //清洗方法
        public Func<List<TItem>, (bool Res, TItem? ErrorItem)>? CheckingRule { get; set; } //检测方法
    }
}
