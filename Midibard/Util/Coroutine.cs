// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

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