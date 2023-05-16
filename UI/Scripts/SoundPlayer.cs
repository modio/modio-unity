using ModIO.Util;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    /// <summary>
    /// The purpose of this class is to enable playing sound via attaching it
    /// to various onhover and onclick properties in the Unity browser.
    /// If you intend to play sounds directly from the code, instead just use
    /// Sound.Play(address);
    /// 
    /// It also helps browser handle volume.
    /// </summary>
    public class SoundPlayer : SelfInstancingMonoSingleton<SoundPlayer>
    {
        const float soundFatiguePreventionTime = 0.05f;
        static float lastPlayedHoverSoundSeconds;
        
        [Tooltip("(Optional) You can assign an audio slider for the browser to listen to, this will automatically adjust the volume of the audio clips being played in the Browser. You can also use the SetVolume(float) method to change the volume manually.")]
        public Slider audioSlider;

        [System.Serializable]
        internal class SoundEffect
        {
            public AudioClip clip;
            
            [Tooltip("You can use this to quickly normalize the audio clip before it is affected by global volume")]
            public float defaultVolume;
        }

        [SerializeField] SoundEffect SoundClick;
        [SerializeField] SoundEffect SoundHover;

        AudioSource aud;
        
        void Start()
        {
            AttachAudioSlider(audioSlider);
        }

        /// <summary>
        /// This will attach a Slider component to the Browser sound player. It will automatically
        /// update the UI volume according to the Slider's value.
        /// </summary>
        /// <param name="slider">the slider to be used for setting the volume</param>
        public static void AttachAudioSlider(Slider slider)
        {
            if(slider == null)
            {
                return;
            }
            
            // Remove the listener in case we have already assigned it
            slider.onValueChanged.RemoveListener(SetVolume);
            
            // Add the new listener
            slider.onValueChanged.AddListener(SetVolume);
        }

        /// <summary>
        /// Sets the ModIO Browser volume (0f - 1.0f).
        /// </summary>
        public static void SetVolume(float volume)
        {
            if(Browser.SingletonIsInstantiated())
            {
                SharedUi.settings.volume = volume;
            }            
        }

        /// <summary>
        /// Gives a reference the the AudioSource being used by the ModIOBrowser
        /// </summary>
        /// <returns></returns>
        public static AudioSource AudioSource()
        {
            if(Instance.aud == null)
            {
                Instance.aud = Instance.gameObject.AddComponent<AudioSource>();
            }
            return Instance.aud;
        }

        void PlaySound(SoundEffect sfx)
        {
            if(SharedUi.settings == null || sfx == null)
            {
                return;
            }

            //This prevents an "rrrrrr" sound if you pass through many objects
            if(lastPlayedHoverSoundSeconds + soundFatiguePreventionTime > Time.realtimeSinceStartup)
            {
                return;
            }
            lastPlayedHoverSoundSeconds = Time.realtimeSinceStartup;
            
            AudioSource().PlayOneShot(sfx.clip, sfx.defaultVolume * SharedUi.settings.volume);
        }

        public static void PlayClick() => Instance.PlaySound(Instance.SoundClick);

        public static void PlayHover() =>Instance.PlaySound(Instance.SoundHover);
    }
}
