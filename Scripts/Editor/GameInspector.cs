using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace CardgameCore
{
	[CustomEditor(typeof(Game))]
	public class GameInspector : Editor
	{
		private Game game;
		private SerializedObject gameSO;
		private SerializedProperty rules;
		private ReorderableList rulesList;
		//private int currentRuleNumber;

		public void OnEnable ()
		{
			game = (Game)target;
			gameSO = new SerializedObject(game);
			rules = gameSO.FindProperty("rules");
			rulesList = new ReorderableList(gameSO, rules, true, true, true, true);
			rulesList.drawHeaderCallback = DrawHeader;
			rulesList.drawElementCallback = DrawElement;
			rulesList.elementHeightCallback = ElementHeight;
			rulesList.onAddCallback = AddElement;
			rulesList.onRemoveCallback = RemoveElement;
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
			return prop.isExpanded ? (prop.objectReferenceValue ? 18 * 7 : 18 * 2) : 18;
		}

		private void AddElement (ReorderableList list)
		{
			Undo.RecordObject(target, "Added a Rule to Game");
			game.rules.Add(null);

			AssetDatabase.SaveAssets();
		}

		private void RemoveElement (ReorderableList list)
		{
			Undo.RecordObject(target, "Removed a Rule from Game");
			int removeIndex = list.index;
			if (removeIndex < 0 || removeIndex >= list.count)
				removeIndex = list.count - 1;

			//if (game.rules[removeIndex])
			//	game.rules[removeIndex] = null;
			//else
				game.rules.RemoveAt(removeIndex);

			list.GrabKeyboardFocus();
			AssetDatabase.SaveAssets();
		}

		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI();
			gameSO.Update();
			rulesList.DoLayoutList();
			gameSO.ApplyModifiedProperties();

			//if (GUILayout.Button("Update Rules"))
			//{
			//	for (int i = 0; i < game.rules.Count; i++)
			//	{
			//		if (game.rules[i] == null)
			//			continue;
			//		game.rules[i].myGame = game;
			//		Rule ruleCopy = CreateInstance<Rule>();
			//		ruleCopy.Copy(game.rules[i]);
			//		AssetDatabase.AddObjectToAsset(ruleCopy, game);
			//		game.rules[i] = ruleCopy;
			//	}
			//}

			AssetDatabase.SaveAssetIfDirty(target);
		}
	}
}
