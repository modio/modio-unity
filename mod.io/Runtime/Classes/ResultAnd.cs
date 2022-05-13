namespace ModIO
{
    /// <summary>Convenience wrapper for essentially a Tuple.</summary>
    public class ResultAnd<T>
    {
        public Result result;
        public T value;
    }
}
