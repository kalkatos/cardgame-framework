﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CardGameFramework
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

		private void Awake ()
		{
			Debug.Log("DEBUG  CGEngine Awake");

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

		}

		public static void StartMatch (Ruleset rules)
		{
			MessageBus.Register("All", Instance);
			Match match = new GameObject("CurrentMatch").AddComponent<Match>();
			match.id = "a" + (++Instance.matchIdTracker);
			match.matchNumber = Instance.matchIdTracker;
			Debug.Log("Created match " + match.id);
			match.Initialize(rules);
		}

		public static void CreateCards(GameObject template, List<CardData> cards, Vector3 position, Transform container = null)
		{
			Vector3 posInc = Vector3.up * 0.01f;
			if (cards != null)
			{
				for (int i = 0; i < cards.Count; i++)
				{
					Card newCard = Instantiate(template, position, Quaternion.identity, container).GetComponent<Card>();
					position += posInc;
					newCard.SetupData(cards[i]);
					newCard.gameObject.name = cards[i].cardDataID;
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
