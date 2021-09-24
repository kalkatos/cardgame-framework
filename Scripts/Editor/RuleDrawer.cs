using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace CardgameCore
{
            
    [CustomPropertyDrawer(typeof(Rule))]
    public class RuleDrawer : PropertyDrawer
    {
		public override VisualElement CreatePropertyGUI (SerializedProperty property)
		{
			var container = new VisualElement();
			container.Add(new PropertyField(property.FindPropertyRelative("name")));
			container.Add(new PropertyField(property.FindPropertyRelative("tags")));
			container.Add(new PropertyField(property.FindPropertyRelative("trigger")));
			container.Add(new PropertyField(property.FindPropertyRelative("condition")));
			container.Add(new PropertyField(property.FindPropertyRelative("commands")));
			return container;
		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			return property.isExpanded ? base.GetPropertyHeight(property, label) * 6 : base.GetPropertyHeight(property, label);
		}

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel; 
            EditorGUI.indentLevel = 0;

			float baseHeight = base.GetPropertyHeight(property, label);
			float customLabelWidth = 70;
			Rect rect = new Rect(position.x, position.y, position.width, baseHeight);

			property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
			if (property.isExpanded)
			{
				rect.y += baseHeight;
				rect.width -= customLabelWidth;
				Rect labelRect = new Rect(rect.x, rect.y, customLabelWidth, baseHeight);
				rect.x += customLabelWidth;
				EditorGUI.LabelField(labelRect, "Name");
				EditorGUI.PropertyField(rect, property.FindPropertyRelative("name"), GUIContent.none);
				rect.y += baseHeight;
				labelRect.y += baseHeight;
				EditorGUI.LabelField(labelRect, "Tags");
				EditorGUI.PropertyField(rect, property.FindPropertyRelative("tags"), GUIContent.none);
				rect.y += baseHeight;
				labelRect.y += baseHeight;
				EditorGUI.LabelField(labelRect, "Trigger");
				EditorGUI.PropertyField(rect, property.FindPropertyRelative("trigger"), GUIContent.none);
				rect.y += baseHeight;
				labelRect.y += baseHeight;
				EditorGUI.LabelField(labelRect, "Condition");
				EditorGUI.PropertyField(rect, property.FindPropertyRelative("condition"), GUIContent.none);
				rect.y += baseHeight;
				labelRect.y += baseHeight;
				EditorGUI.LabelField(labelRect, "Commands");
				EditorGUI.PropertyField(rect, property.FindPropertyRelative("commands"), GUIContent.none);

			}

			// Calculate rects
			//var rect = new Rect(position.x, position.y, position.width, position.height);

			// Draw fields - passs GUIContent.none to each so they are drawn without labels

			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

	}
            
}
