using System.Threading.Tasks;

namespace ModIO.Implementation
{
    /// <summary>Adds convenience functions to the Task class.</summary>
    internal static class TaskExtensions
    {
        /// <summary>Convenience accessor for getting the ModIOResult.</summary>
        public static Result GetResult(this Task<Result> t)
        {
            return t.Result;
        }

        /// <summary>Convenience accessor for getting the ModIOResult.</summary>
        public static Result GetResult<T>(this Task<ResultAnd<T>> t)
        {
            return t.Result.result;
        }

        /// <summary>Convenience accessor for getting the ModIOResult.</summary>
        public static T GetValue<T>(this Task<ResultAnd<T>> t)
        {
            return t.Result.value;
        }
    }
}
