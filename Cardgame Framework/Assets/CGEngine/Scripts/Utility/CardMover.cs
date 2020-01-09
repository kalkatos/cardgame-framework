using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
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

		IEnumerator ArrangeCardsInZoneSideBySide(Zone z, float maxSideDistance, float time = 0.1f)
		{
			if (z.zoneConfig == ZoneConfiguration.Grid || z.zoneConfig == ZoneConfiguration.Undefined)
				yield break;

			Vector3 next = Vector3.zero;
			Vector3 distance = new Vector3(0, 0.01f, 0);
			int quantity = z.Content.Count - 1;
			if (quantity <= 0)
			{
				if (quantity == 0)
					yield return MoveToCoroutine(z.Content[0].gameObject, z.transform.position, time);
				yield break;
			}
			if (z.zoneConfig == ZoneConfiguration.SideBySide)
				distance.x = Mathf.Min((z.bounds.x - maxSideDistance) / quantity, maxSideDistance);
			Vector3 first = new Vector3(z.transform.position.x - (quantity / 2f * distance.x), z.transform.position.y, z.transform.position.z);
			next = first;
			for (int i = 0; i < z.Content.Count; i++)
			{
				StartCoroutine(MoveToCoroutine(z.Content[i].gameObject, next, time));
				next = next + distance;
			}
		}

		public static void MoveTo(Card target, Vector3 to, float time = 0.1f)
		{
			Instance.StartCoroutine(Instance.MoveToCoroutine(target.gameObject, to, time));
		}

		public IEnumerator MoveToCoroutine(GameObject target, Vector3 to, float time)
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
		/*
		public IEnumerator MoveToCoroutine(GameObject target, Vector3 to, float time)
		{
			float delta = Time.deltaTime;
			Vector3 from = target.transform.position;
			float step = 0;
			float inc = 1 / (time <= 0 ? 1 : time / delta);
			while (step < 1)
			{
				step += inc;
				target.transform.position = Vector3.Lerp(from, to, step);
				yield return new WaitForSeconds(delta);
			}
		}
		*/
		public IEnumerator MoveToSimultaneouslyCoroutine(List<Card> cards, List<Vector3> positions, float time)
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