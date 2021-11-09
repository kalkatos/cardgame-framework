using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameFramework
{
    public class FieldView_Sprite : FieldView
    {
        public SpriteRenderer spriteRenderer;

        internal override void SetFieldViewValue (string newValue)
        {
            if (!string.IsNullOrEmpty(newValue))
            {
                string path = newValue;
                Sprite sprite = Resources.Load<Sprite>(path);
                if (sprite)
                    spriteRenderer.sprite = sprite;
                else
                    CustomDebug.LogWarning($"Couldn't load sprite at path \"Resources/{path}\" (Object: {name})");
            }
        }
    }
}