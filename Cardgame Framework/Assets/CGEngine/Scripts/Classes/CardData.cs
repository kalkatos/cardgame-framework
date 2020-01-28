using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[CreateAssetMenu(fileName = "New Card Data", menuName = "CGEngine/Card Data", order = 2)]
	public class CardData : ScriptableObject
	{
		public string cardDataID;  //Defined by Creator
		public string tags;
		public List<CardField> fields;
		public List<ModifierData> cardModifiers;
	}
}