using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace ModIOBrowser
{
    class ImageTransitions
    {

        /// <summary>
        /// This is a generic method for transitioning the alpha of an image component. Currently
        /// being used to show/hide the headers that fade in/out as you scroll dup/down the page
        /// in Collection view and Search results view.
        /// </summary>
        /// <param name="image">component to transition color alpha</param>
        /// <param name="targetAlphaValue">the target alpha to end transition at</param>
        /// <returns></returns>
        public static IEnumerator Alpha(Image image, float targetAlphaValue)
        {
            float incrementSize = 0.05f;
            Color color = image.color;
            while(color.a != targetAlphaValue)
            {
                color.a = color.a > targetAlphaValue ? color.a - incrementSize : color.a + incrementSize;

                // make sure we dont go outside the bounds
                if(color.a < 0f || color.a > 1f)
                {
                    color.a = targetAlphaValue;
                }

                image.color = color;

                yield return new WaitForSecondsRealtime(0.025f);
            }
        }
        public static IEnumerator AlphaFast(Image image, float targetAlphaValue)
        {
            float incrementSize = 0.05f;
            Color color = image.color;
            while(color.a != targetAlphaValue)
            {
                color.a = color.a > targetAlphaValue ? color.a - incrementSize : color.a + incrementSize;

                // make sure we dont go outside the bounds
                if(color.a < 0f || color.a > 1f)
                {
                    color.a = targetAlphaValue;
                }

                image.color = color;

                yield return new WaitForSecondsRealtime(0.01f);
            }
        }
    }

}
