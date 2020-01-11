﻿
using UnityEngine;

namespace CardGameFramework
{
	[CreateAssetMenu(fileName = "New Modifier Data", menuName = "CGEngine/Modifier Data", order = 8)]
	public class ModifierData : ScriptableObject
	{
		public string modifierID;  //Defined by Creator
		public string description;
		public string tags;
		public string condition;
		public string trigger;
		public string affected;
		public string trueEffect;
		public string falseEffect;
		public double startingNumValue;
	}
}