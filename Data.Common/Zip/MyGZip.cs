using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Data.Common.Zip
{
    public class MyGZip
    {
        #region 【核心基础】GZipStream针对流的压缩与解压缩(自己的改进方法)
        /// <summary>
        /// 使用GZipStream对流进行压缩或解压操作(ReadWriteBytes方式)。
        /// 该方式能够取消操作和指示处理进度。
        /// 该方法是模仿的【核心基础】流加密解密与取消EnOrDecryptStreamCore方法。
        /// </summary>
        /// <param name="inStream">输入流</param>
        /// <param name="inStream_Seek">调整输入流的起始读取位置</param>
        /// <param name="outStream">输出流</param>
        /// <param name="outStream_Seek">调整输出流的起始读取位置</param>
        /// <param name="compressDirection">压缩还是解压缩</param>
        /// <param name="token">用于异步方法的取消操作</param>
        /// <param name="progress">用于异步方法的报告处理进度</param>
        /// <param name="bufferSizeMultiple">缓冲区大小倍数调节(为了和加密类统一，以1Mb为基本单元，该参数很影响运算速度。)，默认值为2(即2 * 1Mb = 2Mb)。</param>
        /// <returns>对两个流的操作是否成功</returns>
        public static (bool GZipRes, string GZipMes) GZipCompressStreamCore(Stream inStream, long inStream_Seek, Stream outStream, long outStream_Seek, CompressionMode compressDirection, IProgress<int>? progress, int bufferSizeMultiple, CancellationToken token)
        {
            try
            {
                if (inStream == null || outStream == null)
                    throw new ArgumentException("输入流与输出流是必须的");
                //--调整流的位置(通常是为了避开文件头部分)
                inStream.Seek(inStream_Seek, SeekOrigin.Begin);
                outStream.Seek(outStream_Seek, SeekOrigin.Begin);

                long total_inStreamReadLength = inStream.Length - inStream_Seek;

                int blockSizeBytes = bufferSizeMultiple * 8 * 1024 * 128;//GZipStream缓冲区默认就是81920，8*1024*10=80K。但是为了和加密类统一，就扩大到1024Kb为基础了。
                byte[] data = new byte[blockSizeBytes];//每次读取的数据量80Kb

                if (compressDirection == CompressionMode.Compress)
                {
                    using (GZipStream gZipStream = new(outStream, compressDirection))
                    {
                        int readLen = 0;
                        long total_readLen = 0;
                        do
                        {
                            // 从输入流读取数据
                            readLen = inStream.Read(data, 0, data.Length);
                            if (readLen == 0)
                                break;

                            // 向输出流写入数据
                            gZipStream.Write(data, 0, readLen);
                            total_readLen += readLen;
                            // 汇报读取情况
                            long per = 100 * total_readLen / total_inStreamReadLength;
                            progress?.Report(Convert.ToInt32(per));
                            //停止异步操作（**慎用！**）
                            if (token.IsCancellationRequested == true)
                            {
                                //一旦停止，写入的流就彻底破坏了。
                                gZipStream.Dispose();
                                return (false, "流操作被中止。");//return等效于break                                                         
                            }
                        } while (readLen > 0);
                        gZipStream.Flush();
                    }
                }
                else//解压
                {
                    using (GZipStream gZipStream = new(inStream, compressDirection))
                    {
                        int readLen = 0;
                        long total_readLen = 0;
                        do
                        {
                            // 从输入流读取数据
                            long inStreamPosition_Start = inStream.Position;
                            readLen = gZipStream.Read(data, 0, data.Length);//readLen是解压后的数据长度，不是读取长度。
                            long inStreamPosition_End = inStream.Position;
                            long inStreamReadLen = inStreamPosition_End - inStreamPosition_Start;
                            // 向输出流写入数据
                            outStream.Write(data, 0, readLen);
                            total_readLen += inStreamReadLen;
                            // 汇报读取情况
                            long per = 100 * total_readLen / total_inStreamReadLength;
                            if (readLen > 0)//防止readLen=0的时候还报告，那么就会报告两次。
                            {
                                progress?.Report(Convert.ToInt32(per));
                            }
                            else
                            { }
                            //停止异步操作（**慎用！**）
                            if (token.IsCancellationRequested == true)
                            {
                                //一旦停止，写入的流就彻底破坏了。
                                gZipStream.Dispose();
                                return (false, "流操作被中止。");//return等效于break
                            }
                        } while (readLen > 0);
                        gZipStream.Flush();
                    }
                }
                return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }


        /// <summary>
        /// 使用GZipStream对流进行压缩或解压操作(CopyTo方式)。
        /// 该方式无法取消操作和指示处理进度。
        /// MSDN上GZipStream类的示例方法。
        /// </summary>
        /// <param name="inStream">输入流</param>
        /// <param name="inStream_Seek">调整输入流的起始读取位置</param>
        /// <param name="outStream">输出流</param>
        /// <param name="outStream_Seek">调整输出流的起始读取位置</param>
        /// <param name="compressDirection">压缩还是解压缩</param>
        /// <param name="bufferSizeMultiple">缓冲区大小倍数调节(为了和加密类统一，以1Mb为基本单元，该参数很影响运算速度。)，默认值为2(即2 * 1Mb = 2Mb)。</param>
        /// <returns>对两个流的操作是否成功</returns>
        private static bool GZipCompressStreamCore(Stream inStream, long inStream_Seek, Stream outStream, long outStream_Seek, CompressionMode compressDirection, int bufferSizeMultiple)
        {
            try
            {
                if (inStream == null || outStream == null)
                    throw new ArgumentException("输入流与输出流是必须的");
                //--调整流的位置(通常是为了避开文件头部分)
                //CopyTo方法：从当前流中的当前位置开始复制，在复制操作完成后，不会重置目标流的位置。
                inStream.Seek(inStream_Seek, SeekOrigin.Begin);
                outStream.Seek(outStream_Seek, SeekOrigin.Begin);

                int blockSizeBytes = bufferSizeMultiple * 8 * 1024 * 128; ;//GZipStream缓冲区默认就是81920，8*1024*10=80K。但是为了和加密类统一，就扩大到1024Kb为基础了。
                if (compressDirection == CompressionMode.Compress)
                {
                    using GZipStream gZipStream = new GZipStream(outStream, compressDirection);
                    inStream.CopyTo(gZipStream, blockSizeBytes);
                    gZipStream.Flush();
                }
                else
                {
                    using GZipStream gZipStream = new GZipStream(inStream, compressDirection);
                    gZipStream.CopyTo(outStream, blockSizeBytes);
                    gZipStream.Flush();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 【核心】数据与字节数组的转换
        public static byte[] GZipCompress(byte[] inputData, IProgress<int>? progress, int bufferMultiple, CancellationToken token)
        {
            using MemoryStream inStream_Compress = new(inputData);
            using MemoryStream outStream_Compress = new();
            GZipCompressStreamCore(inStream_Compress, 0, outStream_Compress, 0, CompressionMode.Compress, progress, bufferMultiple, token);
            byte[] buffer_out = outStream_Compress.ToArray();
            return buffer_out;
        }
        public static byte[] GZipDecompress(byte[] inputBytes, IProgress<int>? progress, int bufferMultiple, CancellationToken token)
        {
            using MemoryStream inStream_Compress = new(inputBytes);
            using MemoryStream outStream_Compress = new();
            GZipCompressStreamCore(inStream_Compress, 0, outStream_Compress, 0, CompressionMode.Decompress, progress, bufferMultiple, token);
            byte[] buffer_out = outStream_Compress.ToArray();
            return buffer_out;
        }
        #endregion
    }
}
