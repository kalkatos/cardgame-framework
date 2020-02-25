using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[CreateAssetMenu(fileName = "New Card Game Data", menuName = "CGEngine/Card Game Data", order = 1)]
	public class CardGameData : ScriptableObject
	{
		public string cardgameID;
		public List<string> gameVariableNames;
		public List<string> gameVariableValues;
		public List<Ruleset> rulesets;
		//public List<Cardset> cardsets;
	}
}