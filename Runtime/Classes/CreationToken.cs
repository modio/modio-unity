
namespace ModIO
{
    /// <summary>
    /// This is used with creating new mod profiles. Using a token ensures you dont
    /// create duplicate profiles.
    /// </summary>
    /// <seealso cref="ModIOUnity.GenerateCreationToken"/>
    /// <seealso cref="ModIOUnityAsync.CreateModProfile"/>
    /// <seealso cref="ModIOUnity.CreateModProfile"/>
    public class CreationToken
    {
        string creationTokenFileHash;
    }
}
