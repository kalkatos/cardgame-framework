using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
    public class SpriteFieldView : FieldView
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
                    Debug.LogWarning($"Couldn't load sprite at path \"Resources/{path}\" (Object: {name})");
            }
        }
    }
}