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
        public static (bool IsSuccessHandled, List<T> DataHandled, string ErrorRule, T? ErrorItem) CommonWashing<T>(List<T> dataOrig, List<RuleModel<T>> dataRules, IProgress<string>? progress = null)
        {

            var activeRules = (from r in dataRules
                               where r.IsActive == true && r.WashingRule != null && r.RuleType == RuleType.Washing
                               select r).ToArray();
            progress?.Report($"本次激活的规则数量：{activeRules.Length}，即将开始清洗...");

            List<T> temp = dataOrig;
            foreach (var r in activeRules)
            {
                var wash = r.WashingRule!([.. temp]);
                if (wash.Res)
                {
                    temp.Clear();
                    temp = [.. wash.DataWashed];
                    progress?.Report($"规则“{r.RuleName}”清洗通过，准备下一规则清洗...");
                }
                else
                {
                    progress?.Report($"规则“{r.RuleName}”清洗失败，输出清洗过程记录和失败记录...");
                    return (false, temp, r.RuleName, wash.ErrorItem);
                }
            }
            progress?.Report($"所有规则清洗通过，即将输出最终结果...");
            return (true, temp, string.Empty, default);
        }

        public static (bool IsSuccessHandled, string ErrorRule, T? ErrorItem) CommonChecking<T>(List<T> dataOrig, List<RuleModel<T>> dataRules, IProgress<string>? progress = null)
        {
            var activeRules = (from r in dataRules
                               where r.IsActive == true && r.CheckingRule != null && r.RuleType == RuleType.Checking
                               select r).ToArray();
            progress?.Report($"本次激活的规则数量：{activeRules.Length}，即将开始检测...");

            foreach (var r in activeRules)
            {
                var check = r.CheckingRule!(dataOrig);
                if (check.Res)
                {
                    progress?.Report($"规则“{r.RuleName}”检测通过，准备下一规则检测...");
                }
                else
                {
                    progress?.Report($"规则“{r.RuleName}”检测失败，输出检测失败记录...");
                    return (false, r.RuleName, check.ErrorItem);
                }
            }
            progress?.Report($"所有规则检测通过，即将输出最终结果...");
            return (true, string.Empty, default);
        }
    }
}
