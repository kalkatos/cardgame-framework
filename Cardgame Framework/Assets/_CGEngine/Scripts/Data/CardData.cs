using UnityEngine;

namespace CGEngine
{
	[CreateAssetMenu(fileName = "New Card Data", menuName = "CGEngine/Card Data", order = 2)]
	public class CardData : ScriptableObject
	{
		public string cardDataID;  //Defined by Creator
		public new string name;
		public string tags;
		public CardField[] fields;
		public ModifierData[] cardModifiers;
	}
}