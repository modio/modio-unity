using System;
using Modio.Images;
using Modio.Ratings;
using Modio.Users;

namespace Modio.Mods
{
    public interface IModioResource
    {
        public ModioId Id { get; }
    }
    public interface IModioInfo : IModioResource
    {
        public string Name { get; }
        public string Summary  { get; }
        public string Description  { get; }
        public DateTime DateLive  { get; }
        public DateTime DateUpdated  { get; }
        public ModTag[] Tags  { get; }
        public ModMaturityOptions MaturityOptions { get; }

        public ModioImageSource<Mod.LogoResolution> Logo { get; }
        public UserProfile Creator { get; }
        public ModioRating CurrentUserRating { get;}

        public bool IsSubscribed { get; }
    }
}
