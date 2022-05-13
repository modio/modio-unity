namespace ModIO.Implementation
{
    /// <summary>Convenience class for building Result objects.</summary>
    internal static class ResultBuilder
    {
        /// <summary>Creator for the Result class.</summary>
        public static Result Create(uint resultCode, uint apiCode = 0)
        {
            return new Result() { code = resultCode, code_api = apiCode };
        }

        /// <summary>Constant for Success.</summary>
        public static readonly Result Success = new Result() { code = ResultCode.Success };

        // TODO(@jackson): Remove later
        public static readonly Result Unknown = new Result() { code = ResultCode.Unknown };
    }
}
