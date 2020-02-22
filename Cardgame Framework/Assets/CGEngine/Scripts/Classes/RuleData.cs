
using UnityEngine;

namespace CardGameFramework
{
	//[CreateAssetMenu(fileName = "New Rule Data", menuName = "CGEngine/Rule Data", order = 8)]
	[System.Serializable]
	public class RuleData
	{
		public string ruleID;  //Defined by Creator
		public string tags;
		public string trigger;
		public string condition;
		public string commands;
	}

}