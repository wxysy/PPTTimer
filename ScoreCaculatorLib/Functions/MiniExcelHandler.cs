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
        public static Task<bool> SingleExcelFileReader(object? state, IProgress<string>? pM, Action<object>? callBack)
        {
            Task<bool> myTask = new(s =>
            {
                var indentStr_L1 = "    "; //设定缩进量

                /*--1、从单个Excel文件读取数据--*/
                var inputPath = MyFilePath.ReadFilePath(pM, "Excel文件|*.xlsx|");
                if (string.IsNullOrEmpty(inputPath))
                    return false;
                pM?.Report($"----开始读取数据----");

                // 最终结果记录
                List<OutputRecordModel> scoreList = [];

                // 获取所有页面数据
                List<DpScoreRecordModel> totalRecords = [];
                var sheetNames = MiniExcel.GetSheetNames(inputPath);
                int sIndex = 0;
                foreach (var sName in sheetNames)
                {
                    pM?.Report($"【读取】读取第{++sIndex}个页面");

                    // 读取第i个页面数据
                    //【注意】强烈建议sheetRecords只用于数据从Excel的读取，如果要进行数据处理，建议var data = sheetRecords。
                    //否则sheetRecords_Input显示为0。
                    List<DpScoreRecordModel> sheetRecords = [];
                    int i = 0;
                    MiniExcelExtend.GeneralReadOnStrongType<DpScoreRecordModel>(inputPath, sName, p =>
                    {
                        sheetRecords.Add(p);
                        i++;
                    });
                    pM?.Report($"{indentStr_L1}【读取】读取页面“{sName}”记录：{i}条");

                    // 页面数据加入总集合
                    totalRecords.AddRange(sheetRecords);
                }

                // 清洗页面数据
                List<RuleModel<DpScoreRecordModel>> rules = [];
                RuleCommonHnadler.AddWashingRule<DpScoreRecordModel, MiniExcelRules>(rules, MiniExcelRules.WashingRecordsRule1, s);
                RuleCommonHnadler.AddWashingRule<DpScoreRecordModel, MiniExcelRules>(rules, MiniExcelRules.WashingRecordsRule2, s);               
                RuleCommonHnadler.AddCheckingRule<DpScoreRecordModel, MiniExcelRules>(rules, MiniExcelRules.CheckingRecordsRule1, s);

                var dataWashed = RuleCommonHnadler.CommonWashing(totalRecords, rules, pM);// 通常都是“先清洗、后检测”
                var bufferWashed = dataWashed; 
                var resC = RuleCommonHnadler.CommonChecking(dataWashed, rules, pM);
                if (!resC.IsSuccessHandled)
                    return false;
                var dataCleaned = dataWashed;
                pM?.Report($"{indentStr_L1}【清洗+检测】干净记录：{dataCleaned.Count}条");

                // 获取所需数据
                foreach (var item in dataCleaned)
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
                pM?.Report($"【总计】读取页面：{sheetNames.Count}个，可用记录：{scoreList.Count}条");

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
