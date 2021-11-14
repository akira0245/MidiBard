using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiBard.Util;

public static class Coroutine
{
    /// <summary>
    ///     Blocks while condition is true or timeout occurs.
    /// </summary>
    /// <param name="condition">The condition that will perpetuate the block.</param>
    /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <exception cref="TimeoutException"></exception>
    /// <returns></returns>
    public static async Task WaitWhile(Func<bool> condition, int timeout = -1, int frequency = 25)
    {
        var waitTask = Task.Run(async () =>
        {
            while (condition())
            {
                await Task.Delay(frequency);
            }
        });

        if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
        {
            throw new TimeoutException();
        }
    }

    /// <summary>
    ///     Blocks until condition is true or timeout occurs.
    /// </summary>
    /// <param name="condition">The break condition.</param>
    /// <param name="frequency">The frequency at which the condition will be checked.</param>
    /// <param name="timeout">The timeout in milliseconds.</param>
    /// <returns></returns>
    public static async Task WaitUntil(Func<bool> condition, int timeout = -1, int frequency = 25)
    {
        var waitTask = Task.Run(async () =>
        {
            while (!condition())
            {
                await Task.Delay(frequency);
            }
        });

        if (waitTask != await Task.WhenAny(
                waitTask,
                Task.Delay(timeout)))
        {
            throw new TimeoutException();
        }
    }

    public static bool WaitUntilSync(Func<bool> condition, int frequency = 25, int timeout = int.MaxValue)
    {
        int totalTime = 0;
        while (!condition() && totalTime < timeout)
        {
            totalTime += frequency;
            Thread.Sleep(frequency);
        }

        return condition();
    }
}