using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CardgameCore
{
	[CustomPropertyDrawer(typeof(VariableValuePair))]
	public class VariableValuePairDrawer : PropertyDrawer
	{
        public override VisualElement CreatePropertyGUI (SerializedProperty property)
        {
            // Create property container element.
            var container = new VisualElement();

            // Create property fields.
            var variableProp = new PropertyField(property.FindPropertyRelative("variable"));
            var valueProp = new PropertyField(property.FindPropertyRelative("value"));

            // Add fields to the container.
            container.Add(variableProp);
            container.Add(valueProp);

            return container;
        }

        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var variableRect = new Rect(position.x, position.y, position.width / 2f, position.height);
            var valueRect = new Rect(position.x + position.width / 2f, position.y, position.width / 2f, position.height);

            // Draw fields - passs GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(variableRect, property.FindPropertyRelative("variable"), GUIContent.none);
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("value"), GUIContent.none);

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}