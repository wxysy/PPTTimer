using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Files.FileCommon
{
    public class MyFileErase
    {
        #region 【核心】文件擦除-核心算法(Data.MathematicalAlgorithms项目--MyMathMethods类中也有，单独写是不想产生相互引用。)      
        /// <summary>
        /// 计算位于下限值（包括）和上限值（不包括）之间的随机数列表V2
        /// </summary>
        /// <param name="lower">下限值（包括）</param>
        /// <param name="upper">上限值（不包括）</param>
        /// <param name="number_need">需要的随机数个数（必须大于零）</param>
        /// <returns>位于下限值（包括）和上限值（不包括）之间的随机数列表</returns>
        private static (bool UnRan_res, string UnRan_mes, List<int> UnRan_list) CreatUnduplicatedRandomListV2(int lower, int upper, int number_need)
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
        /// 【核心方法】擦除文件核心方法V2
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="progress">报告处理进度</param>
        /// <param name="bufferSize">缓冲区大小倍数调节(以1Mb为基本单元，该参数很影响运算速度。)，通常设定为2^n，即2、4、8、16、32……(缓存大小就是2^n * 1Mb = 2^n Mb)。</param>
        /// <param name="token">取消操作</param>
        /// <returns>(操作是否成功，显示信息)</returns>
        public static (bool Res, string Mes) FileEraseV2(string filePath, IProgress<int>? progress, int bufferSize, CancellationToken token)
        {
            try
            {
                FileInfo fileInfo = new(filePath);
                if (fileInfo.Exists)
                { }
                else
                { return (false, "该文件不存在。"); }

                var array = new byte[1024 * 1024 * bufferSize];
                Span<byte> buffer = new(array);//从数组创建 Span<Byte>，用Span是为了速度。

                using (FileStream fs = new(filePath, FileMode.Open))
                {
                    long fsTotalLength = fs.Length;
                    long fsWriteLength = 0;
                    fs.Seek(0, SeekOrigin.Begin);

                    do
                    {
                        RandomNumberGenerator.Fill(buffer);
                        fs.Write(buffer);

                        fsWriteLength += buffer.Length;
                        if (fs.Position < fsTotalLength)
                        {
                            var per = 100 * fsWriteLength / fsTotalLength;
                            progress?.Report(Convert.ToInt32(per));
                        }
                        else
                        {
                            progress?.Report(100);
                            break;
                        }

                        if (token.IsCancellationRequested == true)
                        {
                            return (false, $"中止文件{fileInfo.Name}的擦除。");
                        }
                        else
                        { }
                    } while (fs.Position < fsTotalLength);
                }

                fileInfo.Delete();
                return (true, $"文件{fileInfo.Name}已擦除。");
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        /// <summary>
        /// 擦除文件异步方法，重写文件内容后删除。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fastEraseMode">擦除模式设定</param>
        /// <param name="token">用于异步方法的取消操作</param>
        /// <param name="progress">用于异步方法的报告处理进度</param>
        /// <param name="bufferMultiple">缓冲区大小倍数调节(以1Mb为基本单元，该参数很影响运算速度。)，通常设定为2^n，即2、4、8、16、32……(缓存大小就是2^n * 1Mb = 2^n Mb)。</param>
        /// <returns>擦除操作是否成功</returns>
        public static Task<bool> FileEraseAsync(string filePath, CancellationToken token, IProgress<int>? progress = null, int bufferMultiple = 2)
        {
            Task<bool> mytask = new(() =>
            {
                var (res, _) = FileEraseV2(filePath, progress, bufferMultiple, token);
                return res;
            }, token, TaskCreationOptions.LongRunning);
            mytask.Start();
            return mytask;
        }
        #endregion

    }
}
