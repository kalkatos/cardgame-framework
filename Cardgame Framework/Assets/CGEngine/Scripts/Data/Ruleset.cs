using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[CreateAssetMenu(fileName = "New Ruleset", menuName = "CGEngine/Ruleset", order = 3)]
	public class Ruleset : ScriptableObject
	{
		public string rulesetID;
		public string description;
		public bool freeForAll;
		public List<string> playerTeams;
		//public Starter starter;
		//public int starterRoleIndex;
		//public int starterTeamIndex;
		//public List<PlayerRole> playerRoles;
		public string turnStructure;
		public List<ModifierData> matchModifiers;
	}
}