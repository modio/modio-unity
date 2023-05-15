using System;
using System.Collections;
using System.Collections.Generic;
using ModIO.Util;
using UnityEngine;
using UnityEngine.UIElements;


namespace ModIOBrowser.Implementation
{
    class QueueRunner : SelfInstancingMonoSingleton<QueueRunner>
    {
        private List<Action> sequences = new List<Action>();
        private Coroutine coroutine;

        public void Add(Action sequence)
        {
            if(sequence == null)
            {
                return;
            }

            sequences.Add(sequence);
            if(coroutine == null)
            {
                coroutine = StartCoroutine(Run());
            }
        }

        IEnumerator Run()
        {
            while(sequences.Count > 0)
            {
                yield return 0;
                sequences[0]();
                sequences.RemoveAt(0);
            }

            coroutine = null;
        }

        public void AddSpriteCreation(Texture2D texture, Action<Sprite> onConversion)
            => Add(() => onConversion(TextureToSprite(texture)));            
        
        private static Sprite TextureToSprite(Texture2D texture)
        {
            var rect = new Rect(Vector2.zero, new Vector2(texture.width, texture.height));
            var ppi = 100;
            var spritemeshType = SpriteMeshType.FullRect;
            var sprite = Sprite.Create(texture, rect, Vector2.zero, ppi, 0, spritemeshType);
            return sprite;
        }
    }
}
