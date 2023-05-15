using System.Collections;
using ModIOBrowser;
using ModIOBrowser.Implementation;
using UnityEngine;

namespace Plugins.mod.io.UI.Examples
{
    public class ExmapleGlyphSetter : MonoBehaviour
    {
        private bool connected = false;

        void Awake()
        {
            this.StartCoroutine(this.CheckForControllers());
        }

        IEnumerator CheckForControllers()
        {
            while (true)
            {
                var controllers = Input.GetJoystickNames();

                if (!this.connected && controllers.Length > 0)
                {
                    this.connected = true;
                    if(controllers[0].Contains("Microsoft"))
                    {
                        Glyphs.Instance.ChangeGlyphs(GlyphPlatforms.XBOX);
                    }
                    else if(controllers[0].Contains("Sony"))
                    {
                        Glyphs.Instance.ChangeGlyphs(GlyphPlatforms.PLAYSTATION_5);
                    }
                    else if(controllers[0].Contains("Nintendo"))
                    {
                        Glyphs.Instance.ChangeGlyphs(GlyphPlatforms.NINTENDO_SWITCH);
                    }

                    Debug.Log("Connected");

                }
                else if (this.connected && controllers.Length == 0)
                {
                    this.connected = false;
                    Glyphs.Instance.ChangeGlyphs(GlyphPlatforms.PC);
                    Debug.Log("Disconnected");
                }

                yield return new WaitForSeconds(1f);
            }
        }
    }
}
