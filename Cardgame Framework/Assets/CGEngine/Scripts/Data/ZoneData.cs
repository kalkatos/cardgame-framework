using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[CreateAssetMenu(fileName = "New Zone Data", menuName = "CGEngine/Zone Data", order = 7)]
	public class ZoneData : ScriptableObject
	{
		public string zoneType;
		public RevealStatus revealStatus;
		public ZoneConfiguration zoneConfig;
		public int gridRows;
		public int gridColumns;
	}
}