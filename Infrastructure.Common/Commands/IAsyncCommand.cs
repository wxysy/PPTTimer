using System.Threading.Tasks;
using System.Windows.Input;

namespace Infrastructure.Common.Commands
{
    internal interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object? parameter);
    }
}
