using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[DisallowMultipleComponent]
	public class CGEngine : MonoBehaviour
	{
		static CGEngine _instance;
		public static CGEngine instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<CGEngine>();
					if (!_instance)
					{
						GameObject go = new GameObject("CGEngineManager");
						_instance = go.AddComponent<CGEngine>();
					}
				}
				return _instance;
			}
		}

		internal int matchIdTracker;

		private void Awake ()
		{
			if (_instance == null)
			{
				_instance = this;
			}
			else if (_instance != this)
			{
				DestroyImmediate(gameObject);
				return;
			}

			DontDestroyOnLoad(gameObject);
		}

		public static void CreateCards (GameObject template, List<CardData> cards, Vector3 position, Transform container = null)
		{
			Vector3 posInc = Vector3.up * 0.005f;
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

		//DEBUG
		[HideInInspector]
		public float timeForTest;
		public static void NextMeasure (string message)
		{
			if (instance.timeForTest == 0)
			{
				instance.timeForTest = Time.time;
				Debug.Log($"{message} at {Time.time}");
				return;
			}
			float elapsed = Time.time - instance.timeForTest;
			instance.timeForTest = Time.time;
			Debug.Log($"{message} at {Time.time} with elapsed {elapsed} s");
		}
	}
}
