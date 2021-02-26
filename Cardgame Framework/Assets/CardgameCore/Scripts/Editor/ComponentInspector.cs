using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CardgameCore
{
    [CustomEditor(typeof(ComponentData)), CanEditMultipleObjects]
    public class ComponentInspector : Editor
    {
		public override void OnInspectorGUI()
		{
			if (GUILayout.Button("Instantiate in Scene"))
			{
				for (int i = 0; i < targets.Length; i++)
				{
					ComponentData currentTarget = (ComponentData)targets[i];
					Component newComponent = new GameObject(currentTarget.name).AddComponent<Component>();
					newComponent.Set(currentTarget);
				}
			}
			base.OnInspectorGUI();
		}
	}
}
