using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace CardGameFramework
{
	public class AssetHandler
	{
		[OnOpenAsset()]
		public static bool OpenEditor (int instanceId, int line)
		{
			CardGameData obj = EditorUtility.InstanceIDToObject(instanceId) as CardGameData;
			if (obj != null)
			{
				CardGameWindow.ShowWindow();
				return true;
			}
			return false;
		}
	}

	[CustomEditor(typeof(CardGameData))]
	public class CardGameInpector : Editor
	{
		public override void OnInspectorGUI ()
		{
			if (GUILayout.Button("Open Card Game Window"))
			{
				CardGameWindow.ShowWindow();
			}
		}
	}
}