using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	public class ObjectManager : MonoBehaviour
	{
		int matchIDTracker = 10000000;
		int zoneIDTracker = 20000000;
		int cardIDTracker = 30000000;
		int modifierIDTracker = 40000000;
		int playerIDTracker = 50000000;

		List<Match> matches;
		List<Zone> zones;
		List<Card> cards;
		List<Modifier> modifiers;
		List<Player> players;

		public void Initialize ()
		{
			matches = new List<Match>();
			zones = new List<Zone>();
			cards = new List<Card>();
			modifiers = new List<Modifier>();
			players = new List<Player>();

			/*
			matches.AddRange(FindObjectsOfType<Match>());
			for (int i = 0; i < matches.Count; i++)
				matches[i].id = ++matchIDTracker;

			zones.AddRange(FindObjectsOfType<Zone>());
			for (int i = 0; i < zones.Count; i++)
				zones[i].id = ++zoneIDTracker;

			cards.AddRange(FindObjectsOfType<Card>());
			for (int i = 0; i < cards.Count; i++)
				cards[i].id = ++cardIDTracker;

			modifiers.AddRange(FindObjectsOfType<Modifier>());
			for (int i = 0; i < modifiers.Count; i++)
				modifiers[i].id = ++modifierIDTracker;

			players.AddRange(FindObjectsOfType<Player>());
			for (int i = 0; i < players.Count; i++)
				players[i].id = ++playerIDTracker;
			*/
		}
	}
}