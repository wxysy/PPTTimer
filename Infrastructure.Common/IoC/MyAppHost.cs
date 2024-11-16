using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Infrastructure.Common.IoC
{
    /// <summary>
    /// 自己的.Net通用主机类(Ver 3.0.0)
    /// </summary>
    public class MyAppHost : IDisposable
    {
        /* 引用NuGet包：Microsoft.Extensions.Hosting
         * 一、参考文献：
         * 《.NET 依赖项注入》
         * https://learn.microsoft.com/zh-cn/dotnet/core/extensions/dependency-injection
         * 《教程：在 .NET 中使用依赖注入》
         * https://learn.microsoft.com/zh-cn/dotnet/core/extensions/dependency-injection-usage
         * 《.NET 中的辅助角色服务》
         * https://learn.microsoft.com/zh-cn/dotnet/core/extensions/workers?pivots=dotnet-7-0
         * 《.NET 通用主机》
         * https://learn.microsoft.com/zh-cn/dotnet/core/extensions/generic-host
         * 《在 BackgroundService 内使用作用域服务》
         * https://learn.microsoft.com/zh-cn/dotnet/core/extensions/scoped-service?source=recommendations&pivots=dotnet-7-0
         * 二、几个专业术语：
         * 后台服务：引用 BackgroundService 类型。
         * 托管服务：实现 IHostedService 或引用 IHostedService 本身。
         * 长时间运行的服务：持续运行的任何服务。
         * Windows 服务：Windows 服务基础结构，最初以 .NET Framework 为中心，但现在可通过 .NET 访问。
         * 辅助角色服务：引用辅助角色服务模板。*/

        #region 属性和字段
        private readonly IProgress<string>? progress;
        /// <summary>
        /// 该属性为.Net通用主机的实例（可自用，也可供外部调用）
        /// </summary>
        public IHost HostInstance { get; }

        /// <summary>
        /// 该属性为.Net通用主机的服务集合，使用GetService()或者GetRequiredService()获取服务。
        /// </summary>
        public IServiceProvider ServiceProvider { get; }
        #endregion

        /// <summary>
        /// 自己的.Net通用主机构造函数(Ver 3.0.0)
        /// </summary>
        /// <param name="hostArgs">主机启动参数(别乱设，一般都为null)</param>
        /// <param name="serviceCollectionAction">服务集合的增删(如果设置为null则启动演示Demo操作)</param>
        /// <param name="loggerGeneratorAction">主机日志部分生成器</param>
        /// <param name="progress">进程状态反馈</param>
        public MyAppHost(string[]? hostArgs, Action<IServiceCollection>? serviceCollectionAction, Func<HostApplicationBuilder, bool>? loggerGeneratorAction = null, IProgress<string>? progress = null)
        {
            this.progress = progress;
            //1、2、3、4-获取IHost主机
            HostInstance = AppHostGenerator(hostArgs, serviceCollectionAction, loggerGeneratorAction);
            //5-获取服务
            ServiceProvider = HostInstance.Services;
            //6-运行主机
            HostInstance.RunAsync();//host.RunAsync()普通方法不卡死。依据实际情况也可以用 host.Run(); 或者 await host.RunAsync(); 
        }

        /// <summary>
        /// 创建.Net通用主机实例
        /// (MSDN说【不能使用静态方法】)
        /// 【产生的实例需要手动调用myHost.Dispose();进行释放】
        /// </summary>
        /// <param name="hostArgs">主机启动参数(别乱设，一般都为null)</param>
        /// <param name="serviceCollectionAction">服务集合的增删(如果设置为null则启动演示Demo操作)</param>
        /// <returns>.Net通用主机实例</returns>
        private IHost AppHostGenerator(string[]? hostArgs, Action<IServiceCollection>? serviceCollectionAction, Func<HostApplicationBuilder, bool>? loggerGeneratorAction)
        {
            //1-创建主机应用生成器实例（using Microsoft.Extensions.Hosting;）
            string[]? args = hostArgs;
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            //2-注册以下内容来配置生成器服务            
            var serviceCollection = builder.Services;
            serviceCollectionAction?.Invoke(serviceCollection);
            /* 1、【服务种类】
             * Transient 服务总是不同的，每次检索服务时，都会创建一个新实例。
             * Scoped 服务只会随着新范围而改变，但在一个范围中是相同的实例。
             * Singleton 服务总是相同的，新实例仅被创建一次。
             * 2、【AddSingleton和TryAddSingleton的区别】
             * TryAddSingleton<T>仅会在没有同一类型实现的情况下才注册该服务，避免在容器中注册一个实现的多个副本。
             * 3、【添加服务】
             * 只推荐3种添加服务的形式(这3种形式才可以自动对象释放)
             * 《.NET 依赖项注入》- 服务注册方法
             * https://learn.microsoft.com/zh-cn/dotnet/core/extensions/dependency-injection
             * serviceCollection.AddSingleton<IDemoSingleton>(sp => new DemoImplementationSingleton());
             * serviceCollection.AddSingleton<IDemoSingleton, DemoImplementationSingleton>();
             * serviceCollection.AddSingleton<DemoImplementationSingleton>();
             * serviceCollection.AddHostedService<WorkerDemo>();//将后台服务添加为托管服务【辅助角色服务的一种】(默认为单一实例服务)
             */

            //3-配置通用主机日志
            loggerGeneratorAction?.Invoke(builder);

            //4-从生成器生成IHost主机（产生的实例需要手动调用host.Dispose();进行释放）
            IHost host = builder.Build();

            return host;
        }


        #region IDisposable接口实现
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~MyAppHosterClean()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
