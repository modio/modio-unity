using UnityEngine;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// Simplifies hitbox overlap logic for rect transforms
    /// 
    /// This class exists to formalize rect transform hit logic in a way that
    /// bypasses the majority of unitys "helpful" rect transform logic, to make things
    /// more readable and debuggable
    /// </summary>
    public class RectTransformOverlap
    {
        //Contains in this order: top left, bottom left, bottom right, top right
        Vector3[] vectors = new Vector3[4];

        public RectTransformOverlap(RectTransform rt)
        {
            rt.GetWorldCorners(vectors);
        }
        public static explicit operator RectTransformOverlap(RectTransform rt) => new RectTransformOverlap(rt);

        //These exist for readability
        public float xMin { get { return vectors[0].x; } } 
        public float xMax { get { return vectors[2].x; } }
        public float yMin { get { return vectors[0].y; } }
        public float yMax { get { return vectors[2].y; } }
        public float width { get { return xMax - xMin; } }
        public float height { get { return yMax - yMin; } }

        public static float DistanceFromEdgeY(RectTransformOverlap a, RectTransformOverlap b, float paddingPercentage)
        {
            float padding = b.height * paddingPercentage;
            
            // Top
            if(a.yMax > b.yMax - padding)
            {
                return  b.yMax - padding - a.yMax;
            }
            // Bottom
            if(a.yMin < b.yMin + padding)
            {
                return b.yMin + padding - a.yMin;
            }
            return 0f;
        }

        public static float DistanceFromEdgeX(RectTransformOverlap a, RectTransformOverlap b, float paddingPercentage)
        {
            float padding = b.width * paddingPercentage;
            
            // Right
            if(a.xMax > b.xMax - padding)
            {
                return  b.xMax - padding - a.xMax;
            }
            // Left
            if(a.xMin < b.xMin + padding)
            {
                return b.xMin + padding - a.xMin;
            }
            return 0f;
        }

        public bool IsOutsideOfRectY(RectTransformOverlap b, float paddingPercentage)
        {
            float padding = b.height * paddingPercentage;
            
            if(yMin < b.yMin + padding
            || yMax > b.yMax - padding)
            {
                return true;
            }

            return false;
        }

        public bool IsOutsideOfRectX(RectTransformOverlap b, float paddingPercentage)
        {
            float padding = b.width * paddingPercentage;
            
            if(xMin < b.xMin + padding
            || xMax > b.xMax - padding)
            {
                return true;
            }

            return false;
        }
    }
}
