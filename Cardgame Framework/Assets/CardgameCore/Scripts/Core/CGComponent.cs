using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardgameCore
{
	[SelectionBase]
	public class CGComponent : MonoBehaviour
	{
		public Action<string, string, string> OnFieldValueChanged;
		public Action<string> OnTagAdded;
		public Action<string> OnTagRemoved;

		internal string id;
		public new string name { get { if (!data) return gameObject.name; return data.name; } }
		public string tags { get { if (!data) return name; return data.tags; } }
		public List<Rule> rules { get { if (!data) return null; return data.rules; } }

		internal Zone zone;

		[SerializeField] private ComponentData data; 

		private List<string> tagList = new List<string>();
		private Dictionary<string, ComponentField> fields = new Dictionary<string, ComponentField>();
		private Dictionary<string, FieldView[]> fieldViews = new Dictionary<string, FieldView[]>();
		private Dictionary<string, TagEventActor[]> tagActors = new Dictionary<string, TagEventActor[]>();

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
				tagList.AddRange(data.tags.Split(','));
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
				ComponentField field = data.fields[i];
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

		// ========== TAG ================
		public void AddTag (string tag)
		{
			tagList.Add(tag);
			if (tagActors.ContainsKey(tag))
				for (int i = 0; i < tagActors[tag].Length; i++)
					tagActors[tag][i].OnTagAdded();
			OnTagAdded?.Invoke(tag);
			Debug.Log($"  Debug: {name} added tag {tag}");
		}

		public void RemoveTag (string tag)
		{
			tagList.Remove(tag);
			if (tagActors.ContainsKey(tag))
				for (int i = 0; i < tagActors[tag].Length; i++)
					tagActors[tag][i].OnTagRemoved();
			OnTagRemoved?.Invoke(tag);
			Debug.Log($"  Debug: {name} removed tag {tag}");
		}

		public bool HasTag (string tag)
		{
			return tagList.Contains(tag);
		}

		// ========== FIELD ================
		public void SetFieldValue (string fieldName, string value)
		{
			if (fields.ContainsKey(fieldName))
			{
				string oldValue = fields[fieldName].value;
				fields[fieldName].value = value;
				for (int i = 0; fieldViews[fieldName] != null && i < fieldViews[fieldName].Length; i++)
					fieldViews[fieldName][i].SetFieldViewValue(value);
				OnFieldValueChanged?.Invoke(fieldName, oldValue, value);
				Debug.Log($"  Debug: {name} changed field {fieldName} from {oldValue} to {value}");
			}
		}

		public bool HasField (string fieldName)
		{
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

		public override string ToString ()
		{
			return $"Component: {name} (id: {id})";
		}
	}

	[Serializable]
	public class ComponentField
	{
		public string fieldName;
		public FieldType type;
		public string value;
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
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Update Data"))
				((CGComponent)target).Set();
		}
	}
#endif
}