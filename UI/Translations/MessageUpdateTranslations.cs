#if UNITY_EDITOR
#endif

namespace ModIOBrowser.Implementation
{
    /// <summary>    
    /// use SimpleMessageHub.Instance.Publish(new MessageUpdateTranslations()); to
    /// make any translatable text object translate itself
    /// </summary>
    class MessageUpdateTranslations : ModIO.Utility.ISimpleMessage { }
}
