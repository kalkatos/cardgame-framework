﻿using System.Collections;
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

		private void Awake()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
				DestroyImmediate(gameObject);
		}

		public override IEnumerator TreatTrigger(TriggerTag tag, params object[] args)
		{
			switch (tag)
			{
				case TriggerTag.OnCardEnteredZone:
					Card c = (Card)args[1];
					Zone z = (Zone)args[3];
					if (z.zoneConfig != ZoneConfiguration.Grid)
						yield return ArrangeCardsInZoneSideBySide(z, 1.5f);
					else
					{
						Vector3 toPos = new Vector3(z.transform.position.x - (z.gridColumns - 1) * z.cellSize.x / 2 + Mathf.FloorToInt(c.positionInGridZone%z.gridColumns) * z.cellSize.x, 
							z.transform.position.y, 
							z.transform.position.z - (z.gridRows - 1) * z.cellSize.y / 2 + Mathf.FloorToInt(c.positionInGridZone / z.gridColumns) * z.cellSize.y);
						yield return MoveToCoroutine(c.gameObject, toPos, z.transform.rotation.eulerAngles, 0.1f);
					}
					break;
				case TriggerTag.OnCardLeftZone:
					Zone z2 = (Zone)args[3];
					if (z2.zoneConfig != ZoneConfiguration.Grid)
						yield return ArrangeCardsInZoneSideBySide(z2, 1.5f);
					break;
			}
		}

		IEnumerator ArrangeCardsInZoneSideBySide(Zone zone, float maxSideDistance, float time = 0.1f)
		{
			if (zone.zoneConfig == ZoneConfiguration.Undefined)
				yield break;

			int quantity = zone.Content.Count;
			if (quantity == 0)
				yield break;
			Vector3 first = zone.transform.position;
			Vector3 next = first;
			Vector3 distance = new Vector3(0, 0.01f, 0);
			Vector3 rotation = zone.transform.rotation.eulerAngles;

			if (zone.zoneConfig == ZoneConfiguration.Grid)
			{
				distance.Set(zone.cellSize.x, 0, zone.cellSize.y);
				first = new Vector3(zone.transform.position.x - (zone.gridColumns - 1) * distance.x / 2, zone.transform.position.y, zone.transform.position.z - (zone.gridRows - 1) * distance.z / 2);
			}
			else if (zone.zoneConfig == ZoneConfiguration.SideBySide)
			{
				distance.x = Mathf.Min((zone.bounds.x - maxSideDistance) / (quantity - 1), maxSideDistance);
				first = new Vector3(zone.transform.position.x - (quantity - 1) / 2f * distance.x, zone.transform.position.y, zone.transform.position.z);
				next = first;
			}

			for (int i = 0; i < zone.Content.Count; i++)
			{
				StartCoroutine(MoveToCoroutine(zone.Content[i].gameObject, next, rotation, time));
				next = next + distance;
			}
		}

		public static void MoveCardTo(Card card, Vector3 to, float time = 0.1f)
		{
			Instance.StartCoroutine(Instance.MoveToCoroutine(card.gameObject, to, card.transform.rotation.eulerAngles, time));
		}

		public static IEnumerator MoveCardCoroutine(Card card, Vector3 to, float time = 0.1f)
		{
			yield return Instance.MoveToCoroutine(card.gameObject, to, card.transform.rotation.eulerAngles, time);
		}

		IEnumerator MoveToCoroutine(GameObject obj, Vector3 toPosition, Vector3 toRotation, float time)
		{
			float delta = Time.deltaTime;
			float steps = time / delta;
			float currentStep = 0;
			Vector3 fromPosition = obj.transform.position;
			Quaternion fromRotationQuart = obj.transform.rotation;
			Vector3 fromRotation = fromRotationQuart.eulerAngles;
			toRotation.z = fromRotation.z;
			Quaternion toRotationQuart = Quaternion.Euler(toRotation);
			bool doRotation = fromRotation != toRotation;
			do
			{
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
}