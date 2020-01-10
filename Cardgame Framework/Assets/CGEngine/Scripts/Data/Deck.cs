
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	[CreateAssetMenu(fileName = "New Deck", menuName = "CGEngine/Deck", order = 5)]
	public class Deck : ScriptableObject
	{
		public string deckID;
		public new string name;
		public string description;
		public List<CardData> cards;
	}
}