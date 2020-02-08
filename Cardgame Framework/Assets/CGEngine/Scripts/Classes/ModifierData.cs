
using UnityEngine;

namespace CardGameFramework
{
	//[CreateAssetMenu(fileName = "New Modifier Data", menuName = "CGEngine/Modifier Data", order = 8)]
	[System.Serializable]
	public class ModifierData
	{
		public string modifierID;  //Defined by Creator
		public string tags;
		public string trigger;
		public string condition;
		public string commands;
	}

}