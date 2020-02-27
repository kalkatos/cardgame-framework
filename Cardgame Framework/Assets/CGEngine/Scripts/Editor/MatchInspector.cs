using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CardGameFramework
{
	[CustomEditor(typeof(Match))]
	public class MatchInspector : Editor
	{
		Match match;
		bool fold;

		private void OnEnable()
		{
			match = (Match)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (fold = EditorGUILayout.Foldout(fold, "Variables"))
			{
				foreach (KeyValuePair<string, object> item in match.variables)
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(15);
					EditorGUILayout.PrefixLabel(item.Key);
					if (item.Value != null)
						EditorGUILayout.LabelField(item.Value.ToString());
					else
						EditorGUILayout.LabelField("<null>");
					EditorGUILayout.EndHorizontal();
				}
			}
		}
	}
}