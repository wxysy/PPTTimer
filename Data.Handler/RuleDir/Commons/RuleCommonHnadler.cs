using Data.Handler.RuleDir.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Data.Handler.RuleDir.Commons
{
    public class RuleCommonHnadler
    {      
        public static void AddWashingRule<TItem, TRuleClass>(List<RuleModel<TItem>> rules, Func<List<TItem>, object?, List<TItem>> ruleFunc, object? datasState = null)
            where TRuleClass : class //TRuleClass:Rule方法所在的类
        {
            //《Delegate.GetInvocationList 方法》
            //https://learn.microsoft.com/zh-cn/dotnet/api/system.delegate.getinvocationlist?view=net-7.0
            var f = ruleFunc.GetInvocationList().First();
            var fName = f.GetMethodInfo().Name;

            var washingMD = RuleMD.GetRuleMetadata<TRuleClass>(fName);// 获取方法元数据（自定义特性值）
            rules.Add(new RuleModel<TItem>()// 添加方法
            {
                //设定自定义特性值                       
                IsActive = washingMD.IsActive,
                RuleType = washingMD.RuleType,
                RuleTitle = washingMD.RuleTitle,
                RuleDescription = washingMD.RuleDescription,
                //有自定义特性的实际清洗方法
                WashingRule = datas => ruleFunc(datas, datasState),
            });
        }

        public static void AddCheckingRule<TItem, TRuleClass>(List<RuleModel<TItem>> rules, Func<List<TItem>, object?, (bool Res, TItem? ErrorItem)> ruleFunc, object? datasState = null)
            where TRuleClass : class //TRuleClass:Rule方法所在的类
        {
            //《Delegate.GetInvocationList 方法》
            //https://learn.microsoft.com/zh-cn/dotnet/api/system.delegate.getinvocationlist?view=net-7.0
            var f = ruleFunc.GetInvocationList().First();
            var fName = f.GetMethodInfo().Name;

            var checkingMD = RuleMD.GetRuleMetadata<TRuleClass>(fName);// 获取方法元数据（自定义特性值）
            rules.Add(new RuleModel<TItem>()// 添加方法
            {
                //设定自定义特性值                       
                IsActive = checkingMD.IsActive,
                RuleType = checkingMD.RuleType,
                RuleTitle = checkingMD.RuleTitle,
                RuleDescription= checkingMD.RuleDescription,
                //有自定义特性的实际清洗方法
                CheckingRule = datas => ruleFunc(datas, datasState),
            });
        }

        // 清洗记录是不存在什么成不成功的，最多把记录全清洗掉而已
        public static List<TItem> CommonWashing<TItem>(List<TItem> dataOrig, List<RuleModel<TItem>> dataRules, IProgress<string>? progress = null)
        {
            var indentStr_L2 = "    "; //设定缩进量
            var indentStr_L3 = "        "; //设定缩进量

            progress?.Report($"{indentStr_L2}开始数据清洗...");
            var totalRules = from r in dataRules
                             where r.WashingRule != null && r.RuleType == RuleType.Washing
                             select r;
            var activeRules = (from r in totalRules
                               where r.IsActive == true
                               select r).ToArray();
            progress?.Report($"{indentStr_L3}本次数据处理，规则类型：清洗，规则数量：{totalRules.Count()}，激活数量：{activeRules.Length}");

            List<TItem> buffer = dataOrig;
            int rIndex = 0;
            foreach (var r in activeRules)
            {
                if (string.IsNullOrEmpty(r.RuleDescription) != true)
                    progress?.Report($"{indentStr_L3}规则“{r.RuleTitle}”用途：{r.RuleDescription}");
                var dataWashed = r.WashingRule!(buffer);
                buffer.Clear();
                buffer = dataWashed;
                progress?.Report($"{indentStr_L3}{++rIndex}-规则“{r.RuleTitle}”清洗完毕，通过清洗的记录数量：{dataWashed.Count}，准备下一规则清洗...");             
            }
            progress?.Report($"{indentStr_L2}数据清洗完毕");
            return (buffer);
        }

        // 检测记录是存在是否成功的
        public static (bool IsSuccessHandled, string ErrorRule, TItem? ErrorItem) CommonChecking<TItem>(List<TItem> dataOrig, List<RuleModel<TItem>> dataRules, IProgress<string>? progress = null)
        {
            var indentStr_L2 = "    "; //设定缩进量
            var indentStr_L3 = "        "; //设定缩进量

            progress?.Report($"{indentStr_L2}开始数据检测...");
            var totalRules = from r in dataRules
                             where r.CheckingRule != null && r.RuleType == RuleType.Checking
                             select r;
            var activeRules = (from r in totalRules
                               where r.IsActive == true
                               select r).ToArray();
            progress?.Report($"{indentStr_L3}本次数据处理，规则类型：检测，规则数量：{totalRules.Count()}，激活数量：{activeRules.Length}");

            List<TItem> buffer = dataOrig;
            int rIndex = 0;
            foreach (var r in activeRules)
            {
                ++rIndex;
                if (string.IsNullOrEmpty(r.RuleDescription) != true)
                    progress?.Report($"{indentStr_L3}规则“{r.RuleTitle}”用途：{r.RuleDescription}");
                var check = r.CheckingRule!(buffer);
                if (check.Res)
                {
                    progress?.Report($"{indentStr_L3}{rIndex}-规则“{r.RuleTitle}”检测通过，通过检测的记录数量：{buffer.Count}，准备下一规则检测...");
                }
                else
                {
                    progress?.Report($"{indentStr_L2}{rIndex}-规则“{r.RuleTitle}”检测失败，输出检测失败记录...");
                    return (false, r.RuleTitle, check.ErrorItem);
                }
            }
            progress?.Report($"{indentStr_L2}数据检测通过");
            return (true, string.Empty, default);
        }

    }
}
