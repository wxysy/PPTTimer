using Data.Common.Serialize;
using ScoreCaculatorLib.DataRule;
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
        /* ScoreType：
         * 1、A -- 局长 Head Leader
         * 2、B -- 分管领导 Charge Leader
         * 3、C -- 副县级干部 Deputy county-level Cadre
         * 4、D -- 科长、科室负责人 Section Chief
         * 5、E -- 科员、单位干部 Section Member
         */
        private static readonly string[] leaderLevels = ["A", "B", "C"];
        private static readonly string[] otherLevels = ["D", "E"];

        internal static List<DpScoreRecordModel> WashingRecordsRule(List<DpScoreRecordModel> recordsOrig)
        {
            // 清洗结果存储
            List<DpScoreRecordModel> recordsWashed = [];

            /*【清洗条件】
             * 1、非本时段录入
             * 2、重复录入
             * 3、冒充领导
             */

            //1、非本时段录入
            //var dataBuffer = from r in recordsOrig
            //                 where r.SubmissionTime > DateTime.Now - TimeSpan.FromDays(1)
            //                 select r;

            //2、去除重复录入的，以最后录入的为准。（先按名称分组，再组内排序）
            //《linq分组再实现组内排序》https://blog.csdn.net/qq_39585172/article/details/107201634
            //《Linq分组后，再对分组后的每组进行内部排序，获取每组中的第一条记录》https://blog.csdn.net/zzhzhonghua/article/details/121206103
            var dataBuffer2 = (from r in recordsOrig
                               group r by r.Submitter into p  //先分组
                               select p.OrderByDescending(x => x.SubmissionTime).First()  //后组内降序排序
                              ).ToList();

            //3、排除冒充领导的
            //string[] leaders = ["蓝色香巴拉", "一抹红", "上善若水", "李先伟", "gy", "三十六雨", "张性军"]; //李燕艳、朱红梅、宋敬美、李先伟、龚勇、杨建宏、张性军
            string leaderNamePath = $@".\Settings\LeanerNames.xml";
            //MyXmlSerialize.XmlSerializeToFile(leaders, leaderNamePath);
            string[] leaderNames = MyXmlSerialize.XmlDeserializeFromFile<string[]>(leaderNamePath) ?? [];

            var dataLeaders = (from r in dataBuffer2
                               where leaderNames.Contains(r.Submitter) && leaderLevels.Contains(r.PersonType[..1])
                               select r).ToList();
            var dataOthers = (from r in dataBuffer2
                              where !leaderNames.Contains(r.Submitter) && otherLevels.Contains(r.PersonType[..1])
                              select r).ToList();

            //3、添加至最终数据聚合
            recordsWashed.AddRange(dataLeaders);
            recordsWashed.AddRange(dataOthers);

            //4、输出
            return recordsWashed;
        }

        internal static (double Score, string ScoreInfo) ScoreCalculatorV2(List<(string Department, string ScoreType, double Score)> scoreList, string departmentName)
        {
            //0、数据筛选
            var scoreListSelected = scoreList.Where(s => s.Score >= 60 && s.Score < 100).ToList(); //打分不超过99分，不能低于60分。

            //1、计算I类分（去掉一个最高分，去掉一个最低分，多个最高/最低分只去除一个）
            var scoreArray = (from p in scoreListSelected
                              where p.Department == departmentName
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
                           where s.ScoreType == "B" && s.Department == departmentName
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
