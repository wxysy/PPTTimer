using Infrastructure.Files.FileCommon;
using MiniExcelLibs.OpenXml;
using MiniExcelLibs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using ScoreCaculatorLib.DataRule;
using Data.Handler.Models;
using Data.Handler.Commons;
using Data.Handler.CustomAttribute;

namespace ScoreCaculatorLib.Functions
{
    public class MiniExcelHandler
    {
        public static Task<bool> SingleToSingleExcelFileHandler6(IProgress<string>? pM, Action<object>? callBack)
        {
            Task<bool> myTask = new(() =>
            {
                /*--1、从单个Excel文件读取数据--*/
                var inputPath = MyFilePath.ReadFilePath(pM, "Excel文件|*.xlsx|");
                if (string.IsNullOrEmpty(inputPath))
                    return false;
                pM?.Report($"----开始读取数据----");

                // 最终结果记录
                List<(string Department, string ScoreType, double Score)> scoreList = [];

                // 获取所有页面
                var sheetNames = MiniExcel.GetSheetNames(inputPath);

                // 读取每个页面数据
                foreach (var sName in sheetNames)
                {
                    // 读取页面数据
                    List<DpScoreRecordModel> sheetRecords_Input = [];
                    int i = 0;
                    MiniExcelExtend.GeneralReadOnStrongType<DpScoreRecordModel>(inputPath, sName, p =>
                    {
                        sheetRecords_Input.Add(p);
                        i++;
                    });
                    pM?.Report($"--|读取|读取页面“{sName}”记录：{i}条|");

                    // 清洗页面数据
                    List<RuleModel<DpScoreRecordModel>> rules = [];
                    
                    var washingMD = RuleMD.GetRuleMetadata<MiniExcelRules>(nameof(MiniExcelRules.WashingRecordsRule));// 获取方法元数据（自定义特性值）
                    rules.Add(new RuleModel<DpScoreRecordModel>()// 添加方法
                    {
                        //设定自定义特性值                       
                        IsActive = washingMD.IsActive,
                        RuleType = washingMD.RuleType,
                        RuleTitle = washingMD.RuleTitle,
                        //有自定义特性的实际清洗方法
                        WashingRule = datas => MiniExcelRules.WashingRecordsRule(datas),
                    });
                    var checkingMD = RuleMD.GetRuleMetadata<MiniExcelRules>(nameof(MiniExcelRules.CheckingRecordsRule));// 获取方法元数据（自定义特性值）
                    rules.Add(new RuleModel<DpScoreRecordModel>()// 添加方法
                    {
                        //设定自定义特性值                       
                        IsActive = checkingMD.IsActive,
                        RuleType = checkingMD.RuleType,
                        RuleTitle = checkingMD.RuleTitle,
                        //有自定义特性的实际清洗方法
                        CheckingRule = datas => MiniExcelRules.CheckingRecordsRule(datas),
                    });
                    var resW = DataCommonHnadler.CommonWashing(sheetRecords_Input, rules, pM);// 通常都是“先清洗、后检测”
                    var resC = DataCommonHnadler.CommonChecking(sheetRecords_Input, rules, pM);
                    if (!(resW.IsSuccessHandled && resC.IsSuccessHandled))
                        return false;

                    var sheetRecords_Washed = resW.DataHandled;
                    pM?.Report($"--|清洗+检测|保留记录：{sheetRecords_Washed.Count}条|");

                    // 获取所需数据
                    foreach (var item in sheetRecords_Washed)
                    {
                        var dpName = sName[6..^6]; // sheetName格式：科室打分表-数字基础设施建设科（收集结果）
                        var scoreType = item.PersonType[..1];
                        var score = item.Comprehension + item.WorkIdeas + item.WorkEffectiveness + item.WorkAbility + item.WorkReport + item.WorkAdvocacy;
                        scoreList.Add((dpName, scoreType, score));
                    }
                }
                pM?.Report($"|总计|读取页面：{sheetNames.Count}个，保留记录：{scoreList.Count}条|");

                //《不支持从调度程序线程以外的线程对其 SourceCollection 进行的更改》
                //https://blog.csdn.net/Until_youyf/article/details/102720112
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    //foreach (var item in sheetInput_List1)
                    //{
                    //    callBack?.Invoke(item);//一般用于向UI界面列表控件显示数据
                    //}
                    callBack?.Invoke(scoreList);
                });
                pM?.Report("----完成数据读取----");
                return true;
            });

            myTask.Start();
            return myTask;
        }
    }
}
