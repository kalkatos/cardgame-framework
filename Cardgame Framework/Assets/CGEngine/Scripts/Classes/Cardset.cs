using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[System.Serializable]
	[CreateAssetMenu(fileName = "New Cardset", menuName = "CGEngine/Card Set", order = 2)]
	public class Cardset : ScriptableObject
	{
		public string cardsetID;
		public string description;
		public List<CardField> cardFieldDefinitions;
		public List<CardData> cardsData;
	}
}