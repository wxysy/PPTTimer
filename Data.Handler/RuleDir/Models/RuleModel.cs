using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Handler.RuleDir.Models
{
    public class RuleModel<TItem>
    {
        public required bool IsActive { get; set; } //规则是否激活
        public required string RuleTitle { get; set; } //规则标题
        public required RuleType RuleType { get; set; } //规则类型


        public Func<List<TItem>, List<TItem>>? WashingRule { get; set; } //清洗方法(清洗记录是不存在什么成不成功的，最多把记录全清洗掉而已)
        public Func<List<TItem>, (bool Res, TItem? ErrorItem)>? CheckingRule { get; set; } //检测方法
    }
}
