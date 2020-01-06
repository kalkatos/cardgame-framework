
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	[CreateAssetMenu(fileName = "New Player Rules", menuName = "CGEngine/Player Rules", order = 4)]
	public class PlayerRules : ScriptableObject
	{
		public string role;
		public string roleDescription;
		public string team;
		public int quantityNeededOnMatch;
		public bool realtimeTurnWithTeam;
		public bool realtimeTurnWithRole;
		public List<TurnPhase> turnStructure;
		//public List<ZoneData> playerZones;
		//public List<Modifier> playerModifiers;
	}
}