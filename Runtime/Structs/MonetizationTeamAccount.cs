using UnityEngine.Serialization;

namespace ModIO
{
    [System.Serializable]
    public struct MonetizationTeamAccount
    {
        public long Id; //user ID
        public string NameId;
        public string Username;
        public int MonetizationStatus;
        public int MonetizationOptions;
        public int SplitPercentage;
    }
}
