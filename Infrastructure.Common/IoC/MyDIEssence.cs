using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Infrastructure.Common.IoC
{
    public class MyDIEssence
    {
        public MyDIEssence()
        {
            var serviceCol = new ServiceCollection();
            serviceCol.AddLogging(builder => builder.AddDebug());
            serviceCol.AddSingleton<ExampleService>();

            IServiceProvider serviceProvider = serviceCol.BuildServiceProvider();

            ExampleService service = serviceProvider.GetRequiredService<ExampleService>();

            service.DoSomeWork(10, 20);
        }
    }

    class ExampleService
    {
        private readonly ILogger<ExampleService> _logger;

        public ExampleService(ILogger<ExampleService> logger)
        {
            _logger = logger;
        }

        public void DoSomeWork(int x, int y)
        {
            _logger.LogInformation("【看这里】DoSomeWork was called. x={X}, y={Y}", x, y);

        }
    }
}
