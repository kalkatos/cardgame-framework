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

		public float moveSpeed = 0.1f;
		Dictionary<Card, Vector3> movingCards = new Dictionary<Card, Vector3>();

		private void Awake ()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
				DestroyImmediate(gameObject);
		}

		public override IEnumerator TreatTrigger (TriggerTag tag, params object[] args)
		{
			switch (tag)
			{
				case TriggerTag.OnCardEnteredZone:
					Card c = (Card)GetArgumentWithTag("movedCard", args);
					Zone z = (Zone)GetArgumentWithTag("targetZone", args);
					if (z.zoneConfig == ZoneConfiguration.Grid)
					{
						Vector3 toPos = new Vector3(z.transform.position.x - (z.gridColumns - 1) * z.cellSize.x / 2 + Mathf.FloorToInt(c.positionInGridZone % z.gridColumns) * z.cellSize.x,
							z.transform.position.y,
							z.transform.position.z - (z.gridRows - 1) * z.cellSize.y / 2 + Mathf.FloorToInt(c.positionInGridZone / z.gridColumns) * z.cellSize.y);
						yield return MoveToCoroutine(c, z, toPos, 0);
					}
					break;
				case TriggerTag.OnCardLeftZone:
					Zone z2 = (Zone)GetArgumentWithTag("oldZone", args);
					if (z2.zoneConfig != ZoneConfiguration.Grid)
						yield return ArrangeCardsInZone (z2);
					break;
				case TriggerTag.OnCardsEnteredZone:
					Zone z3 = (Zone)GetArgumentWithTag("targetZone", args);
					if (z3.zoneConfig != ZoneConfiguration.Grid)
						yield return ArrangeCardsInZone(z3);
					break;
			}
		}

		public IEnumerator ArrangeCardsInZone (Zone zone, float time = 0)
		{
			if (time == 0) time = Instance.moveSpeed;
			if (zone.zoneConfig == ZoneConfiguration.Undefined)
				yield break;

			int quantity = zone.Content.Count;
			if (quantity == 0)
				yield break;

			yield return new WaitForSeconds(0.1f);
			Vector3 first = zone.transform.position;
			Vector3 next = first;
			Vector3 distance = zone.distanceBetweenCards;
			Vector3 rotation = zone.transform.rotation.eulerAngles;

			if (zone.zoneConfig == ZoneConfiguration.Grid)
			{
				distance.Set(zone.cellSize.x, 0, zone.cellSize.y);
				first = new Vector3(zone.transform.position.x - (zone.gridColumns - 1) * distance.x / 2, zone.transform.position.y, zone.transform.position.z - (zone.gridRows - 1) * distance.z / 2);
			}
			else if (zone.zoneConfig == ZoneConfiguration.SideBySide)
			{
				distance.x = Mathf.Min((zone.bounds.x - zone.distanceBetweenCards.x) / (quantity - 1), zone.distanceBetweenCards.x);
				first = new Vector3(zone.transform.position.x - (quantity - 1) / 2f * distance.x, zone.transform.position.y, zone.transform.position.z);
				next = first;
			}

			for (int i = 0; i < zone.Content.Count; i++)
			{
				StartCoroutine(MoveToCoroutine(zone.Content[i], zone, next, time));
				next = next + distance;
			}
		}


		//public static void MoveCardTo(Card card, Vector3 to, float time = 0)
		//{
		//	if (time == 0) time = Instance.moveSpeed;
		//	Instance.StartCoroutine(Instance.MoveToCoroutine(card.gameObject, to, card.transform.rotation.eulerAngles, time));
		//}

		//public static IEnumerator MoveCardCoroutine(Card card, Zone zone, Vector3 to, float time = 0)
		//{
		//	if (time == 0) time = Instance.moveSpeed;
		//	yield return Instance.MoveToCoroutine(card, zone, to, time);
		//}

		IEnumerator MoveToCoroutine (Card card, Zone zone, Vector3 toPosition, float time)
		{
			if (card.zone != zone || card.transform.position == toPosition)
				yield break;
			if (movingCards.ContainsKey(card))
			{
				if (movingCards[card] == toPosition)
					yield break;
			}
			else
				movingCards.Add(card, toPosition);
			Debug.Log($"Starting a movement on {card.name} to zone {zone.name}");
			if (time == 0) time = Instance.moveSpeed;
			float delta = Time.deltaTime;
			float steps = time / delta;
			float currentStep = 0;
			GameObject obj = card.gameObject;
			Vector3 fromPosition = obj.transform.position;
			Quaternion fromRotationQuart = obj.transform.rotation;
			Vector3 fromRotation = fromRotationQuart.eulerAngles;
			Vector3 toRotation = zone.transform.rotation.eulerAngles;
			toRotation.z = fromRotation.z;
			Quaternion toRotationQuart = Quaternion.Euler(toRotation);
			bool doRotation = fromRotation != toRotation;
			do
			{
				if (card.zone != zone)
				{
					Debug.LogWarning($"@ @ @ @ @ Interrupted for card {card.name} going to zone {zone.name}");
					yield break;
				}
				//Step
				currentStep = currentStep + 1 > steps ? steps : currentStep + 1;
				//Position
				obj.transform.position = Vector3.Lerp(fromPosition, toPosition, currentStep / steps);
				//Rotation
				if (doRotation)
				{
					obj.transform.rotation = Quaternion.Lerp(fromRotationQuart, toRotationQuart, currentStep / steps);
				}
				yield return new WaitForSeconds(delta);
			}
			while (currentStep < steps);
			movingCards.Remove(card);
		}

		//public static IEnumerator MoveToSimultaneouslyCoroutine(List<Card> cards, List<Vector3> positions, float time)
		//{
		//	float delta = Time.deltaTime;
		//	Vector3[] from = new Vector3[cards.Count];
		//	for (int i = 0; i < cards.Count; i++)
		//	{
		//		from[i] = cards[i].transform.position;
		//	}
		//	float steps = time / delta;
		//	float currentStep = 0;
		//	do
		//	{
		//		currentStep = currentStep + 1 > steps ? steps : currentStep + 1;
		//		for (int i = 0; i < cards.Count; i++)
		//		{
		//			cards[i].transform.position = Vector3.Lerp(from[i], positions[i], currentStep / steps);
		//		}
		//		yield return new WaitForSeconds(delta);
		//	}
		//	while (currentStep < steps);
		//}
	}

	class Movement
	{
		public GameObject obj;
		public Vector3 destination;
		public float time;
		public bool ended { get { return currentStep >= steps; } }
		Vector3 origin;
		float steps;
		float stepInc;
		float currentStep;
		float startTime;

		public Movement () { }

		public Movement (GameObject obj, Vector3 destination, float time)
		{
			Set(obj, destination, time);
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
				obj.transform.position = Vector3.Lerp(origin, destination, currentStep / steps);
				return ended;
			}
			return false;
		}

		public void Set (GameObject obj, Vector3 destination, float time)
		{
			this.obj = obj;
			this.destination = destination;
			this.time = time;
			origin = obj.transform.position;
			startTime = Time.time;
			steps = time / Time.deltaTime;
			stepInc = steps / Mathf.Ceil(steps);
			currentStep = 0;
		}

		public void ChangeDestination (Vector3 destination)
		{
			Set(obj, destination, Time.time - startTime);
		}
	}
}