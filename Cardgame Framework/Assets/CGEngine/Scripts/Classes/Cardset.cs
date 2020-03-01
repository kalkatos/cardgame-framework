using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[System.Serializable]
	public class Cardset : ScriptableObject
	{
		public string cardsetID;
		public string description;
		public GameObject cardTemplate;
		public List<CardField> cardFieldDefinitions;
		public List<CardData> cardsData;
	}
}