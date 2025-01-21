using Data.Common.Serialize;
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
            //0、数据筛选
            var scoreListSelected = (from s in scoreList
                                     where s.Score >= 60 && s.Score < 100 //打分不超过99分，不能低于60分。
                                     where s.Department == departmentName
                                     select s).ToList();

            //1、计算I类分（去掉一个最高分，去掉一个最低分，多个最高/最低分只去除一个）
            int rMin = 3;
            if(scoreListSelected.Count < rMin)
                return (0, $"{departmentName}，票数少于{rMin}票，无法计算。");

            var scoreArray = (from p in scoreListSelected
                              orderby p.Score //打分从低到高进行排序
                              select p).ToArray();
            List<double> inputI = [];
            for (int i = 1; i < scoreArray.Length - 1; i++) //去掉一个最高分(index = scoreArray.Length - 1)，去掉一个最低分(index = 0)
            {
                inputI.Add(scoreArray[i].Score);
            }
            var scoreI = inputI.Count > 0 ? inputI.Average() : 0;

            //2、计算II类分（5个副职领导单算）
            var inputII = (from s in scoreListSelected
                           where s.ScoreType == "B"
                           select s.Score).ToList();
            var scoreII = inputII.Count > 0 ? inputII.Average() : 0;

            //3、计算总分（I类分70%，II类分30%）
            var score = scoreI * 0.7 + scoreII * 0.3;

            //4、输出结果
            if (score == 0)
                return (score, $"{departmentName}，没有有效票。");
            else if (scoreI == 0)
                return (score, $"{departmentName}，没有I类有效票。");
            else if (scoreII == 0)
                return (score, $"{departmentName}，没有II类有效票。");
            else
            {
                string mes = $"{departmentName}，收到选票{scoreArray.Length}张，最高分{scoreArray[^1].Score}，最低分{scoreArray[0].Score}。" +
                    $"I类有效票{inputI.Count}张，I类平均分{scoreI:f3}。" +
                    $"II类有效票{inputII.Count}张，II类平均分{scoreII:f3}。" +
                    $"最终得分{score:f3}。";
                return (score, mes);
            }
        }
    }
}
