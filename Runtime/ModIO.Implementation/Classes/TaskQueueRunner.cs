using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ModIO.Implementation
{

    internal class TaskQueueRunner
    {

        class TaskQueueItem
        {
            public Task task;
            public int taskSize;
            public TaskPriority priority;
        }

        const int AutoRunIntervalMs = 15;
        List<TaskQueueItem> tasks = new List<TaskQueueItem>();
        int upperTasksBoundary;
        bool runsAutomatically;
        bool isAutoRunning;
        bool synchronizedJobs = true;

        /// <summary>
        /// Creates a TaskPriorityQueueRunner
        /// </summary>
        /// <param name="upperTasksBoundary">Uppertask boundary is a hard limit to how many or "much" tasks it can run.
        /// When you add a task, you add a task size for it. Each time the TaskPriorityQueueRunner
        /// updates its tasks, it'll only run as many tasks as it can do withing the size of the
        /// upper task boundary
        /// /// </param>
        /// <param name="runsAutomatically">if true, the TaskPriorityQueueRunner will attempt to run itself
        /// when you add a task, if it isn't already running.
        /// <param name="synchronizedJobs">if true, all jobs will be run in order. If false, all jobs
        /// will ge started at the same time and be awaited on together until finish.</param>
        /// </param>
        public TaskQueueRunner(int upperTasksBoundary, bool runsAutomatically = false, bool synchronizedJobs = false)
        {
            this.upperTasksBoundary = upperTasksBoundary;
            this.runsAutomatically = runsAutomatically;
            this.synchronizedJobs = synchronizedJobs;
        }

        async void AutoRun()
        {
            if(isAutoRunning)
            {
                return;
            }

            isAutoRunning = true;

            while(HasTasks())
            {
                try
                {
                    await PerformTasks();
                }
                catch(Exception e)
                {
                    Logger.Log(LogLevel.Warning, $"[TQR] Unhandled exception "
                                                 + $" thrown in AutoRun. Exception: "
                                                 + $"{e.Message} - Inner Exception: "
                                                 + $"{e.InnerException?.Message}");
                }
            }
            isAutoRunning = false;
        }

        /// <summary>
        /// Runs as many tasks as possible given the boundary.
        /// Attempts to run HIGH priority tasks first.
        /// Will add LOW priority tasks if they are within bounds.
        /// </summary>
        /// <returns>Returns a task which is responsible for running all the compiled tasks.</returns>
        public async Task PerformTasks()
        {
            List<TaskQueueItem> runTasks = new List<TaskQueueItem>();
            try
            {
                lock( tasks )
                {
                    int taskAmount = 0;

                    runTasks.AddRange(GetTasks(TaskPriority.HIGH, upperTasksBoundary, ref taskAmount, tasks));
                    runTasks.AddRange(GetTasks(TaskPriority.LOW, upperTasksBoundary, ref taskAmount, tasks));

                    tasks = tasks.Except(runTasks).ToList();

                    if(runTasks.Count == 0)
                    {
                        return;
                    }
                }

                await RunTasksAsync(runTasks, synchronizedJobs);
            }
            catch(Exception e)
            {
                Logger.Log(LogLevel.Warning, $"[TQR] Unhandled exception thrown in "
                                             + $"PerformTasks operation. Exception: {e.Message}"
                                             + $" - Inner Exception: {e.InnerException?.Message}");
            }
        }

        /// <summary>
        /// Returns true if the Task Priority Queue runner has more tasks to run
        /// </summary>
        public bool HasTasks() => tasks.Count > 0;
        
        /// <summary>
        /// Add a task to the queue.
        /// </summary>
        /// <returns>Returns given Task for awaiting purposes.</returns>
        public Task<T> AddTask<T>(TaskPriority prio, int taskSize, Func<Task<T>> function)
        {
           Task<T> task = new Task<T>(() => function().Result);

           tasks.Add(new TaskQueueItem
           {
                task = task,
                taskSize = taskSize,
                priority = prio
            });

            if(runsAutomatically)
            {
                AutoRun();
            }

            return task;
        }

        /// <summary>
        /// Compiles a list of tasks to run given the upper task boundary,
        /// giving consideration to how many tasks we have already considered towards the boundary.
        /// </summary>
        static List<TaskQueueItem> GetTasks(TaskPriority p, int upperTasksBoundary, ref int taskAmount, List<TaskQueueItem> operations)
        {
            int taskAmountCache = taskAmount;

            //Order of tasks is not considered when running them
            //however, it is considered when calculating what to run
            List<TaskQueueItem> tasks = operations
                .Where(x => x.priority == p)
                .OrderBy(x => x.taskSize)
                .Where(x =>
                {
                    if(taskAmountCache + x.taskSize <= upperTasksBoundary)
                    {
                        taskAmountCache += x.taskSize;
                        return true;
                    }

                    return false;
                })
                .ToList();

            taskAmount = taskAmountCache;

            return tasks;
        }

        /// <summary>
        /// Runs a tasks queue item list.
        /// </summary>
        /// <returns>The task responsible for knowing when all tasks are completed.</returns>
        static async Task RunTasksAsync(List<TaskQueueItem> items, bool synchronizedJobs)
        {
            if(synchronizedJobs)
            {
                foreach(TaskQueueItem item in items)
                {
                    if(item.task.IsCanceled || item.task.IsFaulted)
                    {
                        continue;
                    }
                    
                    try
                    {
                        item.task.Start();
                        await item.task;
                    }
                    catch(Exception e)
                    {
                        Logger.Log(LogLevel.Warning, $"[TQR] Unhandled exception thrown in"
                                                     + $" synchronized RunTasksAsync() operation."
                                                     + $" Exception: {e.Message} - Inner Exception:"
                                                     + $" {e.InnerException?.Message}");
                    }
                }
            }
            else
            {
                foreach(var item in items)
                {
                    if(item.task.IsCanceled || item.task.IsFaulted)
                    {
                        continue;
                    }
                    
                    try
                    {
                        item.task.Start();
                    }
                    catch(Exception e)
                    {
                        Logger.Log(LogLevel.Warning, $"[TQR] Unhandled exception thrown in"
                                                     + $" non-synchronized RunTasksAsync() operation."
                                                     + $" Exception: {e.Message} - Inner Exception:"
                                                     + $" {e.InnerException?.Message}");
                    }
                }
                
                await Task.WhenAll(items.Select(x => x.task).ToArray());
            }                
        }
    }
}
