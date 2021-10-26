using UnityEditor;
using UnityEngine;

namespace CardgameCore
{
	[CustomEditor(typeof(Rule))]
	public class RuleInspector : Editor
	{
		private Rule rule;
		private SerializedObject ruleSerialized;

		private void OnEnable ()
		{
			rule = (Rule)target;
			ruleSerialized = new SerializedObject(rule);
			if (rule.myGame && !rule.myGame.rules.Contains(rule))
				rule.myGame = null;
			rule.conditionObject = new NestedConditions(rule.condition);
		}

		public override void OnInspectorGUI ()
		{
			using (new EditorGUI.DisabledScope(true))
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Script", GUILayout.Width(70));
				EditorGUILayout.PropertyField(ruleSerialized.FindProperty("m_Script"), GUIContent.none, true, GUILayout.MinWidth(100));
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Game", GUILayout.Width(70));
				EditorGUILayout.PropertyField(ruleSerialized.FindProperty("myGame"), GUIContent.none, true, GUILayout.MinWidth(100));
				GUILayout.EndHorizontal();
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label("Tags", GUILayout.Width(70));
			EditorGUILayout.PropertyField(ruleSerialized.FindProperty("tags"), GUIContent.none, true, GUILayout.MinWidth(100));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Trigger", GUILayout.Width(70));
			EditorGUILayout.PropertyField(ruleSerialized.FindProperty("trigger"), GUIContent.none, true, GUILayout.MinWidth(100));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Condition", GUILayout.Width(70));
			EditorGUILayout.PropertyField(ruleSerialized.FindProperty("condition"), GUIContent.none, true, GUILayout.MinWidth(100));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Commands", GUILayout.Width(70));
			EditorGUILayout.PropertyField(ruleSerialized.FindProperty("commands"), GUIContent.none, true, GUILayout.MinWidth(100));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("ConditionObj", GUILayout.Width(70));
			//EditorGUILayout.PropertyField(ruleSerialized.FindProperty("conditionObject"), GUIContent.none, true, GUILayout.MinWidth(100));
			ConditionDrawer.Draw(EditorGUILayout.GetControlRect(), rule);
			GUILayout.EndHorizontal();
			AssetDatabase.SaveAssetIfDirty(target);
		}
	}
}
