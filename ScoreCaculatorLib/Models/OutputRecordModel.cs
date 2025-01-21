using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoreCaculatorLib.Models
{
    internal record OutputRecordModel
    {
        public string Department { get; set; } = string.Empty;
        public string ScoreType { get; set; } = string.Empty;
        public double Score { get; set; } = 0;
        public DateTime SubmissionTime { get; set; } = DateTime.MinValue;
    }
}
