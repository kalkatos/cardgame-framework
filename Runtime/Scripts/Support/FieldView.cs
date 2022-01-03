using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameFramework
{
    public abstract class FieldView : MonoBehaviour
    {
        public string targetFieldName;

        internal abstract void SetFieldViewValue (string newValue);
    }
}