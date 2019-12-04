using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	public class CardMover : MatchWatcher
	{
		public static CardMover Instance;

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
			else if (Instance != this)
				Destroy(gameObject);
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
			Vector3 next = Vector3.zero;
			Vector3 distance = new Vector3(0, 0.01f, 0);
			int quantity = z.Content.Count - 1;
			if (quantity <= 0)
			{
				if (quantity == 0)
					yield return MoveToInterpolator(z.Content[0].gameObject, z.transform.position, time);
				yield break;
			}
			if (z.zoneConfig == ZoneConfiguration.SideBySide)
				distance.x = Mathf.Min((z.bounds.x - maxSideDistance) / quantity, maxSideDistance);
			Vector3 first = new Vector3(z.transform.position.x - (quantity / 2f * distance.x), z.transform.position.y, z.transform.position.z);
			next = first;
			for (int i = 0; i < z.Content.Count; i++)
			{
				StartCoroutine(MoveToInterpolator(z.Content[i].gameObject, next, time));
				next = next + distance;
			}
		}

		public static void MoveTo(Card target, Vector3 to, float time = 0.1f)
		{
			Instance.StartCoroutine(Instance.MoveToInterpolator(target.gameObject, to, time));
		}

		IEnumerator MoveToInterpolator(GameObject target, Vector3 to, float time)
		{
			Vector3 from = target.transform.position;
			float step = 0;
			float inc = 1 / (time <= 0 ? 1 : time / Time.deltaTime);
			while (step < 1)
			{
				step += inc;
				target.transform.position = Vector3.Lerp(from, to, step);
				yield return new WaitForSeconds(Time.deltaTime);
			}
		}
	}
}