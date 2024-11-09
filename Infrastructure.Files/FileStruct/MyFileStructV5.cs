using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Infrastructure.Files.FileStruct
{
    /// <summary>
    /// 第5版的文件结构生成方法
    /// </summary>
    [Obsolete("请使用MyFileStructV6类")]
    public class MyFileStructV5
    {
        //V4方法不再保留。

        #region 生成文件V5(分部生成，一次写入)
        //检测输入参数
        private static void CheckInputForFileCreatV5(string filepath, List<byte[]> fileHeadPartList, byte[] fileBody, IProgress<int>? progress, int bufferMultiple, CancellationToken token)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                throw new ArgumentException("文件路径为空", nameof(filepath));
            }
            else if (fileHeadPartList.Count == 0)
            {
                throw new ArgumentException("文件头部信息为空", nameof(fileHeadPartList));
            }
            else if (fileBody.Length == 0)
            {
                throw new ArgumentException("文件本体信息为空", nameof(fileBody));
            }
            else if (bufferMultiple < 1)
            {
                throw new ArgumentException("缓存大小设定错误，推荐值为 2", nameof(bufferMultiple));
            }
            else
            { }
        }
        /// <summary>
        /// 生成自定义结构文件V5（相比于V4，增加了文件头部数量的记载）
        /// </summary>
        /// <param name="filepath">生成文件的路径</param>
        /// <param name="fileHeadPartList">文件头部分列表</param>
        /// <param name="fileBody">文件本体部分</param>
        /// <param name="progress">文件本体生成进度</param>
        /// <param name="bufferMultiple">缓存倍率调节(默认值2，即2MB)</param>
        /// <param name="token">中止程序用</param>
        /// <returns>([生成成功], [相关信息])</returns>
        public static (bool Res, string Mes) FileCreat(string filepath, List<byte[]> fileHeadPartList, byte[] fileBody, IProgress<int>? progress, int bufferMultiple, CancellationToken token)
        {
            // 本方法创建的文件结构如下：
            //【[文件头部分总数(4字节)]-[文件头第1部分长度信息(4字节)]-[文件头第1部分]-[文件头第2部分长度信息(4字节)]-[文件头第2部分]-...-[文件头第N部分长度信息(4字节)]-[文件头第N部分]】-【文件本体】

            try
            {
                //1-检测7个输入参数(token、progress两个参数不检测)
                CheckInputForFileCreatV5(filepath, fileHeadPartList, fileBody, progress, bufferMultiple, token);

                //2-写入文件
                FileInfo fileInfo = new(filepath);
                if (fileInfo.Exists)
                    fileInfo.Delete();//删除已存在的文件
                using (FileStream fs = fileInfo.Create())
                {
                    /*--【就这一段变化相比V4】写入文件头部数量信息--*/
                    int fileHeadPartsCount = fileHeadPartList.Count;
                    byte[] fileHeadPartsCount_IntBytes = BitConverter.GetBytes(fileHeadPartsCount);
                    if (BitConverter.IsLittleEndian)
                    { Array.Reverse(fileHeadPartsCount_IntBytes); }
                    fs.Write(fileHeadPartsCount_IntBytes, 0, fileHeadPartsCount_IntBytes.Length);
                    /*--【就这一段变化相比V4】写入文件头部数量信息END--*/

                    //2.1-写入文件头部
                    foreach (var fileHeadPartN in fileHeadPartList)
                    {
                        //[文件头第N部分长度]数组
                        // 2.1.1-获取文件头第N部分长度
                        int fileHeadPartNLength = fileHeadPartN.Length;
                        // 2.1.2-将长度转化为字节数组(固定长度4字节)
                        byte[] fileHeadPartNLength_IntBytes = BitConverter.GetBytes(fileHeadPartNLength);
                        // 2.1.3-BitConverter应对不同计算机体系结构的处理。
                        // 参考《BitConverter 类》：https://docs.microsoft.com/zh-cn/dotnet/api/system.bitconverter?view=netcore-3.1                                                                                 
                        if (BitConverter.IsLittleEndian)
                        { Array.Reverse(fileHeadPartNLength_IntBytes); }

                        //2.1.4-写入文件头第N部分长度信息(4字节)
                        fs.Write(fileHeadPartNLength_IntBytes, 0, fileHeadPartNLength_IntBytes.Length);
                        //2.1.5-写入文件头第N部分
                        fs.Write(fileHeadPartN, 0, fileHeadPartN.Length);
                    }

                    // 2.2-文件本体(文件本体可能很大，因此要能显示处理进度和停止)
                    using (MemoryStream ms = new(fileBody))
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

                            if (ms.Position < ms.Length)
                                progress?.Report(Convert.ToInt32(per));
                            if (token.IsCancellationRequested)
                                return (false, "中止生成文件本体");
                        } while (ms.Position < ms.Length);
                        progress?.Report(100);
                    }
                }
                return (true, string.Empty);
            }
            catch (Exception e)
            {
                FileInfo fileInfo = new(filepath);
                if (fileInfo.Exists)
                    fileInfo.Delete();//删除已存在的文件
                return (false, e.Message);
            }
        }
        #endregion

        #region 读取文件V5(读取头部，获取头部数量，读取本体)       
        /// <summary>
        /// 【核心】读取自定义结构文件-读取文件头第N部分数据Core
        /// </summary>
        /// <param name="filePathExist">读取文件的路径</param>
        /// <param name="streamOffset">文件流的偏移量</param>
        /// <param name="partNIndex">文件头部第N部分索引值(i=N-1)</param>
        /// <returns>([读取成功], [相关信息], [文件头第N部分], [文件头第N部分总长(4字节+文件头A部分长度)])</returns>
        private static (bool Res, string Mes, byte[]? HeadPartN, int HeadPartN_FullLength) ReadHeadPartNCore(string filePathExist, long streamOffset, int partNIndex, bool readLengthOnly)
        {
            try
            {
                //1-判断输入路径是否存在
                FileInfo fileInfo_head = new(filePathExist);
                if (fileInfo_head.Exists != true)
                    return (false, "文件不存在！", default, default);

                //2-读取文件头N部分
                using (FileStream fs_head = fileInfo_head.Open(FileMode.Open))
                {
                    //2.1-调整流位置
                    fs_head.Seek(streamOffset, SeekOrigin.Begin);
                    //2.2-读取文件头N部分长度存储字节(【固定为4字节】)
                    byte[] fileHeadPartNLength_IntBytes = new byte[4];
                    fs_head.Read(fileHeadPartNLength_IntBytes, 0, fileHeadPartNLength_IntBytes.Length);
                    if (BitConverter.IsLittleEndian)
                    { Array.Reverse(fileHeadPartNLength_IntBytes); }
                    //2.3-转换为文件头N部分长度
                    int fileHeadPartNLength = BitConverter.ToInt32(fileHeadPartNLength_IntBytes, 0);
                    //2.4-计算文件头N部分总长度(4字节 + 文件头B部分长度)
                    int fileHeadPartNTotalLength = fileHeadPartNLength_IntBytes.Length + fileHeadPartNLength;

                    //2.5-判断只读取长度还是长度和内容都要读取。
                    if (readLengthOnly == true)
                    {
                        return (true, $"文件头部第{partNIndex + 1}部分(索引partIndex = {partNIndex})【长度】读取成功", default, fileHeadPartNTotalLength);
                    }
                    else
                    {
                        //读取文件头N部分
                        byte[] fileHeadPartN = new byte[fileHeadPartNLength];
                        var readLen = fs_head.Read(fileHeadPartN, 0, fileHeadPartN.Length);

                        if (readLen == fileHeadPartN.Length)
                        { return (true, $"文件头部第{partNIndex + 1}部分(索引partIndex = {partNIndex})【内容】读取成功", fileHeadPartN, fileHeadPartNTotalLength); }
                        else if (readLen > 0)
                        {
                            //【注意】成功读取的部分不一定都是文件头部！！！
                            return (false, $"文件头部已读取完毕，而且还读取了文件本体部分数据。" +
                                $"最后一次读取正常的是第{partNIndex}部分(索引partIndex = {partNIndex - 1})。\n" +
                                $"注意：哪怕对于最后一次读取正常的部分，读取正常只能说明这个所谓的\"第{partNIndex}部分\"读取成功，但不能认为这个所谓的\"第{partNIndex}部分\"就是文件头部的最后一部分。\n" +
                                $"因为也可能瞎猫撞上死耗子，读完了文件头部接着读文件本体。" +
                                $"如果读取的文件本体部分数据刚好也符合判定标准，就会将文件本体部分数据误认为是文件头部数据。\n" +
                                $"【文件头部到底有几个部分，必须在设计文件的时候就人工设定】", default, default);
                        }
                        else
                        { return (false, $"已到达文件流末尾", default, default); }
                    }
                }
            }
            catch (Exception e)
            {
                return (false, $"文件头部第{partNIndex + 1}部分(索引partIndex = {partNIndex})读取失败\n{e.Message}", default, default);
            }
        }

        /// <summary>
        /// [通用方法]读取自定义结构文件V5-读取文件头第N部分数据
        /// </summary>
        /// <param name="filePathExist">读取文件的路径</param>
        /// <param name="partNIndex">文件头部第N部分索引值(i=N-1)</param>
        /// <param name="partNName">文件头第N部分名称(默认为null)</param>
        /// <returns>([读取成功], [相关信息], [文件头第N部分], [文件头第N部分总长(4字节+文件头A部分长度)], [文件头前N个部分长度之和])</returns>
        public static (bool Res, string Mes, byte[]? FileHeadPartN, int FileHeadPartN_FullLength, int FileHeadTotalLength_ToPartN) ReadHeadPartN(string filePathExist, int partNIndex)
        {
            bool res = false;
            string mes = string.Empty;
            byte[]? fileHeadPart_N = default;
            int fileHeadLength_PartN = 0;
            int fileHeadTotalLength_ToPartN = 0;

            /*--【就这一段变化相比V4】读取文件头部数量--*/
            var p = FileHeadPartsCount(filePathExist);
            int fileHeadPartsCount = p.FileHeadPartNumber;
            if (p.Res != true)
                return (false, p.MES, default, default, default);
            if (partNIndex > fileHeadPartsCount - 1)
                return (false, $"文件头部索引超范围，本次输入索引值：{partNIndex}，文件头部允许索引最大值：{fileHeadPartsCount - 1}", default, default, default);
            /*--【就这一段变化相比V4】读取文件头部数量END--*/

            int offset = 4;//【就这一句变化相比V4】前四个字节表示文件头部数量，跳过。
            bool readLengthOnly = true;
            for (int i = 0; i <= partNIndex; i++)
            {
                if (i == partNIndex)
                { readLengthOnly = false; }//只在索引partNIndex部分时，才读取内容。前面只读长度。
                else
                { }

                //注意规律：offset(N+1) = offset(N) + Part(N)TotalLength;
                var (Res, Mes, HeadPartN, HeadPartN_FullLength) = ReadHeadPartNCore(filePathExist, offset, i, readLengthOnly);
                if (Res == true)
                { offset += HeadPartN_FullLength; }
                else
                { return (Res, Mes, default, default, default); }

                res = Res;
                mes = Mes;
                fileHeadPart_N = HeadPartN;
                fileHeadLength_PartN = HeadPartN_FullLength;
                fileHeadTotalLength_ToPartN = offset;
            }
            return (res, mes, fileHeadPart_N, fileHeadLength_PartN, fileHeadTotalLength_ToPartN);
        }

        /// <summary>
        /// [通用方法]读取自定义结构文件V5-获取文件头部的数量
        /// </summary>
        /// <param name="filePath">读取文件的路径</param>
        /// <returns>([读取成功], [相关信息], [文件头部数量])</returns>
        public static (bool Res, string MES, int FileHeadPartNumber) FileHeadPartsCount(string filePath)
        {
            FileInfo fileInfo_head = new(filePath);
            if (fileInfo_head.Exists != true)
                return (false, "文件不存在", 0);

            try
            {
                int fileHeadPartsCount;
                using (FileStream fs_head = fileInfo_head.Open(FileMode.Open))
                {
                    fs_head.Seek(0, SeekOrigin.Begin);
                    byte[] fileHeadPartsCount_IntBytes = new byte[4];
                    fs_head.Read(fileHeadPartsCount_IntBytes, 0, fileHeadPartsCount_IntBytes.Length);
                    if (BitConverter.IsLittleEndian)
                    { Array.Reverse(fileHeadPartsCount_IntBytes); }
                    fileHeadPartsCount = BitConverter.ToInt32(fileHeadPartsCount_IntBytes, 0);
                }
                if (fileHeadPartsCount >= 1)
                    return (true, string.Empty, fileHeadPartsCount);
                else
                    return (false, $"文件头部的数量获取为0", 0);
            }
            catch (Exception e)
            {
                return (false, $"文件头部的数量获取失败：{e.Message}", 0);
            }
        }

        /// <summary>
        /// 读取自定义结构文件V5-读取本体（相比于V4，能自动读取文件头部数量，无需手动设定）
        /// </summary>
        /// <param name="filePath">读取文件的路径</param>
        /// <param name="progress">文件本体读取进度</param>
        /// <param name="bufferMultiple">缓存倍率调节(默认值2，即2MB)</param>
        /// <param name="token">中止程序用</param>
        /// <returns>([读取成功], [相关信息], [文件本体])</returns>
        public static (bool Res, string Mes, byte[]? FileBody) ReadBody(string filePath, IProgress<int>? progress, int bufferMultiple, CancellationToken token)
        {
            try
            {
                //1-判断输入路径是否存在
                FileInfo fileInfo_head = new(filePath);
                if (fileInfo_head.Exists != true)
                    return (false, "文件不存在！", default);
                else if (bufferMultiple < 1)
                    return (false, "缓存大小设定错误，推荐值为 2", default);

                /*--【就这一段变化相比V4】读取文件头部数量--*/
                var p = FileHeadPartsCount(filePath);
                int fileHeadPartsCount = p.FileHeadPartNumber;
                if (p.Res != true)
                    return (false, p.MES, default);
                /*--【就这一段变化相比V4】读取文件头部数量END--*/

                //2-读取文件头总长
                var fileHeadPartIndex = fileHeadPartsCount - 1;//【就这一句变化相比V4】不再需要参数输入。
                var (RES, MES, _, _, FileHeadTotalLength) = ReadHeadPartN(filePath, fileHeadPartIndex);
                if (RES != true)
                    return (false, MES, default);

                //3-读取文件本体部分(文件本体可能很大，因此要能显示处理进度和停止)
                byte[] res;
                using (FileStream fs = fileInfo_head.Open(FileMode.Open))
                {
                    using (MemoryStream ms = new())
                    {
                        //3.1-调整流位置
                        fs.Seek(FileHeadTotalLength, SeekOrigin.Begin);

                        var buffer = new byte[1024 * 1024 * bufferMultiple];
                        int readTotalLen = 0;
                        do
                        {
                            int readLen = fs.Read(buffer, 0, buffer.Length);
                            ms.Write(buffer, 0, readLen);
                            readTotalLen += readLen;
                            var per = (long)readTotalLen * 100 / fs.Length;

                            if (fs.Position < fs.Length)
                                progress?.Report(Convert.ToInt32(per));
                            if (token.IsCancellationRequested)
                                return (false, "中止读取文件本体", default);
                        } while (fs.Position < fs.Length);
                        progress?.Report(100);

                        ms.Flush();//释放缓存
                        ms.Seek(0, SeekOrigin.Begin);//再次调整流位置
                        res = ms.ToArray();
                        if (res.Length > 0)
                            return (true, string.Empty, res);
                        else
                            return (false, "文件本体读取为空", default);
                    }
                }
            }
            catch (Exception e)
            {
                return (false, e.Message, default);
            }
        }
        #endregion
    }
}
