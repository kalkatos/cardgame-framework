using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	//[CreateAssetMenu(fileName = "New Ruleset", menuName = "CGEngine/Ruleset", order = 3)]
	[System.Serializable]
	public class Ruleset
	{
		public string rulesetID;
		public string description;
		public string turnStructure;
		public List<ModifierData> matchModifiers;
	}
}