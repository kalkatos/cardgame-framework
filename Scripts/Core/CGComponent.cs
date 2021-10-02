using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardgameCore
{
	[SelectionBase]
	public class CGComponent : MonoBehaviour
	{
		public Action<Zone> OnEnteredZone;
		public Action<Zone> OnLeftZone;
		public Action OnUsed;
		public Action<string, string, string> OnFieldValueChanged;
		public Action<string> OnTagAdded;
		public Action<string> OnTagRemoved;

		internal string id;
		public new string name { get { if (!data) return gameObject.name; return data.name; } }
		public string tags { get { if (!data) return name; return data.tags; } }
		public List<Rule> rules { get { if (!data) return null; return data.rules; } }

		public Zone zone;
		internal Zone Zone
		{
			get { return zone; }
			set
			{
				if (zone != null)
					OnLeftZone?.Invoke(zone);
				if (value != null)
					OnEnteredZone?.Invoke(value);
				zone = value;
			}
		}

		internal string[] tagArray;

		[SerializeField] private ComponentData data;

		public List<string> tagList = new List<string>();
		public Dictionary<string, ComponentField> fields = new Dictionary<string, ComponentField>();
		public Dictionary<string, FieldView[]> fieldViews = new Dictionary<string, FieldView[]>();
		public Dictionary<string, TagEventActor[]> tagActors = new Dictionary<string, TagEventActor[]>();

		private void Awake()
		{
			Set();
		}

		public void Set (ComponentData data)
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
							tagActors[currentTag][j].OnTagAdded();
				}
			}
			// Fields
			FieldView[] myFieldViews = GetComponents<FieldView>();
			for (int i = 0; i < data.fields.Count; i++)
			{
				ComponentField field = new ComponentField(data.fields[i]);
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

		internal void BeUsed ()
		{
			OnUsed?.Invoke();
		}

		public void Use ()
		{
			Match.UseComponent(this);
		}

		#region Tag

		public void AddTag (string tag)
		{
			tagList.Add(tag);
			if (tagActors.ContainsKey(tag))
				for (int i = 0; i < tagActors[tag].Length; i++)
					tagActors[tag][i].OnTagAdded();
			tagArray = tagList.ToArray();
			OnTagAdded?.Invoke(tag);
		}

		public void RemoveTag (string tag)
		{
			tagList.Remove(tag);
			if (tagActors.ContainsKey(tag))
				for (int i = 0; i < tagActors[tag].Length; i++)
					tagActors[tag][i].OnTagRemoved();
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
					fields.Add(fieldName, new ComponentField(fieldName, FieldType.Number, value));
				else
					fields.Add(fieldName, new ComponentField(fieldName, FieldType.Text, value));
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
			Debug.LogWarning($"Couldn't find field {fieldName}");
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
					Debug.LogWarning($"Field {fieldName} is not a number");
					return float.NaN;
				}
			}
			Debug.LogWarning($"Couldn't find field {fieldName}");
			return float.NaN;
		}

		public string GetTextFieldValue (string fieldName)
		{
			if (HasField(fieldName))
				return fields[fieldName].value;
			Debug.LogWarning($"Couldn't find field {fieldName}");
			return "";
		}

		#endregion

		public void UseOwnZone ()
		{
			if (Zone)
				Zone.Use();
		}

		public void OrganizeOwnZone ()
		{
			if (Zone)
				Zone.EnqueueOrganize();
		}

		public override string ToString ()
		{
			return $"{name} (id: {id})";
		}

		public void DragEnd ()
		{
			Debug.Log("End Drag - " + ToString());
		}

		public void Drop ()
		{
			Debug.Log("Drop - " + ToString());
		}
	}

	[Serializable]
	public class ComponentField
	{
		public string fieldName;
		public FieldType type;
		public string value;

		public ComponentField (string fieldName, FieldType type, string value)
		{
			this.fieldName = fieldName;
			this.type = type;
			this.value = value;
		}

		public ComponentField (ComponentField other)
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
	[CustomEditor(typeof(CGComponent))]
	public class CGComponentEditor : Editor
	{
		private bool showFields;
		private CGComponent compo;

		private void OnEnable ()
		{
			compo = (CGComponent)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			showFields = EditorGUILayout.Foldout(showFields, "Fields");
			if (showFields)
				foreach (var item in compo.fields)
					GUILayout.Label($"{item.Value.fieldName} | {item.Value.type} | {item.Value.value}");
			if (GUILayout.Button("Update Data"))
				((CGComponent)target).Set();
		}
	}
#endif
}