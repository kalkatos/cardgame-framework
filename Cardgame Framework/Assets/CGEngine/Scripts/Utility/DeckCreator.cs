using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	public class DeckCreator : MonoBehaviour
	{
		public Deck deck;
		public bool created;

		private void OnValidate()
		{
			if (!created && deck != null)
			{
				Create(deck, transform);
				created = true;
			}
		}

		public static void Create (Deck deck, Transform parent)
		{
			Transform container = parent;
			Vector3 position = Vector3.zero;
			Vector3 posInc = Vector3.up * 0.01f;
			if (deck.cards != null)
			{
				for (int i = 0; i < deck.cards.Count; i++)
				{
					Card newCard = Instantiate(CGEngineManager.Instance.cardTemplate, position, Quaternion.identity, container).GetComponent<Card>();
					position += posInc;
					newCard.SetupData(deck.cards[i]);
				}
			}
		}
	}
}