using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace CardgameFramework
{
	public class Zone : MonoBehaviour
	{
		public event Action OnZoneShuffled;
		public event Action OnZoneUsed;

		public int CardCount => cards.Count;

		internal string id;
		public List<string> tags = new List<string>();
		internal string[] tagArray;
		[Header("Configuration")]
		public ZoneOrientation tablePlane = ZoneOrientation.XY;
		public ZoneConfiguration zoneConfig = ZoneConfiguration.FixedDistance;
		[Header("Bounds")]
		public Vector3 distanceBetweenCards = new Vector3(0, 0.05f, 0);
		public float minDistance = 0.5f;
		public float maxDistance = 3f;
		public Vector2 bounds = new Vector2(13f, 4.7f);
		public Vector2Int gridSize = new Vector2Int(1, 1);
		public Transform[] specificPositions;
		[Header("Plane")]
		public Plane zonePlane;
		[Header("Movement")]
		[SerializeField] private float defaultMoveSpeed;
		public ZoneMovement[] movements;
		[Header("Exposed for Debug")]
		public List<Card> cards = new List<Card>();
		public int[] cardIndexes;
		//protected List<Movement> cardMovement = new List<Movement>();
		//protected Dictionary<Card, Vector3> cardTargetPos = new Dictionary<Card, Vector3>();
		protected Vector3 bottomLeftCorner, bottomRightCorner, topLeftCorner, topRightCorner;
		protected Vector3 right { get { return tablePlane == ZoneOrientation.XY || tablePlane == ZoneOrientation.XZ ? transform.right : transform.up; } }
		protected Vector3 forward { get { return tablePlane == ZoneOrientation.XY || tablePlane == ZoneOrientation.YZ ? transform.up : transform.forward; } }
		protected Vector3 up { get { return tablePlane == ZoneOrientation.XY ? transform.forward : tablePlane == ZoneOrientation.XZ ? transform.up : transform.right; } }

		private ZoneMovement defaultMovement = new ZoneMovement();
		private ZoneMovement organizeZoneMovement = new ZoneMovement();

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
				cardIndexes = new int[gridSize.x * gridSize.y];
				for (int i = 0; i < cardIndexes.Length; i++)
					cardIndexes[i] = -1;
			}
			GetCardsInChildren();
			if (defaultMoveSpeed > 0)
				defaultMovement.speed = defaultMoveSpeed;
		}

		private void Update ()
		{
			defaultMovement.Update();
			organizeZoneMovement.Update();
			for (int i = 0; i < movements.Length; i++)
				movements[i].Update();
		}

		#region Core Methods

		private void AddCardToList (Card card, MovementAdditionalInfo addInfo)
		{
			if (cards.Contains(card))
				cards.Remove(card);
			if (addInfo != null && addInfo.toBottom)
			{
				cards.Insert(0, card);
				card.transform.SetSiblingIndex(0);
			}
			else
			{
				cards.Add(card);
				card.transform.SetSiblingIndex(cards.Count - 1);
			}
		}

		public void GetCardsInChildren () //TODO for grid, find the best index based on local position
		{
			if (transform.childCount > 0)
			{
				cards.Clear();
				for (int i = 0; i < transform.childCount; i++)
				{
					Transform child = transform.GetChild(i);
					if (child.TryGetComponent(out Card c))
						Push(c);
				}
				Organize();
			}
		}

		public void Shuffle () //TODO shuffle on grid
		{
			if (cards.Count <= 1)
				return;
			for (int i = cards.Count - 1; i > 0; i--)
			{
				int j = Random.Range(0, i);
				Card temp = cards[j];
				cards[j] = cards[i];
				cards[i] = temp;
			}
			Organize(true);
			OnZoneShuffled?.Invoke();
		}

		internal void BeUsed ()
		{
			OnZoneUsed?.Invoke();
		}
		
		public void Use (string origin)
		{
			Match.UseZone(this, origin);
		}

		public void EnqueueOrganize (string origin)
		{
			Match.OrganizeZone(this, origin);
		}

		public void Push (Card card, MovementAdditionalInfo addInfo = null)
		{
			card.Zone = this;
			card.transform.SetParent(transform);
			if (addInfo != null)
			{
				if (addInfo.flipped)
					card.AddTag("Flipped");
			}

			switch (zoneConfig)
			{
				case ZoneConfiguration.FixedDistance:
				case ZoneConfiguration.FlexibleDistance:
					AddCardToList(card, addInfo);
					break;
				case ZoneConfiguration.Grid:
					cards.Add(card);
					card.transform.SetSiblingIndex(cards.Count - 1);
					int targetPosition = -1;
					int gridX = addInfo != null && addInfo.grid ? (int)addInfo.gridX.Get() : -1;
					int gridY = addInfo != null && addInfo.grid ? (int)addInfo.gridY.Get() : -1;
					if (gridX < 0 || gridY < 0)
					{
						for (int i = 0; i < cardIndexes.Length; i++)
						{
							if (cardIndexes[i] < 0)
							{
								targetPosition = i;
								break;
							}
						}
						if (targetPosition >= 0)
							cardIndexes[targetPosition] = cards.Count - 1;
						else
							CustomDebug.LogWarning($"Zone {name} is a grid and is trying to push {card}, but there is no free grid slot left.");
					}
					else if (gridX < gridSize.x && gridY < gridSize.y)
					{
						targetPosition = gridY * gridSize.x + gridX * gridSize.y;
						cardIndexes[targetPosition] = cards.Count - 1;
					}
					break;
				case ZoneConfiguration.SpecificPositions: //TODO Push SpecificPositions
					if (specificPositions == null || specificPositions.Length == 0)
						CustomDebug.LogWarning($"Zone {name} layout is SpecificPositions but there is no positions defined");
					AddCardToList(card, addInfo);
					break;
				case ZoneConfiguration.Undefined:
					break;
			}
			//if (!cardTargetPos.ContainsKey(card))
			//	cardTargetPos.Add(card, Vector3.zero);
		}

		public void Pop (Card card)
		{
			if (!cards.Contains(card))
				return;

			int index = cards.IndexOf(card);
			cards.Remove(card);
			card.Zone = null;
			if (cardIndexes != null)
			{
				for (int i = 0; i < cardIndexes.Length; i++)
					if (cardIndexes[i] == index)
						cardIndexes[i] = -1;
					else if (cardIndexes[i] > index)
						cardIndexes[i] -= 1;
			}
			//cardTargetPos.Remove(card);
		}

		public Card GetCard (bool fromBottom = false)
		{
			if (cards.Count > 0)
			{
				if (fromBottom)
					return cards[0];
				else
					return cards[cards.Count - 1];
			}
			return null;
		}

		public void GetCards (int quantity, in List<Card> list, bool fromBottom = false)
		{
			if (cards.Count > 0)
			{
				for (int i = 0; i < quantity && i < cards.Count; i++)
				{
					if (fromBottom)
						list.Add(cards[i]);
					else
						list.Add(cards[cards.Count - (i + 1)]);
				}
			}
		}

		public int GetIndexOf (Card card)
		{
			return cards.IndexOf(card);
		}

		#endregion

		#region Movement

		private void ExecuteMovement (Card card, Vector3 targetPosition, Quaternion targetRotation, bool useOrganizeMovement = false)
		{
			if (!Application.isPlaying)
			{
				card.transform.SetPositionAndRotation(targetPosition, targetRotation);
				return;
			}
			if (useOrganizeMovement)
			{
				organizeZoneMovement.Add(card, targetPosition, targetRotation);
				return;
			}
			if (movements.Length == 0)
			{
				defaultMovement.Add(card, targetPosition, targetRotation);
				return;
			}
			for (int j = 0; j < movements.Length; j++)
				if (movements[j].condition.Evaluate())
				{
					movements[j].Add(card, targetPosition, targetRotation);
					break;
				}
		}

		public void Organize (bool justOrganize = false)
		{
			Card card;
			Vector3 targetPosition;
			Quaternion targetRotation;
			switch (zoneConfig)
			{
				case ZoneConfiguration.Undefined:
					break;
				case ZoneConfiguration.SpecificPositions:
					int stackingIndex = 0;
					for (int i = 0; i < cards.Count; i++)
					{
						card = cards[i];
						if (i < specificPositions.Length)
						{
							bool flipped = card.HasTag("Flipped");
							bool tapped = card.HasTag("Tapped");
							targetPosition = specificPositions[i].position;
							targetRotation = Quaternion.Euler(specificPositions[i].rotation.eulerAngles.x, 
								specificPositions[i].rotation.eulerAngles.y + (tapped ? -90 : 0), specificPositions[i].rotation.eulerAngles.z + (flipped ? 180 : 0));
							card.transform.SetSiblingIndex(i);
						}
						else
						{
							bool flipped = card.HasTag("Flipped");
							bool tapped = card.HasTag("Tapped");
							targetPosition = transform.position + right * distanceBetweenCards.x * stackingIndex + up * distanceBetweenCards.y * stackingIndex + forward * distanceBetweenCards.z * stackingIndex;
							targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + (tapped ? -90 : 0), transform.rotation.eulerAngles.z + (flipped ? 180 : 0));
							stackingIndex++;
							card.transform.SetSiblingIndex(i);
						}
						ExecuteMovement(card, targetPosition, targetRotation, justOrganize);
					}
					break;
				case ZoneConfiguration.FixedDistance:
					for (int i = 0; i < cards.Count; i++)
					{
						card = cards[i];
						bool flipped = card.HasTag("Flipped");
						bool tapped = card.HasTag("Tapped");
						targetPosition = transform.position + right * distanceBetweenCards.x * i + up * distanceBetweenCards.y * i + forward * distanceBetweenCards.z * i;
						targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + (tapped ? -90 : 0), transform.rotation.eulerAngles.z + (flipped ? 180 : 0));
						card.transform.SetSiblingIndex(i);
						ExecuteMovement(card, targetPosition, targetRotation, justOrganize);
					}
					break;
				case ZoneConfiguration.FlexibleDistance:
					float actualDistance = 0;
					if (cards.Count > 1)
						actualDistance = Mathf.Clamp(bounds.x / (cards.Count - 1), minDistance, maxDistance);
					Vector3 first = transform.position - right * actualDistance * (cards.Count - 1) / 2;
					for (int i = 0; i < cards.Count; i++)
					{
						card = cards[i];
						bool flipped = card.HasTag("Flipped");
						bool tapped = card.HasTag("Tapped");
						targetPosition = first + right * i * actualDistance + up * distanceBetweenCards.y * i + forward * distanceBetweenCards.z * i;
						targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + (tapped ? -90 : 0), transform.rotation.eulerAngles.z + (flipped ? 180 : 0));
						card.transform.SetSiblingIndex(i);
						ExecuteMovement(card, targetPosition, targetRotation, justOrganize);
					}
					break;
				case ZoneConfiguration.Grid:
					for (int i = 0; i < cardIndexes.Length; i++)
					{
						if (cardIndexes[i] < 0)
							continue;

						int row = i / gridSize.x;
						int col = i % gridSize.x;
						Vector3 offset = new Vector3(-distanceBetweenCards.x * (gridSize.x - 1) / 2f + col * distanceBetweenCards.x, 0,
							distanceBetweenCards.y * (gridSize.y - 1) / 2f - row * distanceBetweenCards.y);
						card = cards[cardIndexes[i]];
						bool flipped = card.HasTag("Flipped");
						bool tapped = card.HasTag("Tapped");
						targetPosition = transform.position + right * offset.x + up * offset.y + forward * offset.z;
						targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + (tapped ? -90 : 0), transform.rotation.eulerAngles.z + (flipped ? 180 : 0));
						ExecuteMovement(card, targetPosition, targetRotation, justOrganize);
					}
					break;
			}
		}

		#endregion

		#region Editor Gizmos
		private void OnValidate ()
		{
			SetWirePoints();
		}

		private void OnDrawGizmos ()
		{
			Gizmos.color = Color.cyan;
			DrawWire();
		}

		private void OnDrawGizmosSelected ()
		{
			Gizmos.color = Color.yellow;
			DrawWire();
		}

		private void SetWirePoints ()
		{
			if (zoneConfig == ZoneConfiguration.Grid)
			{
				bounds.x = distanceBetweenCards.x * gridSize.x;
				bounds.y = distanceBetweenCards.y * gridSize.y;
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

		public void DrawWire ()
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
			for (int i = 0; i < cards.Count; i++)
				DestroyImmediate(cards[0]);
			cards.Clear();
			cardIndexes = null;
		}

		public bool HasTag (string tag)
		{
			return tags.Contains(tag);
		}

		public override string ToString ()
		{
			return name;
		}

#if UNITY_EDITOR
		public void Sort ()
		{
			for (int i = 0; i < cards.Count; i++)
				cards[i].Set();
			cards = cards.OrderBy(c => c.GetNumFieldValue("Value")).OrderBy(c => c.GetFieldValue("Suit")).ToList();
			for (int i = 0; i < cards.Count; i++)
				cards[i].transform.SetSiblingIndex(i);
		} 
#endif
	}

	#region Support classes & enums

	[Serializable]
	public class ZoneMovement
	{
		public NestedConditions condition = new NestedConditions("");
		//TODO Add AnimationCurves
		public bool useAnimationCurves;
		public AnimationCurve curveX;
		public AnimationCurve curveY;
		public AnimationCurve curveZ;
		public float speed = 100f;

		private Dictionary<Card, Movement> movingCards = new Dictionary<Card, Movement>();
		private List<Card> endedMovement = new List<Card>();

		public void Add (Card card, Vector3 destPosition, Quaternion destRotation)
		{
			float moveTime = (destPosition - card.transform.position).magnitude / speed;
			if (moveTime < Time.deltaTime) //Too close
			{
				card.transform.position = destPosition;
				card.transform.rotation = destRotation;
			}
			else
			{
				if (!movingCards.ContainsKey(card))
					movingCards.Add(card, new Movement(card, destPosition, destRotation, moveTime));
				else
					movingCards[card].Change(destPosition, destRotation, moveTime);
			}
		}

		public void Update ()
		{
			foreach (var item in movingCards)
			{
				if (useAnimationCurves)
				{
					if (item.Value.Update(curveX, curveY, curveZ))
						endedMovement.Add(item.Key);
				}
				else
				{
					if (item.Value.Update())
						endedMovement.Add(item.Key);
				}
			}
			for (int i = 0; i < endedMovement.Count; i++)
				movingCards.Remove(endedMovement[i]);
			endedMovement.Clear();
		}

		private struct Movement
		{
			public Card card;
			public Pose origin;
			public Pose destination;
			public float startTime;
			public float totalTime;

			public Movement (Card card, Vector3 destPosition, Quaternion destRotation, float totalTime)
			{
				this.card = card;
				this.totalTime = totalTime;
				destination = new Pose(destPosition, destRotation);
				origin = new Pose(card.transform.position, card.transform.rotation);
				startTime = Time.time;
			}

			public bool Update (AnimationCurve x, AnimationCurve y, AnimationCurve z)
			{
				float t = Mathf.Clamp01((Time.time - startTime) / totalTime);
				card.transform.position = new Vector3(
					Mathf.Lerp(origin.position.x, destination.position.x, t) + x.Evaluate(t),
					Mathf.Lerp(origin.position.y, destination.position.y, t) + y.Evaluate(t),
					Mathf.Lerp(origin.position.z, destination.position.z, t) + z.Evaluate(t));
				card.transform.rotation = Quaternion.Lerp(origin.rotation, destination.rotation, t);
				return t >= 1f;
			}

			public bool Update ()
			{
				float t = Mathf.Clamp01((Time.time - startTime) / totalTime);
				card.transform.position = Vector3.Lerp(origin.position, destination.position, t);
				card.transform.rotation = Quaternion.Lerp(origin.rotation, destination.rotation, t);
				return t >= 1f;
			}

			public void Change (Vector3 destPosition, Quaternion destRotation, float time)
			{
				startTime = Time.time;
				origin = new Pose(card.transform.position, card.transform.rotation);
				destination = new Pose(destPosition, destRotation);
				totalTime = time;
			}
		}
	}

	public class MovementAdditionalInfo
	{
		public string builder;
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
			builder = additionalInfo;
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
			return builder;
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

#endregion

#if UNITY_EDITOR
	[CustomEditor(typeof(Zone)), CanEditMultipleObjects]
	public class ZoneEditor : Editor
	{
		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Organize Child Cards"))
				for (int i = 0; i < targets.Length; i++)
				{
					((Zone)targets[i]).GetCardsInChildren();
					EditorUtility.SetDirty(targets[i]);
				}

			if (GUILayout.Button("Shuffle"))
				for (int i = 0; i < targets.Length; i++)
					((Zone)targets[i]).Shuffle();

			if (GUILayout.Button("Delete All"))
				for (int i = 0; i < targets.Length; i++)
					((Zone)targets[i]).DeleteAll();

			if (GUILayout.Button("Sort"))
				for (int i = 0; i < targets.Length; i++)
				{
					((Zone)targets[i]).Sort();
					EditorUtility.SetDirty(targets[i]);
				}
		}
	}
#endif
}
