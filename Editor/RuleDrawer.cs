using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CardgameFramework.Editor
{
	[CustomPropertyDrawer(typeof(Rule))]
	public class RuleDrawer : PropertyDrawer
	{
		public static event Action OnRuleSizeChanged;

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
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			// Don't make child fields be indented
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			bool nameChanged = false;
			float baseHeight = base.GetPropertyHeight(property, label);
			float addButtonHeight = baseHeight * 0.7f;
			
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
					Rule rule = property.objectReferenceValue as Rule;
					//EditorGUI.LabelField(labelRect, "Object");
					//EditorGUI.PropertyField(rect, property, GUIContent.none);
					//rect.y += baseHeight;
					//labelRect.y += baseHeight;
					if (ruleSerialized == null)
						ruleSerialized = new SerializedObject(property.objectReferenceValue);
					EditorGUI.LabelField(labelRect, "Name");
					EditorGUI.BeginChangeCheck();
					ruleName = EditorGUI.TextField(rect, property.objectReferenceValue.name);
					if (EditorGUI.EndChangeCheck())
					{
						nameChanged = true;
						ruleSerialized.Update();
						EditorUtility.SetDirty(rule);
					}
					rect.y += baseHeight;
					labelRect.y += baseHeight;
					EditorGUI.LabelField(labelRect, "Tags");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("tags"), GUIContent.none);
					rect.y += baseHeight;
					labelRect.y += baseHeight;

					Rect triggerConditionRect = new Rect(rect);
					rect.x += boxPadding;
					rect.y += boxPadding;
					rect.width -= boxPadding * 2;
					labelRect.y += boxPadding;
					triggerConditionRect.height = baseHeight * 2 * (rule.additionalTriggerConditions.Count + 1) + addButtonHeight;
					triggerConditionRect.height += 2 * boxPadding;
					GUI.Box(triggerConditionRect, GUIContent.none, EditorStyles.helpBox);

					int indexToRemove = -1;
					DrawTriggerConditionPair(ref labelRect, ref rect, rect, ref rule.trigger, ref rule.condition);
					for (int i = 0; i < rule.additionalTriggerConditions.Count; i++)
					{
						rect.y += baseHeight;
						labelRect.y += baseHeight;
						triggerConditionListRect.Set(rect.x, rect.y, rect.width - triggerConditionButtonWidth, rect.height);
						deleteTriggerConditionPairButtonRect.Set(rect.x + triggerConditionListRect.width, rect.y, triggerConditionButtonWidth, baseHeight * 2);
						if (GUI.Button(deleteTriggerConditionPairButtonRect, "X"))
							indexToRemove = i;
						DrawTriggerConditionPair(ref labelRect, ref rect, triggerConditionListRect, ref rule.additionalTriggerConditions[i].trigger, ref rule.additionalTriggerConditions[i].condition);
					}
					if (indexToRemove >= 0)
						rule.additionalTriggerConditions.RemoveAt(indexToRemove);

					addTriggerConditionPairButtonRect.Set(rect.x, rect.y + baseHeight, triggerConditionButtonWidth, addButtonHeight);
					if (GUI.Button(addTriggerConditionPairButtonRect, "+"))
						rule.additionalTriggerConditions.Add(new TriggerConditionPair());

					rect.y += addButtonHeight;
					labelRect.y += addButtonHeight;

					rect.x -= boxPadding;
					rect.width += boxPadding * 2;
					rect.y += boxPadding;
					labelRect.y += boxPadding;

					rect.y += baseHeight;
					labelRect.y += baseHeight;
					EditorGUI.LabelField(labelRect, "Commands");
					EditorGUI.PropertyField(rect, ruleSerialized.FindProperty("commands"), GUIContent.none);
					rect.y += baseHeight;
					labelRect.y += baseHeight;
					EditorGUI.LabelField(labelRect, "ConditionObj");
					ConditionDrawer.Draw(rect, rule);
				}
				//else
				//{
				//	Rect objLabel = new Rect(labelRect);
				//	objLabel.width -= 15;
				//	EditorGUI.LabelField(objLabel, "Object");
				//	Rect buttonRect = new Rect(objLabel.x + objLabel.width, objLabel.y, 15, objLabel.height);
				//	if (GUI.Button(buttonRect, "+"))   //    TODO Move this to the game inspector
				//	{
				//		Rule newRule = ScriptableObject.CreateInstance<Rule>();
				//		string newAssetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
				//		newAssetPath = newAssetPath.Remove(newAssetPath.LastIndexOfAny(new char[] { '/', '\\' }) + 1) + "NewRule.asset";
				//		AssetDatabase.CreateAsset(newRule, newAssetPath);
				//		property.objectReferenceValue = newRule;
				//	}
				//	EditorGUI.PropertyField(rect, property, GUIContent.none);
				//	rect.y += baseHeight;
				//	labelRect.y += baseHeight;
				//}
			}

			if (ruleSerialized != null)
				ruleSerialized.ApplyModifiedProperties();

			if (nameChanged)
			{
				property.objectReferenceValue.name = ruleName;
				AssetDatabase.Refresh();
			}

			// Calculate rects
			//var rect = new Rect(position.x, position.y, position.width, position.height);

			// Draw fields - passs GUIContent.none to each so they are drawn without labels

			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
		}

		private void DrawTriggerConditionPair (ref Rect labelRect, ref Rect rect, Rect drawRect, ref TriggerLabel trigger, ref string condition)
		{
			EditorGUI.LabelField(labelRect, "Trigger");
			trigger = (TriggerLabel)EditorGUI.EnumPopup(drawRect, GUIContent.none, trigger);
			rect.y += drawRect.height;
			drawRect.y += drawRect.height;
			labelRect.y += drawRect.height;
			EditorGUI.LabelField(labelRect, "Condition");
			condition = EditorGUI.TextField(drawRect, GUIContent.none, condition);
		}
	}
}
