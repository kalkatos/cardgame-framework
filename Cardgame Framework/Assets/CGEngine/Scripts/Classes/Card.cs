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
		public CardData data;
		public string ID;
		//public Player owner;
		//public Player controller;
		public Zone zone;
		public int positionInGridZone = -1;
		List<Modifier> modifiers;
		public List<Modifier> Modifiers { get { if (modifiers == null) modifiers = new List<Modifier>(); return modifiers; } }
		//public CardField[] fields;
		//CardField[] fields;
		Dictionary<string, CardField> fields;
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

		public double GetNumFieldValue (string fieldName)
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
				Debug.LogWarning("CGEngine: Card (" + gameObject.name + ") doesn't have any data.");
				return;
			}

			fields = new Dictionary<string, CardField>();
			//fields = new CardField[data.fields.Count];
			for (int i = 0; i < data.fields.Count; i++)
			{
				fields.Add(data.fields[i].fieldName, new CardField(data.fields[i]));
				//switch (fields[i].dataType)
				//{
				//	case CardFieldDataType.Text:
				//		fieldValues.Add(fields[i].fieldName, fields[i].stringValue);
				//		break;
				//	case CardFieldDataType.Number:
				//		fieldValues.Add(fields[i].fieldName, fields[i].numValue);
				//		break;
				//	case CardFieldDataType.Image:
				//		fieldValues.Add(fields[i].fieldName, fields[i].imageValue);
				//		break;
				//	case CardFieldDataType.None:
				//		fieldValues.Add(fields[i].fieldName, 0);
				//		break;
				//}
			}
			SetupCardFieldsInChildren(transform);

			//if (!onValidade && data.cardModifiers != null)
			//{
			//	for (int i = 0; i < data.cardModifiers.Length; i++)
			//	{
			//		AddModifiers(data.cardModifiers[i]);
			//	}
			//}

			if (!GetComponent<LayeringHelper>()) gameObject.AddComponent<LayeringHelper>();
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
								Debug.LogWarning("CGEngine: Couldn't find a TextMeshPro object for field " + fieldName);
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
								Debug.LogWarning("CGEngine: Couldn't find a SpriteRenderer object for field " + fieldName);
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

		public bool HasTag (string tag)
		{
			string dataTags = "," + data.tags + ",";
			return dataTags.Contains("," + tag + ",");
		}

		public bool HasField (string fieldName)
		{
			return fields.ContainsKey(fieldName);
		}

		public CardFieldDataType GetFieldDataType (string fieldName)
		{
			return fields[fieldName].dataType;
		}

		public void SetCardFieldValue (string fieldName, double numValue)
		{
			SetCardFieldValue(fieldName, "", numValue);
		}

		public void SetCardFieldValue (string fieldName, string textValue = "", double numValue = 0, Sprite imageValue = null)
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

		internal string GetTagsFromModifiers ()
		{
			StringBuilder sb = new StringBuilder();
			if (Modifiers != null)
			{
				for (int i = 0; i < Modifiers.Count; i++)
				{
					sb.Append(Modifiers[i].tags);
					if (i < Modifiers.Count - 1)
						sb.Append(",");
				}
			}
			return sb.ToString();
		}

		//public void ChangeCardFieldBy (string fieldName, double value)
		//{
		//	CardField field = fields[fieldName];
		//	if (field.dataType != CardFieldDataType.Number)
		//	{
		//		Debug.LogError("CGEngine: Error changing card field " + fieldName + ". It is not a number.");
		//		return;
		//	}
		//	field.numValue += value;
		//	if (fieldToComponents.ContainsKey(field))
		//	{
		//		string valToShow = "";
		//		if (field.hideOption == CardFieldHideOption.AlwaysShow)
		//			valToShow = value.ToString();
		//		else if (field.hideOption == CardFieldHideOption.ShowIfDifferentFromZero)
		//			if (value == 0)
		//				valToShow = "";
		//			else
		//				valToShow = value.ToString();
		//		((TextMeshPro)fieldToComponents[field]).text = valToShow;
		//	}
		//	return;
		//}

		//public void AddModifiers (ModifierData modData)
		//{
		//	Modifier newMod = Match.Current.CreateModifier(modData);
		//	newMod.Origin = ID;
		//	Modifiers.Add(newMod);
		//}

		//public void AddModifiers (string modDefinition)
		//{
		//	Modifiers.Add(Match.Current.CreateModifier(modDefinition));
		//}

		public void AddModifiers (Modifier mod, bool activatedModifier = false)
		{
			Debug.Log("DEBUG " + mod);
			mod.Origin = ID;
			Modifiers.Add(mod);
		}

		//public void RemoveModifiers (Modifier mod)
		//{
		//	mod.Origin = "";
		//	Modifiers.Remove(mod);
		//}
	}
}