using UnityEngine;

namespace Modio.Unity.UI
{
    public static class ModioRectHelper
    {
        static readonly Vector3[] CachedFourCornersArray = new Vector3[4];
        
        public static void GetWorldAABB(RectTransform rectTransform, out Vector3 min, out Vector3 max)
        {
            rectTransform.GetWorldCorners(CachedFourCornersArray);

            min = Vector3.one * float.MaxValue;
            max = Vector3.one * float.MinValue;

            foreach (Vector3 corner in CachedFourCornersArray)
            {
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }
        }
    }
}
