using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class Zone : MonoBehaviour
	{
		public string ID;
		public string zoneTags;
		public RevealStatus revealStatus;
		public ZoneConfiguration zoneConfig;
		public int gridRows;
		public int gridColumns;
		public Card[] slots;
		//public ZoneData data;
		//public Player controller;
		public Vector2 cellSize = new Vector2(1.43f, 2f);
		public Vector2 bounds = new Vector2(1.43f, 2f);
		public Vector3 distanceBetweenCards = new Vector3(0, 0.01f, 0);
		public InputPermissions inputPermissionForCards;
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
		Vector3 bottomLeftCorner, bottomRightCorner, topLeftCorner, topRightCorner;

		private void Start ()
		{
			if (zoneConfig == ZoneConfiguration.Grid)
				slots = new Card[gridRows * gridColumns];
			if (transform.childCount > 0)
			{
				for (int i = 0; i < transform.childCount; i++)
				{
					Card c = transform.GetChild(i).GetComponent<Card>();
					PushCard(c);
				}
			}
		}

		private void OnValidate ()
		{
			SetWirePoints();
		}

		void OnDrawGizmos ()
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(bottomLeftCorner, topLeftCorner);
			Gizmos.DrawLine(topLeftCorner, topRightCorner);
			Gizmos.DrawLine(topRightCorner, bottomRightCorner);
			Gizmos.DrawLine(bottomRightCorner, bottomLeftCorner);
			//if (zoneConfig == ZoneConfiguration.Grid)
			//{
			//	bounds.x = gridColumns * cellSize.x;
			//	bounds.y = gridRows * cellSize.y;
			//	Vector3 gridNextPos = transform.position - Vector3.right * (gridColumns - 1) / 2 * cellSize.x;
			//	for (int i = 0; i < gridRows; i++)
			//	{
			//		for (int j = 0; j < gridColumns; j++)
			//		{
			//			Gizmos.DrawWireCube(gridNextPos, new Vector3(cellSize.x * 0.8f, 0, cellSize.y * 0.8f));
			//			gridNextPos.Set(gridNextPos.x + cellSize.x, 0, gridNextPos.z);
			//		}
			//		gridNextPos.Set(gridNextPos.x, 0, gridNextPos.z + cellSize.y);
			//	}
			//}
			//Vector3 pos = transform.position;
			//Vector3 halfBounds = new Vector3(bounds.x / 2, 0, bounds.y / 2);
			//Gizmos.DrawLine((transform.position - halfBounds) )
		}

		private void OnDrawGizmosSelected ()
		{
			if (transform.hasChanged)
				SetWirePoints();
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(bottomLeftCorner, topLeftCorner);
			Gizmos.DrawLine(topLeftCorner, topRightCorner);
			Gizmos.DrawLine(topRightCorner, bottomRightCorner);
			Gizmos.DrawLine(bottomRightCorner, bottomLeftCorner);
		}

		public void PushCard (Card c, RevealStatus revealStatus, Vector2Int gridPos)
		{
			PushCard(c, revealStatus, false, gridPos);
		}

		public void PushCard (Card c, RevealStatus revealStatus = RevealStatus.ZoneDefinition, bool toBottom = false, Vector2Int? gridPos = null)
		{
			if (!Content.Contains(c))
			{

				if (zoneConfig == ZoneConfiguration.Grid)
				{
					if (!gridPos.HasValue)
						gridPos = FindEmptySlotInGrid();
					if (gridPos.Value.x >= 0 && gridPos.Value.y >= 0)
					{
						int pos = gridPos.Value.x * gridColumns + gridPos.Value.y;
						if (pos < slots.Length)
							slots[pos] = c;
						c.positionInGridZone = pos;
					}
				}

				if (!toBottom)
				{
					Content.Add(c);
				}
				else
				{
					Content.Insert(0, c);
				}
				c.zone = this;
				c.transform.SetParent(transform);
			}
			else
				Debug.LogWarning("CGEngine: Card " + c.ID + " is already in zone " + ID + ".");

			//c.controller = controller;
			if (revealStatus == RevealStatus.ZoneDefinition)
				c.RevealStatus = this.revealStatus;
			else
				c.RevealStatus = revealStatus;

			c.GetComponent<InputObject>().inputPermissions = inputPermissionForCards;
		}

		public Vector2Int FindEmptySlotInGrid ()
		{
			for (int i = 0; i < slots.Length; i++)
			{
				if (slots[i] == null)
				{
					return new Vector2Int(i / gridColumns, i % gridColumns);
				}
			}
			return new Vector2Int(-1, -1);
		}

		public Card PopCard (Card c)
		{
			if (!Content.Contains(c))
				Debug.LogWarning("CGEngine: Zone " + zoneTags + " does not contain the card " + c.ID + " - " + c.name);
			else
			{
				if (zoneConfig == ZoneConfiguration.Grid)
				{
					if (c.positionInGridZone >= 0)
					{
						if (slots[c.positionInGridZone] == c)
							slots[c.positionInGridZone] = null;
						else
							Debug.LogWarning("DEBUG Didn't find card in grid!  <<<<<<<<<<<<<<<<<<<<<<<<<<< <<<<<<<  <<<<<<<<<< <<<<  <<<");
						c.positionInGridZone = -1;
					}
				}
				Content.Remove(c);
				c.zone = null;
			}
			return c;
		}

		public void Shuffle ()
		{
			if (Content.Count <= 1)
				return;

			for (int i = Content.Count - 1; i > 0; i--)
			{
				Content[i].transform.SetParent(null);
				int j = Random.Range(0, i);
				Card temp = Content[j];
				Vector3 pos = Content[j].transform.position;
				Content[j].transform.position = Content[i].transform.position;
				Content[j] = Content[i];
				Content[i] = temp;
				Content[i].transform.position = pos;
			}

			for (int i = 0; i < Content.Count; i++)
			{
				Content[i].transform.SetParent(transform);
			}

			/*
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
			*/
		}

		void SetWirePoints ()
		{
			float halfWidth = bounds.x / 2;
			float halfHeight = bounds.y / 2;
			bottomLeftCorner = transform.TransformPoint(new Vector3(-halfWidth, 0, -halfHeight));
			bottomRightCorner = transform.TransformPoint(new Vector3(halfWidth, 0, -halfHeight));
			topLeftCorner = transform.TransformPoint(new Vector3(-halfWidth, 0, halfHeight));
			topRightCorner = transform.TransformPoint(new Vector3(halfWidth, 0, halfHeight));
		}
	}
}