﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CGEngine
{
	public class CGEngine : MonoBehaviour, IMessageReceiver
	{
		static CGEngine instance;
		public static CGEngine Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<CGEngine>();
					if (!instance)
					{
						GameObject go = new GameObject("CGEngineManager");
						instance = go.AddComponent<CGEngine>();
					}
				}
				return instance;
			}
		}

		int matchIdTracker;
		//public CardGameData gameData;
		//public GameObject cardTemplate;
		//public User localUser;
		//public List<object> initializers;
		//BasicSceneManager CurrentScene;

		private void Awake ()
		{
			if (instance == null)
			{
				instance = this;
			}
			else if (instance != this)
			{
				DestroyImmediate(gameObject);
				return;
			}

			DontDestroyOnLoad(gameObject);

			MessageBus.Register("All", this);
			//gameData = AssetDatabase.LoadAssetAtPath<CardGameData>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CardGameData")[0]));
			//if (gameData == null)
			//{
			//	Debug.LogError("CGEngine: You must create and attach a Card Game Data to the CGEngineManager object.");
			//	return;
			//}
			//cardTemplate = gameData.cardTemplate;
			//if (cardTemplate == null)
			//{
			//	Debug.LogError("CGEngine: You must create a card template prefab and set it up on the Card Game Data.");
			//	return;
			//}
			//localUser = new User(UserType.Local);
			//SceneManager.sceneLoaded += OnSceneLoaded;
		}

		public static void StartMatch (Ruleset rules)
		{
			Match match = new GameObject("CurrentMatch").AddComponent<Match>();
			match.id = "a" + (++Instance.matchIdTracker);
			match.matchNumber = Instance.matchIdTracker;
			Debug.Log("Created match " + match.id);
			match.Initialize(rules);
		}

		public static void CreateCards(GameObject template, List<CardData> cards, Vector3 position, Transform container = null, Player owner = null)
		{
			Vector3 posInc = Vector3.up * 0.02f;
			if (cards != null)
			{
				for (int i = 0; i < cards.Count; i++)
				{
					Card newCard = Instantiate(template, position, Quaternion.identity, container).GetComponent<Card>();
					position += posInc;
					newCard.SetupData(cards[i]);
					if (owner) newCard.owner = owner;
				}
			}
		}

		public void TreatMessage(string type, InputObject inputObject)
		{
			switch (type)
			{
				case "ObjectClicked":
					Card c = inputObject.GetComponent<Card>();
					if (c) Match.Current.ClickCard(c);
					break;
			}
		}

		/*
		public void SceneEnded ()
		{
			//FOR DEBUG
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}

		void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			CurrentScene = FindObjectOfType<BasicSceneManager>();
			if (CurrentScene == null)
			{
				Debug.LogError("CGEngine: Every scene must have a BasicSceneManager object.");
				return;
			}

			//FOR DEBUG
			User otherUser = new User(UserType.CPU);
			CurrentScene.Initialize(gameData.rules[0], localUser, otherUser);
		}

		[Header("Debug")]
		public bool debug;
		public string str;

		public void Test ()
		{
			Match.Current.Test(str);
		}
		*/
	}
}