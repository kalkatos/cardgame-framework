﻿using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	[CreateAssetMenu(fileName = "New Card Game Data", menuName = "CGEngine/Card Game Data", order = 1)]
	public class CardGameData : ScriptableObject
	{
		public string cardgameID;
		public List<CardData> allCardsData;
		public GameObject cardTemplate;
		public List<CardField> cardFieldDefinitions;
		public List<Ruleset> rules;
	}
}