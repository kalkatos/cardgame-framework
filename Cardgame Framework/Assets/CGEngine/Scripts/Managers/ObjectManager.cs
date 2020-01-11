using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class ObjectManager : MonoBehaviour
	{
		static ObjectManager instance;
		public static ObjectManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new GameObject("ObjectManager").AddComponent<ObjectManager>();
				}
				return instance;
			}
		}

		private void Awake()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
				DestroyImmediate(gameObject);
		}

		//int matchIDTracker = 10000000;
		//int zoneIDTracker = 20000000;
		//int cardIDTracker = 30000000;
		//int modifierIDTracker = 40000000;
		//int playerIDTracker = 50000000;

		List<Match> matches;
		List<Zone> zones;
		List<Card> cards;
		List<Modifier> modifiers;
		List<Player> players;
		bool initialized;

		void Start()
		{
			Debug.Log("ObjectManager, initializing from Start.");
			Initialize();
		}

		public void Initialize ()
		{
			if (initialized)
				return;

			matches = new List<Match>();
			zones = new List<Zone>();
			cards = new List<Card>();
			modifiers = new List<Modifier>();
			players = new List<Player>();

			
			matches.AddRange(FindObjectsOfType<Match>());
			for (int i = 0; i < matches.Count; i++)
				matches[i].id = i.ToString();

			zones.AddRange(FindObjectsOfType<Zone>());
			for (int i = 0; i < zones.Count; i++)
				zones[i].id = i.ToString();

			cards.AddRange(FindObjectsOfType<Card>());
			for (int i = 0; i < cards.Count; i++)
				cards[i].ID = i.ToString();

			modifiers.AddRange(FindObjectsOfType<Modifier>());
			for (int i = 0; i < modifiers.Count; i++)
				modifiers[i].id = i.ToString();

			players.AddRange(FindObjectsOfType<Player>());
			for (int i = 0; i < players.Count; i++)
				players[i].id = i.ToString();

			initialized = true;
		}

		public void Identify (Card c)
		{
			c.ID = cards.Count.ToString();
			cards.Add(c);			
		}

		public void Identify(Match m)
		{
			m.id = matches.Count.ToString();
			matches.Add(m);
		}

		public void Identify(Zone z)
		{
			z.id = zones.Count.ToString();
			zones.Add(z);
		}

		//TODO players & modifiers

		public Card GetCardById (string id)
		{
			int idInt = int.Parse(id);
			return cards[idInt];
		}

		public Zone GetZoneById (string id)
		{
			int idInt = int.Parse(id);
			return zones[idInt];
		}
	}
}