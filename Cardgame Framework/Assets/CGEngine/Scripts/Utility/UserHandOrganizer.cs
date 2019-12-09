using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	public class UserHandOrganizer : MonoBehaviour
	{
		public float maxSideDistance = 1.5f;
		public float handDragCardHeight = 1.2f;

		Zone hand;
		Card cardBeingDragged;
		int cardBeingDraggedIndex = -1;
		Vector3 offset;
		CardInteractionPack cardInteractionPack;
		Plane dragCardPlane;
		int clickCounter;
		float lastClickTime;

		private void Start()
		{
			hand = GetComponent<Zone>();
			cardInteractionPack = new CardInteractionPack();
			MatchManager.Instance.RegisterCardInteractionListener(cardInteractionPack);
			dragCardPlane = new Plane(Vector3.up, new Vector3(0, handDragCardHeight, 0));
		}

		private void Update()
		{
			if (cardInteractionPack.mouseDownCard)// ==== DOWN
			{
				Card card = cardInteractionPack.mouseDownCard;
				if (hand.Content.Contains(card))
				{
					offset = card.transform.position - MatchManager.mouseWorldPosition;
					offset.y = 0;
				}
				cardInteractionPack.mouseDownCard = null;
			}
			if (cardInteractionPack.mouseUpCard)// ==== UP
			{
				Card card = cardInteractionPack.mouseUpCard;
				if (hand.Content.Contains(card) && cardBeingDragged == card)
				{
					CardMover.MoveTo(cardBeingDragged, PositionByIndex(cardBeingDraggedIndex, maxSideDistance), 0.1f);
				}
				cardInteractionPack.mouseUpCard = null;
			}
			if (cardInteractionPack.mouseOverCard)// ==== OVER
			{
				
				cardInteractionPack.mouseOverCard = null;
			}
			if (cardInteractionPack.mouseEnterCard)// ==== ENTER
			{
				
				cardInteractionPack.mouseEnterCard = null;
			}
			if (cardInteractionPack.mouseExitCard)// ==== EXIT
			{
				
				cardInteractionPack.mouseExitCard = null;
			}
			if (cardInteractionPack.mouseDragCard)// ==== DRAG
			{
				cardBeingDragged = cardInteractionPack.mouseDragCard;
				if (hand.Content.Contains(cardBeingDragged))
				{
					cardBeingDraggedIndex = hand.Content.IndexOf(cardBeingDragged);
					Vector3 pos = InputManager.GetMouseWorldPosition(dragCardPlane) + offset;
					//pos.y = 1.2f;
					cardBeingDragged.transform.position = pos;
					int newIndex = IndexByPosition(MatchManager.mouseWorldPosition, maxSideDistance);
					if (newIndex != cardBeingDraggedIndex)
					{
						Card replace = hand.Content[newIndex];
						if (newIndex > cardBeingDraggedIndex)
						{
							hand.Content.Remove(replace);
							hand.Content.Insert(cardBeingDraggedIndex, replace);
							hand.Content.Remove(cardBeingDragged);
							hand.Content.Insert(newIndex, cardBeingDragged);
							CardMover.MoveTo(replace, PositionByIndex(cardBeingDraggedIndex, maxSideDistance));
						}
						else
						{
							hand.Content.Remove(cardBeingDragged);
							hand.Content.Insert(newIndex, cardBeingDragged);
							hand.Content.Remove(replace);
							hand.Content.Insert(cardBeingDraggedIndex, replace);
							CardMover.MoveTo(replace, PositionByIndex(cardBeingDraggedIndex, maxSideDistance));
						}
						cardBeingDraggedIndex = newIndex;
					}
				}
				cardInteractionPack.mouseDragCard = null;
			}
			if (cardInteractionPack.mouseClickCard)// ==== CLICK
			{
				//clickCounter++;
				//if (cardInteractionPack.mouseClickCard == clickedCard && clickCounter == 2 && Time.time - lastClickTime <= 0.5f)
				//{
				//	Debug.Log("OO");
				//	Match.Current.UseCard(cardInteractionPack.mouseClickCard);
				//	clickCounter = 0;
				//	clickedCard = null;
				//}
				//else
				//	clickedCard = cardInteractionPack.mouseClickCard;
				//lastClickTime = Time.time;
				cardInteractionPack.mouseClickCard = null;
			}
		}

		Vector3 PositionByIndex (int index, float maxSideDistance)
		{
			Vector3 distance = new Vector3(0, 0.01f, 0);
			int quantity = hand.Content.Count - 1;
			distance.x = Mathf.Min((hand.bounds.x - maxSideDistance) / quantity, maxSideDistance);
			Vector3 first = new Vector3(hand.transform.position.x - (quantity / 2f * distance.x), hand.transform.position.y, hand.transform.position.z);
			distance.x *= index;
			return first + distance;
		}

		int IndexByPosition (Vector3 position, float maxSideDistance)
		{
			int quantity = hand.Content.Count - 1;
			float handSideDistance = Mathf.Min((hand.bounds.x - maxSideDistance) / quantity, maxSideDistance);
			float positionDistanceToHand = position.x - hand.transform.position.x + hand.bounds.x / 2f;
			int index = (int)(positionDistanceToHand / handSideDistance);
			if (index < 0)
				index = 0;
			else if (index > quantity)
				index = quantity;
			//Debug.Log("Index by position found to be " + index + " from position.x = " + position.x + " where handSideDistance = "+handSideDistance+ " and positionDistanceToHand = "+ positionDistanceToHand + "  ("+ position.x + " - " + hand.transform.position.x + " + " + (hand.bounds.x  - maxSideDistance) / 2f + ")");
			return index;
		}
	}
}