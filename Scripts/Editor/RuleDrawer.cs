using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace CardgameCore
{
	[CustomPropertyDrawer(typeof(Rule))]
	public class RuleDrawer : PropertyDrawer
	{
		private SerializedObject ruleSerialized;

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			float baseHeight = base.GetPropertyHeight(property, label);
			if (property.isExpanded)
			{
				if (property.objectReferenceValue)
					return baseHeight * 7;
				return baseHeight * 2;
			}
			return baseHeight;
		}

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.objectReferenceValue)
				label = new GUIContent(property.objectReferenceValue.name);
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

				if (property.objectReferenceValue)
				{
					EditorGUI.LabelField(labelRect, "Object");
					EditorGUI.PropertyField(rect, property, GUIContent.none);
					rect.y += baseHeight;
					labelRect.y += baseHeight;
					if (ruleSerialized == null)
						ruleSerialized = new SerializedObject(property.objectReferenceValue);
					EditorGUI.BeginChangeCheck();
					SerializedProperty nameProperty = ruleSerialized.FindProperty("m_Name");
					EditorGUI.LabelField(labelRect, "Name");
					EditorGUI.PropertyField(rect, nameProperty, GUIContent.none);
					if (EditorGUI.EndChangeCheck())
						AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(property.objectReferenceValue), nameProperty.stringValue);
					rect.y += baseHeight;
					labelRect.y += baseHeight;
					EditorGUI.LabelField(labelRect, "Tags");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("tags"), GUIContent.none);
					rect.y += baseHeight;
					labelRect.y += baseHeight;
					EditorGUI.LabelField(labelRect, "Trigger");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("trigger"), GUIContent.none);
					rect.y += baseHeight;
					labelRect.y += baseHeight;
					EditorGUI.LabelField(labelRect, "Condition");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("condition"), GUIContent.none);
					rect.y += baseHeight;
					labelRect.y += baseHeight;
					EditorGUI.LabelField(labelRect, "Commands");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("commands"), GUIContent.none);
				}
				else
				{
					Rect objLabel = new Rect(labelRect);
					objLabel.width -= 15;
					EditorGUI.LabelField(objLabel, "Object");
					Rect buttonRect = new Rect(objLabel.x + objLabel.width, objLabel.y, 15, objLabel.height);
					if (GUI.Button(buttonRect, "+"))
					{
						Rule newRule = ScriptableObject.CreateInstance<Rule>();
						string newAssetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
						newAssetPath = newAssetPath.Remove(newAssetPath.LastIndexOfAny(new char[] { '/', '\\' }) + 1) + "NewRule.asset";
						AssetDatabase.CreateAsset(newRule, newAssetPath);
						property.objectReferenceValue = newRule;
					}
					EditorGUI.PropertyField(rect, property, GUIContent.none);
					rect.y += baseHeight;
					labelRect.y += baseHeight;
				}
			}

			if (ruleSerialized != null)
				ruleSerialized.ApplyModifiedProperties();

			// Calculate rects
			//var rect = new Rect(position.x, position.y, position.width, position.height);

			// Draw fields - passs GUIContent.none to each so they are drawn without labels

			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
		}
	}
}
