using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameFramework
{
	[CreateAssetMenu(fileName = "New Card", menuName = "Cardgame/Card", order = 2)]
	public class CardData : ScriptableObject
	{
		public new string name = "";
		public string tags = "";
		public List<CardField> fields = new List<CardField>();
		public List<Rule> rules = new List<Rule>();
	}
}
