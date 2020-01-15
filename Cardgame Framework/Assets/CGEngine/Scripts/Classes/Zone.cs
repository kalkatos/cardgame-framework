using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class Zone : MonoBehaviour
	{
		
		public Texture zoneIcon;

		public string zoneType;
		public RevealStatus revealStatus;
		public ZoneConfiguration zoneConfig;
		public int gridRows;
		public int gridColumns;
		//public ZoneData data;
		//public Player controller;
		public Vector2 bounds = new Vector2(1.43f, 2f);
		public string id;

		List<Card> content;
		public List<Card> Content
		{
			get
			{
				if (content == null)
					content = new List<Card>();
				return content;
			}
		}

		private void Start()
		{
			if (transform.childCount > 0)
			{
				for (int i = 0; i < transform.childCount; i++)
				{
					Card c = transform.GetChild(i).GetComponent<Card>();
					PushCard(c);
				}
			}
		}

		void OnDrawGizmos ()
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(transform.position, new Vector3(bounds.x, 0, bounds.y));

			//Vector3 pos = transform.position;
			//Vector3 halfBounds = new Vector3(bounds.x / 2, 0, bounds.y / 2);
			//Gizmos.DrawLine((transform.position - halfBounds) )
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireCube(transform.position, new Vector3(bounds.x, 0, bounds.y));
		}

		public void PushCard (Card c, RevealStatus revealStatus = RevealStatus.ZoneDefinition, bool toBottom = false)
		{
			if (!Content.Contains(c))
			{
				if (!toBottom)
				{
					Content.Add(c);
				}
				else
				{
					Content.Insert(0, c);
				}
				c.transform.SetParent(transform);
			}
			else
				Debug.LogWarning("CGEngine: Card " + c.ID + " is already in zone " + id + ".");
			c.zone = this;
			//c.controller = controller;
			switch (revealStatus)
			{
				case RevealStatus.Hidden:
				case RevealStatus.RevealedToController:
				case RevealStatus.RevealedToEveryone:
				case RevealStatus.HiddenOnlyToController:
					c.RevealStatus = revealStatus;
					break;
				case RevealStatus.ZoneDefinition:
					c.RevealStatus = this.revealStatus;
					break;
			}
		}

		public Card PopCard (Card c)
		{
			if (!Content.Contains(c))
				Debug.LogWarning("CGEngine: Zone " + id + " does not contain the card " + c.ID + ".");
			else
			{
				Content.Remove(c);
				c.zone = null;
			}
			return c;
		}

		public void Shuffle()
		{
			Debug.Log("Shuffling " + id);

			if (Content.Count <= 1)
				return;

			Vector3[] positions = new Vector3[Content.Count];

			for (int i = 0; i < Content.Count; i++)
			{
				positions[i] = Content[i].transform.position;
				Content[i].transform.SetParent(null);
			}

			for (int i = 0; i < 7; i++)
			{
				List<Card> s1, s2, chosen;
				int half = Content.Count / 2;
				s1 = Content.GetRange(0, half);
				s2 = Content.GetRange(half, Content.Count - half);
				Content.Clear();
				while (s1.Count > 0 || s2.Count > 0)
				{
					int rand = Random.Range(0, 2);
					chosen = rand == 0 ? (s1.Count > 0 ? s1 : s2) : (s2.Count > 0 ? s2 : s1);
					Card top = chosen[chosen.Count - 1];
					chosen.Remove(top);
					Content.Add(top);
				}
			}

			for (int i = 0; i < Content.Count; i++)
			{
				Content[i].transform.position = positions[i];
				Content[i].transform.SetParent(transform);
			}
		}

		
	}
}