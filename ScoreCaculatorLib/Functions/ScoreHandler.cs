using ScoreCaculatorLib.Models;

namespace ScoreCaculatorLib.Functions
{
    internal class ScoreHandler
    {        
        internal static (double Score, string ScoreInfo) ScoreCalculatorV2(List<OutputRecordModel> scoreList, string departmentName)
        {
            var indentStr_L2 = "      "; //设定缩进量

            //0、取分数段内投票
            var scoreSelected = (from s in scoreList
                                 where s.Score > 60 && s.Score < 100 //打分小于100分，高于60分。
                                 where s.Department == departmentName
                                 select s).ToList();

            //1、计算A票分（去掉一个最高分，去掉一个最低分，多个最高/最低分只去除一个）            
            int rMin = 3;
            if(scoreSelected.Count < rMin)
                return (0, $"【{departmentName}】最终得分：（无）\n" +
                    $"{indentStr_L2}有效票数{scoreSelected.Count}张，少于{rMin}张，无法计算。");

            var scoreAOrderBy = (from p in scoreSelected
                              orderby p.Score //打分从低到高进行排序
                              select p).ToArray();
            List<double> inputA = [];
            for (int i = 1; i < scoreAOrderBy.Length - 1; i++) //去掉一个最高分(index = scoreArray.Length - 1)，去掉一个最低分(index = 0)
            {
                inputA.Add(scoreAOrderBy[i].Score);
            }
            var scoreA = inputA.Count > 0 ? inputA.Average() : 0;

            //2、计算B票分（各分管领导，取全部平均分）
            var inputB = (from s in scoreSelected
                          where s.ScoreType == "B"
                          select s.Score).ToList();
            var scoreB = inputB.Count > 0 ? inputB.Average() : 0;

            //3、计算总分（I类分70%，II类分30%）
            var score = scoreA * 0.7 + scoreB * 0.3;

            //4、输出结果
            var countD = (from s in scoreList
                          where s.Department == departmentName
                          select s).Count();
            var countA = scoreSelected.Count;
            var countB = (from s in scoreSelected
                          where s.ScoreType == "B"
                          select s).Count();

            string mes = $"【{departmentName}】最终得分：{score:f3}\n" +
                    $"{indentStr_L2}收到投票{countD}张。" +
                    $"有效A票{countA}张，有效B票{countB}张。\n" +
                    $"{indentStr_L2}（A票平均分{scoreA:f3}，最高分{scoreAOrderBy[^1].Score}，最低分{scoreAOrderBy[0].Score}" +
                    $" | B票平均分{scoreB:f3}）";
            return (score, mes);
        }
    }
}
