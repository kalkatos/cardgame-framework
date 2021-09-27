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
			rules = gameSO.FindProperty("rulesSO");
			rulesList = new ReorderableList(gameSO, rules, true, true, true, true);
			rulesList.drawHeaderCallback = DrawHeader;
			rulesList.drawElementCallback = DrawElement;
			rulesList.elementHeightCallback = ElementHeight;
			rulesList.onAddCallback = AddElement;
			rulesList.onRemoveCallback = RemoveElement;
		}

		private void DrawHeader (Rect rect)
		{
			EditorGUI.LabelField(rect, "Rules");
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
			game.rules.Add(null);

			Undo.SetCurrentGroupName("Add Element To Array");
			AssetDatabase.SaveAssets();
		}

		private void RemoveElement (ReorderableList list)
		{
			int removeIndex = list.index;
			if (removeIndex < 0 || removeIndex >= list.count)
				removeIndex = list.count - 1;

			if (game.rules[removeIndex])
				game.rules[removeIndex] = null;
			else
				game.rules.RemoveAt(removeIndex);

			list.GrabKeyboardFocus();
			Undo.SetCurrentGroupName("Remove Element From Array");
			AssetDatabase.SaveAssets();
		}

		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI();
			//if (currentRuleNumber != game.rulesSO.Count)
			//{
			//	if (currentRuleNumber < game.rulesSO.Count)
			//	{
			//		for (int i = 0; i < game.rulesSO.Count; i++)
			//		{
			//			if (game.rulesSO[i] == null)
			//			{
			//				game.rulesSO[i] = CreateInstance<RuleSO>();
			//				game.rulesSO[i].name = "NewRule";
			//				AssetDatabase.AddObjectToAsset(game.rulesSO[i], game);
			//			}
			//			currentRuleNumber++;
			//		}
			//	}
			//	else
			//	{
			//		Object[] subObjects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(game));
			//		if (subObjects.Length > 1)
			//		{
			//			for (int i = 0; i < subObjects.Length; i++)
			//			{
			//				if (subObjects[i] == game)
			//					continue;
			//				if (!game.rulesSO.Contains((RuleSO)subObjects[i]))
			//				{
			//					DestroyImmediate(subObjects[i], true);
			//					game.rulesSO.Remove((RuleSO)subObjects[i]);
			//					currentRuleNumber--;
			//				}
			//			}
			//			currentRuleNumber = subObjects.Length - 1;
			//		}
			//	}
			//}
			gameSO.Update();
			rulesList.DoLayoutList();
			gameSO.ApplyModifiedProperties();

			//if (!Application.isPlaying && game.rules.Count > 0)
			//{
			//	if (GUILayout.Button("Create RuleSO's"))
			//	{
			//		game.rulesSO.Clear();
			//		string gamePath = AssetDatabase.GetAssetPath(game);
			//		Object[] subObjects = AssetDatabase.LoadAllAssetsAtPath(gamePath);
			//		char[] slashChars = new char[] { '/', '\\' };
			//		if (subObjects.Length > 1)
			//		{
			//			for (int i = 0; i < subObjects.Length; i++)
			//				if (subObjects[i] != game)
			//					DestroyImmediate(subObjects[i], true);
			//		}
			//		for (int i = 0; i < game.rules.Count; i++)
			//		{
			//			RuleSO newRuleSO = CreateInstance<RuleSO>();
			//			game.rulesSO.Add(newRuleSO);
			//			newRuleSO.name = game.rules[i].name;
			//			newRuleSO.id = game.rules[i].id;
			//			newRuleSO.tags = game.rules[i].tags;
			//			newRuleSO.trigger = game.rules[i].trigger;
			//			newRuleSO.condition = game.rules[i].condition;
			//			newRuleSO.commands = game.rules[i].commands;
			//			string rulePath = gamePath.Remove(gamePath.LastIndexOfAny(slashChars) + 1) + newRuleSO.name + ".asset";
			//			AssetDatabase.CreateAsset(newRuleSO, rulePath);
			//			AssetDatabase.AddObjectToAsset(newRuleSO, game);
			//		}
			//		AssetDatabase.SaveAssets();
			//	}
			//}
			AssetDatabase.SaveAssetIfDirty(target);
		}
	}
}
