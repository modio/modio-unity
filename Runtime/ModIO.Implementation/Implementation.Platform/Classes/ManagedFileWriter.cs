using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ModIO.Implementation.Platform
{
    // The ManagedFileWriter uses sequential writes in order to guarantee write speed on disk.
    // It does this by cutting up a writes into smaller chunks, and then measuring the time it
    // takes to write a chunk and sleeping for a certain duration to guarantee write speed.
    // Per platform settings can be set in the settings config

    internal static class ManagedFileWriter
    {
        const int SecondMs = 1000;
        const int BytesPerKilobyte = 1024;

        static int writeSpeedInKbPerSecond;
        static int bytesPerWrite;
        static float writeSpeedReductionThreshold;

        // Task queue runner is used to managed write job priority
        static readonly TaskQueueRunner TaskQueueRunner = new TaskQueueRunner(1, true, true);
        static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        public static async Task WriteToFile(FileStream fs, byte[] bytes, int offset, int count, CancellationToken cancellationToken, Action<bool> onComplete = null) => await WriteToFile(fs, bytes, offset, count, cancellationToken, TaskPriority.LOW, onComplete);
        public static async Task WriteToFile(FileStream fs, byte[] bytes, CancellationToken cancellationToken, Action<bool> onComplete = null) => await WriteToFile(fs, bytes, 0, bytes.Length, cancellationToken, TaskPriority.LOW, onComplete);
        public static async Task WriteToFile(FileStream fs, byte[] bytes, CancellationToken cancellationToken, TaskPriority taskPriority, Action<bool> onComplete = null) => await WriteToFile(fs, bytes, 0, bytes.Length, cancellationToken, taskPriority, onComplete);
        public static async Task WriteToFile(FileStream fs, byte[] bytes, int offset, int count, CancellationToken cancellationToken, TaskPriority taskPriority, Action<bool> onComplete = null)
        {
            bytesPerWrite = Settings.build.bytesPerWrite;
            writeSpeedInKbPerSecond = Settings.build.writeSpeedInKbPerSecond;
            writeSpeedReductionThreshold = Settings.build.writeSpeedReductionThreshold;

            if (bytesPerWrite <= 0)
                bytesPerWrite = bytes.Length;//Write as fast as possible
            if (writeSpeedInKbPerSecond <= 0)
                writeSpeedInKbPerSecond = bytes.Length;//Write as fast as possible
            if (writeSpeedReductionThreshold <= 0)
                writeSpeedReductionThreshold = 1;//100%, No slowdown

            await WriteToFileWithLimitedSpeed_Internal(fs, bytes, offset, count, cancellationToken, taskPriority, onComplete);
        }

        static async Task WriteToFileWithLimitedSpeed_Internal(FileStream fs, byte[] bytes, int offset, int count, CancellationToken cancellationToken, TaskPriority priority, Action<bool> onComplete)
        {
            bool success = false;
            try
            {
                var bytesWritten = 0;
                var start = DateTime.UtcNow.Ticks;
                var tasks = new List<Task<Result>>();
                for (int i = offset; i < count; i += bytesPerWrite)
                {
                    var index = i;
                    tasks.Add(TaskQueueRunner.AddTask(priority, 1, async () =>
                    {
                        try
                        {
                            // We use this semaphore to guarantee writespeed on HD by blocking other write tasks
                            await Semaphore.WaitAsync(cancellationToken);
                            bytesWritten = await Write(fs, bytes, cancellationToken, bytesWritten, start, index, count);
                        }
                        catch (Exception taskException)
                        {
                            Logger.Log(LogLevel.Verbose, $"Error within task execution: {taskException.Message}");
                            throw;
                        }
                        finally
                        {
                            start = DateTime.UtcNow.Ticks;
                            Semaphore.Release();
                        }

                        return ResultBuilder.Success;
                    },
                    true));
                    success = true;
                }

                // We need to wait for all tasks at the same time, so that if we admit a high prio write job it'll get prioritized and finish first.
                // Otherwise, this starts skipping between multiple tasks. Like this: Define previous write tasks as A, B, C. Define high prio task as P.
                // We add in A B C P at the same time, in that sequence.
                // It would then proceed to run -> A, P, A, P, B, P finish, C
                // Where A/B/C would be selected randomly from existing tasks as they're low prio tasks, except for the first A task.
                // Possibly refactor taskqueuemanager to use an int instead of TaskPriority. That's not necessarily a small change.
                // By using the Task.WhenAll, we instead get: A, P, P, P finish, B, C, A
                // The reason why it starts A first, is that the task queue runner immediately starts running when we add the first object.

                await Task.WhenAll(tasks);
            }
            catch (IOException ioEx)
            {
                Logger.Log(LogLevel.Error, $"I/O exception: {ioEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"An error occurred: {ex.Message}");
                throw;
            }
            finally
            {
                onComplete?.Invoke(success);
            }
        }

        //-----Tracking write speed------//
        static double totalSeconds = 0;
        static double totalKbWritten = 0;
        public static double GetCurrentWriteSpeedInKbPerSeconds()
        {
            return totalKbWritten/totalSeconds;
        }
        //-------------------------------//

        static double GetWriteSpeed()
        {
            var bytesPerSecond = (double)writeSpeedInKbPerSecond * BytesPerKilobyte;
            var percentageUsed = PercentageOfBudgetUsed();
            if (percentageUsed > writeSpeedReductionThreshold)
            {
                var currentPercentagePastThreshold = percentageUsed - writeSpeedReductionThreshold;
                var maxPercentagePastThreshold = 1 - writeSpeedReductionThreshold;
                var writeSpeedReductionPercentage = 1 - ( currentPercentagePastThreshold / maxPercentagePastThreshold );
                bytesPerSecond = writeSpeedReductionPercentage * writeSpeedInKbPerSecond * BytesPerKilobyte;
                Logger.Log(LogLevel.Verbose,$"Slowing down to {bytesPerSecond} bytes per second");
            }
            return bytesPerSecond;
        }

        static double PercentageOfBudgetUsed()
        {
#if UNITY_GAMECORE && !UNITY_EDITOR
            var hrResult = Unity.GameCore.SDK.XPackageGetWriteStats(out Unity.GameCore.XPackageWriteStats writeStats);
            if (Unity.GameCore.HR.SUCCEEDED(hrResult))
            {
                var usedPercentage = Math.Min(1, writeStats.BytesWritten / (double)writeStats.Budget );
                Logger.Log(LogLevel.Verbose, $"Percentage of write budget used is {usedPercentage*100}%");
                Logger.Log(LogLevel.Verbose, $"Write Stats - Interval {writeStats.Elapsed}/{writeStats.Interval}, Budget {writeStats.BytesWritten}/{writeStats.Budget}");
                return usedPercentage;
            }
#endif
            return 0;
        }

        public static bool IsOverBudget(ulong bytesRequested, out ulong intervalTimeRemainingMs)
        {
            intervalTimeRemainingMs = 0;
#if UNITY_GAMECORE && !UNITY_EDITOR
            var hrResult = Unity.GameCore.SDK.XPackageGetWriteStats(out Unity.GameCore.XPackageWriteStats writeStats);
            if (Unity.GameCore.HR.SUCCEEDED(hrResult) && writeStats.BytesWritten + bytesRequested > writeStats.Budget)
            {
                intervalTimeRemainingMs = Math.Max(0, writeStats.Interval - writeStats.Elapsed);
                Logger.Log(LogLevel.Verbose, $"Write Stats - Interval {writeStats.Elapsed}/{writeStats.Interval}, Budget {writeStats.BytesWritten}/{writeStats.Budget}");
                return true;
            }
#endif
            return false;
        }

        static async Task<int> Write(FileStream fs, byte[] bytes, CancellationToken cancellationToken, int bytesWritten, long start, int offset, int count)
        {
            count = Math.Min(bytesPerWrite, count - offset);

            //Time remaining in the current interval before the write budget resets
            ulong intervalTimeRemainingMs;
            while (IsOverBudget((ulong)count, out intervalTimeRemainingMs))
            {
                Logger.Log(LogLevel.Verbose, $"Over write budget, delaying writes for {intervalTimeRemainingMs}ms");
                await Task.Delay((int)intervalTimeRemainingMs, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                return 0;

            fs.Write(bytes, offset, count);
            fs.Flush();

            bytesWritten += count;
            var ticks = DateTime.UtcNow.Ticks;
            var elapsedTicks = ticks - start;
            var elapsedSeconds = new TimeSpan(elapsedTicks).TotalSeconds;
            var bytesPerSecond = GetWriteSpeed();
            var expectedTimeToWrite = bytesPerSecond > 0 ? count / bytesPerSecond : 0;

            //-----Tracking write speed------//
            totalSeconds += elapsedSeconds;
            totalKbWritten += (double)count / BytesPerKilobyte;
            //-------------------------------//

            Logger.Log(LogLevel.Verbose, $"Count: {count}, ElapsedSeconds: {elapsedSeconds}, BytesPerSecond: {bytesPerSecond}, ExpectedTimeToWrite: {expectedTimeToWrite}");

            //To maintain a specific write/sec rate we need to wait if we are writing too quickly
            if (elapsedSeconds < expectedTimeToWrite || expectedTimeToWrite <= 0)
            {
                int sleepTime = (int)((expectedTimeToWrite - elapsedSeconds) * SecondMs);
                //sleepTime should never exceed the time left in the current interval
                if (intervalTimeRemainingMs > 0 && (expectedTimeToWrite <= 0 || sleepTime > (int)intervalTimeRemainingMs))
                {
                    sleepTime = (int)intervalTimeRemainingMs;
                }

                //-----Tracking write speed------//
                totalSeconds += (float)sleepTime/SecondMs;
                //------------------------------//

                if (sleepTime > 0)
                {
                    Logger.Log(LogLevel.Verbose, "Sleeping for " + sleepTime + "ms to regulate speed for config " + fs.Name);
                    await Task.Delay(sleepTime, cancellationToken);
                }
            }

            return bytesWritten;
        }
    }
}
