using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace CardGameFramework
{
	/// <summary>
	/// Class for a card in game.
	/// </summary>
	public class Card : MonoBehaviour
	{
		public string ID;
		public CardData data;
		//public Player owner;
		//public Player controller;
		public Zone zone;
		public List<string> tags;
		public int positionInGridZone = -1;
		List<Rule> rules;
		public List<Rule> Rules { get { if (rules == null) rules = new List<Rule>(); return rules; } }
		//public CardField[] fields;
		//CardField[] fields;
		public Dictionary<string, CardField> fields { get; private set; }
		Dictionary<CardField, Component> fieldToComponents;
		RevealStatus revealStatus;
		public RevealStatus RevealStatus
		{
			get
			{
				return revealStatus;
			}
			set
			{
				switch (value)
				{
					case RevealStatus.Hidden:
					case RevealStatus.HiddenOnlyToController:
						transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 0, transform.rotation.w);
						break;
					case RevealStatus.RevealedToEveryone:
					case RevealStatus.RevealedToController:
						transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 180, transform.rotation.w);
						break;
				}
				revealStatus = value;
			}
		}

		void Start ()
		{
			if (data != null) SetupData();
		}

		void OnValidate ()
		{
			if (data != null) SetupData(true);
		}

		public void SetupData (CardData data)
		{
			this.data = data;
			SetupData();
		}

		public float GetNumFieldValue (string fieldName)
		{
			return fields[fieldName].numValue;
		}

		public string GetTextFieldValue (string fieldName)
		{
			return fields[fieldName].stringValue;
		}

		public Sprite GetImageFieldValue (string fieldName)
		{
			return fields[fieldName].imageValue;
		}

		public void SetupData (bool onValidade = false)
		{
			if (data == null)
			{
				Debug.LogWarning($"[CGEngine] Card ({gameObject.name}) doesn't have any data.");
				return;
			}

			fields = new Dictionary<string, CardField>();
			tags = new List<string>();
			tags.AddRange(StringUtility.GetCleanStringForInstructions(data.tags).Split(','));
			
			for (int i = 0; i < data.fields.Count; i++)
			{
				fields.Add(data.fields[i].fieldName, new CardField(data.fields[i]));
			}
			SetupCardFieldsInChildren(transform);

			if (!GetComponent<LayeringHelper>()) gameObject.AddComponent<LayeringHelper>();
		}

		public bool HasTag (string tag)
		{
			return tags.Contains(tag);
		}

		public void AddTag (string tag)
		{
			if (!HasTag(tag))
				tags.Add(tag);
		}

		public void RemoveTag (string tag)
		{
			tags.Remove(tag);
		}

		public bool HasField (string fieldName)
		{
			return fields.ContainsKey(fieldName);
		}

		public CardFieldDataType GetFieldDataType (string fieldName)
		{
			if (HasField(fieldName))
				return fields[fieldName].dataType;
			return CardFieldDataType.None;
		}

		public void SetCardFieldValue (string fieldName, float numValue)
		{
			SetCardFieldValue(fieldName, "", numValue);
		}

		public void SetCardFieldValue (string fieldName, string textValue = "", float numValue = 0, Sprite imageValue = null)
		{
			CardField field = fields[fieldName];
			bool hasComponent = fieldToComponents.ContainsKey(field);
			switch (field.dataType)
			{
				case CardFieldDataType.Text:
					field.stringValue = textValue;
					if (hasComponent)
						((TextMeshPro)fieldToComponents[field]).text = textValue;
					break;
				case CardFieldDataType.Number:
					field.numValue = numValue;
					string valToShow = "";
					if (field.hideOption == CardFieldHideOption.AlwaysHide)
						valToShow = "";
					else if (field.hideOption == CardFieldHideOption.AlwaysShow)
						valToShow = numValue.ToString();
					else if (field.hideOption == CardFieldHideOption.ShowIfDifferentFromZero)
						if (numValue == 0)
							valToShow = "";
						else
							valToShow = numValue.ToString();
					if (hasComponent)
						((TextMeshPro)fieldToComponents[field]).text = valToShow;
					break;
				case CardFieldDataType.Image:
					field.imageValue = imageValue;
					if (hasComponent)
						((SpriteRenderer)fieldToComponents[field]).sprite = imageValue;
					break;
			}
		}

		public void AddRule (Rule rule)
		{
			Rules.Add(rule);
		}

		public void Use ()
		{
			Match.Current.UseCard(this);
		}

		public override string ToString ()
		{
			return $"{name} : {data.cardDataID} , tags( {data.tags} )";
		}

		internal string GetTagsFromRules ()
		{
			StringBuilder sb = new StringBuilder();
			if (Rules != null)
			{
				for (int i = 0; i < Rules.Count; i++)
				{
					sb.Append(Rules[i].tags);
					if (i < Rules.Count - 1)
						sb.Append(",");
				}
			}
			return sb.ToString();
		}

		void SetupCardFieldsInChildren (Transform cardObject)
		{
			if (fieldToComponents == null)
				fieldToComponents = new Dictionary<CardField, Component>();

			foreach (KeyValuePair<string, CardField> item in fields)
			{
				string fieldName = item.Key;
				Transform fieldObject = FindChildWithFieldName(cardObject, fieldName);
				if (fieldObject != null)
				{
					switch (item.Value.dataType)
					{
						case CardFieldDataType.Text:
						case CardFieldDataType.Number:
							TextMeshPro textObject = fieldObject.GetComponent<TextMeshPro>();
							if (textObject)
							{
								if (!fieldToComponents.ContainsKey(item.Value))
									fieldToComponents.Add(item.Value, textObject);
								if (item.Value.dataType == CardFieldDataType.Number)
								{
									if (item.Value.hideOption == CardFieldHideOption.AlwaysShow)
										textObject.text = item.Value.numValue.ToString();
									else if (item.Value.hideOption == CardFieldHideOption.AlwaysHide)
										textObject.text = "";
									else if (item.Value.hideOption == CardFieldHideOption.ShowIfDifferentFromZero)
										textObject.text = item.Value.numValue == 0 ? "" : item.Value.numValue.ToString();
								}
								else
								{
									textObject.text = item.Value.stringValue;
								}
							}
							else
								Debug.LogWarning("[CGEngine] Couldn't find a TextMeshPro object for field " + fieldName);
							break;
						case CardFieldDataType.Image:
							SpriteRenderer spriteObject = fieldObject.GetComponent<SpriteRenderer>();
							if (spriteObject)
							{
								if (!fieldToComponents.ContainsKey(item.Value))
									fieldToComponents.Add(item.Value, spriteObject);
								spriteObject.sprite = item.Value.imageValue;
							}
							else
								Debug.LogWarning("[CGEngine] Couldn't find a SpriteRenderer object for field " + fieldName);
							break;
						case CardFieldDataType.None:
							break;
						default:
							break;
					}
				}
			}
		}

		Transform FindChildWithFieldName (Transform parent, string name)
		{
			if (parent.name == "Field-" + name)
				return parent;

			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				if (child.name == "Field-" + name)
					return child;

				Transform found = null;
				if (child.childCount > 0)
					found = FindChildWithFieldName(child, name);

				if (found)
					return found;
			}
			return null;
		}

	}
}