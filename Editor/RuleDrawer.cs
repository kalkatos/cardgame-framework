using UnityEngine;
using UnityEditor;

namespace CardgameFramework.Editor
{
	[CustomPropertyDrawer(typeof(Rule))]
	public class RuleDrawer : PropertyDrawer
	{
		private const float customLabelWidth = 70;
		private const float boxPadding = 5f;
		private const float triggerConditionButtonWidth = 25f;

		private SerializedObject ruleSerialized;
		private string ruleName;

		private Rect addTriggerConditionPairButtonRect = new Rect();
		private Rect deleteTriggerConditionPairButtonRect = new Rect();
		private Rect triggerConditionListRect = new Rect();

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			float baseHeight = base.GetPropertyHeight(property, label);
			float additionalHeight = boxPadding * 2;
			if (property.isExpanded)
			{
				int multiplier = 2;
				if (property.objectReferenceValue != null)
				{
					Rule rule = property.objectReferenceValue as Rule;
					multiplier += 6 + rule.additionalTriggerConditions.Count * 2;
				}
				return baseHeight * multiplier + additionalHeight;
			}
			return baseHeight;
		}

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.objectReferenceValue)
				label = new GUIContent(property.objectReferenceValue.name);

			EditorGUI.BeginProperty(position, label, property);

			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			float baseHeight = base.GetPropertyHeight(property, label);
			float addButtonHeight = baseHeight * 0.7f;
			Rect rect = new Rect(position.x, position.y, position.width, baseHeight);
			property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
			if (property.isExpanded)
			{
				NewLine(baseHeight, ref rect);
				rect.width -= customLabelWidth;
				Rect labelRect = new Rect(rect.x, rect.y, customLabelWidth, baseHeight);
				rect.x += customLabelWidth;
				if (property.objectReferenceValue)
				{
					Rule rule = property.objectReferenceValue as Rule;
					if (ruleSerialized == null)
						ruleSerialized = new SerializedObject(property.objectReferenceValue);
					EditorGUI.LabelField(labelRect, "Name");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("m_Name"), GUIContent.none);
					NewLine(baseHeight, ref rect, ref labelRect);
					EditorGUI.LabelField(labelRect, "Tags");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("tags"), GUIContent.none);
					NewLine(baseHeight, ref rect, ref labelRect);
					Rect triggerConditionRect = new Rect(rect);
					rect.x += boxPadding;
					rect.width -= boxPadding * 2;
					NewLine(boxPadding, ref rect, ref labelRect);
					triggerConditionRect.height = baseHeight * 2 * (rule.additionalTriggerConditions.Count + 1) + addButtonHeight;
					triggerConditionRect.height += 2 * boxPadding;
					GUI.Box(triggerConditionRect, GUIContent.none, EditorStyles.helpBox);
					int indexToRemove = -1;
					EditorGUI.LabelField(labelRect, "Trigger");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("trigger"), GUIContent.none);
					NewLine(baseHeight, ref rect, ref labelRect);
					EditorGUI.LabelField(labelRect, "Condition");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("condition"), GUIContent.none);
					SerializedProperty triggerConditionListProp = ruleSerialized.FindProperty("additionalTriggerConditions");
					for (int i = 0; i < triggerConditionListProp.arraySize; i++)
					{
						NewLine(baseHeight, ref rect, ref labelRect);
						triggerConditionListRect.Set(rect.x, rect.y, rect.width - triggerConditionButtonWidth, rect.height);
						deleteTriggerConditionPairButtonRect.Set(rect.x + triggerConditionListRect.width, rect.y, triggerConditionButtonWidth, baseHeight * 2);
						if (GUI.Button(deleteTriggerConditionPairButtonRect, "X"))
							indexToRemove = i;
						SerializedProperty element = triggerConditionListProp.GetArrayElementAtIndex(i);
						EditorGUI.LabelField(labelRect, "Trigger");
						EditorGUI.PropertyField(triggerConditionListRect, element.FindPropertyRelative("trigger"), GUIContent.none);
						NewLine(baseHeight, ref rect, ref labelRect, ref triggerConditionListRect);
						EditorGUI.LabelField(labelRect, "Condition");
						EditorGUI.PropertyField(triggerConditionListRect, element.FindPropertyRelative("condition"), GUIContent.none);
					}
					if (indexToRemove >= 0)
						triggerConditionListProp.DeleteArrayElementAtIndex(indexToRemove);
					addTriggerConditionPairButtonRect.Set(rect.x, rect.y + baseHeight, triggerConditionButtonWidth, addButtonHeight);
					if (GUI.Button(addTriggerConditionPairButtonRect, "+"))
						triggerConditionListProp.InsertArrayElementAtIndex(triggerConditionListProp.arraySize);
					NewLine(addButtonHeight, ref rect, ref labelRect);
					rect.x -= boxPadding;
					rect.width += boxPadding * 2;
					NewLine(boxPadding, ref rect, ref labelRect);
					NewLine(baseHeight, ref rect, ref labelRect);
					EditorGUI.LabelField(labelRect, "Commands");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("commands"), GUIContent.none);
					NewLine(baseHeight, ref rect, ref labelRect);
					EditorGUI.LabelField(labelRect, "ConditionObj");
					ConditionDrawer.Draw(rect, rule);
				}
			}

			if (ruleSerialized != null)
				ruleSerialized.ApplyModifiedProperties();

			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
		}

		private void NewLine (float height, ref Rect rect1)
		{
			rect1.y += height;
		}

		private void NewLine (float height, ref Rect rect1, ref Rect rect2)
		{
			rect1.y += height;
			rect2.y += height;
		}

		private void NewLine (float height, ref Rect rect1, ref Rect rect2, ref Rect rect3)
		{
			rect1.y += height;
			rect2.y += height;
			rect3.y += height;
		}
	}
}
