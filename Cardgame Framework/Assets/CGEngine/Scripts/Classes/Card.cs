﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CGEngine
{
	public class Card : InputObject
	{
		public CardData data;
		string id;
		public string ID
		{
			get { return id; }
			set
			{
				id = value;
				if (!gameObject.name.Contains("(id"))
				{
					gameObject.name.Remove(gameObject.name.IndexOf('('));
				}
				gameObject.name = gameObject.name + "(id:" + id + ")";
			}
		}
		public Player owner;
		public Player controller;
		public Zone zone;
		List<Modifier> modifiers;
		public List<Modifier> Modifiers { get { if (modifiers == null) modifiers = new List<Modifier>(); return modifiers; } }
		public CardField[] fields;
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
						transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 0, transform.rotation.w);
						break;
					case RevealStatus.RevealedToController:
						if (controller && controller.userType == UserType.Local)
							transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 180, transform.rotation.w);
						else
							transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 0, transform.rotation.w);
						break;
					case RevealStatus.RevealedToEveryone:
						transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 180, transform.rotation.w);
						break;
					case RevealStatus.HiddenOnlyToController:
						if (controller && controller.userType == UserType.Local)
							transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 0, transform.rotation.w);
						else
							transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 180, transform.rotation.w);
						break;
				}
				revealStatus = value;
			}
		}

		void Start()
		{
			if (data) SetupData();
		}

		void OnValidate ()
		{
			if (data) SetupData();
		}

		public void SetupData(CardData data)
		{
			this.data = data;
			SetupData();
		}

		public void SetupData()
		{
			if (!data)
			{
				Debug.LogWarning("CGEngine: Card ("+gameObject.name+") doesn't have any data.");
				return;
			}
			if (!gameObject.name.Contains("(id"))
			{
				gameObject.name.Remove(gameObject.name.IndexOf('('));
			}
			gameObject.name = data.name + "(id:" + ID + ")";
			fields = (CardField[])data.fields.Clone();
			SetupCardFieldsInChildren(transform);

			if (!GetComponent<LayeringHelper>()) gameObject.AddComponent<LayeringHelper>();
		}

		void SetupCardFieldsInChildren(Transform cardObject)
		{
			for (int i = 0; i < cardObject.childCount; i++)
			{
				string fieldName = cardObject.GetChild(i).gameObject.name;
				if (fieldName.StartsWith("Field"))
				{
					bool found = false;
					for (int j = 0; j < data.fields.Length; j++)
					{
						if (fieldName.Contains(data.fields[j].name))
						{
							found = true;
							if (data.fields[j].dataType == CardFieldDataType.Text || data.fields[j].dataType == CardFieldDataType.Number)
							{
								TextMeshPro tmp;
								if (!(tmp = cardObject.GetChild(i).GetComponent<TextMeshPro>()))
									tmp = cardObject.GetChild(i).gameObject.AddComponent<TextMeshPro>();
								tmp.text = data.fields[j].dataType == CardFieldDataType.Text ? data.fields[j].stringValue : data.fields[j].numValue.ToString();
								fields[j].linkedTextElement = tmp;
							}
							else
							{
								SpriteRenderer sr;
								if (!(sr = cardObject.GetChild(i).GetComponent<SpriteRenderer>()))
									sr = cardObject.GetChild(i).gameObject.AddComponent<SpriteRenderer>();
								sr.sprite = data.fields[j].imageValue;
								fields[j].linkedImageElement = sr;
							}
							break;
						}
					}
					if (!found)
					{
						Debug.LogWarning("CGEngine: Card field (" + fieldName + ") of Card (" + transform.gameObject.name + ") was not found in Card Data (" + data.name + ") definitions");
					}
				}
				if (cardObject.GetChild(i).childCount > 0)
					SetupCardFieldsInChildren(cardObject.GetChild(i));
			}
		}

		public void ChangeCardField(string fieldName, string textValue = "", double numValue = 0, Sprite imageValue = null)
		{
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].name == fieldName)
				{
					switch (fields[i].dataType)
					{
						case CardFieldDataType.Text:
							fields[i].stringValue = textValue;
							fields[i].linkedTextElement.text = textValue;
							break;
						case CardFieldDataType.Number:
							fields[i].numValue = numValue;
							fields[i].linkedTextElement.text = numValue.ToString();
							break;
						case CardFieldDataType.Image:
							fields[i].imageValue = imageValue;
							fields[i].linkedImageElement.sprite = imageValue;
							break;
					}
					return;
				}
			}
		}

		public void ChangeCardFieldBy(string fieldName, double value)
		{
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].name == fieldName)
				{
					if (fields[i].dataType != CardFieldDataType.Number)
					{
						Debug.LogError("CGEngine: Error changing card field " + fieldName + ". It is not a number.");
						return;
					}
					fields[i].numValue += value;
					fields[i].linkedTextElement.text = fields[i].numValue.ToString();
					return;
				}
			}
		}



	}
}