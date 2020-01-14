using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class UserHandOrganizer : MonoBehaviour
	{
		public float maxSideDistance = 1.5f;
		public float handDragCardHeight = 1.2f;

		Zone hand;
		Card cardBeingDragged;
		Vector3 offset;
		Plane dragCardPlane;
		int clickCounter;
		float lastClickTime;
		Ray mouseRay;
		Camera mainCamera;
		Vector3 mouseWorldPosition;
		Plane xz = new Plane(Vector3.up, Vector3.zero);

		private void Start()
		{
			mainCamera = Camera.main;
			hand = GetComponent<Zone>();
			dragCardPlane = new Plane(Vector3.up, new Vector3(0, handDragCardHeight, 0));
		}

		Vector3 GetMouseWorldPosition(Plane plane)
		{
			float distance;
			plane.Raycast(mouseRay, out distance);
			return mouseRay.GetPoint(distance);
		}

		private void Update()
		{
			mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
			float distanceForMouseRay;
			xz.Raycast(mouseRay, out distanceForMouseRay);
			mouseWorldPosition = mouseRay.GetPoint(distanceForMouseRay);
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