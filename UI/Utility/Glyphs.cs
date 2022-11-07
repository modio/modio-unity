using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{

    class Glyphs : SimpleMonoSingleton<Glyphs>
    {        
        public enum GlyphPlatforms
        {
            PC,
            XBOX,
            PLAYSTATION_4,
            PLAYSTATION_5,
            NINTENDO_SWITCH
        }

        public enum Glyph
        {            
            //Playstation / Switch / Xbox:

            //X / B / A
            ACTION_BUTTONS_BOTTOM,

            //Square / Y / X
            ACTION_BUTTONS_LEFT,

            //Circle / A / B
            ACTION_BUTTONS_RIGHT,

            //Triangle / X / Y
            ACTION_BUTTONS_UP,

            //This is an image of the button which the glyph resides on
            ACTION_BUTTONS_BACKGROUND,

            //L1 / L / LB
            LB,
            LB_Background,

            //R1 / R / RB
            RB,
            RB_Background,

            //L2 / LZ / LT
            LT,
            LT_Background,

            //R2 / RZ / RT
            RT,
            RT_Background,

            //Burger / + / Burger
            MENU,
            MENU_Background,

            RightStick,
            RightStickBackground
        }

        public ColorScheme colorScheme;
        public GlyphPlatforms platformType;

        public Color glyphColorFallback;
        public Sprite fallbackSprite;
        public Color fallbackColor = Color.white;

        [NonSerialized]
        public bool ready = false;


        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if(colorScheme == null)
            {
                colorScheme = Browser.Instance.colorScheme;                
            }

            ready = true;
        }

        public Color GetColor(ColorSetterType colorSetter)
        {
            Color color = colorScheme.GetSchemeColor(colorSetter);

            if(color == default(Color))
            {
                color = fallbackColor;
            }

            return color;
        }
    }
}
