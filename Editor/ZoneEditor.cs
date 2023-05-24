using UnityEngine;
using UnityEditor;

namespace CardgameFramework
{
	[CustomEditor(typeof(Zone)), CanEditMultipleObjects]
	public class ZoneEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Organize Child Cards"))
				for (int i = 0; i < targets.Length; i++)
				{
					((Zone)targets[i]).GetCardsInChildren();
					EditorUtility.SetDirty(targets[i]);
				}

			if (GUILayout.Button("Shuffle"))
				for (int i = 0; i < targets.Length; i++)
					((Zone)targets[i]).Shuffle();

			if (GUILayout.Button("Delete All"))
				for (int i = 0; i < targets.Length; i++)
					((Zone)targets[i]).DeleteAll();

			if (GUILayout.Button("Sort"))
				for (int i = 0; i < targets.Length; i++)
				{
					((Zone)targets[i]).Sort();
					EditorUtility.SetDirty(targets[i]);
				}
		}
	}
}
