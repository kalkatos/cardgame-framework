﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CardGameFramework
{
	[CustomEditor(typeof(Card))]
	public class CardInspector : Editor
	{
		Card card;
		bool fold;

		private void OnEnable()
		{
			card = (Card)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (fold = EditorGUILayout.Foldout(fold, "Fields"))
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
			
		}
		
		//public override void OnInspectorGUI()
		//{
		//	GUILayout.Label("Please refer to \"CGEngine > Cardgame Definitions\" for editing.");
		//	if (GUILayout.Button("Open Cardgame Definitions"))
		//	{
		//		EditorWindow.GetWindow<CardgameWindow>("Cardgame Definitions");
		//	}
		//}
	}
}