using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardgameCore
{
    public class TextFieldView : FieldView
    {
        public TextMeshPro textMesh;

        internal override void SetFieldViewValue (string newValue)
        {
            textMesh.text = newValue;
        }
    }
}