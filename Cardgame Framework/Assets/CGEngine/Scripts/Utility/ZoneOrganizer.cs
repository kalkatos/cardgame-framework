using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class ZoneOrganizer : MonoBehaviour
	{
		Zone myZone;
		//InputObject handInputObject;
		Card cardDragged;

		private void Awake ()
		{
			if (!TryGetComponent(out myZone))
				Debug.LogError($"The {GetType().Name} component needs a Zone component to work properly.");
			//if (!TryGetComponent(out handInputObject))
			//	Debug.LogError("The UserHandOrganizer component needs an InputObject component to work properly. Please add one.");
		}

		private void Start ()
		{
			InputManager.Instance.onBeginDragEvent.AddListener(CardDragBegin);
			InputManager.Instance.onDragEvent.AddListener(CardDragging);
			InputManager.Instance.onEndDragEvent.AddListener(CardDragEnd);
		}

		Vector3 PositionByIndex (int index, float maxSideDistance)
		{
			Vector3 distance = myZone.distanceBetweenCards;
			int quantity = myZone.Content.Count - 1;
			distance.x = Mathf.Min((myZone.bounds.x - maxSideDistance) / quantity, maxSideDistance);
			Vector3 first = new Vector3(myZone.transform.position.x - (quantity / 2f * distance.x), myZone.transform.position.y, myZone.transform.position.z);
			distance.x *= index;
			return first + distance;
		}

		int IndexByPosition (Vector3 position, float maxSideDistance)
		{
			/*
			distance.x = Mathf.Min((zone.bounds.x - zone.distanceBetweenCards.x) / (quantity - 1), zone.distanceBetweenCards.x);
			first = new Vector3(zone.transform.position.x - (quantity - 1) / 2f * distance.x, zone.transform.position.y, zone.transform.position.z);
			next = first;
			*/

			int quantity = myZone.Content.Count - 1;
			float sideDistance = Mathf.Min(myZone.bounds.x / quantity, maxSideDistance);
			float positionDistanceToHand = position.x - myZone.transform.position.x + (sideDistance * quantity + sideDistance) / 2f;
			int index = (int)(positionDistanceToHand / sideDistance);
			index = Mathf.Clamp(index, 0, quantity);
			return index;
		}



		public void ArrangeCards ()
		{
			StartCoroutine(CardMover.Instance.ArrangeCardsInZone(myZone));
		}

		public void CardDragEnd ()
		{
			cardDragged = null;
			ArrangeCards();
		}

		public void CardDragBegin ()
		{
			if (InputManager.Instance.draggedObject.transform.parent == myZone.transform)
				cardDragged = InputManager.Instance.draggedObject.GetComponent<Card>();
		}

		public void CardDragging ()
		{
			if (cardDragged)
			{
				int index = IndexByPosition(cardDragged.transform.position, myZone.distanceBetweenCards.x);
				if (myZone.Content.IndexOf(cardDragged) != index)
				{
					myZone.Content.Remove(cardDragged);
					myZone.Content.Insert(index, cardDragged);
					ArrangeCards();
				}
			}
		}
	}
}