using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CardgameCore
{
    [CustomPropertyDrawer(typeof(VariableValuePair))]
    public class ConditionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI (SerializedProperty property)
        {
            var container = new VisualElement();
            return container;
        }

        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
        }
    }
}
