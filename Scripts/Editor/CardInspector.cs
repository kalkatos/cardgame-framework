using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CardgameCore
{
	[CustomEditor(typeof(CardData)), CanEditMultipleObjects]
	public class CardInspector : Editor
	{
		public GameObject prefab;

		public override void OnInspectorGUI ()
		{
			prefab = (GameObject)EditorGUILayout.ObjectField("Card Prefab", prefab, typeof(GameObject), true);
			if (GUILayout.Button("Instantiate in Scene") && prefab)
			{
				for (int i = 0; i < targets.Length; i++)
				{
					CardData currentTarget = (CardData)targets[i];
					GameObject newCard = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
					if (newCard.TryGetComponent(out Card card))
						card.Set(currentTarget);
					newCard.name = currentTarget.name;
				}
			}
			base.OnInspectorGUI();
		}
	}
}
