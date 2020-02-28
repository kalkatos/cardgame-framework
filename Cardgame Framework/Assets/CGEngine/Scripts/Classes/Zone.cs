using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[System.Serializable]
	public class Zone : MonoBehaviour
	{
		public string ID { get; internal set; }
		public string zoneTags;
		public RevealStatus revealStatus;
		public ZoneConfiguration zoneConfig;
		public InputPermissions inputPermissionForCards;
		[HideInInspector] public Vector3 distanceBetweenCards = new Vector3(0, 0.005f, 0);
		[HideInInspector] public Vector2 bounds = new Vector2(1.1f, 1.45f);
		[HideInInspector] public Vector2Int gridSize;
		[HideInInspector] public Card[] slots;
		[HideInInspector] public Vector2 cellSize = new Vector2(1.1f, 1.45f);
		[HideInInspector] public List<Transform> specificPositions = new List<Transform>();
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
				slots = new Card[gridSize.y * gridSize.x];
			if (zoneConfig == ZoneConfiguration.SpecificPositions)
				slots = new Card[specificPositions.Count];
			if (transform.childCount > 0)
			{
				for (int i = 0; i < transform.childCount; i++)
				{
					if (transform.GetChild(i).TryGetComponent(out Card c))
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
			DrawWire();
		}

		private void OnDrawGizmosSelected ()
		{
			Gizmos.color = Color.yellow;
			DrawWire();
		}

		public void DrawWire ()
		{
			if (zoneConfig != ZoneConfiguration.SpecificPositions)
			{
				if (transform.hasChanged)
					SetWirePoints();
				Gizmos.DrawLine(bottomLeftCorner, topLeftCorner);
				Gizmos.DrawLine(topLeftCorner, topRightCorner);
				Gizmos.DrawLine(topRightCorner, bottomRightCorner);
				Gizmos.DrawLine(bottomRightCorner, bottomLeftCorner);
				if (zoneConfig == ZoneConfiguration.Grid)
				{
					for (int i = 1; i < gridSize.x; i++)
						Gizmos.DrawLine(topLeftCorner + ((topRightCorner - topLeftCorner) / gridSize.x * i), bottomLeftCorner + ((bottomRightCorner - bottomLeftCorner) / gridSize.x * i));
					for (int i = 1; i < gridSize.y; i++)
						Gizmos.DrawLine(bottomLeftCorner + ((topLeftCorner - bottomLeftCorner) / gridSize.y * i), bottomRightCorner + ((topRightCorner - bottomRightCorner) / gridSize.y * i));
				}
			}
			else if (specificPositions != null)
			{
				for (int i = 0; i < specificPositions.Count; i++)
				{
					DrawCellSizeWireOnTransform(specificPositions[i]);
				}
			}
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

		public void DrawCellSizeWireOnTransform (Transform target)
		{
			float halfWidth = cellSize.x / 2f;
			float halfHeight = cellSize.y / 2f;
			Gizmos.DrawLine(target.TransformPoint(new Vector3(-halfWidth, 0, -halfHeight)), target.TransformPoint(new Vector3(-halfWidth, 0, halfHeight)));
			Gizmos.DrawLine(target.TransformPoint(new Vector3(-halfWidth, 0, halfHeight)), target.TransformPoint(new Vector3(halfWidth, 0, halfHeight)));
			Gizmos.DrawLine(target.TransformPoint(new Vector3(halfWidth, 0, halfHeight)), target.TransformPoint(new Vector3(halfWidth, 0, -halfHeight)));
			Gizmos.DrawLine(target.TransformPoint(new Vector3(halfWidth, 0, -halfHeight)), target.TransformPoint(new Vector3(-halfWidth, 0, -halfHeight)));
		}

		public void PushCard (Card c, RevealStatus revealStatus, Vector2Int gridPos)
		{
			PushCard(c, revealStatus, false, gridPos);
		}

		public void PushCard (Card c, RevealStatus revealStatus = RevealStatus.ZoneDefinition, bool toBottom = false, Vector2Int? gridPos = null)
		{
			if (!Content.Contains(c))
			{

				if (zoneConfig == ZoneConfiguration.Grid || zoneConfig == ZoneConfiguration.SpecificPositions)
				{
					if (!gridPos.HasValue)
						gridPos = FindEmptySlotInGrid();
					if (gridPos.Value.x >= 0 && gridPos.Value.y >= 0)
					{
						int pos = gridPos.Value.x * gridSize.x + gridPos.Value.y;
						if (pos < slots.Length)
							slots[pos] = c;
						c.slotInZone = pos;
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
				Debug.LogWarning("[CGEngine] Card " + c.ID + " is already in zone " + ID + ".");

			if (revealStatus == RevealStatus.ZoneDefinition)
				c.RevealStatus = this.revealStatus;
			else
				c.RevealStatus = revealStatus;

			if (c.TryGetComponent(out InputObject inputObject))
			{
				inputObject.inputPermissions = inputPermissionForCards;
				if (inputPermissionForCards == 0)
					inputObject.inputCollider.enabled = false;
				else
					inputObject.inputCollider.enabled = true;
			}
		}

		public Vector2Int FindEmptySlotInGrid ()
		{
			for (int i = 0; i < slots.Length; i++)
			{
				if (slots[i] == null)
				{
					return new Vector2Int(i / gridSize.x, i % gridSize.x);
				}
			}
			return new Vector2Int(-1, -1);
		}

		public Card PopCard (Card c)
		{
			if (!Content.Contains(c))
				Debug.LogWarning("[CGEngine] Zone " + zoneTags + " does not contain the card " + c.ID + " - " + c.name);
			else
			{
				if (zoneConfig == ZoneConfiguration.Grid || zoneConfig == ZoneConfiguration.SpecificPositions)
				{
					if (c.slotInZone >= 0)
					{
						if (slots[c.slotInZone] == c)
							slots[c.slotInZone] = null;
						else
							Debug.LogWarning("DEBUG Didn't find card in grid!  <<<<<<<<<<<<<<<<<<<<<<<<<<< <<<<<<<  <<<<<<<<<< <<<<  <<<");
						c.slotInZone = -1;
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

			transform.DetachChildren();

			for (int i = Content.Count - 1; i > 0; i--)
			{
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
		}

		

		public void Use ()
		{
			Match.Current.UseZone(this);
		}
	}
}