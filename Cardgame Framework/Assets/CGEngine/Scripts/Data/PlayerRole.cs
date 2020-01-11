
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[System.Serializable]
	public class PlayerRole 
	{
		public string roleName;
		public string roleDescription;
		public string team;
		public int quantityNeededOnMatch;
		//public bool realtimeTurnWithTeam;
		//public bool realtimeTurnWithRole;
		public string turnStructure;
		//public List<ZoneData> playerZones;
		//public List<Modifier> playerModifiers;
	}
}