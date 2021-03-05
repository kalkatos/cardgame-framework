using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardgameCore
{
    public class Zone : CGObject
    {
        public List<string> tags = new List<string>();
        public RevealStatus revealStatus;
		public ZoneOrientation zoneOrientation = ZoneOrientation.XY;
		public ZoneConfiguration zoneConfig = ZoneConfiguration.FixedDistance;
		public Vector3 distanceBetweenCards = new Vector3(0, 0.05f, 0);
		public Vector2 bounds = new Vector2(8f, 11f);
		public Vector2Int gridSize;
		public Vector2 cellSize = new Vector2(8f, 11f);
		[Space(10)]
		public List<CGComponent> components = new List<CGComponent>();
        private Vector3 bottomLeftCorner, bottomRightCorner, topLeftCorner, topRightCorner;

        private void Awake ()
		{
			Organize();
        }

		public void Organize ()
		{
			if (transform.childCount > 0)
				for (int i = 0; i < transform.childCount; i++)
				{
					Transform child = transform.GetChild(i);
					if (child.TryGetComponent(out CGComponent c))
					{
						Push(c);
						child.position = transform.position + distanceBetweenCards * i;
						child.SetSiblingIndex(i);
					}
				}
		}

		public void Shuffle ()
		{
            if (components.Count <= 1)
                return;
            for (int i = components.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i);
                CGComponent temp = components[j];
                components[j] = components[i];
                components[i] = temp;
			}
			for (int i = 0; i < components.Count; i++)
			{
				components[i].transform.SetParent(null);
				components[i].transform.SetParent(transform);
				components[i].transform.position = transform.position + distanceBetweenCards * i;
			}
        }

        public void Push (CGComponent component)
		{
            Push(component, revealStatus);
		}

        public void Push (CGComponent component, RevealStatus revealStatus = RevealStatus.Ignore, bool toBottom = false)
		{
			if (components.Contains(component))
				components.Remove(component);
            component.zone = this;
            component.transform.SetParent(transform);
			if (toBottom)
			{
				components.Insert(0, component);
				component.transform.SetSiblingIndex(0);
			}
			else
			{
				components.Add(component);
				component.transform.SetSiblingIndex(components.Count - 1);
			}
        }

        public void Pop (CGComponent component)
		{
            components.Remove(component);
            component.zone = null;
        }

        public int GetIndexOf (CGComponent component)
		{
            return components.IndexOf(component);
		}

		public override string ToString ()
		{
			return $"Zone: {name} (id: {id})";
		}

		#region Editor Gizmos
		private void OnValidate()
		{
			SetWirePoints();
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.cyan;
			DrawWire();
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.yellow;
			DrawWire();
		}

		private void SetWirePoints()
		{
			float halfWidth = bounds.x / 2;
			float halfHeight = bounds.y / 2;
			switch (zoneOrientation)
			{
				case ZoneOrientation.XY:
					bottomLeftCorner = transform.TransformPoint(new Vector3(-halfWidth, -halfHeight, 0));
					bottomRightCorner = transform.TransformPoint(new Vector3(halfWidth, -halfHeight, 0));
					topLeftCorner = transform.TransformPoint(new Vector3(-halfWidth, halfHeight, 0));
					topRightCorner = transform.TransformPoint(new Vector3(halfWidth, halfHeight, 0));
					break;
				case ZoneOrientation.XZ:
					bottomLeftCorner = transform.TransformPoint(new Vector3(-halfWidth, 0, -halfHeight));
					bottomRightCorner = transform.TransformPoint(new Vector3(halfWidth, 0, -halfHeight));
					topLeftCorner = transform.TransformPoint(new Vector3(-halfWidth, 0, halfHeight));
					topRightCorner = transform.TransformPoint(new Vector3(halfWidth, 0, halfHeight));
					break;
				case ZoneOrientation.YZ:
					bottomLeftCorner = transform.TransformPoint(new Vector3(0, -halfWidth, -halfHeight));
					bottomRightCorner = transform.TransformPoint(new Vector3(0, halfWidth, -halfHeight));
					topLeftCorner = transform.TransformPoint(new Vector3(0, -halfWidth, halfHeight));
					topRightCorner = transform.TransformPoint(new Vector3(0, halfWidth, halfHeight));
					break;
				default:
					break;
			}
		}

		public void DrawWire()
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
		#endregion

	}

	public enum ZoneConfiguration
	{
		FixedDistance,
		Grid,
		SpecificPositions,
		Undefined
	}

	public enum ZoneOrientation
	{
		XY,
		XZ,
		YZ
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(Zone))]
	public class ZoneEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Organize Components"))
				((Zone)target).Organize();

			if (GUILayout.Button("Shuffle"))
				((Zone)target).Shuffle();
		}
	}
#endif
}
