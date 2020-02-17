using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class UserHandOrganizer : MonoBehaviour, IInputEventReceiver
	{
		Zone hand;
		InputObject handInputObject;

		private void Awake ()
		{
			hand = GetComponent<Zone>();
			if (!hand)
				Debug.LogError("The UserHandOrganizer component needs a Zone component to work properly. Please add one.");
			handInputObject = GetComponent<InputObject>();
			if (!handInputObject)
				Debug.LogError("The UserHandOrganizer component needs an InputObject component to work properly. Please add one.");
		}

		private void Start ()
		{
			InputManager.Register(InputType.All, this);
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

		public void TreatEvent (InputType type, InputObject inputObject)
		{
			if (type == InputType.ObjectDropInto && inputObject == handInputObject)
			{
				StartCoroutine(CardMover.Instance.ArrangeCardsInZoneSideBySide(hand));
			}
			else if (type == InputType.ObjectHoverWhileDrag && inputObject == handInputObject)
			{
				
			}
		}
	}
}