using Data.Common.Serialize;
using ScoreCaculatorLib.DataRule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoreCaculatorLib.Functions
{
    public class MiniExcelRules
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

        public static List<DpScoreRecordModel> WashingRecordsRule(List<DpScoreRecordModel> recordsOrig)
        {
            // 清洗结果存储
            List<DpScoreRecordModel> recordsWashed = [];

            /*【清洗条件】
             * 1、非本时段录入(只收集当天的投票)
             * 2、重复录入
             * 3、冒充领导
             */

            //1、非本时段录入(只收集当天的投票)
            var date = (from r in recordsOrig
                        orderby r.SubmissionTime descending
                        select r).First().SubmissionTime.Date; //找出最近投票日期
            var dataBuffer = (from r in recordsOrig
                              where r.SubmissionTime.Date == date
                              select r).ToList();

            //2、去除重复录入的，以最后录入的为准。（先按名称分组，再组内排序）
            //《linq分组再实现组内排序》https://blog.csdn.net/qq_39585172/article/details/107201634
            //《Linq分组后，再对分组后的每组进行内部排序，获取每组中的第一条记录》https://blog.csdn.net/zzhzhonghua/article/details/121206103
            var dataBuffer2 = (from r in dataBuffer
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
    }
}
