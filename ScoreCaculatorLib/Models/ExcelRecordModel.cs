using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MiniExcelLibs.Attributes;

namespace ScoreCaculatorLib.DataRule
{
    public class ScoreRecordModel
    {
        static readonly string[] departments =
           ["大数据中心", "办公室", "政策和规划科", "数据资源科", "数字经济科", "数字科技和基础设施建设科", "数字政务与应用科", "政务改革协调科",
            "安全管理科", "投资服务科", "项目管理科", "审批服务一科", "审批服务二科", "政务服务科", "政务监督科", "机关党委（人事科）", "机关纪委",];

        [ExcelColumnName("类型")]
        public string ScoreType { get; set; } = "";

        [ExcelColumnName("大数据中心")]
        public double DP00 { get; set; }

        [ExcelColumnName("办公室")]
        public double DP01 { get; set; }

        [ExcelColumnName("政策和规划科")]
        public double DP02 { get; set; }

        [ExcelColumnName("数据资源科")]
        public double DP03 { get; set; }

        [ExcelColumnName("数字经济科")]
        public double DP04 { get; set; }

        [ExcelColumnName("数字科技和基础设施建设科")]
        public double DP05 { get; set; }

        [ExcelColumnName("数字政务与应用科")]
        public double DP06 { get; set; }

        [ExcelColumnName("政务改革协调科")]
        public double DP07 { get; set; }

        [ExcelColumnName("安全管理科")]
        public double DP08 { get; set; }

        [ExcelColumnName("投资服务科")]
        public double DP09 { get; set; }

        [ExcelColumnName("项目管理科")]
        public double DP10 { get; set; }

        [ExcelColumnName("审批服务一科")]
        public double DP11 { get; set; }

        [ExcelColumnName("审批服务二科")]
        public double DP12 { get; set; }

        [ExcelColumnName("政务服务科")]
        public double DP13 { get; set; }

        [ExcelColumnName("政务监督科")]
        public double DP14 { get; set; }

        [ExcelColumnName("机关党委（人事科）")]
        public double DP15 { get; set; }

        [ExcelColumnName("机关纪委")]
        public double DP16 { get; set; }

    }

    public record DpScoreRecordModel
    {
        static readonly string[] departments =
           ["大数据中心", "办公室", "政策和规划科", "数据资源科", "数字经济科", "数字科技和基础设施建设科", "数字政务与应用科", "政务改革协调科",
            "安全管理科", "投资服务科", "项目管理科", "审批服务一科", "审批服务二科", "政务服务科", "政务监督科", "机关党委（人事科）", "机关纪委",];

        [ExcelColumnName("提交者（自动）")]
        public string Submitter { get; set; } = "";

        [ExcelColumnName("提交时间（自动）")]
        public DateTime SubmissionTime { get; set; }

        [ExcelColumnName("您本次要评价的科室（必填）")]
        public string DepartmentName { get; set; } = "";

        [ExcelColumnName("您的人员类型（必填）")]
        public string PersonType { get; set; } = "";

        [ExcelColumnName("对标领悟二十届三中全会情况（必填）")]
        public double Comprehension { get; set; } //领悟力

        [ExcelColumnName("工作思路（必填）")]
        public double WorkIdeas { get; set; }

        [ExcelColumnName("工作进展实效（必填）")]
        public double WorkEffectiveness { get; set; }

        [ExcelColumnName("能力作风表现（必填）")]
        public double WorkAbility { get; set; }

        [ExcelColumnName("工作汇报真实度（必填）")]
        public double WorkReport { get; set; }

        [ExcelColumnName("总结宣传情况（必填）")]
        public double WorkAdvocacy { get; set; }
    }
}
