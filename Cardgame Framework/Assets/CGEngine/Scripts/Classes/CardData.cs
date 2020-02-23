using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[CreateAssetMenu(fileName = "New Card", menuName = "CGEngine/Card", order = 2)]
	public class CardData : ScriptableObject
	{
		public string cardDataID;
		public string tags;
		public List<CardField> fields;
		public List<RuleData> cardRules; 
	}
}