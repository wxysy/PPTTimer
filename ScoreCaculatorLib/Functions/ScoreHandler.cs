using Data.Common.Serialize;
using Microsoft.VisualBasic;
using ScoreCaculatorLib.DataRule;
using ScoreCaculatorLib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoreCaculatorLib.Functions
{
    internal class ScoreHandler
    {        
        internal static (double Score, string ScoreInfo) ScoreCalculatorV2(List<OutputRecordModel> scoreList, string departmentName)
        {
            var indentStr_L2 = "      "; //设定缩进量

            //0、数据筛选
            var scoreListSelected = (from s in scoreList
                                     where s.Score >= 60 && s.Score < 100 //打分不超过99分，不能低于60分。
                                     where s.Department == departmentName
                                     select s).ToList();

            //1、计算A票分（去掉一个最高分，去掉一个最低分，多个最高/最低分只去除一个）
            int rMin = 3;
            if(scoreListSelected.Count < rMin)
                return (0, $"【{departmentName}】最终得分：（无）\n" +
                    $"{indentStr_L2}有效票数{scoreListSelected.Count}张，少于{rMin}张，无法计算。");

            var scoreArray = (from p in scoreListSelected
                              orderby p.Score //打分从低到高进行排序
                              select p).ToArray();
            List<double> inputA = [];
            for (int i = 1; i < scoreArray.Length - 1; i++) //去掉一个最高分(index = scoreArray.Length - 1)，去掉一个最低分(index = 0)
            {
                inputA.Add(scoreArray[i].Score);
            }
            var scoreA = inputA.Count > 0 ? inputA.Average() : 0;

            //2、计算B票分（各分管领导）
            var inputB = (from s in scoreListSelected
                           where s.ScoreType == "B"
                           select s.Score).ToList();
            var scoreB = inputB.Count > 0 ? inputB.Average() : 0;

            //3、计算总分（I类分70%，II类分30%）
            var score = scoreA * 0.7 + scoreB * 0.3;

            //4、输出结果
            string mes = $"【{departmentName}】最终得分：{score:f3}\n" +
                    $"{indentStr_L2}收到选票{scoreArray.Length}张" +
                    $"（有效A票{inputA.Count}张，A票平均分{scoreA:f3}，最高分{scoreArray[^1].Score}，最低分{scoreArray[0].Score}" +
                    $" | 有效B票{inputB.Count}张，B票平均分{scoreB:f3}）";
            return (score, mes);
        }
    }
}
