// using System;
// using System.Collections;
// using ModIO;
// using ModIOBrowser.Implementation;
// using UnityEngine;
// using static ModIO.Utility;
//
// namespace ModIOBrowser
// {
//     /// <summary>
//     /// Plays a sound
//     /// </summary>
//     class Sound : MonoBehaviour
//     {
//         const float soundFatiguePreventionTime = 0.05f;
//         const float UNSET_VOLUME = -1f;
//
//         static float lastPlayedHoverSoundSeconds;
//
//         public const string Click = "SoundClick";
//         public const string Hover = "SoundHover";
//
//         public AudioSource audioSource;
//         public float originalVolume = UNSET_VOLUME;
//
//         /// <summary>
//         /// Play a clicking sound.
//         /// </summary>
//         public static void PlayClick() => Play(Click);
//
//         /// <summary>
//         /// Play a hovering sound.
//         /// </summary>
//         public static void PlayHover()
//         {
//             //This prevents an "rrrrrr" sound if you pass through many objects
//             if(lastPlayedHoverSoundSeconds + soundFatiguePreventionTime > Time.realtimeSinceStartup)
//             {
//                 return;
//             }
//             lastPlayedHoverSoundSeconds = Time.realtimeSinceStartup;
//
//             Play(Hover);
//         }
//
//         /// <summary>
//         /// Plays a sound from the path. The path will be used as an ID/key
//         /// for the object pool. The sound will automatically allocate and
//         /// return itself to the pool.
//         /// </summary>
//         /// <param name="idPath"></param>
//         public static void Play(string idPath)
//         {         
//             var mm = PrefabPool.Instance.Get<Sound>(idPath);
//
//             if(mm.originalVolume == UNSET_VOLUME)
//             {
//                 mm.originalVolume = mm.audioSource.volume;
//             }
//
//             mm.audioSource.volume = mm.originalVolume * Browser.Instance.uiConfig.volume;
//             mm.audioSource.Play();
//
//             CoroutineRunner.Instance.Run(mm.Return(idPath, mm.audioSource.clip.length));
//         }
//
//         /// <summary>
//         /// Return the sound to the pool after a given amount of time.
//         /// </summary>
//         private IEnumerator Return(string id, float delay)
//         {
//             yield return new WaitForSeconds(delay);
//             PrefabPool.Instance.Return(id, this);
//         }
//     }
// }
