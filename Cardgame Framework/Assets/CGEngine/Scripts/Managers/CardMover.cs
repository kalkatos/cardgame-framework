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
		protected Dictionary<Card, Movement> moveObjects = new Dictionary<Card, Movement>();
		protected List<Movement> activeMovements = new List<Movement>();

		private void Awake ()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
			{
				DestroyImmediate(gameObject);
				return;
			}
		}

		private void Update ()
		{
			for (int i = 0; i < activeMovements.Count; i++)
			{
				if (activeMovements[i].Step())
				{
					activeMovements.RemoveAt(i);
					i--;
				}
			}
		}

		public override IEnumerator OnCardEnteredZone (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters)
		{
			ArrangeCardsInZone(newZone);
			yield return null;
		}

		public override IEnumerator OnCardLeftZone (Card card, Zone oldZone)
		{
			ArrangeCardsInZone(oldZone);
			yield return null;
		}

		public virtual void ArrangeCardsInZone (Zone zone)
		{
			Vector3 first = zone.transform.position;
			Vector3 distance = zone.distanceBetweenCards;
			Quaternion zoneRotation = zone.transform.rotation;
			switch (zone.zoneConfig)
			{
				case ZoneConfiguration.Stack:
				case ZoneConfiguration.SideBySide:
					if (zone.zoneConfig == ZoneConfiguration.SideBySide)
					{
						distance.x = Mathf.Clamp(zone.bounds.x / (zone.Content.Count + 2), 0.001f, distance.x);
						first.x = first.x - Mathf.Max(0, zone.Content.Count - 1) * distance.x / 2;
					}
					distance = zone.transform.TransformDirection(distance);
					float distanceMag = distance.magnitude;
					for (int i = 0; i < zone.Content.Count; i++)
					{
						Card c = zone.Content[i];
						if (!InputManager.instance.draggedObject || !InputManager.instance.draggedObject.TryGetComponent(out Card draggedCard) || draggedCard != c)
							SetupMovement(c, first + distance * i, zoneRotation, moveTime);
					}
					break;
				case ZoneConfiguration.Grid:
					break;
				case ZoneConfiguration.SpecificPositions:
					break;
				case ZoneConfiguration.Undefined:
					break;
				default:
					break;
			}
		}

		protected virtual Movement SetupMovement (Card card, Vector3 position, Quaternion rotation, float duration)
		{
			Movement cardMovement = null;
			if (!moveObjects.ContainsKey(card))
			{
				cardMovement = new Movement(card);
				moveObjects.Add(card, cardMovement);
			}
			else
				cardMovement = moveObjects[card];
			cardMovement.Set(position, rotation, duration);
			activeMovements.Add(cardMovement);
			return cardMovement;
		}

		protected virtual IEnumerator MoveCard (Card card, Vector3 position, Quaternion rotation, float duration = 0)
		{
			if (duration == 0)
				duration = moveTime;
			Movement movement = SetupMovement(card, position, rotation, duration);
			while (!movement.ended)
			{
				if (movement.Step())
					activeMovements.Remove(movement);
				yield return null;
			}
		}

		private Vector3 GetNextPosition (Zone zone)
		{
			Vector3 result = zone.transform.position;
			switch (zone.zoneConfig)
			{
				case ZoneConfiguration.Stack:
					result += zone.transform.TransformDirection(zone.distanceBetweenCards) * zone.transform.childCount;
					break;
				case ZoneConfiguration.SideBySide:
					Vector3 sideBySideDistance = zone.distanceBetweenCards;
					sideBySideDistance.x = Mathf.Clamp(zone.bounds.x / Mathf.Max(zone.transform.childCount, 1), 0.01f, sideBySideDistance.x);
					result += zone.transform.TransformDirection(sideBySideDistance) * zone.transform.childCount / 2;
					break;
				case ZoneConfiguration.Grid:
					break;
				case ZoneConfiguration.SpecificPositions:
					break;
				case ZoneConfiguration.Undefined:
					break;
				default:
					break;
			}
			return result;
		}
	}

	public class Movement
	{
		public Card card;
		public bool ended => currentStep >= 1f;

		Vector3 originPosition;
		Quaternion originRotation;
		Vector3 originScale;
		Vector3 targetPosition;
		Quaternion targetRotation;
		float currentDuration;
		float currentStep = 1f;
		float startTime;
		bool started;

		public Movement (Card card)
		{
			this.card = card;
		}

		public void Set (Vector3 targetPosition, Quaternion targetRotation, float duration)
		{
			if (duration <= 0)
				return;

			if (ended)
			{
				originPosition = card.transform.position;
				originRotation = card.transform.rotation;
				originScale = card.transform.localScale;
				this.targetPosition = targetPosition;
				this.targetRotation = targetRotation;
				currentDuration = duration;
				currentStep = 0;
				started = false;
				card.collider.enabled = false;
			}
			else
			{
				this.targetPosition = targetPosition;
				this.targetRotation = targetRotation;
			}
		}

		public bool Step ()
		{
			if (ended)
				return false;
			bool endedInThisStep = false;
			if (!started)
			{
				startTime = Time.time;
				started = true;
			}
			float currentTime = Time.time - startTime;
			currentStep = Mathf.Clamp01(currentTime / currentDuration);
			endedInThisStep = currentStep >= 1f;
			card.transform.position = Vector3.Lerp(originPosition, targetPosition, currentStep);
			card.transform.rotation = Quaternion.Lerp(originRotation, targetRotation, currentStep);
			card.transform.localScale = Vector3.Lerp(originScale, Vector3.one, currentStep);
			if (endedInThisStep)
				card.collider.enabled = true;
			return endedInThisStep;
		}
	}
}