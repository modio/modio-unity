
namespace ModIO.Implementation
{
    /// <summary>Convenience wrapper for creating a ResultAnd<T>.</summary>
    internal static class ResultAnd
    {
        public static ResultAnd<U> Create<U>(Result result, U value)
        {
            var ra = new ResultAnd<U>();
            ra.result = result;
            ra.value = value;
            return ra;
        }

        public static ResultAnd<U> Create<U>(uint result, U value)
        {
            var ra = new ResultAnd<U>();
            ra.result = ResultBuilder.Create(result);
            ra.value = value;
            return ra;
        }
    }
}
