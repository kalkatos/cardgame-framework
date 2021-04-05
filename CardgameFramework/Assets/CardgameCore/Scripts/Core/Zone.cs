using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardgameCore
{
	[CanEditMultipleObjects]
    public class Zone : MonoBehaviour
    {
		public Action OnZoneShuffled;
		public Action OnZoneUsed;

		internal string id;
		public List<string> tags = new List<string>();
		internal string[] tagArray;
		[Header("Configuration")]
		public InputPermissions inputPermissions;
		public ZoneOrientation tablePlane = ZoneOrientation.XY;
		public ZoneConfiguration zoneConfig = ZoneConfiguration.FixedDistance;
		[Header("Bounds")]
		public Vector3 distanceBetweenComps = new Vector3(0, 0.05f, 0);
		public float minDistance = 0.5f;
		public float maxDistance = 3f;
		public Vector2 bounds = new Vector2(13f, 4.7f);
		public Vector2Int gridSize = new Vector2Int(1, 1);
		[Header("Plane")]
		public Plane zonePlane;
		[Header("Debug")]
		public List<CGComponent> components = new List<CGComponent>();
		public int[] gridIndexes;
		//protected List<Movement> compMovement = new List<Movement>();
		//protected Dictionary<CGComponent, Vector3> compTargetPos = new Dictionary<CGComponent, Vector3>();
		protected Vector3 bottomLeftCorner, bottomRightCorner, topLeftCorner, topRightCorner;
		protected Vector3 right { get { return tablePlane == ZoneOrientation.XY || tablePlane == ZoneOrientation.XZ ? transform.right : transform.up; } }
		protected Vector3 forward { get { return tablePlane == ZoneOrientation.XY || tablePlane == ZoneOrientation.YZ ? transform.up : transform.forward; } }
		protected Vector3 up { get { return tablePlane == ZoneOrientation.XY ? transform.forward : tablePlane == ZoneOrientation.XZ ? transform.up : transform.right; } }

		private void Awake ()
		{
			tags.Add(name);
			tagArray = tags.ToArray();
			//Plane
			zonePlane = new Plane(up, transform.position);
			//Grid
			if (zoneConfig == ZoneConfiguration.Grid)
			{
				if (gridSize.x * gridSize.y < 1)
					gridSize.x = gridSize.y = 1;
				gridIndexes = new int[gridSize.x * gridSize.y];
				for (int i = 0; i < gridIndexes.Length; i++)
					gridIndexes[i] = -1;
			}
			if (TryGetComponent(out InputHandler inputHandler))
				inputHandler.inputPermissions = inputPermissions;
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
						Push(c);
				}
				Organize();
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
			Organize();
			OnZoneShuffled?.Invoke();
		}

		internal void BeUsed ()
		{
			OnZoneUsed?.Invoke();
		}

		public void Use ()
		{
			Match.EnqueueZoneUse(this);
		}

		public void EnqueueOrganize ()
		{
			Match.EnqueueZoneOrganization(this);
		}

		public void Push (CGComponent component, MovementAdditionalInfo addInfo = null)
		{
            component.Zone = this;
            component.transform.SetParent(transform);
			component.InputPermissions = inputPermissions;
			bool haveAddInfo = addInfo != null;
			if (haveAddInfo)
			{
				if (addInfo.flipped)
					component.AddTag("Flipped");
			}

			switch (zoneConfig)
			{
				case ZoneConfiguration.FixedDistance:
				case ZoneConfiguration.FlexibleDistance:
					if (components.Contains(component))
						components.Remove(component);
					if (haveAddInfo && addInfo.toBottom)
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
				case ZoneConfiguration.Grid:
					components.Add(component);
					component.transform.SetSiblingIndex(components.Count - 1);
					int targetPosition = -1;
					int gridX = haveAddInfo && addInfo.grid ? (int)addInfo.gridX.Get() : -1;
					int gridY = haveAddInfo && addInfo.grid ? (int)addInfo.gridY.Get() : -1;
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
					Debug.LogWarning("Push for SpecificPositions zone layout is not implemented.");
					break;
				case ZoneConfiguration.Undefined:
					break;
			}
			//if (!compTargetPos.ContainsKey(component))
			//	compTargetPos.Add(component, Vector3.zero);
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
			//compTargetPos.Remove(component);
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


		#endregion

		#region Movement

		public void Organize ()
		{
			switch (zoneConfig)
			{
				case ZoneConfiguration.Undefined:
					break;
				case ZoneConfiguration.SpecificPositions: //TODO SpecificPositions
				case ZoneConfiguration.FixedDistance:
					for (int i = 0; i < components.Count; i++)
					{
						components[i].transform.position = transform.position + right * distanceBetweenComps.x * i + up * distanceBetweenComps.y * i + forward * distanceBetweenComps.z * i;
						components[i].transform.rotation = transform.rotation;
						bool flipped = components[i].HasTag("Flipped");
						bool tapped = components[i].HasTag("Tapped");
						components[i].transform.localRotation = Quaternion.Euler(tapped ? 90 : 0, 0, flipped ? 180 : 0);
						components[i].transform.SetSiblingIndex(i);
					}
					break;
				case ZoneConfiguration.FlexibleDistance:
					float actualDistance = 0;
					if (components.Count > 1)
						actualDistance = Mathf.Clamp(bounds.x / (components.Count - 1), minDistance, maxDistance);
					Vector3 first = transform.position - right * actualDistance * (components.Count - 1) / 2;
					for (int i = 0; i < components.Count; i++)
					{
						components[i].transform.position = first + right * i * actualDistance + up * distanceBetweenComps.y * i + forward * distanceBetweenComps.z * i;
						components[i].transform.rotation = transform.rotation;
						bool flipped = components[i].HasTag("Flipped");
						bool tapped = components[i].HasTag("Tapped");
						components[i].transform.localRotation = Quaternion.Euler(tapped ? 90 : 0, 0, flipped ? 180 : 0);
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
						Vector3 offset = new Vector3(-distanceBetweenComps.x * (gridSize.x - 1) / 2f + col * distanceBetweenComps.x, 0,
							distanceBetweenComps.y * (gridSize.y - 1) / 2f - row * distanceBetweenComps.y);
						CGComponent comp = components[gridIndexes[i]];
						comp.transform.position = transform.position + right * offset.x + up * offset.y + forward * offset.z;
						comp.transform.rotation = transform.rotation;
						bool flipped = comp.HasTag("Flipped");
						bool tapped = comp.HasTag("Tapped");
						comp.transform.localRotation = Quaternion.Euler(tapped ? 90 : 0, 0, flipped ? 180 : 0);
					}
					break;
			}
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
			switch (tablePlane)
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

		public void DeleteAll ()
		{
			for (int i = 0; i < components.Count; i++)
				DestroyImmediate(components[0]);
			components.Clear();
			gridIndexes = null;
		}

		public override string ToString ()
		{
			return $"{name} (id: {id})";
		}

		public void Drop ()
		{
			Debug.Log("Drop - " + ToString());
		}
	}

	public class MovementAdditionalInfo
	{
		public bool toBottom;
		public bool flipped;
		public bool grid;
		public Getter gridX, gridY;
		public bool keepOrder;

		public MovementAdditionalInfo ()
		{
			toBottom = false;
			flipped = false;
			gridX = Getter.Build("-1");
			gridY = Getter.Build("-1");
		}

		public MovementAdditionalInfo (string builder) : this()
		{
			SetFromString(builder);
		}

		public void SetFromString (string additionalInfo)
		{
			if (string.IsNullOrEmpty(additionalInfo))
				return;
			string[] addInfoBreak = StringUtility.ArgumentsBreakdown(additionalInfo, 0);
			for (int i = 0; i < addInfoBreak.Length; i++)
			{
				if (addInfoBreak[i] == "Bottom")
					toBottom = true;
				else if (addInfoBreak[i] == "Flipped")
					flipped = true;
				else if (addInfoBreak[i] == "KeepOrder")
					keepOrder = true;
				else if (addInfoBreak[i] == "Grid")
				{
					grid = true;
					string[] gridPosBuilders = StringUtility.ArgumentsBreakdown(addInfoBreak[i + 1], 0);
					gridX = Getter.Build(gridPosBuilders[0]);
					gridY = Getter.Build(gridPosBuilders[1]);
					i++;
				}
			}
		}

		public override string ToString ()
		{
			string result = "MovementInfo( ";
			if (toBottom) result += "Bottom ";
			if (flipped) result += "Flipped ";
			if (grid) result += $"Grid({gridX},{gridY}) ";
			result += ")";
			return result;
		}
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

			if (GUILayout.Button("Delete All"))
				((Zone)target).DeleteAll();
		}
	}
#endif
}
