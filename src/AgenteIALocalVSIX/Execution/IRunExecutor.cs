using System.Threading.Tasks;
using System.Windows;

namespace AgenteIALocalVSIX.Execution
{
    internal interface IRunExecutor
    {
        Task RunAsync(object sender, RoutedEventArgs e);
        void RequestStop();
    }
}
