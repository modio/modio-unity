using ModIO;

namespace ModIOBrowser.Implementation
{
    public struct CollectionProfile
    {
        public ModProfile modProfile;
        public bool subscribed;
        public bool enabled;
        public int subscribers;
        public string installationStatus;
        public ModId id => modProfile.id;
        public string name => modProfile.name;

        public CollectionProfile(ModProfile profile, bool subscribed, bool enabled, int subscribers, string installationStatus)
        {
            modProfile = profile;
            this.subscribed = subscribed;
            this.enabled = enabled;
            this.subscribers = subscribers;
            this.installationStatus = installationStatus;
        }
    }
}
