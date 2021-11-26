using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardgameFramework
{
    public class FieldView_Text : FieldView
    {
        [SerializeField] private TMP_Text textMesh;

        internal override void SetFieldViewValue (string newValue)
        {
            textMesh.text = newValue;
        }
    }
}