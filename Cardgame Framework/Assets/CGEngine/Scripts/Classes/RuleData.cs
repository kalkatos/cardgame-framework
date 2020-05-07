
using UnityEngine;

namespace CardGameFramework
{
	[System.Serializable]
	public class RuleData
	{
		public string ruleID;  //Defined by Creator
		public string tags;
		public string trigger;
		public string condition;
		public string commands;
		public string elseCommands;
	}

}