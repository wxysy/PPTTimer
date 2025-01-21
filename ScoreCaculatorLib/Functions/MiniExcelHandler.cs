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
using Data.Handler.CustomAttribute;
using ScoreCaculatorLib.Models;
using System.Reflection;
using Data.Handler.RuleDir.Models;
using Data.Handler.RuleDir.Commons;

namespace ScoreCaculatorLib.Functions
{
    public class MiniExcelHandler
    {
        public static Task<bool> SingleToSingleExcelFileHandler7(object? state, IProgress<string>? pM, Action<object>? callBack)
        {
            Task<bool> myTask = new(s =>
            {
                var indentStr_L1 = "--"; //设定缩进量

                /*--1、从单个Excel文件读取数据--*/
                var inputPath = MyFilePath.ReadFilePath(pM, "Excel文件|*.xlsx|");
                if (string.IsNullOrEmpty(inputPath))
                    return false;
                pM?.Report($"----开始读取数据----");

                // 最终结果记录
                List<OutputRecordModel> scoreList = [];

                // 获取所有页面
                var sheetNames = MiniExcel.GetSheetNames(inputPath);

                // 读取第1个页面数据
                var sName = sheetNames[0];
                List<DpScoreRecordModel> sheetRecords_Input = [];
                int i = 0;
                MiniExcelExtend.GeneralReadOnStrongType<DpScoreRecordModel>(inputPath, sName, p =>
                {
                    sheetRecords_Input.Add(p);
                    i++;
                });
                pM?.Report($"{indentStr_L1}|读取|读取页面“{sName}”记录：{i}条|");

                // 清洗页面数据
                List<RuleModel<DpScoreRecordModel>> rules = [];

                RuleCommonHnadler.AddWashingRule<DpScoreRecordModel, MiniExcelRules>(rules, MiniExcelRules.WashingRecordsRule, s);

                RuleCommonHnadler.AddWashingRule<DpScoreRecordModel, MiniExcelRules>(rules, MiniExcelRules.WashingRecordsRule2, s);               

                RuleCommonHnadler.AddCheckingRule<DpScoreRecordModel, MiniExcelRules>(rules, MiniExcelRules.CheckingRecordsRule, s);

                var resW = RuleCommonHnadler.CommonWashing(sheetRecords_Input, rules, pM);// 通常都是“先清洗、后检测”
                var washingDatas = resW.DataHandled; //不能直接用sheetRecords_Input输入到CommonChecking，否则sheetRecords_Input显示为0。
                var resC = RuleCommonHnadler.CommonChecking(washingDatas, rules, pM);
                if (!(resW.IsSuccessHandled && resC.IsSuccessHandled))
                    return false;

                var sheetRecords_Washed = washingDatas;
                pM?.Report($"{indentStr_L1}|清洗+检测|保留记录：{sheetRecords_Washed.Count}条|");

                // 获取所需数据
                foreach (var item in sheetRecords_Washed)
                {
                    var dpName = item.DepartmentName;
                    var scoreType = item.PersonType[..1];
                    var score = item.Comprehension + item.WorkIdeas + item.WorkEffectiveness + item.WorkAbility + item.WorkReport + item.WorkAdvocacy;
                    var time = item.SubmissionTime;

                    var outRD = new OutputRecordModel()
                    {
                        Department = dpName,
                        SubmissionTime = time,
                        ScoreType = scoreType,
                        Score = score,
                    };
                    scoreList.Add(outRD);
                }
                pM?.Report($"{indentStr_L1}|总计|读取页面：{sheetNames.Count}个，保留记录：{scoreList.Count}条|");

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
            }, state);

            myTask.Start();
            return myTask;
        }       
    }
}
