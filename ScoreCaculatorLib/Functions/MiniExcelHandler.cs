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
            Task<bool> myTask = new(datasState =>
            {
                var indentStr_L1 = "    "; //设定缩进量
                var indentStr_L2 = "          "; //设定缩进量

                /*--1、从单个Excel文件读取数据--*/
                var inputOrigPath = MyFilePath.ReadFilePath(pM, "Excel文件|*.xlsx|");
                if (string.IsNullOrEmpty(inputOrigPath))
                    return false;
                pM?.Report($"----开始读取数据----");

                // 最终结果记录
                List<OutputRecordModel> scoreList = [];

                // 获取所有页面数据
                List<DpScoreRecordModel> totalRecords = [];
                var sheetNames = MiniExcel.GetSheetNames(inputOrigPath);
                int sIndex = 0;
                foreach (var sName in sheetNames)
                {
                    pM?.Report($"{indentStr_L1}【读取】读取第{++sIndex}个页面");

                    // 读取第i个页面数据
                    //【注意】强烈建议sheetRecords只用于数据从Excel的读取，如果要进行数据处理，建议var data = sheetRecords。
                    //否则sheetRecords_Input显示为0。
                    List<DpScoreRecordModel> sheetRecords = [];
                    int i = 0;
                    MiniExcelExtend.GeneralReadOnStrongType<DpScoreRecordModel>(inputOrigPath, sName, p =>
                    {
                        sheetRecords.Add(p);
                        i++;
                    });
                    pM?.Report($"{indentStr_L2}【读取】读取页面“{sName}”记录：{i}条");

                    // 页面数据加入总集合
                    totalRecords.AddRange(sheetRecords);
                }

                // 清洗页面数据
                List<RuleModel<DpScoreRecordModel>> rules = [];
                RuleCommonHnadler.AddWashingRule<DpScoreRecordModel, MiniExcelRules>(rules, MiniExcelRules.WashingRecordsRule1, datasState);
                RuleCommonHnadler.AddWashingRule<DpScoreRecordModel, MiniExcelRules>(rules, MiniExcelRules.WashingRecordsRule2, datasState);               
                RuleCommonHnadler.AddCheckingRule<DpScoreRecordModel, MiniExcelRules>(rules, MiniExcelRules.CheckingRecordsRule1, datasState);

                var dataWashed = RuleCommonHnadler.CommonWashing(totalRecords, rules, pM);// 通常都是“先清洗、后检测”
                var bufferWashed = dataWashed; 
                var resC = RuleCommonHnadler.CommonChecking(dataWashed, rules, pM);
                if (!resC.IsSuccessHandled)
                    return false;
                var dataCleaned = dataWashed;
                pM?.Report($"{indentStr_L1}【清洗+检测】干净记录：{dataCleaned.Count}条");

                //保存清洗后的数据
                var resSave = SaveCleanedDatas(inputOrigPath, dataCleaned, datasState);
                pM?.Report($"{indentStr_L1}【保存】保存干净的原始数据：{resSave}");

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

        private static bool SaveCleanedDatas(string inputOrigPath, List<DpScoreRecordModel> datas, object? datasState)
        {
            //获取开始时间
            var startDT = (DateTime)datasState!;
            var startDTStr = $"{startDT:yyyyMMdd_HHmm}";

            //新的存储文件名称
            var dir = Path.GetDirectoryName(inputOrigPath);
            var newExcelName = $"{startDTStr}_干净原始数据.xlsx";
            var newExcelFilePath = Path.Combine(dir!, newExcelName);

            //一共有多少人
            var scoreGroupNames = (from r in datas
                                   group r by r.Submitter into g
                                   select g.First().Submitter).ToList();

            //新的随机人名
            var numList = MyRandom.CreatUnduplicatedRandomListV2(1, 1 + scoreGroupNames.Count, scoreGroupNames.Count);//随机序号
            if (numList.UnRan_res != true)
                return false;
            var personNameList = new List<string>();
            foreach (var num in numList.UnRan_list)
            {
                personNameList.Add($"p{num.ToString().PadLeft(3, '0')}");
            }

            //替换人名
            var scoreListNew = new List<DpScoreRecordModel>();
            int i = 0;
            foreach (var pName in scoreGroupNames)
            {
                var selcetedRecords = from r in datas
                                      where r.Submitter == pName
                                      select r;
                var pNewName = personNameList[i++];
                foreach (var record in selcetedRecords)
                {
                    var rNew = record with { Submitter = pNewName };
                    scoreListNew.Add(rNew);
                }
            }

            //保存
            Dictionary<string, List<DpScoreRecordModel>> excelSheets = new()
            {
                ["投票原始记录"] = scoreListNew,
            };
            var resWrite = MiniExcelExtend.GeneralWriteOnStrongType(newExcelFilePath, excelSheets);
            return resWrite;
        }
    }
}
