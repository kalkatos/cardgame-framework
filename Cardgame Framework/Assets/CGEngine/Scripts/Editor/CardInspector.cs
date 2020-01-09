using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CGEngine
{
	[CustomEditor(typeof(Card))]
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
	}
}