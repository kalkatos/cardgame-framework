using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace CardgameCore
{
	[CustomPropertyDrawer(typeof(NestedConditions))]
	public class ConditionDrawer : PropertyDrawer
	{
		private SerializedObject conditionSerialized;

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			if (property.objectReferenceValue)
			{
				if (conditionSerialized == null)
					conditionSerialized = new SerializedObject(property.objectReferenceValue);
				EditorGUI.PropertyField(position, conditionSerialized.FindProperty("myString"), GUIContent.none);
			}
			else
				EditorGUI.LabelField(position, "(empty)");

			EditorGUI.EndProperty();
		}
	}

		//[CustomPropertyDrawer(typeof(Condition))]
		//public class ConditionDrawer : PropertyDrawer
		//{
		//	public override VisualElement CreatePropertyGUI (SerializedProperty property)
		//	{
		//		var container = new VisualElement();
		//		container.Add(new PropertyField(property.FindPropertyRelative("Value")));
		//		return container;
		//	}

		//	//public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		//	//{
		//	//	return (property.isExpanded ? base.GetPropertyHeight(property, label) * 6 : base.GetPropertyHeight(property, label)) + 4;
		//	//}

		//	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
		//	{
		//		EditorGUI.BeginProperty(position, label, property);

		//		// Don't make child fields be indented
		//		var indent = EditorGUI.indentLevel;
		//		EditorGUI.indentLevel = 0;

		//		EditorGUI.PropertyField(position, property.FindPropertyRelative("V alue"), GUIContent.none);

		//		// Set indent back to what it was
		//		EditorGUI.indentLevel = indent;

		//		EditorGUI.EndProperty();
		//	}
		//}
}
