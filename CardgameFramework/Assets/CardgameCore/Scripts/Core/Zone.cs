using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardgameCore
{
    public class Zone : MonoBehaviour
    {
		public Action OnZoneShuffled;

		internal string id;
		public List<string> tags = new List<string>();
		public InputPermissions inputPermissions;
		public ZoneOrientation zoneOrientation = ZoneOrientation.XY;
		public ZoneConfiguration zoneConfig = ZoneConfiguration.FixedDistance;
		public Vector3 distanceBetweenComps = new Vector3(0, 0.05f, 0);
		public Vector3 minDistance = new Vector3(0, 0.05f, 0);
		public Vector3 maxDistance = new Vector3(0, 0.05f, 0);
		public Vector2 bounds = new Vector2(8f, 11f);
		public Vector2Int gridSize = new Vector2Int(1, 1);
		[Space(10)]
		public List<CGComponent> components = new List<CGComponent>();
		public int[] gridIndexes;
		//protected List<Movement> compMovement = new List<Movement>();
		protected Dictionary<CGComponent, Vector3> compTargetPos = new Dictionary<CGComponent, Vector3>();
		protected Vector3 bottomLeftCorner, bottomRightCorner, topLeftCorner, topRightCorner;

        private void Awake ()
		{
			tags.Add(name);
			//Grid
			if (zoneConfig == ZoneConfiguration.Grid)
			{
				if (gridSize.x * gridSize.y < 1)
					gridSize.x = gridSize.y = 1;
				gridIndexes = new int[gridSize.x * gridSize.y];
				for (int i = 0; i < gridIndexes.Length; i++)
					gridIndexes[i] = -1;
			}
			GetComponentsInChildren();
        }

		#region Core Methods

		public void GetComponentsInChildren () //TODO for grid, find the best index based on local position
		{
			if (transform.childCount > 0)
			{
				components.Clear();
				for (int i = 0; i < transform.childCount; i++)
				{
					Transform child = transform.GetChild(i);
					if (child.TryGetComponent(out CGComponent c))
					{
						Push(c);
						child.position = transform.position + distanceBetweenComps * i;
						child.rotation = transform.rotation;
					}
				}
			}
		}

		public void Shuffle () //TODO shuffle on grid
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
			//for (int i = 0; i < components.Count; i++)
			//{
			//	components[i].transform.SetParent(null);
			//	components[i].transform.SetParent(transform);
			//	Vector3 newPos = transform.position + distanceBetweenCards * i;
			//	components[i].transform.position = transform.position + distanceBetweenCards * i;

			//	compTargetPos[components[i]] = newPos;
			//}
			Organize();
			OnZoneShuffled?.Invoke();
		}

		public void Push (CGComponent component, bool toBottom = false)
		{
			Push(component, toBottom, -1, -1);
		}

		public void Push (CGComponent component, int gridX, int gridY)
		{
			Push(component, false, gridX, gridY);
		}

        public void Push (CGComponent component, bool toBottom, int gridX, int gridY)
		{
            component.Zone = this;
            component.transform.SetParent(transform);
			component.InputPermissions = inputPermissions;

			switch (zoneConfig)
			{
				case ZoneConfiguration.FixedDistance:
					if (components.Contains(component))
						components.Remove(component);
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
					break;
				case ZoneConfiguration.FlexibleDistance: //TODO Push FlexibleDistance
					break;
				case ZoneConfiguration.Grid:
					components.Add(component);
					component.transform.SetSiblingIndex(components.Count - 1);
					int targetPosition = -1;
					if (gridX < 0 || gridY < 0)
					{
						for (int i = 0; i < gridIndexes.Length; i++)
						{
							if (gridIndexes[i] < 0)
							{
								targetPosition = i;
								break;
							}
						}
						if (targetPosition >= 0)
							gridIndexes[targetPosition] = components.Count - 1;
						else
							Debug.LogWarning($"Zone {name} is a grid and is trying to push {component}, but there is no free grid slot left.");
					}
					else if (gridX < gridSize.x && gridY < gridSize.y)
					{
						targetPosition = gridY * gridSize.x + gridX * gridSize.y;
						gridIndexes[targetPosition] = components.Count - 1;
					}
					break;
				case ZoneConfiguration.SpecificPositions: //TODO Push SpecificPositions
					break;
				case ZoneConfiguration.Undefined:
					break;
			}

			if (zoneConfig == ZoneConfiguration.FixedDistance)
			{ }

			if (!compTargetPos.ContainsKey(component))
				compTargetPos.Add(component, Vector3.zero);
			Organize();
		}

        public void Pop (CGComponent component)
		{
			if (!components.Contains(component))
				return;

			int index = components.IndexOf(component);
            components.Remove(component);
            component.Zone = null;
			if (gridIndexes != null)
			{
				for (int i = 0; i < gridIndexes.Length; i++)
					if (gridIndexes[i] == index)
						gridIndexes[i] = -1;
					else if (gridIndexes[i] > index)
						gridIndexes[i] -= 1;
			}
			compTargetPos.Remove(component);
			Organize();
        }

		public CGComponent GetComp (bool fromBottom = false)
		{
			if (components.Count > 0)
			{
				if (fromBottom)
					return components[0];
				else
					return components[components.Count - 1];
			}
			return null;
		}

		public List<CGComponent> GetComps (int quantity, bool fromBottom = false)
		{
			if (components.Count > 0)
			{
				List<CGComponent> compList = new List<CGComponent>();
				for (int i = 0; i < quantity && i < components.Count; i++)
				{
					if (fromBottom)
						compList.Add(components[i]);
					else
						compList.Add(components[components.Count - (i + 1)]);
				}
				return compList;
			}
			return null;
		}

		public int GetIndexOf (CGComponent component)
		{
            return components.IndexOf(component);
		}

		public override string ToString ()
		{
			return $"Zone: {name} (id: {id})";
		}

		#endregion

		#region Movement

		public void Organize ()
		{
			switch (zoneConfig)
			{
				case ZoneConfiguration.Undefined:
					break;
				case ZoneConfiguration.FixedDistance:
				case ZoneConfiguration.FlexibleDistance: //TODO FlexibleDistance
				case ZoneConfiguration.SpecificPositions: //TODO SpecificPositions
					for (int i = 0; i < components.Count; i++)
					{
						components[i].transform.position = transform.position + distanceBetweenComps * i;
						components[i].transform.rotation = transform.rotation;
						components[i].transform.SetSiblingIndex(i);
					}
					break;
				case ZoneConfiguration.Grid:
					for (int i = 0; i < gridIndexes.Length; i++)
					{
						if (gridIndexes[i] < 0)
							continue;

						int row = i / gridSize.x;
						int col = i % gridSize.x;
						Vector3 offset = new Vector3(-(distanceBetweenComps.x * (gridSize.x - 1)) / 2f + col * distanceBetweenComps.x, 0,
							distanceBetweenComps.y * (gridSize.y - 1) / 2f - row * distanceBetweenComps.y);
						components[gridIndexes[i]].transform.position = transform.position + offset;
						components[gridIndexes[i]].transform.rotation = transform.rotation;
					}
					break;
			}
		}

		protected virtual void UpdateMovements ()
		{
			
		}

		#endregion

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
			if (zoneConfig == ZoneConfiguration.Grid)
			{
				bounds.x = distanceBetweenComps.x * gridSize.x;
				bounds.y = distanceBetweenComps.y * gridSize.y;
			}
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

		//[Serializable]
		//protected class Movement
		//{
		//	public CGComponent comp;
		//	public Vector3 targetPos;
		//}
	}


	public enum ZoneConfiguration
	{
		FixedDistance,
		FlexibleDistance,
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
			if (GUILayout.Button("Organize Child Components"))
				((Zone)target).GetComponentsInChildren();

			if (GUILayout.Button("Shuffle"))
				((Zone)target).Shuffle();
		}
	}
#endif
}
