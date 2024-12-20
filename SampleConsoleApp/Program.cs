// See https://aka.ms/new-console-template for more information
using PeriodicParallelExecution;

Console.WriteLine("Starting...");
Console.WriteLine("Press ENTER to exit.");

var interval = TimeSpan.FromSeconds(7);
CancellationTokenSource? cts = new CancellationTokenSource();

try
{
    var t = Task.Run(() => TaskExecutor.PeriodicParallelExecution(ActionMethod, interval, 4, cts.Token, "Sample Task"));
    //var t = TaskExecutor.PeriodicParallelExecution(ActionMethod, interval, 4, cts.Token, "Sample Task");

    Console.ReadLine();
    cts.Cancel();
}
catch
{

}
finally
{
    cts.Dispose();
    cts = null;
}

Console.ReadLine();

static async Task<int> ActionMethod(CancellationToken ct)
{
    // faking action that lasts one minute
    for (int i = 0; i < 60; i++)
    {
        await Task.Delay(1000);
        ct.ThrowIfCancellationRequested();
    }
    return 0;
}

static async Task<int> ActionMethodException(CancellationToken ct)
{
    // same as ActionMethod but throws exception after 15 seconds. Used to test how exception is handled/propagated.
    for (int i = 0; i < 60; i++)
    {
        await Task.Delay(1000);
        if (i == 15) throw new Exception("Fake exception");
        ct.ThrowIfCancellationRequested();
    }
    return 0;
}