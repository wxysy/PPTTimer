using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Data.Common.Serialize;

namespace Data.Common.Serialize
{
    public class MyHash
    {
        #region 通用型计算与比较方法
        /// <summary>
        /// 通用型哈希值字符串计算方法V2(以流的思想统一处理，什么类型都通用，并且可以报告处理进度。)
        /// 用前一定要确定算法实例中有ComputeHash(byte[])方法。
        /// 需和Hash实例配合使用，如：SHA384 sha384 = SHA384.Create()。
        /// 参考：[C#] 计算大文件的MD5的两种方式(直接调用方法计算，流计算-适用于大文件)
        /// https://blog.csdn.net/qiujuer/article/details/19344527
        /// </summary>
        /// <typeparam name="T">待计算哈希值的类型(流类型的话从流当前指针位置开始计算Hash)</typeparam>
        /// <param name="hashAlgorithm">算法实例，HashAlgorithm类是所有加密哈希算法实现均必须从中派生的基类。</param>
        /// <param name="obj">待计算哈希值的类型实例</param>
        /// <param name="progress">报告处理进度(百分比形式字符串，如：15.22%。)</param>
        /// <returns>哈希值字符串</returns>
        public static string HashHexStringGen<T>(HashAlgorithm hashAlgorithm, T? obj, IProgress<double>? progress, CancellationToken? token)
        {
            try
            {
                //--1、声明统一的输入流
                Stream inputStream;

                //--2、输入为不同类型时的处理，统一转化为输入流的实例。--
                if (obj is null)//为空时，is 这里的用法等同于 input_T == null。
                {
                    return string.Empty;
                }
                else if (obj is string str && string.IsNullOrEmpty(str))//为空字符串时。
                {
                    return string.Empty;
                }
                else if (obj is Stream stream_T)//为流时
                {
                    //复制流
                    inputStream = stream_T;

                    //确保指针在流的开始位置(因为流stream_T的指针未必在开始位置)
                    //还是要加，排除指针的影响，原因如下。
                    inputStream.Seek(0, SeekOrigin.Begin);//等同于inputStream.Position = 0;

                    /* 关于byte[] buffer 和 byte[]生成的流Stream 哈希值是否相等问题
                     * 【是否相等由流stream的初始状态决定！】
                     * 【stream的哈希值固定，不论是否读取数据。】
                     * 1、如果是using MemoryStream stream = new(buffer);生成的流，
                     * 哪怕在之后又读了数据，stream.Read(buffer2);
                     * 只要是重新调整流的 Position = 0 。
                     * buffer 和 stream 哈希值都 【相等】。
                     * 2、但如果是using MemoryStream stream = new();生成的流，
                     * 哪怕再用了 stream.Read(buffer); + stream.Seek(0, SeekOrigin.Begin);
                     * buffer 和 stream 哈希值都 【不相等】。
                     */
                }
                else if (obj is byte[] inputBytes)//为数组时，转化为流。
                {
                    byte[] bytes_Encoding = inputBytes;
                    inputStream = new MemoryStream(bytes_Encoding);
                }
                else//为其他类型时，先转化为数组，再转化为流。
                {
                    byte[] bytes_Encoding = MySerialize.DataContractSerializeToBytes(obj);
                    inputStream = new MemoryStream(bytes_Encoding);
                }

                //--3、对输入流进行处理--
                int bufferSize = 16 * 1024;//缓冲区大小16Kb
                byte[] buffer_in = new byte[bufferSize];
                byte[] buffer_out = new byte[bufferSize];
                long totalReadLength = 0;
                long totalLength = inputStream.Length;
                int readLength;//每次实际读取的长度
                while ((readLength = inputStream.Read(buffer_in, 0, buffer_in.Length)) > 0)
                {
                    //计算每块的哈希值
                    hashAlgorithm.TransformBlock(buffer_in, 0, readLength, buffer_out, 0);
                    //显示进度
                    totalReadLength += readLength;
                    double per = totalReadLength / totalLength;
                    progress?.Report(per);//处理进度用百分比显示，1位小数，如：15.2%。
                    //取消操作
                    if (token?.IsCancellationRequested == true)
                    {
                        return string.Empty;
                    }
                }
                hashAlgorithm.TransformFinalBlock(buffer_in, 0, 0);//完成最后计算，必须调用(由于上一部循环已经完成所有运算，所以调用此方法时后面的两个参数都为0)
                                                                   //获取哈希值的计算结果
                byte[]? bytes_Caculated = hashAlgorithm.Hash;//最后的计算结果
                                                             //输出哈希值字符串
                string res;
                if (bytes_Caculated != null)
                {
                    res = Convert.ToHexString(bytes_Caculated);//转化为字符串显示输出
                }
                else
                {
                    res = string.Empty;
                }
                return res;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 通用型哈希值比较方法
        /// 需和Hash实例配合使用，如：SHA384 sha384 = SHA384.Create()。
        /// 用前一定要确定算法实例中有ComputeHash(byte[])方法
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="hashHexStringCompared">待比较的哈希值字符串</param>
        /// <param name="obj">待计算哈希值的实例</param>
        /// <param name="hashAlgorithm">Hash实例（如：SHA384 sha384 = SHA384.Create()）</param>
        /// <param name="ignoreCase">比较时是否忽略大小写，默认是true-忽略。</param>
        /// <returns>true-相同，false-不同</returns>
        public static bool HashHexStringCompare<T>(HashAlgorithm hashAlgorithm, T? obj, string hashHexStringCompared, IProgress<double>? progress, CancellationToken? token, bool ignoreCase = true)
        {
            #region 计算Hash值
            string caculatedHashString = HashHexStringGen(hashAlgorithm, obj, progress, token);
            #endregion

            #region 比较Hash值
            int res_int = string.Compare(hashHexStringCompared, caculatedHashString, ignoreCase);
            if (res_int == 0)
                return true;
            else
                return false;
            #endregion
        }

        /// <summary>
        /// 计算单个文件的哈希值异步方法
        /// 用前一定要确定算法实例中有ComputeHash(byte[])方法
        /// 需和Hash实例配合使用，如：SHA384 sha384 = SHA384.Create()。
        /// </summary>
        /// <param name="hashAlgorithm">算法实例，HashAlgorithm类是所有加密哈希算法实现均必须从中派生的基类。</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="progress">处理进度</param>
        /// <param name="token">用于取消异步操作的实例</param>
        /// <returns>此文件的哈希值字符串</returns>
        public static Task<string> HashStringGenSingleFileAsync(HashAlgorithm hashAlgorithm, string filePath, IProgress<double>? progress, CancellationToken token)
        {
            Task<string> mytask = new(() =>
            {
                var res = HashStringGenSingleFile(hashAlgorithm, filePath, progress, token);
                return res;
            }, token, TaskCreationOptions.LongRunning);
            mytask.Start();
            return mytask;
        }
        private static string HashStringGenSingleFile(HashAlgorithm hashAlgorithm, string filePath, IProgress<double>? progres, CancellationToken? token)
        {
            //判断输入路径在不在
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists == false)
                return string.Empty;

            using (FileStream fileStream = fileInfo.Open(FileMode.Open))
            {
                var res = HashHexStringGen(hashAlgorithm, fileStream, progres, token);
                return res;
            }
        }

        /// <summary>
        /// 计算文件夹内所有文件的哈希值异步方法
        /// 用前一定要确定算法实例中有ComputeHash(byte[])方法
        /// 需和Hash实例配合使用，如：SHA384 sha384 = SHA384.Create()。
        /// </summary>
        /// <param name="callBack">每算完一个文件后操作</param>
        /// <param name="hashAlgorithm">算法实例，HashAlgorithm类是所有加密哈希算法实现均必须从中派生的基类。</param>
        /// <param name="directoryPath">文件夹路径</param>
        /// <param name="progress">报告单个文件处理进度</param>
        /// <param name="token">用于取消异步操作的实例</param>
        /// <returns></returns>
        public static Task HashHexStringGenMultiFilesAsync(HashAlgorithm hashAlgorithm, string directoryPath, Action<(string FileName, string FileHashHexString)> callBack, IProgress<double>? progress, CancellationToken token)
        {
            Task mytask = new(() =>
            {
                DirectoryInfo directory = new(directoryPath);
                //文件夹存不存在
                if (directory.Exists != true)
                { }
                else
                {
                    var files = directory.GetFiles();//可以使用searchPattern和SearchOption。
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (token.IsCancellationRequested == true)
                            break;
                        var filePath = files[i].FullName;
                        var fileHashValve = HashStringGenSingleFile(hashAlgorithm, filePath, progress, token);
                        var fileName = Path.GetFileName(filePath);
                        (string, string) res = new(fileName, fileHashValve);

                        callBack.Invoke(res);

                        // 对callBack的处理一般放到MainViewModel中，不放在这。
                        //《不支持从调度程序线程以外的线程对其 SourceCollection 进行的更改》
                        //https://blog.csdn.net/Until_youyf/article/details/102720112
                        //System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        //{
                        //    callBack.Invoke(res);
                        //});
                    }
                }
            });
            mytask.Start();
            return mytask;
        }
        #endregion

