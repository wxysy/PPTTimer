using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ScoreCaculatorLib.Functions
{
    public class MyRandom
    {
        //使用RandomNumberGenerator代替Random，因为更安全。

        /// <summary>
        /// 计算位于下限值（包括）和上限值（不包括）之间的随机数列表V2，使用RandomNumberGenerator代替Random。
        /// </summary>
        /// <param name="lower">下限值（包括）</param>
        /// <param name="upper">上限值（不包括）</param>
        /// <param name="number_need">需要的随机数个数（必须大于零）</param>
        /// <returns>位于下限值（包括）和上限值（不包括）之间的随机数列表</returns>
        public static (bool UnRan_res, string UnRan_mes, List<int> UnRan_list) CreatUnduplicatedRandomListV2(int lower, int upper, int number_need)
        {
            bool result = false;//计算结果
            string res_mes;//结果信息
            List<int> res_list = new();//计算列表

            if (lower >= upper)
            {
                res_mes = "下限值大于等于上限值。";
            }
            else if (number_need <= 0)
            {
                res_mes = "需要的随机数个数小于等于零。";
            }
            else if (number_need > upper - lower)
            {
                res_mes = "需要的随机数个数大于上下限范围。";
            }
            else
            {
                try
                {
                    int number_total = upper - lower;

                    //生成索引
                    int[] random_index = new int[number_total];
                    int random_index_result;
                    for (int i = 0; i < number_total; i++)
                    {
                        random_index_result = lower + i;
                        random_index[i] = random_index_result;
                    }

                    //随机取索引号
                    int random_index_choose;
                    int tempValue;
                    for (int j = 1; j <= number_need; j++)
                    {
                        //随机生成一个索引位置
                        //RandomNumberGenerator代替Random，因为更安全。
                        //会选择[0, number_total)之间的数(包括最小值索引0，不包括最大索引number_total)。
                        random_index_choose = RandomNumberGenerator.GetInt32(0, number_total);

                        //获取这个索引位置的值，并将这个索引位置的值与最后一个值交换
                        tempValue = random_index[random_index_choose];
                        random_index[random_index_choose] = random_index[number_total - 1];
                        random_index[number_total - 1] = tempValue;

                        //保存第一个结果选出的值
                        /*将每次random_index[]的最后一个值保存到最终的结果数列中*/
                        res_list.Add(random_index[number_total - 1]);

                        //将索引值减小，确保最后一个索引只用一次。
                        number_total--;
                    }

                    result = true;
                    res_mes = "不重复随机数列表生成成功。";
                }
                catch (Exception ex)
                {
                    res_mes = ex.Message;
                }
            }
            return (result, res_mes, res_list);
            //return (result, res_mes, new ObservableCollection<int>(res_list));//返回ObservableCollection<T>
            //ObservableCollection<int>转化为List<T>，直接用ToList()方法：
            //OC_List.ToList();
            //需添加using System.Collections.ObjectModel; 引用
        }

        /// <summary>
        /// 将字符串内字符随机打乱重排
        /// </summary>
        /// <param name="origStr">待打乱字符串</param>
        /// <returns>打乱后字符串</returns>
        public static string ConvertStringToRandomChanged(string origStr)
        {
            try
            {
                int len = origStr.Length;
                if (len > 0)
                {
                    var (_, _, randomlist) = CreatUnduplicatedRandomListV2(0, len, len);
                    char[] list_s = origStr.ToCharArray();
                    StringBuilder sb = new();
                    for (int i = 0; i < len; i++)
                    {
                        var index = randomlist[i];
                        sb.Append(list_s[index]);
                    }
                    string res = sb.ToString();
                    return res;
                }
                else
                { return string.Empty; }
            }
            catch (Exception)
            { return string.Empty; }
        }

        /// <summary>
        /// 随机生成指定长度的16进制字符串
        /// </summary>
        /// <param name="p_HexStringLength">16进制字符串长度</param>
        /// <returns>指定长度的16进制字符串</returns>
        public string CreatRandomHexString(ushort p_HexStringLength)
        {
            if (p_HexStringLength > 0)
            {
                var lower = 16 ^ p_HexStringLength - 1 + 1;
                var upper = 16 ^ p_HexStringLength;
                var res_temp = RandomNumberGenerator.GetInt32(lower, upper);
                var res = Convert.ToString(res_temp, 16).ToUpper();
                return res;
            }
            else
            {
                return "0";
            }
        }

        /// <summary>
        /// 创建验证码的方法
        /// 既能生成纯数字验证码，也能生成数字字母混合验证码。
        /// </summary>
        /// <param name="baseCodeString">验证码基础字符串（验证码的字符将从基础基础字符串中产生）</param>
        /// <param name="codeLength">验证码的位数</param>
        /// <returns>验证码字符串</returns>
        public string VerificationCodeCreat(string baseCodeString, uint codeLength)
        {
            var baseCodeStringTrimed = baseCodeString.Trim();
            if (string.IsNullOrEmpty(baseCodeStringTrimed))
            { return string.Empty; }
            else if (codeLength == 0)
            { return string.Empty; }

            char[] buffer = baseCodeStringTrimed.ToCharArray();
            StringBuilder builder = new();
            for (int i = 1; i <= codeLength; i++)
            {
                var index = RandomNumberGenerator.GetInt32(0, baseCodeString.Length);
                builder.Append(buffer[index]);
            }
            var res = builder.ToString();
            return res;
        }
    }
}
