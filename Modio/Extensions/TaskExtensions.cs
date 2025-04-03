using System;
using System.Threading.Tasks;

namespace Modio.Extensions
{
    public static class TaskExtensions
    {
        public static async void ForgetTaskSafely(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log(e);
            }
        }
    }
}
