namespace Modio.Unity.UI.Components
{
    public interface IPropertyMonoBehaviourEvents
    {
        void Start();

        void OnDestroy();

        void OnEnable();

        void OnDisable();
    }
}
