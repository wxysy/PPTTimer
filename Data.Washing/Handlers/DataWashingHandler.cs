using Data.Washing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Washing.Handlers
{
    public class DataWashingHandler
    {
        public static (bool IsSuccessHandled, List<T> DataHandled) CommonHandler<T>(List<T> dataOrig, List<RuleModel<T>> dataRules, IProgress<string>? progress = null)
        {

            var activeRules = (from r in dataRules
                               where r.IsActive == true && !(r.CheckingRule == null && r.WashingRule == null)
                               select r).ToArray();
            progress?.Report($"本次激活的规则数量：{activeRules.Length}，即将开始...");
            progress?.Report($"规则处理逻辑：【先清洗，后检测】...");


            var washingRules = (from r in dataRules
                                where r.RuleType == RuleType.Washing && r.WashingRule != null
                                select r).ToArray();
            List<T> temp = dataOrig;
            foreach (var r in washingRules)
            {
                var (RuleResult, RuleOut) = r.WashingRule!([.. temp]);
                if (RuleResult)
                {
                    temp.Clear();
                    temp = [.. RuleOut];
                    progress?.Report($"规则“{r.RuleName}”清洗通过，准备下一规则清洗...");
                }
                else
                {
                    progress?.Report($"规则“{r.RuleName}”清洗失败，准备输出中间结果...");
                    return (false, temp);
                }
            }
            progress?.Report($"所有规则清洗通过，即将开始检测...");


            var checkingRules = (from r in dataRules
                                 where r.RuleType == RuleType.Checking && r.CheckingRule != null
                                 select r).ToArray();
            foreach (var r in checkingRules)
            {
                var check = r.CheckingRule!(dataOrig);
                if (check)
                {
                    progress?.Report($"规则“{r.RuleName}”检测通过，准备下一规则检测...");
                }
                else
                {
                    progress?.Report($"规则“{r.RuleName}”检测失败，准备输出先前清洗结果...");
                    return (false, temp);
                }
            }
            progress?.Report($"所有规则检测通过，即将输出最终结果...");
            return (true, temp);
        }
    }
}