        #region SHA384专用型计算、比较哈希值
        /// <summary>
        /// 计算一个实体的sha384值
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="obj">待计算哈希值的实例</param>
        /// <returns>实体的sha384值</returns>
        public static string HashHexStringGen_SHA384<T>(T? obj, IProgress<double>? progress, CancellationToken? token)
        {
            using (SHA384 sha384 = SHA384.Create())
            {
                //使用sha384实例时，输出字符串固定长度96字节。
                var res = HashHexStringGen(sha384, obj, progress, token);
                return res;
            }
        }

        /// <summary>
        /// 计算一个实体的sha384值并与给定值相比较
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="hashHexStringCompared">sha384给定值字符串</param>
        /// <param name="input_T">待计算sha384值的实例</param>
        /// <param name="progress">处理进度</param>
        /// <param name="token">用于取消异步操作</param>
        /// <param name="ignoreCase">比较时是否忽略大小写，默认是true-忽略。</param>
        /// <returns></returns>
        public static bool HashHexStringCompare_SHA384<T>(T? obj, string hashHexStringCompared, IProgress<double>? progress, CancellationToken? token, bool ignoreCase = true)
        {
            using (SHA384 sha384 = SHA384.Create())
            {
                var res = HashHexStringCompare(sha384, obj, hashHexStringCompared, progress, token, ignoreCase);
                return res;
            }
        }
        #endregion

