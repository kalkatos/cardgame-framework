using System;
using System.Collections.Generic;
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
		List<Modifier> modifiers;
		public List<Modifier> Modifiers { get { if (modifiers == null) modifiers = new List<Modifier>(); return modifiers; } }
		public CardField[] fields;
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
				//TEST
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

		void Start()
		{
			if (data != null) SetupData();
		}

		void OnValidate()
		{
			if (data != null) SetupData(true);
		}

		public void SetupData(CardData data)
		{
			this.data = data;
			SetupData();
		}

		public void SetupData(bool onValidade = false)
		{
			if (data == null)
			{
				Debug.LogWarning("CGEngine: Card (" + gameObject.name + ") doesn't have any data.");
				return;
			}

			fields = new CardField[data.fields.Count];
			for (int i = 0; i < data.fields.Count; i++)
			{
				fields[i] = new CardField(data.fields[i]);
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

		void SetupCardFieldsInChildren(Transform cardObject)
		{
			if (fieldToComponents == null)
				fieldToComponents = new Dictionary<CardField, Component>();

			for (int i = 0; i < fields.Length; i++)
			{
				string fieldName = fields[i].fieldName;
				Transform fieldObject = FindChildWithName(cardObject, fieldName);
				if (fieldObject != null)
				{
					switch (fields[i].dataType)
					{
						case CardFieldDataType.Text:
						case CardFieldDataType.Number:
							TextMeshPro textObject = fieldObject.GetComponent<TextMeshPro>();
							if (textObject)
							{
								if (!fieldToComponents.ContainsKey(fields[i]))
									fieldToComponents.Add(fields[i], textObject);
								if (fields[i].dataType == CardFieldDataType.Number)
								{
									if (fields[i].hideOption == CardFieldHideOption.AlwaysShow)
										textObject.text = fields[i].numValue.ToString();
									else if (fields[i].hideOption == CardFieldHideOption.AlwaysHide)
										textObject.text = "";
									else if (fields[i].hideOption == CardFieldHideOption.ShowIfDifferentFromZero)
										textObject.text = fields[i].numValue == 0 ? "" : fields[i].numValue.ToString();
								}
								else
								{
									textObject.text = fields[i].stringValue;
								}
							}
							else
								Debug.LogWarning("CGEngine: Couldn't find a TextMeshPro object for field " + fieldName);
							break;
						case CardFieldDataType.Image:
							SpriteRenderer spriteObject = fieldObject.GetComponent<SpriteRenderer>();
							if (spriteObject)
							{
								if (!fieldToComponents.ContainsKey(fields[i]))
									fieldToComponents.Add(fields[i], spriteObject);
								spriteObject.sprite = fields[i].imageValue;
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
				//else
				//{
				//	Debug.LogWarning("CGEngine: Couldn't find an object named with the field "+fieldName);
				//}
			}

			//for (int i = 0; i < cardObject.childCount; i++)
			//{
			//	Transform child = cardObject.GetChild(i);
			//	string childName = child.gameObject.name;
			//	if (childName.StartsWith("Field"))
			//	{
			//		bool found = false;
			//		for (int j = 0; j < fields.Length; j++)
			//		{
			//			Debug.Log("DEBUG Treating field " + fields[i].name + " from object " + childName + " child of " + cardObject.name);
			//			if (childName.Contains(fields[j].name))
			//			{
			//				found = true;
			//				if (fields[j].dataType == CardFieldDataType.Text || fields[j].dataType == CardFieldDataType.Number)
			//				{
			//					TextMeshPro tmp;
			//					if (!(tmp = child.GetComponent<TextMeshPro>()))
			//						tmp = child.gameObject.AddComponent<TextMeshPro>();
			//					tmp.text = fields[j].dataType == CardFieldDataType.Text ? fields[j].stringValue : fields[j].numValue.ToString();
			//					//TEST
			//					fieldToComponents.Add(fields[j], tmp);
			//					//fields[j].linkedTextElement = tmp;
			//				}
			//				else
			//				{
			//					SpriteRenderer sr;
			//					if (!(sr = child.GetComponent<SpriteRenderer>()))
			//						sr = child.gameObject.AddComponent<SpriteRenderer>();
			//					sr.sprite = fields[j].imageValue;
			//					//TEST
			//					fieldToComponents.Add(fields[j], sr);
			//					//fields[j].linkedImageElement = sr;
			//				}
			//				break;
			//			}
			//		}
			//		if (!found)
			//		{
			//			Debug.LogWarning("CGEngine: Card field (" + childName + ") of Card (" + transform.gameObject.name + ") was not found in Card Data (" + data.name + ") definitions");
			//		}
			//	}
			//	if (child.childCount > 0)
			//		SetupCardFieldsInChildren(child);
			//}
		}

		Transform FindChildWithName (Transform parent, string name)
		{
			if (parent.name == "Field-" + name)
				return parent;

			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				if (child.name == "Field-"+name)
					return child;

				Transform found = null;
				if (child.childCount > 0)
					 found = FindChildWithName(child, name);

				if (found)
					return found;
			}
			return null;
		}

		public void ChangeCardField(string fieldName, double numValue)
		{
			ChangeCardField(fieldName, "", numValue);
		}

		public void ChangeCardField(string fieldName, string textValue = "", double numValue = 0, Sprite imageValue = null)
		{
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].fieldName == fieldName)
				{
					switch (fields[i].dataType)
					{
						case CardFieldDataType.Text:
							fields[i].stringValue = textValue;
							if (fieldToComponents.ContainsKey(fields[i]))
								((TextMeshPro)fieldToComponents[fields[i]]).text = textValue;
							//fields[i].linkedTextElement.text = textValue;
							break;
						case CardFieldDataType.Number:
							fields[i].numValue = numValue;
							string valToShow = "";
							if (fields[i].hideOption == CardFieldHideOption.AlwaysHide)
								valToShow = "";
							else if (fields[i].hideOption == CardFieldHideOption.AlwaysShow)
								valToShow = numValue.ToString();
							else if (fields[i].hideOption == CardFieldHideOption.ShowIfDifferentFromZero)
								if (numValue == 0)
									valToShow = "";
								else
									valToShow = numValue.ToString();
							if (fieldToComponents.ContainsKey(fields[i]))
								((TextMeshPro)fieldToComponents[fields[i]]).text = valToShow;
							break;
						case CardFieldDataType.Image:
							fields[i].imageValue = imageValue;
							if (fieldToComponents.ContainsKey(fields[i]))
								((SpriteRenderer)fieldToComponents[fields[i]]).sprite = imageValue;
							break;
						case CardFieldDataType.None:
							//BUG dos valores numéricos
							break;
					}
					return;
				}
			}
		}

		internal bool HasModifierWithTag (string v)
		{
			if (Modifiers != null)
			{
				for (int i = 0; i < Modifiers.Count; i++)
				{
					if (Modifiers[i].tags != null)
					{
						for (int j = 0; j < Modifiers[i].tags.Count; j++)
						{
							if (!string.Equals(v, Modifiers[i].tags[j]))
								continue;
							return true;
						}
					}
				}
			}
			return false;
		}

		public void ChangeCardFieldBy(string fieldName, double value)
		{
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].fieldName == fieldName)
				{
					if (fields[i].dataType != CardFieldDataType.Number)
					{
						Debug.LogError("CGEngine: Error changing card field " + fieldName + ". It is not a number.");
						return;
					}
					fields[i].numValue += value;
					((TextMeshPro)fieldToComponents[fields[i]]).text = fields[i].numValue.ToString();
					//fields[i].linkedTextElement.text = fields[i].numValue.ToString();
					return;
				}
			}
		}

		//public void AddModifiers (ModifierData modData)
		//{
		//	Modifier newMod = Match.Current.CreateModifier(modData);
		//	newMod.Origin = ID;
		//	Modifiers.Add(newMod);
		//}

		public void AddModifiers (string modDefinition)
		{
			Modifiers.Add(Match.Current.CreateModifier(modDefinition));
		}

		public void AddModifiers(Modifier mod, bool activatedModifier = false)
		{
			Debug.Log("DEBUG " + mod);
			mod.Origin = ID;
			Modifiers.Add(mod);
		}

		public void RemoveModifiers (Modifier mod)
		{
			mod.Origin = "";
			Modifiers.Remove(mod);
		}
	}
}