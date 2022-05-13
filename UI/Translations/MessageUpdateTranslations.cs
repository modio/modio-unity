#if UNITY_EDITOR
#endif

using ModIO.Util;

namespace ModIOBrowser.Implementation
{
    /// <summary>    
    /// use SimpleMessageHub.Instance.Publish(new MessageUpdateTranslations()); to
    /// make any translatable text object translate itself
    /// </summary>
    class MessageUpdateTranslations : ISimpleMessage { }
}
