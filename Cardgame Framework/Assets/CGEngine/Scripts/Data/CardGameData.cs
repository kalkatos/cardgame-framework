using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	[CreateAssetMenu(fileName = "New Card Game", menuName = "CGEngine/Card Game Data", order = 1)]
	public class CardGameData : ScriptableObject
	{
		public new string name;
		public List<CardData> allCardsData;
		public GameObject cardTemplate;
		public List<CardField> cardFieldDefinitions;
		public List<Ruleset> rules;
	}
}