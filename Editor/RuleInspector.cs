using UnityEditor;
using UnityEngine;

namespace CardgameFramework.Editor
{
	[CustomEditor(typeof(Rule))]
	public class RuleInspector : UnityEditor.Editor
	{
		private Rule rule;
		private SerializedObject ruleSerialized;

		private void OnEnable ()
		{
			rule = (Rule)target;
			ruleSerialized = new SerializedObject(rule);
			if (rule.game && !rule.game.rules.Contains(rule))
				rule.game = null;
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
				EditorGUILayout.PropertyField(ruleSerialized.FindProperty("game"), GUIContent.none, true, GUILayout.MinWidth(100));
				GUILayout.EndHorizontal();
			}
			if (rule.self == null)
			{
				rule.self = rule;
				ruleSerialized.Update();
			}
			SerializedProperty selfProperty = ruleSerialized.FindProperty("self");
			selfProperty.isExpanded = true;
			EditorGUILayout.PropertyField(selfProperty, GUIContent.none, true, GUILayout.MinWidth(100));
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Tags", GUILayout.Width(70));
			//EditorGUILayout.PropertyField(ruleSerialized.FindProperty("tags"), GUIContent.none, true, GUILayout.MinWidth(100));
			//GUILayout.EndHorizontal();
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Trigger", GUILayout.Width(70));
			//EditorGUILayout.PropertyField(ruleSerialized.FindProperty("trigger"), GUIContent.none, true, GUILayout.MinWidth(100));
			//GUILayout.EndHorizontal();
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Condition", GUILayout.Width(70));
			//EditorGUILayout.PropertyField(ruleSerialized.FindProperty("condition"), GUIContent.none, true, GUILayout.MinWidth(100));
			//GUILayout.EndHorizontal();
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Commands", GUILayout.Width(70));
			//EditorGUILayout.PropertyField(ruleSerialized.FindProperty("commands"), GUIContent.none, true, GUILayout.MinWidth(100));
			//GUILayout.EndHorizontal();
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("ConditionObj", GUILayout.Width(70));
			////EditorGUILayout.PropertyField(ruleSerialized.FindProperty("conditionObject"), GUIContent.none, true, GUILayout.MinWidth(100));
			//ConditionDrawer.Draw(EditorGUILayout.GetControlRect(), rule);
			//GUILayout.EndHorizontal();
			//ruleSerialized.ApplyModifiedProperties();
			//AssetDatabase.SaveAssetIfDirty(target);
		}
	}
}
