﻿using Data.Common.Serialize;
using Data.Handler.RuleDir.Models;
using ScoreCaculatorLib.DataRule;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        [Rule(IsActive = false, RuleTitle = "评分清洗1", RuleType = RuleType.Washing, RuleDescription = "无")] // 清洗记录是不存在什么成不成功的，最多把记录全清洗掉而已
        public static List<DpScoreRecordModel> WashingRecordsRule1(List<DpScoreRecordModel> datasOrig, object? datasState = null) //参数datasState，用于表示处理数据需要的额外参数(例如时间)
        {
            // 清洗结果存储
            List<DpScoreRecordModel> recordsWashed = [];

            /*【清洗条件】
             * 1、非本时段录入(只收集开始时间之后的投票)
             * 2、重复录入
             * 3、冒充领导
             */

            //1、非本时段录入(只收集开始时间之后的投票)
            var date = (from r in datasOrig
                        orderby r.SubmissionTime descending
                        select r).First().SubmissionTime.Date; //找出最近投票日期

            var dataBuffer = (from r in datasOrig
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
            //string leaderNamePath = $@".\Settings\LeanerNames.xml";
            string leaderNamePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $@".\Settings\LeanerNames.xml";


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
       
        [Rule(IsActive = true, RuleTitle = "评分清洗2", RuleType = RuleType.Washing, RuleDescription = "清洗掉非设定时段录入和重复录入数据")] // 清洗记录是不存在什么成不成功的，最多把记录全清洗掉而已
        public static List<DpScoreRecordModel> WashingRecordsRule2(List<DpScoreRecordModel> datasOrig, object? datasState = null) //参数datasState，用于表示处理数据需要的额外参数(例如时间)
        {
            // 清洗结果存储
            List<DpScoreRecordModel> recordsWashed = [];

            /*【清洗条件】
             * 1、非本时段录入(只收集开始时间之后的投票)
             * 2、单人重复录入(以最后一次投票为准)
             */

            //1、非本时段录入(只收集开始时间之后的投票)
            var startDT = (DateTime)datasState!;
            var dataBuffer = (from r in datasOrig
                              where r.SubmissionTime >= startDT
                              select r).ToList();

            //2、单人重复录入(以最后一次投票为准)
            //2.1-先按部门分组
            var dataGroups = (from r in dataBuffer
                             group r by r.DepartmentName into p
                             select p);

            //2.2-部门分组中找出重复投票的
            foreach (var g in dataGroups)
            {
                //《linq分组再实现组内排序》https://blog.csdn.net/qq_39585172/article/details/107201634
                //《Linq分组后，再对分组后的每组进行内部排序，获取每组中的第一条记录》https://blog.csdn.net/zzhzhonghua/article/details/121206103
                var recordsPerDP = (from r in g
                                  group r by r.Submitter into p  //先分组
                                  select p.OrderByDescending(x => x.SubmissionTime).First()  //后组内降序排序
                                  ).ToList();
                //3、添加至最终数据聚合
                recordsWashed.AddRange(recordsPerDP);
            }          

            //4、输出
            return recordsWashed;
        }

        
        [Rule(IsActive = true, RuleTitle = "评分检测", RuleType = RuleType.Checking)] // 检测记录是存在是否成功的
        public static (bool Res, DpScoreRecordModel? ErrorItem) CheckingRecordsRule1(List<DpScoreRecordModel> datasOrig, object? datasState = null) //参数datasState，用于表示处理数据需要的额外参数(例如时间)
        {
            return (true, default);
        }
    }
}
