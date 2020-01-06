using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
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