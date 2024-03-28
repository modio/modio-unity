namespace ModIO
{
    public class ModMonetizationTeamDetails
    {
        /// <summary>Unique ID of the user.</summary>
        public long userId;

        /// <summary>User monetization split.</summary>
        public int split;

        public ModMonetizationTeamDetails(long userId, int split)
        {
            this.userId = userId;
            this.split = split;
        }
    }
}