        #region 计算CRC32值（.Net6中竟然没有！这个是完全参考别人的方法）
        /// <summary>
        /// 计算类型实例的CRC32值（Stream流类型除外）
        /// </summary>
        /// <typeparam name="T">输入类型</typeparam>
        /// <param name="p_inputT">输入类型实例</param>
        /// <param name="p_InitialCRC">初始值[默认值:0xFFFFFFFF]</param>
        /// <param name="p_ReverseInput">输入是否反转[默认值:true]</param>
        /// <param name="p_Polynomial">多项式的值[默认值:0x04C11DB7]</param>
        /// <param name="p_ReverseOutput">输出是否反转[默认值:true]</param>
        /// <param name="p_OutputXOR">异或值XOR[默认值:0x00000000]</param>
        /// <returns></returns>
        public static string CaculateCRC32HexString<T>(T p_inputT, bool p_ReverseInput = true, bool p_ReverseOutput = true, uint p_InitialCRC = 0xFFFFFFFF, uint p_Polynomial = 0x04C11DB7, uint p_OutputXOR = 0x00000000)
        {
            if (p_inputT != null)
            {
                //1、实例序列化
                byte[] temp_buffer = MySerialize.DataContractSerializeToBytes(p_inputT);
                //2、计算CRC32值
                uint temp_res = GetCRC32_Core(temp_buffer, p_InitialCRC, p_ReverseInput, p_Polynomial, p_ReverseOutput, p_OutputXOR);
                //3、将结果转化为十六进制字符串
                var res = Convert.ToString(temp_res, 16).ToUpper();//7位或8位
                //4、填充为统一长度字符
                res = res.PadLeft(8, '0');//长度设定为8位，不够的话左边填充字符0。

                // 填充语句也可以用下面替换，效果一样。
                //if (res.Length < 8)
                //res = "0" + res;

                return res;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 计算CRC32值-核心方法（这个是完全参考别人的方法）
        /// CRC校验位数（宽度）：32位。
        /// 参考文献：《浅谈CRC校验：C#实现CRC8、CRC16、CRC32》
        /// https://blog.csdn.net/huijunma2010/article/details/124151471
        /// 在线验证网页
        /// https://www.lddgo.net/encrypt/crc
        /// </summary>
        /// <param name="p_buffer">输入字节</param>
        /// <param name="p_InitialCRC">初始值[默认值:0xFFFFFFFF]</param>
        /// <param name="p_ReverseInput">输入是否反转[默认值:true]</param>
        /// <param name="p_Polynomial">多项式的值[默认值:0x04C11DB7]</param>
        /// <param name="p_ReverseOutput">输出是否反转[默认值:true]</param>
        /// <param name="p_OutputXOR">异或值XOR[默认值:0x00000000]</param>
        /// <returns></returns>
        private static uint GetCRC32_Core(byte[] p_buffer, uint p_InitialCRC = 0xFFFFFFFF, bool p_ReverseInput = true, uint p_Polynomial = 0x04C11DB7, bool p_ReverseOutput = true, uint p_OutputXOR = 0x00000000)
        {
            int length = p_buffer.Length;
            byte data;
            uint crc = p_InitialCRC; //初始值0xFFFFFFFF（16位举个例子0xFFFF,8位0xFF)
            int i;
            for (int j = 0; j < length; j++)
            {
                data = p_buffer[j];

                if (p_ReverseInput == true)
                {
                    data = GetCRC32_Reverse8(data);//输入是否反转，如果不需要则这句注释掉；
                }
                else
                { }

                crc = (uint)(crc ^ data << 24); //32位左移24位，16位左移8位，8位左移0位；
                for (i = 0; i < 8; i++)
                {
                    if ((crc & 0x80000000) == 0x80000000)//16位 0x8000  8位 0x80
                    {
                        crc = crc << 1 ^ p_Polynomial;//多项式0x04C11DB7，没什么好说的；
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }

            if (p_ReverseOutput == true)
            {
                crc = GetCRC32_Reverse32(crc);//输出是否需要反转，如果不需要则注释掉这句话，注意如果是16位则调用Reverse16(crc),8位调用Reverse8（crc）
            }
            else
            { }

            crc ^= p_OutputXOR;// xor 异或值0x00000000（与上面初始值类似，16位 0x0000  8位 0x00;
            return crc;

            /* 《浅谈CRC校验：C#实现CRC8、CRC16、CRC32》
             * https://blog.csdn.net/huijunma2010/article/details/124151471
             * 对于一个CRC校验来说，基本只需要关注六点（校验算法这点我们不做考虑）：
             * 1、多少位的校验（8、16、32）即宽度；
             * 2、CRC初始值；
             * 3、输入是否反转；
             * 4、多项式的值；
             * 5、输出是否反转；
             * 6、异或值是多少；
             */
        }
        private static uint GetCRC32_Reverse32(uint data)
        {
            byte i;
            uint temp = 0;
            for (i = 0; i < 32; i++)
            {
                temp |= (data >> i & 0x01) << 31 - i;
            }
            return temp;
        }
        private static uint GetCRC32_Reverse16(uint data)
        {
            byte i;
            uint temp = 0;
            for (i = 0; i < 16; i++)
            {
                temp |= (data >> i & 0x01) << 15 - i;
            }
            return temp;
        }
        private static byte GetCRC32_Reverse8(byte data)
        {
            byte i;
            byte temp = 0;
            for (i = 0; i < 8; i++)
            {
                temp = (byte)(temp | (data >> i & 0x01) << 7 - i);
            }
            return temp;
        }
        #endregion
    }
}
