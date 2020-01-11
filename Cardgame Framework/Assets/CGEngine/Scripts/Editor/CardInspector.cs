using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CardGameFramework
{
	[CustomEditor(typeof(CardData))]
	public class CardInspector : Editor
	{
		/*
		Card card;
		string dataName;

		private void OnEnable()
		{
			card = (Card)target;
			if (card.data) dataName = card.data.name;
			else dataName = "";
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (card.data && card.data.name != dataName)
			{
				card.SetupData();
				dataName = card.data.name;
			}
		}
		*/
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