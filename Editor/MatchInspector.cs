using UnityEditor;
using UnityEngine;

namespace CardgameFramework.Editor
{
	[CustomEditor(typeof(Match))]
	public class MatchInspector : UnityEditor.Editor
	{
		private Match match;
		private string[] variableNames;

		private void OnEnable ()
		{
			match = (Match)target;
			variableNames = match.GetAllVariableNames();
		}

		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI();
			if (!Application.isPlaying)
				return;
			GUILayout.Space(20);
			EditorGUILayout.LabelField("Game Variables", EditorStyles.boldLabel);
			if (GUILayout.Button("Force Refresh"))
				variableNames = match.GetAllVariableNames();
			foreach (string name in variableNames)
				EditorGUILayout.LabelField($"{name} = {Match.GetVariable(name)}");
		}
	}
}
