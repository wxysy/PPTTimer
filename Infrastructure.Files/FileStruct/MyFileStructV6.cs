using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Infrastructure.Files.FileStruct
{
    /// <summary>
    /// 【文件结构生成方法（第6版）】
    /// 在第5版的基础上继续改进：
    /// 1、文件头部只保存后面各固定部分的长度信息，在读取的时候可是通过头部信息直接跳转至指定固定长度部分，不需要逐一读取所有固定长度部分。
    /// 2、固定长度部分现在也可以保存大数据量的信息，且写入和读取都有进度指示。
    /// 3、文件头部字典损坏时，可以使用应急方法读取固定部分信息。
    /// </summary>
    public class MyFileStructV6
    {
        /*  第6版文件结构
         * 【文件头部】--【固定部分1】--【固定部分2】···【固定部分n】--【可变部分】
         * 【[头部长度(4字节)][头部信息]】--【[固定部分1长度(4字节)][固定部分1信息]】···【[固定部分n长度(4字节)][固定部分n信息]】--【[可变部分信息]】
         */

        #region 生成文件V6(分部生成，一次写入)
        /// <summary>
        /// 生成文件Ver6.0
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fixedPartList">文件固定长度部分（可以无内容）</param>
        /// <param name="variablePart">文件可变长度部分（可以无内容）</param>
        /// <param name="progress">进度指示</param>
        /// <param name="bufferMultiple">设定缓存大小（单位：MB，推荐值：2）</param>
        /// <param name="token">传播有关应取消操作的通知</param>
        /// <returns>（是否生成成功，错误消息）</returns>
        public static (bool Res, string Mes) CreatFile(string filePath, List<byte[]>? fixedPartList, byte[]? variablePart, IProgress<string>? progress, int bufferMultiple, CancellationToken token)
        {
            /*  第6版文件结构
             * 【文件头部】--【固定部分1】--【固定部分2】···【固定部分n】--【可变部分】
             * 【[头部长度(4字节)][头部信息]】--【[固定部分1长度(4字节)][固定部分1信息]】···【[固定部分n长度(4字节)][固定部分n信息]】--【[可变部分信息]】
             */
            try
            {
                //为空时处理(两者都为空也允许)
                fixedPartList ??= [];
                variablePart ??= [];

                //【生成文件头部】
                Dictionary<string, string> fileHeadDict = [];
                int i = 0;
                foreach (var fixedPart in fixedPartList) //每个固定部分的长度
                {
                    i++;
                    string key = $"FP_{i}";//FixedPart_i
                    string value = fixedPart.Length.ToString();
                    fileHeadDict.Add(key, value);
                }
                fileHeadDict.Add("FixedPartCount", fixedPartList.Count.ToString()); //固定部分总数量
                byte[] fileHeadDictBytes = JsonSerializer.SerializeToUtf8Bytes(fileHeadDict);//Json序列化为 UTF-8

                //《Json序列化为 UTF-8》-- https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/system-text-json/how-to#serialize-to-utf-8
                //《Json从 UTF-8 进行反序列化》-- https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/system-text-json/deserialization#deserialize-from-utf-8

                int fileHeadLength = fileHeadDictBytes.Length;
                byte[] fileHeadLengthBytes = BitConverter.GetBytes(fileHeadLength);//int32类型转化为byte[]，长度均为4。
                if (BitConverter.IsLittleEndian)
                {
                    // BitConverter应对不同计算机体系结构的处理。
                    // 参考《BitConverter 类》：https://learn.microsoft.com/zh-cn/dotnet/api/system.bitconverter?view=net-8.0                                                                                
                    Array.Reverse(fileHeadLengthBytes);
                }

                //【将各部分写入文件】
                FileInfo fileInfo = new(filePath);
                if (fileInfo.Exists)
                    fileInfo.Delete();//删除已存在的文件
                using (FileStream fs = fileInfo.Create())
                {
                    // 写入文件头部
                    fs.Write(fileHeadLengthBytes, 0, fileHeadLengthBytes.Length);//头部字典长度
                    fs.Write(fileHeadDictBytes, 0, fileHeadDictBytes.Length);//头部字典
                    progress?.Report($"文件生成（文件头部）--已写入");

                    // 写入固定部分
                    int fixedPartNumber = 0;
                    foreach (var fixedPart in fixedPartList)
                    {
                        fixedPartNumber++;

                        //2.1-写入固定部分长度信息
                        byte[] fixedPartLengthBytes = BitConverter.GetBytes(fixedPart.Length);
                        if (BitConverter.IsLittleEndian)
                        { Array.Reverse(fixedPartLengthBytes); }
                        fs.Write(fixedPartLengthBytes, 0, fixedPartLengthBytes.Length);

                        //2.2-直接写入fixedPart（一般大小适用）
                        //fs.Write(fixedPart, 0, fixedPart.Length);

                        //2.3-分步写入（如果写入部分很大，则需要显示读取进度）
                        var fixedPartLengthExist = fixedPart.Length;
                        using (MemoryStream ms = new(fixedPart))
                        {
                            var buffer = new byte[1024 * 1024 * bufferMultiple];
                            ms.Seek(0, SeekOrigin.Begin);
                            do
                            {
                                int readLen = 0;
                                if (buffer.Length >= fixedPartLengthExist)
                                {
                                    readLen = ms.Read(buffer, 0, fixedPartLengthExist);
                                    fixedPartLengthExist = 0;
                                }
                                else
                                {
                                    readLen = ms.Read(buffer, 0, buffer.Length);
                                    fixedPartLengthExist -= buffer.Length;
                                }
                                fs.Write(buffer, 0, readLen);

                                if (token.IsCancellationRequested)
                                    return (false, $"文件生成（第{fixedPartNumber}固定部分）--中止写入");
                            } while (fixedPartLengthExist > 0);
                        }

                        progress?.Report($"文件生成（第{fixedPartNumber}固定部分）--已写入");
                    }

                    // 写入可变部分(可变部分可能很大，因此要能显示处理进度和停止)
                    if (variablePart.Length > 0)
                    {
                        using (MemoryStream ms = new(variablePart))
                        {
                            var buffer = new byte[1024 * 1024 * bufferMultiple];
                            ms.Seek(0, SeekOrigin.Begin);
                            int readTotalLen = 0;
                            do
                            {
                                int readLen = ms.Read(buffer, 0, buffer.Length);
                                fs.Write(buffer, 0, readLen);
                                readTotalLen += readLen;
                                var per = (long)readTotalLen * 100 / ms.Length;

                                progress?.Report($"文件生成（可变部分）--写入进度：{per}");
                                if (token.IsCancellationRequested)
                                    return (false, "文件生成（可变部分）--中止写入");
                            } while (ms.Position < ms.Length);
                            progress?.Report("文件生成--【成功】");
                        }
                    }
                    else
                    { }
                }
                return (true, string.Empty);
            }
            catch (Exception e)
            {
                FileInfo fileInfo = new(filePath);
                if (fileInfo.Exists)
                    fileInfo.Delete();//删除已存在的文件
                return (false, e.Message);
            }
        }
        #endregion

        #region 读取文件V6(读取头部，读取固定部分，读取可变部分)  
        /// <summary>
        /// 读取文件Ver6.0--读取头部信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>（是否读取成功，文件头部字典，文件头部总长度[含指示文件头部长度的4字节]）</returns>
        public static (bool Res, Dictionary<string, string> FileHeadDict, int FileHeadFullLength) ReadFileHead(string filePath)
        {
            try
            {
                //1-判断输入路径是否存在
                FileInfo fileInfo_head = new(filePath);
                if (fileInfo_head.Exists != true)
                    return (false, [], 0);

                //2-读取文件头N部分
                using (FileStream fs = fileInfo_head.Open(FileMode.Open))
                {
                    //2.1-调整流位置
                    fs.Seek(0, SeekOrigin.Begin);
                    //2.2-读取文件头部长度存储字节(【固定为4字节】)
                    byte[] fileHeadLengthBytes = new byte[4];
                    fs.Read(fileHeadLengthBytes, 0, fileHeadLengthBytes.Length);
                    if (BitConverter.IsLittleEndian)
                    {
                        // BitConverter应对不同计算机体系结构的处理。
                        // 参考《BitConverter 类》：https://learn.microsoft.com/zh-cn/dotnet/api/system.bitconverter?view=net-8.0                                                                       
                        Array.Reverse(fileHeadLengthBytes);
                    }
                    //2.3-转换为文件头部长度
                    int fileHeadLength = BitConverter.ToInt32(fileHeadLengthBytes, 0);
                    int fileHeadFullLength = fileHeadLengthBytes.Length + fileHeadLength;
                    //2.4-读取文件头部
                    byte[] fileHeadDictBytes = new byte[fileHeadLength];
                    var readLen = fs.Read(fileHeadDictBytes, 0, fileHeadDictBytes.Length);
                    if (readLen != fileHeadLength)
                        return (false, [], fileHeadFullLength);
                    //2.5-Json反序列化
                    var readOnlySpan = new ReadOnlySpan<byte>(fileHeadDictBytes);
                    var fileHeadDict = JsonSerializer.Deserialize<Dictionary<string, string>>(readOnlySpan)!;
                    //《Json序列化为 UTF-8》-- https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/system-text-json/how-to#serialize-to-utf-8
                    //《Json从 UTF-8 进行反序列化》-- https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/system-text-json/deserialization#deserialize-from-utf-8

                    return (true, fileHeadDict, fileHeadFullLength);
                }
            }
            catch
            {
                return (false, [], 0);
            }
        }

        /// <summary>
        /// 读取文件Ver6.0--读取指定固定部分
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fixedPartNumber">指定第几固定部分（从1开始，1 2 3···）</param>
        /// <param name="progress">进度指示</param>
        /// <param name="bufferMultiple">设定缓存大小（单位：MB，推荐值：2）</param>
        /// <param name="token">传播有关应取消操作的通知</param>
        /// <returns>（是否读取成功，指定固定部分，读取指定固定部分后文件流指针所在位置）</returns>
        public static (bool Res, byte[] FixedPart, long EndPosition) ReadFileFixedPart(string filePath, int fixedPartNumber, IProgress<string>? progress, int bufferMultiple, CancellationToken token)
        {
            var readHead = ReadFileHead(filePath);
            if (readHead.Res != true)
                return (false, [], 0);
            var fileHeadDict = readHead.FileHeadDict;
            int fileHeadFullLength = readHead.FileHeadFullLength;

            int fixedPartStartPosition = 0;
            string key = $"FP_{fixedPartNumber}";//FixedPart_i
            int j = fixedPartNumber - 1;
            if (fileHeadDict.ContainsKey(key))
            {
                string value = fileHeadDict[key];
                int fixedPartLength = int.Parse(value);

                int passedFixedPartLength = 0;
                for (int i = 1; i <= j; i++)
                {
                    string keyPassed = $"FP_{i}";
                    int valuePassed = int.Parse(fileHeadDict[keyPassed]);
                    passedFixedPartLength += 4 + valuePassed;
                }
                fixedPartStartPosition = fileHeadFullLength + passedFixedPartLength + 4;

                FileInfo fileInfo = new(filePath);
                using (FileStream fs = fileInfo.Open(FileMode.Open))
                {
                    //2.1-调整流位置
                    fs.Seek(fixedPartStartPosition, SeekOrigin.Begin);

                    //2.2-直接读取（一般大小适用）
                    //byte[] fixedPart = new byte[fixedPartLength];
                    //var readLen = fs.Read(fixedPart, 0, fixedPart.Length);
                    //if (readLen != fixedPartLength)
                    //    return (false, [], fs.Position);
                    //return (true, fixedPart, fs.Position);

                    //2.3-分步读取（如果读取部分很大，则需要显示读取进度）
                    var fixedPartLengthExist = fixedPartLength;
                    using (MemoryStream ms = new())
                    {
                        var buffer = new byte[1024 * 1024 * bufferMultiple];
                        int readTotalLen = 0;
                        do
                        {
                            int readLen = 0;
                            if (buffer.Length >= fixedPartLengthExist)
                            {
                                readLen = fs.Read(buffer, 0, fixedPartLengthExist);
                                fixedPartLengthExist = 0;
                            }
                            else
                            {
                                readLen = fs.Read(buffer, 0, buffer.Length);
                                fixedPartLengthExist -= buffer.Length;
                            }
                            ms.Write(buffer, 0, readLen);
                            readTotalLen += readLen;
                            var per = (long)readTotalLen * 100 / fixedPartLength;
                            progress?.Report($"文件读取（第{fixedPartNumber}固定部分）--读取进度：{per}");
                            if (token.IsCancellationRequested)
                                return (false, [], fs.Position);
                        } while (fixedPartLengthExist > 0);
                        progress?.Report($"文件读取（第{fixedPartNumber}固定部分）--【成功】");

                        ms.Flush();//释放缓存
                        ms.Seek(0, SeekOrigin.Begin);//再次调整流位置
                        var res = ms.ToArray();
                        if (res.Length > 0)
                            return (true, res, fs.Position);
                        else
                            return (false, [], fs.Position);
                    }
                }
            }
            else
            { return (false, [], 0); }
        }

        /// <summary>
        /// 读取文件Ver6.0--读取指定固定部分（应急方法）
        /// 仅在文件头字典损坏时使用，且此方法无法知道固定部分的最终数量。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fixedPartNumber">指定第几固定部分（从1开始，1 2 3···）</param>
        /// <param name="progress">进度指示</param>
        /// <param name="bufferMultiple">设定缓存大小（单位：MB，推荐值：2）</param>
        /// <param name="token">传播有关应取消操作的通知</param>
        /// <returns>（是否读取成功，指定固定部分，读取指定固定部分后文件流指针所在位置）</returns>
        public static (bool Res, byte[] FixedPart, long EndPosition) ReadFileFixedPartEmergency(string filePath, int fixedPartNumber, IProgress<string>? progress, int bufferMultiple, CancellationToken token)
        {
            if (fixedPartNumber < 1)
                return (false, [], 0);

            var readHead = ReadFileHead(filePath);
            if (readHead.FileHeadFullLength == 0) //如果文件头部长度信息读取失败，那彻底没有办法了。
                return (false, [], 0);
            var fileHeadFullLength = readHead.FileHeadFullLength;
            int fixedPartStartPosition = fileHeadFullLength;

            var fixedPartLength = 0;
            FileInfo fileInfo = new(filePath);
            using (FileStream fs = fileInfo.Open(FileMode.Open))
            {
                //2.1-调整流位置
                fs.Seek(fixedPartStartPosition, SeekOrigin.Begin);

                for (int i = 1; i <= fixedPartNumber; i++)
                {
                    byte[] buffer_i = new byte[4];
                    fs.Read(buffer_i, 0, buffer_i.Length);
                    if (BitConverter.IsLittleEndian)
                    {
                        // BitConverter应对不同计算机体系结构的处理。
                        // 参考《BitConverter 类》：https://learn.microsoft.com/zh-cn/dotnet/api/system.bitconverter?view=net-8.0                                                                       
                        Array.Reverse(buffer_i);
                    }
                    //2.3-转换为文件头部长度
                    int fixedPart_i_Length = BitConverter.ToInt32(buffer_i, 0);

                    if (i < fixedPartNumber) //没读取到所需部分时
                    {
                        //继续调整流的位置
                        fs.Seek(fixedPart_i_Length, SeekOrigin.Current);
                    }
                    else //读取到所需部分时
                    {
                        fixedPartLength = fixedPart_i_Length;
                    }
                }

                //2.3-分步读取（如果读取部分很大，则需要显示读取进度）             
                var fixedPartLengthExist = fixedPartLength;
                using (MemoryStream ms = new())
                {
                    var buffer = new byte[1024 * 1024 * bufferMultiple];
                    int readTotalLen = 0;
                    do
                    {
                        int readLen = 0;
                        if (buffer.Length >= fixedPartLengthExist)
                        {
                            readLen = fs.Read(buffer, 0, fixedPartLengthExist);
                            fixedPartLengthExist = 0;
                        }
                        else
                        {
                            readLen = fs.Read(buffer, 0, buffer.Length);
                            fixedPartLengthExist -= buffer.Length;
                        }
                        ms.Write(buffer, 0, readLen);
                        readTotalLen += readLen;
                        var per = (long)readTotalLen * 100 / fixedPartLength;
                        progress?.Report($"文件读取（第{fixedPartNumber}固定部分）Emergency--读取进度：{per}");
                        if (token.IsCancellationRequested)
                            return (false, [], fs.Position);
                    } while (fixedPartLengthExist > 0);
                    progress?.Report($"文件读取（第{fixedPartNumber}固定部分）Emergency--【成功】");

                    ms.Flush();//释放缓存
                    ms.Seek(0, SeekOrigin.Begin);//再次调整流位置
                    var res = ms.ToArray();
                    if (res.Length > 0)
                        return (true, res, fs.Position);
                    else
                        return (false, [], fs.Position);
                }
            }
        }

        /// <summary>
        /// 读取文件Ver6.0--读取可变部分
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="progress">进度指示</param>
        /// <param name="bufferMultiple">设定缓存大小（单位：MB，推荐值：2）</param>
        /// <param name="token">传播有关应取消操作的通知</param>
        /// <returns>（是否读取成功，可变部分，错误信息）</returns>
        public static (bool Res, byte[] VariablePart, string Mes) ReadFileVariablePart(string filePath, IProgress<string>? progress, int bufferMultiple, CancellationToken token)
        {
            //读取文件头部
            var readHead = ReadFileHead(filePath);
            if (!readHead.Res)
                return (false, [], "文件读取（文件可变部分）--文件头部读取：失败");
            var fileHeadDict = readHead.FileHeadDict;
            int fileHeadFullLength = readHead.FileHeadFullLength;
            int fixedPartCount = int.Parse(fileHeadDict["FixedPartCount"]);

            //获取文件可变部分初始位置
            long startPosition = 0;
            if (fixedPartCount > 0)
            {
                var (Res, _, EndPosition) = ReadFileFixedPart(filePath, fixedPartCount, null, bufferMultiple, token);
                if (Res)
                    startPosition = EndPosition;
                else
                    return (false, [], $"文件读取（文件可变部分）--起始位置获取：失败");
            }
            else
            {
                startPosition = fileHeadFullLength;
            }
            progress?.Report($"文件读取（文件可变部分）--起始位置获取：成功");

            //读取文件可变部分(可变部分可能很大，因此要能显示处理进度和停止)
            FileInfo fileInfo = new(filePath);
            using (FileStream fs = fileInfo.Open(FileMode.Open))
            {
                //3.1-调整流位置
                fs.Seek(startPosition, SeekOrigin.Begin);

                using (MemoryStream ms = new())
                {
                    var buffer = new byte[1024 * 1024 * bufferMultiple];
                    int readTotalLen = 0;
                    do
                    {
                        int readLen = fs.Read(buffer, 0, buffer.Length);
                        ms.Write(buffer, 0, readLen);
                        readTotalLen += readLen;
                        var per = (long)readTotalLen * 100 / (fs.Length - startPosition);
                        progress?.Report($"文件读取（文件可变部分）--读取进度：{per}");
                        if (token.IsCancellationRequested)
                            return (false, [], "文件读取（文件可变部分）--读取中止");
                    } while (fs.Position < fs.Length);
                    progress?.Report("文件读取（文件可变部分）--【成功】");

                    ms.Flush();//释放缓存
                    ms.Seek(0, SeekOrigin.Begin);//再次调整流位置
                    var res = ms.ToArray();
                    if (res.Length > 0)
                        return (true, res, string.Empty);
                    else
                        return (false, [], "文件读取（文件可变部分）--为空");
                }
            }
        }
        #endregion
    }
}
