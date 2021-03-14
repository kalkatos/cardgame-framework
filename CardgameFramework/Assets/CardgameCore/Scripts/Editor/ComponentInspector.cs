using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CardgameCore
{
    [CustomEditor(typeof(ComponentData)), CanEditMultipleObjects]
    public class ComponentInspector : Editor
    {
		public GameObject prefab;

		public override void OnInspectorGUI()
		{
			prefab = (GameObject)EditorGUILayout.ObjectField("Component Prefab", prefab, typeof(GameObject), true);
			if (GUILayout.Button("Instantiate in Scene") && prefab)
			{
				for (int i = 0; i < targets.Length; i++)
				{
					ComponentData currentTarget = (ComponentData)targets[i];
					GameObject newComponent = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
					if (newComponent.TryGetComponent(out CGComponent comp))
						comp.Set(currentTarget);
					newComponent.name = currentTarget.name;
				}
			}
			base.OnInspectorGUI();
		}

		[MenuItem("CGEngine/Align Cards")]
		public static void AlignObjects ()
		{
			GameObject[] selection = Selection.gameObjects;
			for (int i = 0; i < selection.Length; i++)
			{
				selection[i].transform.position = Vector3.up * 0.05f * i;
			}
		}
	}
}
