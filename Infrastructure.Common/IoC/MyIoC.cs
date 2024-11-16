using Microsoft.Extensions.DependencyInjection;
using System;

namespace Infrastructure.Common.IoC
{
    [Obsolete($"此类已被弃用，请使用{nameof(MyAppHost)}类替代", true)]
    public class MyIoC
    {
        private IServiceCollection serviceCollection;

        private Lazy<IServiceProvider> serviceProviderLazy;

        public IServiceProvider DIServiceProvider => serviceProviderLazy.Value;

        public MyIoC(IServiceCollection? services = null)
        {
            serviceCollection = services ?? CreatExampleServices();
            var serviceProvider = ConfigureServicesProvider(serviceCollection);
            serviceProviderLazy = new Lazy<IServiceProvider>(serviceProvider);
        }

        public static IServiceCollection CreatExampleServices()
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton<IServiceExample1, ImplementationExample1>()
                .AddSingleton<IServiceExample2, ImplementationExample2>(p =>
                { return new ImplementationExample2("通过依赖注入无法提供的参数值"); })
                .AddSingleton<IServiceExample3>(new ImplementationExample3())
                .AddSingleton<IServiceExample4>(p => new ImplementationExample4())
                .AddSingleton<ImplementationExample5>()
                .AddSingleton(new ImplementationExample6())
                .AddSingleton(p => new ImplementationExample7())
                .AddSingleton(typeof(IServiceExample10), p => new ImplementationExample10())
                .AddSingleton(typeof(ImplementationExample11), p => new ImplementationExample11());
            return serviceCollection;
        }

        public void ReloadServices(Action<IServiceCollection> action)
        {
            action(serviceCollection);
            var serviceProvider = ConfigureServicesProvider(serviceCollection);
            serviceProviderLazy = new Lazy<IServiceProvider>(serviceProvider);

        }

        private static IServiceProvider ConfigureServicesProvider(IServiceCollection services)
        {
            var res = services.BuildServiceProvider();
            return res;
        }

        public T? GetServiceImplementationInstance<T>()
            where T : class
        {
            var service = DIServiceProvider.GetService<T>();
            return service;

        }
    }

    #region 服务与实现11种示例
    interface IServiceExample1
    { void TestMethod(IProgress<string>? progress); }
    class ImplementationExample1 : IServiceExample1
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"方式1-基本型：AddSingleton<[接口类]，[实现类]>()。";
            progress?.Report(s);
        }
    }

    interface IServiceExample2
    { void TestMethod(IProgress<string>? progress); }
    class ImplementationExample2 : IServiceExample2
    {
        string input;
        public ImplementationExample2(string para)
        {
            input = para;
        }
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"方式2-AddSingleton<[接口类]，[实现类]>(ServiceProvider委托 => 实现类实例)：当实现类的构造函数有通过依赖注入无法提供的参数时使用。输入的参数值：{input}。";
            progress?.Report(s);
        }
    }

    interface IServiceExample3
    { void TestMethod(IProgress<string>? progress); }
    class ImplementationExample3 : IServiceExample3
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"方式3-AddSingleton<接口类>(实现类实例)。";
            progress?.Report(s);
        }
    }

    interface IServiceExample4
    { void TestMethod(IProgress<string>? progress); }
    class ImplementationExample4 : IServiceExample4
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"方式4-AddSingleton<接口类>(实现类委托)。";
            progress?.Report(s);
        }
    }

    class ImplementationExample5
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"方式5-AddSingleton<实现类>()。";
            progress?.Report(s);
        }
    }

    class ImplementationExample6
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"方式6-AddSingleton(实现类实例)。";
            progress?.Report(s);
        }
    }

    class ImplementationExample7
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"方式7-AddSingleton(ServiceProvider委托 => 实现类实例)。";
            progress?.Report(s);
        }
    }

    interface IServiceExample8
    { void TestMethod(IProgress<string>? progress); }
    class ImplementationExample8 : IServiceExample8
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"服务8：后添加，将被删除的服务（定义了服务与实现）。";
            progress?.Report(s);
        }
    }

    class ImplementationExample9
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"服务9：后添加，将被删除的服务（仅定义了实现）。";
            progress?.Report(s);
        }
    }

    interface IServiceExample10
    { void TestMethod(IProgress<string>? progress); }
    class ImplementationExample10 : IServiceExample10
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"方式10-AddSingleton(服务类的类型实例, ServiceProvider委托 => 实现类实例)。";
            progress?.Report(s);
        }
    }

    class ImplementationExample11
    {
        public void TestMethod(IProgress<string>? progress)
        {
            string s = $"方式11-AddSingleton(实现类的类型实例, ServiceProvider委托 => 实现类实例)。";
            progress?.Report(s);
        }
    }
    #endregion
}
