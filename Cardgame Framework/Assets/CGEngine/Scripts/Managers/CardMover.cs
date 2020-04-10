using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class CardMover : MatchWatcher
	{
		static CardMover instance;
		public static CardMover Instance
		{
			get
			{
				if (instance == null)
					instance = new GameObject("CardMover").AddComponent<CardMover>();
				return instance;
			}
		}

		public float moveTime = 0.1f;
		Dictionary<Card, Movement> movingCards = new Dictionary<Card, Movement>();
		List<Movement> reusableMovements = new List<Movement>();

		private void Awake ()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
			{
				DestroyImmediate(gameObject);
				return;
			}

			for (int i = 0; i < 52; i++)
			{
				reusableMovements.Add(new Movement());
			}
		}
		
		public override IEnumerator OnCardEnteredZone (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters)
		{
			switch (newZone.zoneConfig)
			{
				case ZoneConfiguration.Stack:
				case ZoneConfiguration.SideBySide:
					yield return ArrangeCardsInZone(newZone);
					break;
				case ZoneConfiguration.Grid:
					Vector3 toPos = new Vector3(newZone.transform.position.x - (newZone.gridSize.x - 1) * newZone.cellSize.x / 2 + Mathf.FloorToInt(card.slotInZone % newZone.gridSize.x) * newZone.cellSize.x,
					newZone.transform.position.y,
					newZone.transform.position.z - (newZone.gridSize.y - 1) * newZone.cellSize.y / 2 + Mathf.FloorToInt(card.slotInZone / newZone.gridSize.x) * newZone.cellSize.y);
					yield return MoveToCoroutine(card, newZone, toPos, moveTime);
					break;
				case ZoneConfiguration.SpecificPositions:
					int index = card.slotInZone;
					if (newZone.specificPositions != null && index < newZone.specificPositions.Count)
					{
						Transform toTransform = newZone.specificPositions[index];
						yield return MoveToCoroutine(card, newZone, toTransform.position, toTransform.rotation, moveTime);
					}
					else
						yield return ArrangeCardsInZone(newZone);
					break;
			}
		}

		public override IEnumerator OnCardLeftZone (Card card, Zone oldZone)
		{
			if (oldZone.zoneConfig != ZoneConfiguration.Grid)
				yield return ArrangeCardsInZone(oldZone);
		}

		public IEnumerator ArrangeCardsInZone (Zone zone, float time = 0)
		{
			if (zone.zoneConfig == ZoneConfiguration.Undefined || zone.zoneConfig == ZoneConfiguration.SpecificPositions)
				yield break;

			int quantity = zone.Content.Count;
			if (quantity == 0)
				yield break;
			
			//if (movingCards.Count == 0)
				yield return new WaitForSeconds(0.1f);
			if (time == 0) time = Instance.moveTime;
			Vector3 first = zone.transform.position;
			Vector3 next = first;
			Vector3 distance = zone.distanceBetweenCards;
			Vector3 rotation = zone.transform.rotation.eulerAngles;

			if (zone.zoneConfig == ZoneConfiguration.Grid)
			{
				distance.Set(zone.cellSize.x, 0, zone.cellSize.y);
				first = new Vector3(zone.transform.position.x - (zone.gridSize.x - 1) * distance.x / 2, zone.transform.position.y, zone.transform.position.z - (zone.gridSize.y - 1) * distance.z / 2);
			}
			else if (zone.zoneConfig == ZoneConfiguration.SideBySide)
			{
				distance.x = Mathf.Min((zone.bounds.x - zone.distanceBetweenCards.x) / (quantity - 1), zone.distanceBetweenCards.x);
				first = new Vector3(zone.transform.position.x - (quantity - 1) / 2f * distance.x, zone.transform.position.y, zone.transform.position.z);
				next = first;
			}

			for (int i = 0; i < zone.Content.Count; i++)
			{
				if (!InputManager.Instance.draggedObject || zone.Content[i].gameObject != InputManager.Instance.draggedObject.gameObject)
					StartCoroutine(MoveToCoroutine(zone.Content[i], zone, next, time));
				next = next + distance;
			}
			yield return null;
		}

		IEnumerator MoveToCoroutine (Card card, Zone zone, Vector3 toPosition, float time)
		{
			yield return MoveToCoroutine(card, zone, toPosition, zone.transform.rotation, time);
		}

		IEnumerator MoveToCoroutine (Card card, Zone zone, Vector3 toPosition, Quaternion toRotation, float time)
		{
			if (card.zone != zone || card.transform.position == toPosition)
				yield break;
			Vector3 toRotationEuler = toRotation.eulerAngles;
			toRotationEuler.z = card.transform.rotation.eulerAngles.z;
			Quaternion toRotationFixed = Quaternion.Euler(toRotationEuler);
			Movement currentMovement = null;
			if (time == 0) time = moveTime;
			if (movingCards.ContainsKey(card))
			{
				if (movingCards[card].destination == toPosition)
					yield break;
				currentMovement = movingCards[card];
				currentMovement.ChangeDestination(toPosition, toRotationFixed);
				yield break;
			}
			else
			{
				if (reusableMovements.Count == 0)
					currentMovement = new Movement(card.gameObject, toPosition, toRotationFixed, time);
				else
				{
					currentMovement = reusableMovements[0];
					reusableMovements.Remove(currentMovement);
				}
				currentMovement.Set(card.gameObject, toPosition, toRotationFixed, time);
				movingCards.Add(card, currentMovement);
			}

			while (!currentMovement.ended)
			{
				yield return new WaitForSeconds(Time.deltaTime);
				currentMovement.Step();
			}
			reusableMovements.Add(currentMovement);
			
			movingCards.Remove(card);
		}
	}

	public class Movement
	{
		public GameObject obj;
		public Vector3 destination;
		public Quaternion targetRotation;
		public float time;
		public bool ended { get { return currentStep >= steps; } }
		Vector3 origin;
		Quaternion originRotation;
		float steps;
		float stepInc;
		float currentStep;
		float startTime;

		public Movement () { }

		public Movement (GameObject obj, Vector3 destination, Quaternion targetRotation, float time)
		{
			Set(obj, destination, targetRotation, time);
		}

		/// <summary>
		/// Steps the movement one frame.
		/// </summary>
		/// <returns>True at the exact frame where the movement ended.</returns>
		public bool Step ()
		{
			if (!ended)
			{
				currentStep += stepInc;
				if (ended)
					currentStep = steps;
				float stepPhase = currentStep / steps;
				obj.transform.position = Vector3.Lerp(origin, destination, stepPhase);
				obj.transform.rotation = Quaternion.Lerp(originRotation, targetRotation, stepPhase);
				return ended;
			}
			return false;
		}

		public void Set (GameObject obj, Vector3 destination, Quaternion targetRotation, float time)
		{
			this.obj = obj;
			this.destination = destination;
			this.targetRotation = targetRotation;
			this.time = time;
			origin = obj.transform.position;
			originRotation = obj.transform.rotation;
			startTime = Time.time;
			steps = time / Time.deltaTime;
			stepInc = steps / Mathf.Ceil(steps);
			currentStep = 0;
		}

		public void ChangeDestination (Vector3 destination, Quaternion targetRotation)
		{
			Set(obj, destination, targetRotation, Time.time - startTime);
		}
	}
}