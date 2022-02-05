using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardgameFramework
{
    public class FieldView_Text : FieldView
    {
        [SerializeField] private TMP_Text textMesh;
		[SerializeField] private string prefix;
		[SerializeField] private string sufix;

		internal override void SetFieldViewValue (string newValue)
        {
            textMesh.text = prefix+newValue+sufix;
        }
    }
}