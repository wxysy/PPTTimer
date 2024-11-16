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
        //《lock 语句 - 确保对共享资源的独占访问权限。》
        //https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/statements/lock

        /// <summary>
        /// 并行计算异步方法（Task有返回值，List(TR)）
        /// </summary>
        /// <typeparam name="T">待处理集合中元素的类型</typeparam>
        /// <typeparam name="TR">返回值集合中元素的类型</typeparam>
        /// <param name="input">待处理的集合</param>
        /// <param name="func">元素的处理方法（func返回值为Task(TR)，普通方法返回Task.FromResult()，没有返回值用Task.CompletedTask）</param>
        /// <param name="continueAction">每个循环完成后的后续动作</param>
        /// <param name="token">传播有关应取消操作的通知</param>
        /// <param name="progress">进度指示</param>
        /// <param name="maxDegreeOfParallelism">最大并行度</param>
        /// <returns></returns>
        public static async Task<List<TR>> ParallelHandlerAsync<T, TR>(IEnumerable<T> input, Func<T, CancellationToken, Task<TR>> func, Action<TR>? continueAction, CancellationToken token, IProgress<string>? progress = null, int maxDegreeOfParallelism = 0)
        {
            //【并行计算中，务必在并行计算部分用ConcurrentBag替代List，ConcurrentDictionary替代Dictionary。】

            //List --> ConcurrentBag
            ConcurrentBag<T> cbSource = new(input);
            ConcurrentBag<TR> cbResult = [];
            if (cbSource.IsEmpty)
                return [];

            ParallelOptions options = new()
            {
                CancellationToken = token,//同步方法同样可用cts.Cancel();取消
                MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount, //最大并行度设定，默认值为 Environment.ProcessorCount,当前机器的CPU数量。
            };
            progress?.Report($"并行异步方法开始执行(最大并行度：{options.MaxDegreeOfParallelism})......");
            try
            {
                await Parallel.ForEachAsync(cbSource, options, (itemP, tokenP) =>
                {
                    //Task task = new(() =>
                    //{
                    //    try
                    //    {
                    //        var r = func(itemP, tokenP);//耗时操作
                    //        r.Wait(tokenP);
                    //        cbResult.Add(r.Result);
                    //    }
                    //    catch (Exception)
                    //    {
                    //        progress?.Report($"并行异步方法分循环执行中止......");
                    //    }                       
                    //}, tokenP, TaskCreationOptions.LongRunning);
                    //task.Start();
                    //return new ValueTask(task);

                    /****下面的写法由上面的写法改进而来，避免了两次task嵌套****/
                    try
                    {
                        var r = func(itemP, tokenP);//耗时操作
                        r.Wait(tokenP);
                        lock (balanceLock)
                        {
                            //不耗时方法
                            continueAction?.Invoke(r.Result);
                        }

                        cbResult.Add(r.Result);
                        return new ValueTask(r);
                    }
                    catch (Exception)
                    {
                        progress?.Report($"并行异步方法分循环执行中止......");
                        return ValueTask.CompletedTask;
                    }
                });

                progress?.Report("并行异步方法执行完毕，输出结果......");
            }
            catch (TaskCanceledException)
            {
                progress?.Report($"并行异步方法执行中止，已执行循环数量：{cbResult.Count}，输出部分结果......");
            }

            //ConcurrentBag --> List
            List<TR> listRes = [.. cbResult];//cbResult.ToList();
            return listRes;
        }

        /// <summary>
        /// 并行计算异步方法（Task无返回值，仅void）
        /// </summary>
        /// <typeparam name="T">待处理集合中元素的类型</typeparam>
        /// <param name="input">待处理的集合</param>
        /// <param name="func">元素的处理方法（func返回值为Task(TR)，普通方法没有返回值用Task.CompletedTask）</param>
        /// <param name="continueAction">每个循环完成后的后续动作</param>
        /// <param name="token">传播有关应取消操作的通知</param>
        /// <param name="progress">进度指示</param>
        /// <param name="maxDegreeOfParallelism">最大并行度</param>
        /// <returns></returns>
        public static async Task ParallelHandlerAsync<T>(IEnumerable<T> input, Func<T, CancellationToken, Task> func, Action? continueAction, CancellationToken token, IProgress<string>? progress = null, int maxDegreeOfParallelism = 0)
        {
            /****该方法由ParallelHandlerAsync<T, TR>简化而来****/
            //【并行计算中，务必在并行计算部分用ConcurrentBag替代List，ConcurrentDictionary替代Dictionary。】

            //List --> ConcurrentBag
            ConcurrentBag<T> cbSource = new(input);
            ConcurrentBag<Task> cbResult = [];
            if (cbSource.IsEmpty)
                return;

            ParallelOptions options = new()
            {
                CancellationToken = token,//同步方法同样可用cts.Cancel();取消
                MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount, //最大并行度设定，默认值为 Environment.ProcessorCount,当前机器的CPU数量。
            };
            progress?.Report($"并行异步方法开始执行(最大并行度：{options.MaxDegreeOfParallelism})......");
            try
            {
                await Parallel.ForEachAsync(cbSource, options, (itemP, tokenP) =>
                {
                    try
                    {
                        var r = func(itemP, tokenP);//耗时操作
                        r.Wait(tokenP);
                        lock (balanceLock)
                        {
                            continueAction?.Invoke();//不耗时方法
                        }

                        cbResult.Add(r);
                        return new ValueTask(r);
                    }
                    catch (Exception)
                    {
                        progress?.Report($"并行异步方法分循环执行中止......");
                        return ValueTask.CompletedTask;
                    }
                });

                progress?.Report("并行异步方法执行完毕......");
            }
            catch (TaskCanceledException)
            {
                progress?.Report($"并行异步方法执行中止，已执行循环数量：{cbResult.Count}，输出部分结果......");
            }
        }


        /// <summary>
        /// 并行计算同步方法（有返回值，List(TR)）
        /// </summary>
        /// <typeparam name="T">待处理集合中元素的类型</typeparam>
        /// <typeparam name="TR">返回值集合中元素的类型</typeparam>
        /// <param name="input">待处理的集合</param>
        /// <param name="func">元素的处理方法（func返回值为TR）</param>
        /// <param name="loopStatePredicate">同步并行循环取消操作的条件</param>
        /// <param name="progress">进度指示</param>
        /// <param name="maxDegreeOfParallelism">最大并行度</param>
        /// <returns></returns>
        public static List<TR> ParallelHandler<T, TR>(IEnumerable<T> input, Func<T, ParallelLoopState, TR> func, Predicate<T>? loopStatePredicate, IProgress<string>? progress = null, int maxDegreeOfParallelism = 0)
        {
            //【并行计算中，务必在并行计算部分用ConcurrentBag替代List，ConcurrentDictionary替代Dictionary。】

            //List --> ConcurrentBag
            ConcurrentBag<T> cbSource = new(input);
            ConcurrentBag<TR> cbResult = [];
            if (cbSource.IsEmpty)
                return [];

            ParallelOptions options = new()
            {
                //CancellationToken = token,//同步方法同样可用cts.Cancel();取消，但一般没人这么用。
                MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount, //最大并行度设定，默认值为 Environment.ProcessorCount,当前机器的CPU数量。
            };

            progress?.Report($"并行同步方法开始执行(最大并行度：{options.MaxDegreeOfParallelism})......");

            int j = 0;//指示第几个循环
            var loopResult = Parallel.ForEach(cbSource, options, (item, loopState) =>
            {
                int i;
                lock (balanceLock)
                {
                    i = ++j;
                    //为什么要单独放一个变量？因为在代码运行的过程中，由于并行计算，j就已经改变了。
                }

                bool loopStateCondition;
                if (loopStatePredicate == null)
                    loopStateCondition = false;
                else
                    loopStateCondition = loopStatePredicate(item);
                if (loopStateCondition)
                {
                    progress?.Report($"触发中止条件，并行同步方法正在执行的第{i}循环中止......");
                    loopState.Break();
                }

                var r = func(item, loopState);
                cbResult.Add(r);
            });
            if (loopResult.IsCompleted)
                progress?.Report("并行同步方法执行完毕，输出结果......");
            else if (loopResult.LowestBreakIteration.HasValue)
                progress?.Report($"并行同步方法已执行的最低循环数量：{loopResult.LowestBreakIteration}");

            //ConcurrentBag --> List
            List<TR> listRes = [.. cbResult];
            return listRes;
        }

        /// <summary>
        /// 并行计算同步方法（无返回值，仅void）
        /// </summary>
        /// <typeparam name="T">待处理集合中元素的类型</typeparam>
        /// <param name="input">待处理的集合</param>
        /// <param name="action">元素的处理方法（action无返回值）</param>
        /// <param name="loopStatePredicate">同步并行循环取消操作的条件</param>
        /// <param name="progress">进度指示</param>
        /// <param name="maxDegreeOfParallelism">最大并行度</param>
        public static void ParallelHandler<T>(IEnumerable<T> input, Action<T, ParallelLoopState> action, Predicate<T>? loopStatePredicate, IProgress<string>? progress = null, int maxDegreeOfParallelism = 0)
        {
            /****该方法由ParallelHandler<T, TR>简化而来****/
            //【并行计算中，务必在并行计算部分用ConcurrentBag替代List，ConcurrentDictionary替代Dictionary。】

            //List --> ConcurrentBag
            ConcurrentBag<T> cbSource = new(input);
            //ConcurrentBag<TR> cbResult = [];
            if (cbSource.IsEmpty)
                return;

            ParallelOptions options = new()
            {
                //CancellationToken = token,//同步方法同样可用cts.Cancel();取消，但一般没人这么用。
                MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount, //最大并行度设定，默认值为 Environment.ProcessorCount,当前机器的CPU数量。
            };

            progress?.Report($"并行同步方法开始执行(最大并行度：{options.MaxDegreeOfParallelism})......");

            int j = 0;//指示第几个循环
            var loopResult = Parallel.ForEach(cbSource, options, (item, loopState) =>
            {
                int i;
                lock (balanceLock)
                {
                    i = ++j;
                    //为什么要单独放一个变量？因为在代码运行的过程中，由于并行计算，j就已经改变了。
                }

                bool loopStateCondition;
                if (loopStatePredicate == null)
                    loopStateCondition = false;
                else
                    loopStateCondition = loopStatePredicate(item);
                if (loopStateCondition)
                {
                    progress?.Report($"触发中止条件，并行同步方法正在执行的第{i}循环中止......");
                    loopState.Break();
                }

                //var r = func(item, loopState);
                //cbResult.Add(r);
                action(item, loopState);
            });
            if (loopResult.IsCompleted)
                progress?.Report("并行同步方法执行完毕，输出结果......");
            else if (loopResult.LowestBreakIteration.HasValue)
                progress?.Report($"并行同步方法已执行的最低循环数量：{loopResult.LowestBreakIteration}");
        }
    }

}
