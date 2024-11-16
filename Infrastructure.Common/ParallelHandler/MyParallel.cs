using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Common.ParallelHandler
{
    public class MyParallel
    {
        private static readonly object balanceLock = new();
        //��lock ��� - ȷ���Թ�����Դ�Ķ�ռ����Ȩ�ޡ���
        //https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/statements/lock

        /// <summary>
        /// ���м����첽������Task�з���ֵ��List(TR)��
        /// </summary>
        /// <typeparam name="T">����������Ԫ�ص�����</typeparam>
        /// <typeparam name="TR">����ֵ������Ԫ�ص�����</typeparam>
        /// <param name="input">������ļ���</param>
        /// <param name="func">Ԫ�صĴ�������func����ֵΪTask(TR)����ͨ��������Task.FromResult()��û�з���ֵ��Task.CompletedTask��</param>
        /// <param name="continueAction">ÿ��ѭ����ɺ�ĺ�������</param>
        /// <param name="token">�����й�Ӧȡ��������֪ͨ</param>
        /// <param name="progress">����ָʾ</param>
        /// <param name="maxDegreeOfParallelism">����ж�</param>
        /// <returns></returns>
        public static async Task<List<TR>> ParallelHandlerAsync<T, TR>(IEnumerable<T> input, Func<T, CancellationToken, Task<TR>> func, Action<TR>? continueAction, CancellationToken token, IProgress<string>? progress = null, int maxDegreeOfParallelism = 0)
        {
            //�����м����У�����ڲ��м��㲿����ConcurrentBag���List��ConcurrentDictionary���Dictionary����

            //List --> ConcurrentBag
            ConcurrentBag<T> cbSource = new(input);
            ConcurrentBag<TR> cbResult = [];
            if (cbSource.IsEmpty)
                return [];

            ParallelOptions options = new()
            {
                CancellationToken = token,//ͬ������ͬ������cts.Cancel();ȡ��
                MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount, //����ж��趨��Ĭ��ֵΪ Environment.ProcessorCount,��ǰ������CPU������
            };
            progress?.Report($"�����첽������ʼִ��(����жȣ�{options.MaxDegreeOfParallelism})......");
            try
            {
                await Parallel.ForEachAsync(cbSource, options, (itemP, tokenP) =>
                {
                    //Task task = new(() =>
                    //{
                    //    try
                    //    {
                    //        var r = func(itemP, tokenP);//��ʱ����
                    //        r.Wait(tokenP);
                    //        cbResult.Add(r.Result);
                    //    }
                    //    catch (Exception)
                    //    {
                    //        progress?.Report($"�����첽������ѭ��ִ����ֹ......");
                    //    }                       
                    //}, tokenP, TaskCreationOptions.LongRunning);
                    //task.Start();
                    //return new ValueTask(task);

                    /****�����д���������д���Ľ�����������������taskǶ��****/
                    try
                    {
                        var r = func(itemP, tokenP);//��ʱ����
                        r.Wait(tokenP);
                        lock (balanceLock)
                        {
                            //����ʱ����
                            continueAction?.Invoke(r.Result);
                        }

                        cbResult.Add(r.Result);
                        return new ValueTask(r);
                    }
                    catch (Exception)
                    {
                        progress?.Report($"�����첽������ѭ��ִ����ֹ......");
                        return ValueTask.CompletedTask;
                    }
                });

                progress?.Report("�����첽����ִ����ϣ�������......");
            }
            catch (TaskCanceledException)
            {
                progress?.Report($"�����첽����ִ����ֹ����ִ��ѭ��������{cbResult.Count}��������ֽ��......");
            }

            //ConcurrentBag --> List
            List<TR> listRes = [.. cbResult];//cbResult.ToList();
            return listRes;
        }

        /// <summary>
        /// ���м����첽������Task�޷���ֵ����void��
        /// </summary>
        /// <typeparam name="T">����������Ԫ�ص�����</typeparam>
        /// <param name="input">������ļ���</param>
        /// <param name="func">Ԫ�صĴ�������func����ֵΪTask(TR)����ͨ����û�з���ֵ��Task.CompletedTask��</param>
        /// <param name="continueAction">ÿ��ѭ����ɺ�ĺ�������</param>
        /// <param name="token">�����й�Ӧȡ��������֪ͨ</param>
        /// <param name="progress">����ָʾ</param>
        /// <param name="maxDegreeOfParallelism">����ж�</param>
        /// <returns></returns>
        public static async Task ParallelHandlerAsync<T>(IEnumerable<T> input, Func<T, CancellationToken, Task> func, Action? continueAction, CancellationToken token, IProgress<string>? progress = null, int maxDegreeOfParallelism = 0)
        {
            /****�÷�����ParallelHandlerAsync<T, TR>�򻯶���****/
            //�����м����У�����ڲ��м��㲿����ConcurrentBag���List��ConcurrentDictionary���Dictionary����

            //List --> ConcurrentBag
            ConcurrentBag<T> cbSource = new(input);
            ConcurrentBag<Task> cbResult = [];
            if (cbSource.IsEmpty)
                return;

            ParallelOptions options = new()
            {
                CancellationToken = token,//ͬ������ͬ������cts.Cancel();ȡ��
                MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount, //����ж��趨��Ĭ��ֵΪ Environment.ProcessorCount,��ǰ������CPU������
            };
            progress?.Report($"�����첽������ʼִ��(����жȣ�{options.MaxDegreeOfParallelism})......");
            try
            {
                await Parallel.ForEachAsync(cbSource, options, (itemP, tokenP) =>
                {
                    try
                    {
                        var r = func(itemP, tokenP);//��ʱ����
                        r.Wait(tokenP);
                        lock (balanceLock)
                        {
                            continueAction?.Invoke();//����ʱ����
                        }

                        cbResult.Add(r);
                        return new ValueTask(r);
                    }
                    catch (Exception)
                    {
                        progress?.Report($"�����첽������ѭ��ִ����ֹ......");
                        return ValueTask.CompletedTask;
                    }
                });

                progress?.Report("�����첽����ִ�����......");
            }
            catch (TaskCanceledException)
            {
                progress?.Report($"�����첽����ִ����ֹ����ִ��ѭ��������{cbResult.Count}��������ֽ��......");
            }
        }


        /// <summary>
        /// ���м���ͬ���������з���ֵ��List(TR)��
        /// </summary>
        /// <typeparam name="T">����������Ԫ�ص�����</typeparam>
        /// <typeparam name="TR">����ֵ������Ԫ�ص�����</typeparam>
        /// <param name="input">������ļ���</param>
        /// <param name="func">Ԫ�صĴ�������func����ֵΪTR��</param>
        /// <param name="loopStatePredicate">ͬ������ѭ��ȡ������������</param>
        /// <param name="progress">����ָʾ</param>
        /// <param name="maxDegreeOfParallelism">����ж�</param>
        /// <returns></returns>
        public static List<TR> ParallelHandler<T, TR>(IEnumerable<T> input, Func<T, ParallelLoopState, TR> func, Predicate<T>? loopStatePredicate, IProgress<string>? progress = null, int maxDegreeOfParallelism = 0)
        {
            //�����м����У�����ڲ��м��㲿����ConcurrentBag���List��ConcurrentDictionary���Dictionary����

            //List --> ConcurrentBag
            ConcurrentBag<T> cbSource = new(input);
            ConcurrentBag<TR> cbResult = [];
            if (cbSource.IsEmpty)
                return [];

            ParallelOptions options = new()
            {
                //CancellationToken = token,//ͬ������ͬ������cts.Cancel();ȡ������һ��û����ô�á�
                MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount, //����ж��趨��Ĭ��ֵΪ Environment.ProcessorCount,��ǰ������CPU������
            };

            progress?.Report($"����ͬ��������ʼִ��(����жȣ�{options.MaxDegreeOfParallelism})......");

            int j = 0;//ָʾ�ڼ���ѭ��
            var loopResult = Parallel.ForEach(cbSource, options, (item, loopState) =>
            {
                int i;
                lock (balanceLock)
                {
                    i = ++j;
                    //ΪʲôҪ������һ����������Ϊ�ڴ������еĹ����У����ڲ��м��㣬j���Ѿ��ı��ˡ�
                }

                bool loopStateCondition;
                if (loopStatePredicate == null)
                    loopStateCondition = false;
                else
                    loopStateCondition = loopStatePredicate(item);
                if (loopStateCondition)
                {
                    progress?.Report($"������ֹ����������ͬ����������ִ�еĵ�{i}ѭ����ֹ......");
                    loopState.Break();
                }

                var r = func(item, loopState);
                cbResult.Add(r);
            });
            if (loopResult.IsCompleted)
                progress?.Report("����ͬ������ִ����ϣ�������......");
            else if (loopResult.LowestBreakIteration.HasValue)
                progress?.Report($"����ͬ��������ִ�е����ѭ��������{loopResult.LowestBreakIteration}");

            //ConcurrentBag --> List
            List<TR> listRes = [.. cbResult];
            return listRes;
        }

        /// <summary>
        /// ���м���ͬ���������޷���ֵ����void��
        /// </summary>
        /// <typeparam name="T">����������Ԫ�ص�����</typeparam>
        /// <param name="input">������ļ���</param>
        /// <param name="action">Ԫ�صĴ�������action�޷���ֵ��</param>
        /// <param name="loopStatePredicate">ͬ������ѭ��ȡ������������</param>
        /// <param name="progress">����ָʾ</param>
        /// <param name="maxDegreeOfParallelism">����ж�</param>
        public static void ParallelHandler<T>(IEnumerable<T> input, Action<T, ParallelLoopState> action, Predicate<T>? loopStatePredicate, IProgress<string>? progress = null, int maxDegreeOfParallelism = 0)
        {
            /****�÷�����ParallelHandler<T, TR>�򻯶���****/
            //�����м����У�����ڲ��м��㲿����ConcurrentBag���List��ConcurrentDictionary���Dictionary����

            //List --> ConcurrentBag
            ConcurrentBag<T> cbSource = new(input);
            //ConcurrentBag<TR> cbResult = [];
            if (cbSource.IsEmpty)
                return;

            ParallelOptions options = new()
            {
                //CancellationToken = token,//ͬ������ͬ������cts.Cancel();ȡ������һ��û����ô�á�
                MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount, //����ж��趨��Ĭ��ֵΪ Environment.ProcessorCount,��ǰ������CPU������
            };

            progress?.Report($"����ͬ��������ʼִ��(����жȣ�{options.MaxDegreeOfParallelism})......");

            int j = 0;//ָʾ�ڼ���ѭ��
            var loopResult = Parallel.ForEach(cbSource, options, (item, loopState) =>
            {
                int i;
                lock (balanceLock)
                {
                    i = ++j;
                    //ΪʲôҪ������һ����������Ϊ�ڴ������еĹ����У����ڲ��м��㣬j���Ѿ��ı��ˡ�
                }

                bool loopStateCondition;
                if (loopStatePredicate == null)
                    loopStateCondition = false;
                else
                    loopStateCondition = loopStatePredicate(item);
                if (loopStateCondition)
                {
                    progress?.Report($"������ֹ����������ͬ����������ִ�еĵ�{i}ѭ����ֹ......");
                    loopState.Break();
                }

                //var r = func(item, loopState);
                //cbResult.Add(r);
                action(item, loopState);
            });
            if (loopResult.IsCompleted)
                progress?.Report("����ͬ������ִ����ϣ�������......");
            else if (loopResult.LowestBreakIteration.HasValue)
                progress?.Report($"����ͬ��������ִ�е����ѭ��������{loopResult.LowestBreakIteration}");
        }
    }

}
