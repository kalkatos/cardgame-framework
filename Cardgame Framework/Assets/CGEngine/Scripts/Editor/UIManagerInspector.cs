using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace CardGameFramework
{
	[CustomEditor(typeof(UIManager))]
	public class UIManagerInspector : Editor
	{
		public const float padding = 4f;
		public const float labelSize = 83f;

		UIManager manager;

		ReorderableList triggerList;

		ReorderableList messageList;
		
		ReorderableList variableWatchingList;

		ReorderableList sfxList;

		private void OnEnable ()
		{
			manager = (UIManager)target;
			float lineHeight = EditorGUIUtility.singleLineHeight;
			//Trigger Events
			triggerList = new ReorderableList(serializedObject, serializedObject.FindProperty("triggerEvents"), true, true, true, true);
			triggerList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Trigger Events"); };
			triggerList.elementHeightCallback = (int index) =>
			{
				return Mathf.Max(lineHeight * (5f + 3.5f * manager.triggerEvents[index].conditionEvent.GetPersistentEventCount()), 140f);
			};
			triggerList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				rect.y += 2f;
				EditorGUI.LabelField(new Rect(rect.x, rect.y, labelSize, lineHeight), "Trigger");
				EditorGUI.PropertyField(
					new Rect(rect.x + labelSize, rect.y, rect.width - labelSize, lineHeight),
					triggerList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("triggerLabel"), GUIContent.none);
				EditorGUI.LabelField(new Rect(rect.x, rect.y + lineHeight + padding, labelSize, lineHeight), "Condition");
				EditorGUI.PropertyField(
					new Rect(rect.x + labelSize, rect.y + lineHeight + padding, rect.width - labelSize, lineHeight),
					triggerList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("condition"), GUIContent.none);
				EditorGUI.PropertyField(
					new Rect(rect.x, rect.y + lineHeight * 2 + padding, rect.width, rect.height),
					triggerList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("conditionEvent"), GUIContent.none);
			};
			triggerList.onAddCallback = (ReorderableList list) =>
			{
				var index = list.serializedProperty.arraySize;
				list.serializedProperty.arraySize++;
				list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("triggerLabel").enumValueIndex = 0;
				list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("condition").stringValue = "";
				serializedObject.ApplyModifiedProperties();
				manager.triggerEvents[index].conditionEvent = null;
			};

			//Message Events
			messageList = new ReorderableList(serializedObject, serializedObject.FindProperty("messageEvents"), true, true, true, true);
			messageList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Message Events"); };
			messageList.elementHeightCallback = (int index) =>
			{
				return Mathf.Max(lineHeight * (3.5f + 3 * manager.messageEvents[index].eventToExecute.GetPersistentEventCount()), 122f);
			};
			messageList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				rect.y += 2f;
				EditorGUI.LabelField(new Rect(rect.x, rect.y, labelSize, lineHeight), "Message");
				EditorGUI.PropertyField(
					new Rect(rect.x + labelSize, rect.y, rect.width - labelSize, lineHeight),
					messageList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("message"), GUIContent.none);
				EditorGUI.PropertyField(
					new Rect(rect.x, rect.y + lineHeight + padding, rect.width, rect.height),
					messageList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("eventToExecute"), GUIContent.none);
			};
			messageList.onAddCallback = (ReorderableList list) =>
			{
				var index = list.serializedProperty.arraySize;
				list.serializedProperty.arraySize++;
				list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("message").stringValue = "";
				serializedObject.ApplyModifiedProperties();
				manager.messageEvents[index].eventToExecute = null;
			};

			//Variable Watcher Text
			variableWatchingList = new ReorderableList(serializedObject, serializedObject.FindProperty("variableDisplayTexts"), true, true, true, true);
			variableWatchingList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "UI Text Elements Watching for Variables"); };
			variableWatchingList.elementHeight = 2 * lineHeight + 2 * padding + 2;
			variableWatchingList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				rect.y += 2f;
				EditorGUI.LabelField(new Rect(rect.x, rect.y, labelSize, lineHeight), "Formatting");
				EditorGUI.PropertyField(
					new Rect(rect.x + labelSize, rect.y, rect.width - labelSize, lineHeight),
					variableWatchingList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("displayFormat"), GUIContent.none);
				EditorGUI.LabelField(new Rect(rect.x, rect.y + lineHeight + padding, labelSize, lineHeight), "UI Text");
				EditorGUI.PropertyField(
					new Rect(rect.x + labelSize, rect.y + lineHeight + padding, rect.width - labelSize, lineHeight),
					variableWatchingList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("uiText"), GUIContent.none);
			};
			variableWatchingList.onAddCallback = (ReorderableList list) =>
			{
				var index = list.serializedProperty.arraySize;
				list.serializedProperty.arraySize++;
				list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("displayFormat").stringValue = "";
				serializedObject.ApplyModifiedProperties();
				manager.variableDisplayTexts[index].uiText = null;
			};

			//Sound Effects
			sfxList = new ReorderableList(serializedObject, serializedObject.FindProperty("messageToSFX"), true, true, true, true);
			sfxList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Message To Random Sound Effects"); };
			sfxList.elementHeightCallback = (int index) =>
			{
				return lineHeight * (3.2f + manager.messageToSFX[index].sfx.Count);
			};
			sfxList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				rect.y += 2f;
				EditorGUI.LabelField(new Rect(rect.x, rect.y, labelSize, lineHeight), "Message");
				EditorGUI.PropertyField(
					new Rect(rect.x + labelSize, rect.y, rect.width - labelSize, lineHeight),
					sfxList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("message"), GUIContent.none);
				EditorGUI.LabelField(new Rect(rect.x, rect.y + lineHeight + padding, labelSize, lineHeight), "Sound Effects");
				SerializedProperty clipArray = sfxList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("sfx");
				int removeAt = -1;
				for (int i = 0; i <= clipArray.arraySize; i++)
				{
					if (i == clipArray.arraySize)
					{
						if (GUI.Button(new Rect(rect.x + labelSize, rect.y + lineHeight + lineHeight * i + padding + padding + i, rect.width - labelSize, lineHeight), "+"))
						{
							clipArray.arraySize++;
						}
					}
					else
					{
						EditorGUI.PropertyField(
						new Rect(rect.x + labelSize, rect.y + lineHeight + lineHeight * i + padding + padding + i, rect.width - labelSize - 15, lineHeight),
						clipArray.GetArrayElementAtIndex(i), GUIContent.none);
						
						if (GUI.Button(new Rect(rect.x + rect.width - 15, rect.y + lineHeight + lineHeight * i + padding + padding + i, 15, lineHeight), "–"))
						{
							removeAt = i;
						}
					}
				}
				if (removeAt >= 0)
					clipArray.DeleteArrayElementAtIndex(removeAt);

			};
			sfxList.onAddCallback = (ReorderableList list) =>
			{
				var index = list.serializedProperty.arraySize;
				list.serializedProperty.arraySize++;
				list.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("message").stringValue = "";
				serializedObject.ApplyModifiedProperties();
				manager.messageToSFX[index].sfx.Clear();
			};

			//InputEvents
			
		}

		public override void OnInspectorGUI ()
		{
			GUILayout.Space(15);
			serializedObject.Update();
			triggerList.DoLayoutList();
			messageList.DoLayoutList();
			variableWatchingList.DoLayoutList();
			sfxList.DoLayoutList();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onPointerClickEvent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onPointerEnterEvent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onPointerExitEvent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onPointerDownEvent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onPointerUpEvent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onBeginDragEvent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onDragEvent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onEndDragEvent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onDropEvent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("onScrollEvent"));
			serializedObject.ApplyModifiedProperties();
		}
	}

	[CustomPropertyDrawer(typeof(ConditionedEvent))]
	public class ConditionedEventDrawer : PropertyDrawer
	{
		public override void OnGUI (Rect rect, SerializedProperty property, GUIContent label)
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;
			float padding = UIManagerInspector.padding;
			float labelSize = UIManagerInspector.labelSize;

			EditorGUI.BeginProperty(rect, label, property);
			int eventCount = property.FindPropertyRelative("conditionEvent").FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize;
			float eventRectHeight = 2.6f * eventCount;
			rect.height += 200;
			//GUILayout.Space(Mathf.Max(lineHeight * (2.6f + eventRectHeight), lineHeight * 5.2f));

			EditorGUI.LabelField(new Rect(rect.x, rect.y, labelSize, lineHeight), "Condition");
			EditorGUI.PropertyField(
				new Rect(rect.x + labelSize, rect.y, rect.width - labelSize, lineHeight),
				property.FindPropertyRelative("condition"), GUIContent.none);
			EditorGUI.PropertyField(
				new Rect(rect.x, rect.y + lineHeight + padding, rect.width, eventRectHeight),
				property.FindPropertyRelative("conditionEvent"), GUIContent.none);

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;
			int eventCount = property.FindPropertyRelative("conditionEvent").FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize;
			float eventRectHeight = 2.6f * eventCount;
			return Mathf.Max(lineHeight * (2.6f + eventRectHeight), lineHeight * 5.2f);
		}
	}

	[CustomPropertyDrawer(typeof(TriggeredConditionedEvent))]
	public class TriggeredConditionedEventDrawer : ConditionedEventDrawer
	{
		public override void OnGUI (Rect rect, SerializedProperty property, GUIContent label)
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;
			float padding = UIManagerInspector.padding;
			float labelSize = UIManagerInspector.labelSize;

			EditorGUI.BeginProperty(rect, label, property);
			EditorGUI.LabelField(new Rect(rect.x, rect.y, labelSize, lineHeight), "Trigger");
			EditorGUI.PropertyField(
				new Rect(rect.x + labelSize, rect.y, rect.width - labelSize, lineHeight),
				property.FindPropertyRelative("triggerLabel"), GUIContent.none);
			rect.y += lineHeight + padding;
			rect.height += lineHeight + padding;
			EditorGUI.EndProperty();
			base.OnGUI(rect, property, label);
		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			float lineHeight = EditorGUIUtility.singleLineHeight;
			float padding = UIManagerInspector.padding;
			return 2.5f * lineHeight + padding + base.GetPropertyHeight(property, label);
		}
	}
}