using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[System.Serializable]
	public class Cardset
	{
		public string cardsetID;
		public string description;
		public List<CardField> cardFieldDefinitions;
		public List<CardData> cardsData;
	}
}