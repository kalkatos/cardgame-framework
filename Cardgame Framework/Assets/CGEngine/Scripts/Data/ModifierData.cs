
using UnityEngine;

namespace CardGameFramework
{
	//[CreateAssetMenu(fileName = "New Modifier Data", menuName = "CGEngine/Modifier Data", order = 8)]
	[System.Serializable]
	public class ModifierData
	{
		public string modifierID;  //Defined by Creator
		//public int modType; // 0 = trigger,  1 = number
		public string tags;
		public string condition;
		public string trigger;
		public string affected;
		public string trueEffect;
		public string falseEffect;
		//public float startingNumValue;
		//public float minValue = -9999;
		//public float maxValue = 9999;
	}
}