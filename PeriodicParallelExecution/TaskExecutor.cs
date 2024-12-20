namespace PeriodicParallelExecution;

public static class TaskExecutor
{
    public static async Task PeriodicParallelExecution(
        this Func<CancellationToken, Task> action, // task to be executed
        TimeSpan interval,                         // time interval after which new parallel task for the action is added
        int maxTaskInstances,                      // maximum parallel instances, after reached some need to complete before new one is added
        CancellationToken cancellationToken, 
        string name)
    {
        var intervalTask = Task.Delay(interval, cancellationToken); // this task is used to be notified when an interval expires
        var actionTask = action(cancellationToken);

        List<Task> tasks = new List<Task> { intervalTask, actionTask };
        Console.WriteLine("\n + Time: \t" + DateTime.Now.ToString() + ". Concurrent task count: \t" + (tasks.Count - 1) + ". Id: \t" + actionTask.Id + ". Added new parallel task.");

        while (true)
        {
            var completedTask = await Task.WhenAny(tasks);

            if (completedTask.IsCanceled) // triggered by cts.Cancel();
            {
                Console.WriteLine("\n * Time: \t" + DateTime.Now.ToString() + ". Concurrent task count: \t" + (tasks.Count - 1) + ". Cancellation requested.");
                try
                {
                    await Task.WhenAll(tasks.ToArray());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" * Time: \t" + DateTime.Now.ToString() + ". Concurrent task count: \t" + (tasks.Count - 1) + ". Caught exception: " + ex.Message);
                    tasks.Remove(intervalTask);
                    var count = 0;
                    foreach (var task in tasks)
                    {
                        if (task.IsCanceled)
                        {
                            count++;
                            Console.WriteLine(" - Time: \t" + DateTime.Now.ToString() + ". Concurrent task count: \t" + ((tasks.Count) - count) + ". Id: \t" + task.Id + ". Task cancelled.");
                        }
                    }

                    if (count == tasks.Count)
                    {
                        Console.WriteLine(" * All tasks cancelled.");
                    }
                    else
                    {
                        // ending up here means not all children tasks were cancelled. Happens when there is no ct.ThrowIfCancellationRequested() call in the action method.
                        Console.WriteLine(" * Some tasks are not cancelled. Make sure action method handles cancellation token properly.");
                    }
                }
                finally
                {
                    Console.WriteLine(" * Exiting loop.");
                }
                break;
            }

            if (completedTask == intervalTask) // an interval expired, now needs to be renewed
            {

                intervalTask = Task.Delay(interval, cancellationToken);
                tasks.Remove(completedTask);
                tasks.Add(intervalTask);
                Console.WriteLine("\n * Time: \t" + DateTime.Now.ToString() + ". Concurrent task count: \t" + (tasks.Count - 1) + ". Interval cycle completed. Duration: " + interval.Hours + ":" + interval.Minutes + ":" + interval.Seconds);
                if (tasks.Count - 1 < maxTaskInstances)
                {
                    actionTask = action(cancellationToken);
                    tasks.Add(actionTask);
                    Console.WriteLine(" + Time: \t" + DateTime.Now.ToString() + ". Concurrent task count: \t" + (tasks.Count - 1) + ". Id: \t" + actionTask.Id + ". Added new parallel task.");
                }
                else
                {
                    Console.WriteLine(" * Not adding new tasks, queue full (" + (tasks.Count - 1) + ").");
                }
            }
            else // one of the action tasks completed - either with exception if faulted or successfully
            {
                tasks.Remove(completedTask);
                if (completedTask.IsFaulted)
                {
                    Console.WriteLine("\n - Time: \t" + DateTime.Now.ToString() + ". Concurrent task count: \t" + (tasks.Count - 1) + ". Id: \t" + completedTask.Id + ". Faulted task. Caught exception: " + completedTask.Exception?.Message);
                }
                else // completedTask.IsCompletedSuccessfully
                {
                    Console.WriteLine("\n - Time: \t" + DateTime.Now.ToString() + ". Concurrent task count: \t" + (tasks.Count - 1) + ". Id: \t" + completedTask.Id + ". Task completed successfully.");
                }
            }
        }

        Console.WriteLine(" * \"" + name + "\" work is finished.");
    }
}
