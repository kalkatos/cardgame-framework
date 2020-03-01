using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class CardData : ScriptableObject
	{
		public string cardDataID;
		public string tags;
		public List<CardField> fields;
		public List<RuleData> cardRules; 
	}
}