using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardgameFramework
{
	[SelectionBase]
	public class Card : MonoBehaviour
	{
		public event Action<Zone> OnWillEnterZone;
		public event Action<Zone> OnEnteredZone;
		public event Action<Zone> OnWillLeaveZone;
		public event Action<Zone> OnLeftZone;
		public event Action OnUsed;
		public event Action<string, string, string> OnFieldValueChanged;
		public event Action<string> OnTagAdded;
		public event Action<string> OnTagRemoved;

		internal string id;
		public string Tags { get { if (!data) return name; return data.tags; } }
		public List<Rule> Rules { get { if (!data) return null; return data.rules; } }

		private Zone zone;
		public Zone Zone
		{
			get { return zone; }
			internal set { zone = value; }
		}

		internal string[] tagArray;

		[SerializeField] private CardData data;

		public List<string> tagList = new List<string>();
		public Dictionary<string, CardField> fields = new Dictionary<string, CardField>();
		public Dictionary<string, FieldView[]> fieldViews = new Dictionary<string, FieldView[]>();
		public Dictionary<string, TagEventActor[]> tagActors = new Dictionary<string, TagEventActor[]>();

		private void Awake()
		{
			Set();
		}

		public void Set (CardData data)
		{
			this.data = data;
			Set();
		}

		public void Set ()
		{
			if (!data)
				return;
			// TAGS
			TagEventActor[] myTagActors = GetComponents<TagEventActor>();
			if (myTagActors != null)
				for (int i = 0; i < myTagActors.Length && myTagActors[i]; i++)
				{
					List<TagEventActor> actorsFound = new List<TagEventActor>();
					actorsFound.Add(myTagActors[i]);
					for (int j = i + 1; j < myTagActors.Length; j++)
					{
						if (myTagActors[j].tagToAct == myTagActors[i].tagToAct)
						{
							actorsFound.Add(myTagActors[j]);
							myTagActors[j] = null;
						}
					}
					if (!tagActors.ContainsKey(myTagActors[i].tagToAct))
						tagActors.Add(myTagActors[i].tagToAct, actorsFound.ToArray());
					else
						tagActors[myTagActors[i].tagToAct] = actorsFound.ToArray();
				}
			tagList.Clear();
			if (!string.IsNullOrEmpty(data.tags))
			{
				tagArray = data.tags.Split(',');
				tagList.AddRange(tagArray);
				for (int i = 0; i < tagList.Count; i++)
				{
					string currentTag = tagList[i];
					if (tagActors.ContainsKey(currentTag))
						for (int j = 0; j < tagActors[currentTag].Length; j++)
							tagActors[currentTag][j].OnTagAdded(currentTag);
				}
			}
			// Fields
			FieldView[] myFieldViews = GetComponents<FieldView>();
			for (int i = 0; i < data.fields.Count; i++)
			{
				CardField field = new CardField(data.fields[i]);
				List<FieldView> viewsFound = new List<FieldView>();
				if (!fields.ContainsKey(field.fieldName))
					fields.Add(field.fieldName, field);
				if (myFieldViews != null)
					for (int j = 0; j < myFieldViews.Length; j++)
						if (myFieldViews[j].targetFieldName == field.fieldName)
						{
							viewsFound.Add(myFieldViews[j]);
							myFieldViews[j].SetFieldViewValue(field.value);
						}
				if (!fieldViews.ContainsKey(field.fieldName))
					fieldViews.Add(field.fieldName, viewsFound.ToArray());
				else
					fieldViews[field.fieldName] = viewsFound.ToArray();
			}
		}

		internal void RaiseUsedEvent ()
		{
			OnUsed?.Invoke();
		}

		internal void RaiseWillLeaveZoneEvent (Zone zone)
		{
			OnWillLeaveZone?.Invoke(zone);
		}
		
		internal void RaiseZoneLeftEvent (Zone zone)
		{
			OnLeftZone?.Invoke(zone);
		}

		internal void RaiseWillEnterZoneEvent (Zone zone)
		{
			OnWillEnterZone?.Invoke(zone);
		}

		internal void RaiseEnteredZoneEvent (Zone zone)
		{
			OnEnteredZone?.Invoke(zone);
		}

		public void Use (string origin)
		{
			Match.UseCard(this, origin);
		}

		public void Use (string origin, string additionalInfo)
		{
			Match.UseCard(this, origin, additionalInfo);
		}

		#region Tag

		public void AddTag (string tag)
		{
			tagList.Add(tag);
			if (tagActors.ContainsKey(tag))
				for (int i = 0; i < tagActors[tag].Length; i++)
					tagActors[tag][i].OnTagAdded(tag);
			tagArray = tagList.ToArray();
			OnTagAdded?.Invoke(tag);
		}

		public void RemoveTag (string tag)
		{
			tagList.Remove(tag);
			if (tagActors.ContainsKey(tag))
				for (int i = 0; i < tagActors[tag].Length; i++)
					tagActors[tag][i].OnTagRemoved(tag);
			tagArray = tagList.ToArray();
			OnTagRemoved?.Invoke(tag);
		}

		public bool HasTag (string tag)
		{
			return tagList.Contains(tag);
		}

		#endregion

		#region Field

		public void SetFieldValue (string fieldName, string value, string additionalInfo)
		{
			if (fields.ContainsKey(fieldName))
			{
				string oldValue = fields[fieldName].value;
				char firstVarChar = value[0];
				if (firstVarChar == '+' || firstVarChar == '*' || firstVarChar == '/' || firstVarChar == '%' || firstVarChar == '^')
					value = Getter.Build(oldValue + firstVarChar + value).Get().ToString();
				fields[fieldName].value = value;
				for (int i = 0; fieldViews[fieldName] != null && i < fieldViews[fieldName].Length; i++)
					fieldViews[fieldName][i].SetFieldViewValue(value);
				OnFieldValueChanged?.Invoke(fieldName, oldValue, value);
			}
			else if (!string.IsNullOrEmpty(fieldName))
			{
				if (float.TryParse(value, out float numValue))
					fields.Add(fieldName, new CardField(fieldName, FieldType.Number, value));
				else
					fields.Add(fieldName, new CardField(fieldName, FieldType.Text, value));
			}
		}

		public bool HasField (string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
				return false;
			return fields.ContainsKey(fieldName);
		}

		public string GetFieldValue (string fieldName)
		{
			if (HasField(fieldName))
				return fields[fieldName].value;
			return string.Empty;
		}

		public FieldType GetFieldDataType (string fieldName)
		{
			if (HasField(fieldName))
				return fields[fieldName].type;
			CustomDebug.LogWarning($"Couldn't find field {fieldName}");
			return FieldType.Undefined;
		}

		public float GetNumFieldValue (string fieldName)
		{
			if (HasField(fieldName))
			{
				if (GetFieldDataType(fieldName) == FieldType.Number)
					return float.Parse(fields[fieldName].value);
				else
				{
					CustomDebug.LogWarning($"Field {fieldName} is not a number");
					return float.NaN;
				}
			}
			CustomDebug.LogWarning($"Couldn't find field {fieldName}");
			return float.NaN;
		}

		public string GetTextFieldValue (string fieldName)
		{
			if (HasField(fieldName))
				return fields[fieldName].value;
			CustomDebug.LogWarning($"Couldn't find field {fieldName}");
			return "";
		}

		#endregion
		
		public void UseOwnZone (string origin)
		{
			if (Zone)
				Zone.Use(origin);
		}

		public void UseOwnZone (string origin, string additionalInfo)
		{
			if (Zone)
				Zone.Use(origin, additionalInfo);
		}

		public void OrganizeOwnZone (string origin)
		{
			if (Zone)
				Zone.EnqueueOrganize(origin);
		}

		public override string ToString ()
		{
			return name;
		}
	}

	[Serializable]
	public class CardField
	{
		public string fieldName;
		public FieldType type;
		public string value;

		public CardField (string fieldName, FieldType type, string value)
		{
			this.fieldName = fieldName;
			this.type = type;
			this.value = value;
		}

		public CardField (CardField other)
		{
			fieldName = other.fieldName;
			type = other.type;
			value = other.value;
		}
	}

	public enum FieldType
	{
		Undefined,
		Text,
		Number,
		Image
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(Card)), CanEditMultipleObjects]
	public class CardEditor : Editor
	{
		private bool showFields;
		private Card card;

		private void OnEnable ()
		{
			card = (Card)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			showFields = EditorGUILayout.Foldout(showFields, "Fields");
			if (showFields)
				foreach (var item in card.fields)
					GUILayout.Label($"{item.Value.fieldName} | {item.Value.type} | {item.Value.value}");
			if (GUILayout.Button("Update Data"))
				for (int i = 0; i < targets.Length; i++)
				{
					((Card)targets[i]).Set();
					EditorUtility.SetDirty(targets[i]);
				}
		}
	}
#endif
}