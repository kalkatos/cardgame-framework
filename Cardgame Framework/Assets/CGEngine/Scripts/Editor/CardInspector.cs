using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CardGameFramework
{
	[CustomEditor(typeof(Card)), CanEditMultipleObjects]
	public class CardInspector : Editor
	{
		Card card;
		bool fold = true;

		private void OnEnable ()
		{
			card = (Card)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Current Zone");
			EditorGUILayout.LabelField(card.zone ? card.zone.ToString() : card.transform.parent ? card.transform.parent.name : "<none>");
			EditorGUILayout.EndHorizontal();
			fold = EditorGUILayout.Foldout(fold, "Fields");
			if (card.fields != null && fold)
			{
				foreach (KeyValuePair<string, CardField> item in card.fields)
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(15);
					EditorGUILayout.PrefixLabel(item.Key);
					if (item.Value.dataType == CardFieldDataType.Number)
						EditorGUILayout.LabelField(item.Value.numValue.ToString());
					else if (item.Value.dataType == CardFieldDataType.Text)
						EditorGUILayout.LabelField(item.Value.stringValue);
					else if (item.Value.dataType == CardFieldDataType.Image)
						EditorGUILayout.ObjectField(item.Value.imageValue, typeof(Sprite), false);
					EditorGUILayout.EndHorizontal();
				}
			}
			string buttonLabel = "";
			float angle = 0;
			if (card.transform.rotation.eulerAngles.z == 180)
			{
				buttonLabel = "Face Down";
				angle = 0;
			}
			else
			{
				buttonLabel = "Face Up";
				angle = 180;
			}

			if (GUILayout.Button(buttonLabel))
			{
				Vector3 rotation = card.transform.rotation.eulerAngles;
				rotation.z = angle;
				card.transform.rotation = Quaternion.Euler(rotation);
			}
		}

		[MenuItem("GameObject/Card Game/Card", false, 9)]
		static void CreateZone (MenuCommand menuCommand)
		{
			// Create a card template
			Instantiate(Resources.Load("DefaultCardPrefab") as GameObject);
			
		}
	}
}