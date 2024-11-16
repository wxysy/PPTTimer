using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Infrastructure.Common.IoC
{
    /// <summary>
    /// 自己的.Net通用主机类(Ver 3.0.0)
    /// </summary>
    public class MyAppHostHelper
    {
        public static void AddKeyedServiceDemo(IServiceCollection collection)
        {
            /****添加后台服务****/
            //【后台服务一般适用于耗时长的服务】主机启动后不会等待后台服务运行完毕，后台服务启动后会直接执行其他工作。
            collection.AddHostedService<WorkerDemo>();//添加托管服务【后台服务 - 辅助角色服务的一种】(默认为单一实例服务)
            collection.AddHostedService<Worker2Demo>();//【后台托管服务在通用主机启动后即自动执行】

            /****添加前台服务****/
            //【前台服务依次加载并等待】主机启动之后会等待，只有前一个前台服务加载完毕，才会加载另后一个服务。
            collection.AddKeyedTransient<IDemoTransient>("DT", (sp, skey) => new DemoImplementationTransient());//添加临时服务
            collection.AddKeyedScoped<IDemoScoped>("DSD", (sp, skey) => new DemoImplementationScoped());//添加作用域服务
            collection.AddKeyedSingleton<IDemoSingleton>("DST", (sp, skey) => new DemoImplementationSingleton());//添加单一实例服务

            //只推荐3种添加服务的形式，因为这3种形式，才可以自动对象释放。
            //《.NET 依赖项注入》- 服务注册方法
            //https://learn.microsoft.com/zh-cn/dotnet/core/extensions/dependency-injection
            //collection.AddSingleton<IDemoSingleton>(sp => new DemoImplementationSingleton());
            //collection.AddSingleton<IDemoSingleton, DemoImplementationSingleton>();
            //collection.AddSingleton<DemoImplementationSingleton>();
            //collection.AddHostedService<WorkerDemo>();//将后台服务添加为托管服务【辅助角色服务的一种】(默认为单一实例服务)

            //添加服务：serviceCollection.AddSingleton<TService>
            //删除服务：serviceCollection.RemoveAll<TService>
            //serviceCollection.Remove 是删除服务实现，不好用。
        }

        public static void ApplyKeyedServiceDemo(IServiceProvider? serviceProvider, IProgress<string>? progress)
        {
            /* 几个重点：
             * 1、GetService 和 GetRequiredService 区别
             * 服务能找到时都一样。服务找不到时，GetService --> null，GetRequiredService --> 抛出异常。
             * 2、GetService<T>方法中T到底是服务(TService)还是服务实现(TImplementation)？
             * 只能是服务(TService)，不能是服务实现(TImplementation)，否则找不到。
             * 3、GetService<T>方法中T为服务(TService)时，返回的是服务(TService)还是服务实现(TImplementation)？
             * 在运行时打断点，查看编辑器信息。
             * 返回为服务实现(TImplementation)实例。[只不过自动隐式引用转换为服务(TService)实例罢了]
             */
            progress?.Report("1.1-【后台服务一般适用于耗时长的服务】主机启动后不会等待后台服务运行完毕，后台服务启动后会直接执行其他工作。");
            progress?.Report("1.2-【前台服务依次加载并等待】主机启动之后会等待，只有前一个前台服务加载完毕，才会加载另后一个服务。");

            //排除空类型
            if (serviceProvider == null)
            { return; }
            else
            { }

            //启动Transient服务（第1次）
            var serviceT1 = serviceProvider.GetRequiredKeyedService<IDemoTransient>("DT");
            var mesT1 = $"2.1-第1次启动Transient服务，ID：{serviceT1.ID}";
            progress?.Report(mesT1);

            //启动Transient服务（第2次）
            var serviceT2 = serviceProvider.GetRequiredKeyedService<IDemoTransient>("DT");
            var mesT2 = $"2.2-第2次启动Transient服务，ID：{serviceT2.ID}";
            progress?.Report(mesT2);


            //启动AddScope服务（第1次、第2次）（当然其他类型也能这么用）
            using (IServiceScope serviceScope = serviceProvider.CreateScope())//设定服务作用域
            {
                IServiceProvider provider = serviceScope.ServiceProvider;
                var serviceA1 = provider.GetRequiredKeyedService<IDemoScoped>("DSD");
                var mesA1 = $"3.1.1-(同一作用域内)第1次启动AddScope服务，ID：{serviceA1.ID}";
                progress?.Report(mesA1);

                var serviceA2 = provider.GetRequiredKeyedService<IDemoScoped>("DSD");
                var mesA2 = $"3.1.2-(同一作用域内)第2次启动AddScope服务，ID：{serviceA2.ID}";
                progress?.Report(mesA2);
            }

            //启动AddScope服务（第3次）（当然其他类型也能这么用）
            using (IServiceScope serviceScope = serviceProvider.CreateScope())//设定服务作用域
            {
                IServiceProvider provider = serviceScope.ServiceProvider;

                var serviceA3 = provider.GetRequiredKeyedService<IDemoScoped>("DSD");
                var mesA3 = $"3.2-(不同作用域内)第3次启动AddScope服务，ID：{serviceA3.ID}";
                progress?.Report(mesA3);
            }

            //启动Singleton服务（第1次）
            var serviceS1 = serviceProvider.GetRequiredKeyedService<IDemoSingleton>("DST");
            var mesS1 = $"4.1-第1次启动Singleton服务，ID：{serviceS1.ID}";
            progress?.Report(mesS1);

            //启动Singleton服务（第2次）
            var serviceS2 = serviceProvider.GetRequiredKeyedService<IDemoSingleton>("DST");
            var mesS2 = $"4.2-第2次启动Singleton服务，ID：{serviceS2.ID}";
            progress?.Report(mesS2);

            //后台托管服务会自动执行，而且默认为默认为单一实例服务（与容器实例同样生命周期）。
            //后台托管服务会最后启动。先Worker2Demo，后WorkerDemo。
            //而且目前只能通过IHostedService获取到最后一个注册的Worker2Demo，GetRequiredService<WorkerDemo>();不行。
            //强行使用GetRequiredService<Worker>();会导致程序关闭。
            //【后台托管服务在通用主机启动后即自动执行，尽量不要手动操作他】
            //取消后台操作属于Warning级别日志记录。
            progress?.Report("【后台托管服务在通用主机启动后即自动执行】准备手动执行托管服务IHostedService实例【尽量不要手动操作他】");
            var serviceB3 = serviceProvider.GetRequiredService<IHostedService>();
            CancellationTokenSource cts = new();
            serviceB3.StartAsync(cts.Token);
            serviceB3.StopAsync(cts.Token);
        }
    }

    public interface IDemoTransient
    {
        string ID { get; }
        string MesCallBack(string message);
    }

    public interface IDemoScoped
    {
        string ID { get; }
        string MesCallBack(string message);
    }

    public interface IDemoSingleton
    {
        string ID { get; }
        string MesCallBack(string message);
    }

    public class DemoImplementationTransient : IDemoTransient
    {
        public string ID { get; }

        public DemoImplementationTransient()
        {
            ID = Guid.NewGuid().ToString();
        }

        public string MesCallBack(string message)
        {
            return $"{nameof(DemoImplementationTransient)}.MesCallBack()返回信息：{message}";
        }
    }

    public class DemoImplementationScoped : IDemoScoped
    {
        public string ID { get; }

        public DemoImplementationScoped()
        {
            ID = Guid.NewGuid().ToString();
        }

        public string MesCallBack(string message)
        {
            return $"{nameof(DemoImplementationScoped)}.MesCallBack()返回信息：{message}";
        }
    }

    public class DemoImplementationSingleton : IDemoSingleton
    {
        public string ID { get; }

        public DemoImplementationSingleton()
        {
            ID = Guid.NewGuid().ToString();
        }

        public string MesCallBack(string message)
        {
            return $"{nameof(DemoImplementationSingleton)}.MesCallBack()返回信息：{message}";
        }
    }

    public sealed class WorkerDemo : BackgroundService//后台服务：引用 BackgroundService 类型
    {
        private readonly IDemoTransient _messageWriter;

        public WorkerDemo([FromKeyedServices("DT")] IDemoTransient messageWriter) =>
            _messageWriter = messageWriter;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var mes = _messageWriter.MesCallBack($"后台服务WorkerDemo，调用了IDemoTransient的KeyedService。");
                MessageBox.Show(mes);
                await Task.Delay(1000, stoppingToken);
                break;
            }
        }
    }

    public sealed class Worker2Demo : BackgroundService//后台服务：引用 BackgroundService 类型
    {
        //必须重写该方法
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                MessageBox.Show($"后台服务Worker2Demo类启动时间:  {DateTimeOffset.Now}");
                await Task.Delay(1000, stoppingToken);
                break;
            }
        }
    }
}
