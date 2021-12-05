using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

namespace CardgameFramework.Editor
{
	[CustomEditor(typeof(Game))]
	public class GameInspector : UnityEditor.Editor
	{
		private Game game;
		private SerializedObject gameSO;
		private SerializedProperty rules;
		private ReorderableList rulesList;

		public void OnEnable ()
		{
			game = (Game)target;
			//for (int i = 0; i < game.rules.Count; i++)
			//	if (game.rules[i].game == game)
			gameSO = new SerializedObject(game);
			rules = gameSO.FindProperty("rules");
			CreateNestedConditions();

			rulesList = new ReorderableList(gameSO, rules, true, true, true, true);
			rulesList.drawHeaderCallback = DrawHeader;
			rulesList.drawElementCallback = DrawElement;
			rulesList.elementHeightCallback = ElementHeight;
			rulesList.onAddCallback = AddElement;
			rulesList.onRemoveCallback = RemoveElement;

			for (int index = 0; index < game.rules.Count; index++)
			{
				Rule rule = game.rules[index];
				rule.game = game;
				if (!AssetDatabase.IsSubAsset(rule))
					AssetDatabase.AddObjectToAsset(rule, game);
			}
		}

		private void DrawHeader (Rect rect)
		{
			Rect labelRect = new Rect(rect);
			labelRect.width -= 150;
			EditorGUI.LabelField(labelRect, "Rules");
			//Expand All Button
			Rect buttonRect = new Rect(rect.x + labelRect.width, rect.y, 75, rect.height);
			if (GUI.Button(buttonRect, "Expand All"))
			{
				for (int i = 0; i < game.rules.Count; i++)
					rulesList.serializedProperty.GetArrayElementAtIndex(i).isExpanded = true;
				InternalEditorUtility.RepaintAllViews();
				ActiveEditorTracker.sharedTracker.ForceRebuild();
				
			}
			//Collapse All Button
			buttonRect = new Rect(rect.x + labelRect.width + 75, rect.y, 75, rect.height);
			if (GUI.Button(buttonRect, "Collapse All"))
			{
				for (int i = 0; i < game.rules.Count; i++)
					rulesList.serializedProperty.GetArrayElementAtIndex(i).isExpanded = false;
				InternalEditorUtility.RepaintAllViews();
				ActiveEditorTracker.sharedTracker.ForceRebuild();
			}
			//Dropable
			Event evt = Event.current;
			switch (evt.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:
					if (!rect.Contains(evt.mousePosition))
						return;
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					if (evt.type == EventType.DragPerform)
					{
						Undo.RecordObject(target, "Added Rules to Game");
						DragAndDrop.AcceptDrag();
						foreach (Object item in DragAndDrop.objectReferences)
						{
							if (item is Rule)
								game.rules.Add((Rule)item);
						}
					}
					break;
			}
		}

		private void DrawElement (Rect rect, int index, bool isActive, bool isFocused)
		{
			rect.x += 8;
			rect.width -= 8;
			EditorGUI.PropertyField(rect, rulesList.serializedProperty.GetArrayElementAtIndex(index));
		}

		private float ElementHeight (int index)
		{
			SerializedProperty prop = rulesList.serializedProperty.GetArrayElementAtIndex(index);
			return prop.isExpanded ? (prop.objectReferenceValue ? 18 * 8 : 18 * 2) : 18;
		}

		private void AddElement (ReorderableList list)
		{
			Undo.RecordObject(target, "Rule Added");
			game.rules.Add(null);

			AssetDatabase.SaveAssets();
		}

		private void RemoveElement (ReorderableList list)
		{
			Undo.RecordObject(target, "Rule Removed");
			int removeIndex = list.index;
			if (removeIndex < 0 || removeIndex >= list.count)
				removeIndex = list.count - 1;

			game.rules.RemoveAt(removeIndex);

			list.GrabKeyboardFocus();
			AssetDatabase.SaveAssets();
		}

		private void CreateNestedConditions ()
		{
			for (int i = 0; i < game.rules.Count; i++)
			{
				Rule rule = game.rules[i];
				rule.conditionObject = new NestedConditions(rule.condition);
			}
		}

		public override void OnInspectorGUI ()
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(gameSO.FindProperty("m_Script"));
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginChangeCheck();
			SerializedProperty nameProperty = gameSO.FindProperty("m_Name");
			EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name"));
			if (EditorGUI.EndChangeCheck())
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(game), nameProperty.stringValue);
			EditorGUILayout.PropertyField(gameSO.FindProperty("phases"));
			EditorGUILayout.PropertyField(gameSO.FindProperty("variablesAndValues"));
			gameSO.Update();
			rulesList.DoLayoutList();
			gameSO.ApplyModifiedProperties();

			//if (GUILayout.Button("Create Nested Conditions"))
			//	CreateNestedConditions();

			AssetDatabase.SaveAssetIfDirty(target);
		}
	}
}
