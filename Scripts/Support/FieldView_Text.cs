using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardgameCore
{
    public class FieldView_Text : FieldView
    {
        public TMP_Text textMesh;

        internal override void SetFieldViewValue (string newValue)
        {
            textMesh.text = newValue;
        }
    }
}