using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
	public class Component : CardgameObject
	{
		public new string name { get { if (!data) return ""; return data.name; } }
		public string tags { get { if (!data) return ""; return data.tags; } }
		public List<Rule> rules { get { if (!data) return null; return data.rules; } }

		internal Zone zone;

		[SerializeField] private ComponentData data;

		private List<string> tagList = new List<string>();
		private Dictionary<string, ComponentField> fields = new Dictionary<string, ComponentField>();

		public void Set (ComponentData data)
		{
			this.data = data;
			Set();
		}

		public void Set ()
		{
			if (!data)
				return;

			tagList.Clear();
			if (!string.IsNullOrEmpty(data.tags))
				tagList.AddRange(data.tags.Split(','));
			for (int i = 0; i < data.fields.Count; i++)
			{
				ComponentField field = data.fields[i];
				if (!fields.ContainsKey(field.fieldName))
					fields.Add(field.fieldName, field);
			}
		}

		public void AddTag (string tag)
		{
			tagList.Add(tag);
		}

		public void RemoveTag (string tag)
		{
			tagList.Remove(tag);
		}

		public void SetFieldValue (string fieldName, string value)
		{
			if (fields.ContainsKey(fieldName))
				fields[fieldName].value = value;
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

		public bool HasTag (string tag)
		{
			return tagList.Contains(tag);
		}

		public override string ToString ()
		{
			return $"Component: {name} (id: {id})";
		}

		public FieldType GetFieldDataType(string fieldName)
		{
			if (HasField(fieldName))
				return fields[fieldName].type;
			Debug.LogWarning($"Couldn't find field {fieldName}");
			return FieldType.Undefined;
		}

		public float GetNumFieldValue(string fieldName)
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

		public string GetTextFieldValue(string fieldName)
		{
			if (HasField(fieldName))
				return fields[fieldName].value;
			Debug.LogWarning($"Couldn't find field {fieldName}");
			return "";
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
}