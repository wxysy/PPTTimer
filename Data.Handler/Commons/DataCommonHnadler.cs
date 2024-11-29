using Data.Handler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Handler.Commons
{
    public class DataCommonHnadler
    {
        public static (bool IsSuccessHandled, List<TItem> DataHandled, string ErrorRule, TItem? ErrorItem) CommonWashing<TItem>(List<TItem> dataOrig, List<RuleModel<TItem>> dataRules, IProgress<string>? progress = null)
        {

            var activeRules = (from r in dataRules
                               where r.IsActive == true && r.WashingRule != null && r.RuleType == RuleType.Washing
                               select r).ToArray();
            progress?.Report($"----|本次数据处理，规则类型：清洗，激活数量：{activeRules.Length}，即将开始...|");

            List<TItem> buffer = dataOrig;
            foreach (var r in activeRules)
            {
                var wash = r.WashingRule!(buffer);
                if (wash.Res)
                {
                    buffer.Clear();
                    buffer = wash.DataWashed;
                    progress?.Report($"------|规则“{r.RuleName}”清洗通过，准备下一规则清洗...|");
                }
                else
                {
                    progress?.Report($"----|规则“{r.RuleName}”清洗失败，输出清洗过程记录和失败记录...|");
                    return (false, buffer, r.RuleName, wash.ErrorItem);
                }
            }
            progress?.Report($"----|所有规则清洗通过，即将输出最终结果...|");
            return (true, buffer, string.Empty, default);
        }

        public static (bool IsSuccessHandled, string ErrorRule, TItem? ErrorItem) CommonChecking<TItem>(List<TItem> dataOrig, List<RuleModel<TItem>> dataRules, IProgress<string>? progress = null)
        {
            var activeRules = (from r in dataRules
                               where r.IsActive == true && r.CheckingRule != null && r.RuleType == RuleType.Checking
                               select r).ToArray();
            progress?.Report($"----|本次数据处理，规则类型：检测，激活数量：{activeRules.Length}，即将开始...|");

            foreach (var r in activeRules)
            {
                var check = r.CheckingRule!(dataOrig);
                if (check.Res)
                {
                    progress?.Report($"------|规则“{r.RuleName}”检测通过，准备下一规则检测...|");
                }
                else
                {
                    progress?.Report($"----|规则“{r.RuleName}”检测失败，输出检测失败记录...|");
                    return (false, r.RuleName, check.ErrorItem);
                }
            }
            progress?.Report($"----|所有规则检测通过，即将输出最终结果...|");
            return (true, string.Empty, default);
        }
    }
}
