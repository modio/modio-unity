namespace Modio.Platforms
{
    public interface ITitleSafeAreaProvider
    {
        public (float horizontal, float vertical) GetTitleSafeArea();
    }
}
