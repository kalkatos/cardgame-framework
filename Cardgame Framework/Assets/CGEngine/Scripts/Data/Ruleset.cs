using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	[CreateAssetMenu(fileName = "New Ruleset", menuName = "CGEngine/Ruleset", order = 3)]
	public class Ruleset : ScriptableObject
	{
		public string rulesetID;
		public string description;
		public string[] playerRoles;
		public bool freeForAll;
		public string[] playerTeams;
		public Starter starter;
		public int starterRoleIndex;
		public int starterTeamIndex;
		public List<PlayerRules> playerRules;
		public List<ModifierData> matchModifiers;
		public List<Deck> neutralDecks;
		public List<ZoneData> neutralZones;
	}
}