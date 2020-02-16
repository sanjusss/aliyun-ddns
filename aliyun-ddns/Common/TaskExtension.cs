using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aliyun_ddns.Common
{
    public static class TaskExtension
    {
        public static async Task<T> WhenAny<T>(this IEnumerable<Task<T>> tasks, Func<Task<T>, bool> check)
        {
            var runningTasks = new HashSet<Task<T>>(tasks);
            while (runningTasks.Count > 0)
            {
                var current = await Task.WhenAny(runningTasks);
                if (current.IsCompletedSuccessfully &&
                    check(current))
                {
                    return current.Result;
                }
                else
                {
                    runningTasks.Remove(current);
                }
            }

            return default;
        }

        public static async Task<T> WhenAny<T>(this IEnumerable<Task<T>> tasks, Func<Task<T>, bool> check, TimeSpan timeout)
        {
            var task = WhenAny(tasks, check);
            if (timeout == Timeout.InfiniteTimeSpan ||
                await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                return await task;
            }
            else
            {
                return default;
            }
        }
    }
}
