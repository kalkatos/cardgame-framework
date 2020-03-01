using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class CardGameData : ScriptableObject
	{
		public string cardgameID;
		public List<string> gameVariableNames;
		public List<string> gameVariableValues;
		public List<Ruleset> rulesets;
	}
}