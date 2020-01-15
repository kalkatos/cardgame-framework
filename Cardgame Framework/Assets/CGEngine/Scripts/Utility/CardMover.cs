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

		private void Awake()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
				DestroyImmediate(gameObject);
		}

		public override IEnumerator TreatTrigger(string triggerTag, params object[] args)
		{
			switch (triggerTag)
			{
				case "OnCardEnteredZone":
					Card c = (Card)args[1];
					Zone z = (Zone)args[3];
					yield return ArrangeCardsInZoneSideBySide(z, 1.5f);
					break;
				case "OnCardLeftZone":
					Zone z2 = (Zone)args[3];
					yield return ArrangeCardsInZoneSideBySide(z2, 1.5f);
					break;
			}
		}

		IEnumerator ArrangeCardsInZoneSideBySide(Zone zone, float maxSideDistance, float time = 0.1f, bool toBottom = false)
		{
			if (zone.zoneConfig == ZoneConfiguration.Grid || zone.zoneConfig == ZoneConfiguration.Undefined)
				yield break;

			Vector3 next = Vector3.zero;
			Vector3 distance = new Vector3(0, 0.01f, 0);
			int quantity = zone.Content.Count - 1;
			Vector3 rotation = zone.transform.rotation.eulerAngles;
			if (quantity <= 0)
			{
				if (quantity == 0)
				{
					rotation.z = zone.Content[0].transform.rotation.eulerAngles.z;
					zone.Content[0].transform.rotation = Quaternion.Euler(rotation);
					yield return MoveToCoroutine(zone.Content[0].gameObject, zone.transform.position, time);
				}
				yield break;
			}
			if (zone.zoneConfig == ZoneConfiguration.SideBySide)
				distance.x = Mathf.Min((zone.bounds.x - maxSideDistance) / quantity, maxSideDistance);
			Vector3 first = new Vector3(zone.transform.position.x - (quantity / 2f * distance.x), zone.transform.position.y, zone.transform.position.z);
			next = first;
			if (zone.zoneType == "Discard")
				Debug.Log("Here");

			for (int i = 0; i < zone.Content.Count; i++)
			{
				rotation.z = zone.Content[i].transform.rotation.eulerAngles.z;
				zone.Content[i].transform.rotation = Quaternion.Euler(rotation);
				StartCoroutine(MoveToCoroutine(zone.Content[i].gameObject, next, time));
				next = next + distance;
			}
		}

		public static void MoveTo(Card target, Vector3 to, float time = 0.1f)
		{
			Instance.StartCoroutine(Instance.MoveToCoroutine(target.gameObject, to, time));
		}

		public static IEnumerator MoveToCoroutine(Card target, Vector3 to, float time = 0.1f)
		{
			yield return Instance.MoveToCoroutine(target.gameObject, to, time);
		}

		IEnumerator MoveToCoroutine(GameObject target, Vector3 to, float time)
		{
			float delta = Time.deltaTime;
			Vector3 from = target.transform.position;
			float steps = time / delta;
			float currentStep = 0;
			do
			{
				currentStep = currentStep + 1 > steps ? steps : currentStep + 1;
				target.transform.position = Vector3.Lerp(from, to, currentStep / steps);
				yield return new WaitForSeconds(delta);
			}
			while (currentStep < steps);
		}

		public static IEnumerator MoveToSimultaneouslyCoroutine(List<Card> cards, List<Vector3> positions, float time)
		{
			float delta = Time.deltaTime;
			Vector3[] from = new Vector3[cards.Count];
			for (int i = 0; i < cards.Count; i++)
			{
				from[i] = cards[i].transform.position;
			}
			float steps = time / delta;
			float currentStep = 0;
			do
			{
				currentStep = currentStep + 1 > steps ? steps : currentStep + 1;
				for (int i = 0; i < cards.Count; i++)
				{
					cards[i].transform.position = Vector3.Lerp(from[i], positions[i], currentStep / steps);
				}
				yield return new WaitForSeconds(delta);
			}
			while (currentStep < steps);
		}
	}
}