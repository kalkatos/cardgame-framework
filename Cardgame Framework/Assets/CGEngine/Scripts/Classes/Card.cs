using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace CardGameFramework
{
	[SelectionBase]
	/// <summary>
	/// Class for a card in game.
	/// </summary>
	public class Card : MonoBehaviour
	{
		public string ID { get; internal set; }
		public CardData data;
		public Zone zone { get; internal set; }
		public List<string> tags;
		public int slotInZone { get; internal set; }
		private List<Rule> _rules;
		public List<Rule> rules { get { if (_rules == null) _rules = new List<Rule>(); return _rules; } }
		public Dictionary<string, CardField> fields { get; private set; }
		private Dictionary<string, List<Component>> fieldToComponents;
		private Dictionary<string, List<GameObject>> tagsToObjects;
		private RevealStatus _revealStatus;
		private Transform _frontObject;
		public Transform frontObject { get { if (!_frontObject) _frontObject = transform.Find("FrontObject"); return _frontObject; } }
		private Transform _backObject;
		public Transform backObject { get { if (!_backObject) _backObject = transform.Find("BackObject"); return _backObject; } }
		public bool faceup { get { return frontObject.localEulerAngles.x < 180; } }
		public RevealStatus revealStatus
		{
			get
			{
				return _revealStatus;
			}
			set
			{
				switch (value)
				{
					case RevealStatus.Hidden:
					case RevealStatus.HiddenOnlyToController:
						Flip(false);
						break;
					case RevealStatus.RevealedToEveryone:
					case RevealStatus.RevealedToController:
						Flip(true);
						break;
				}
				_revealStatus = value;
			}
		}
		private InputObject _inputObject;
		public InputObject inputObject { get { if (_inputObject == null) _inputObject = GetComponent<InputObject>(); return _inputObject; } }
		private Collider _collider;
		public new Collider collider { get { if (_collider == null) _collider = GetComponent<Collider>(); return _collider; } }

		public UnityEvent onTagAdded;
		public UnityEvent onTagRemoved;

		void Start ()
		{
			if (data != null) SetupData();
		}

		void OnValidate ()
		{
			if (data != null) SetupData(true);
		}

		protected virtual void OnTagAdded (string tag) { }

		protected virtual void OnTagRemoved (string tag) { }

		public void Flip ()
		{
			Flip(!faceup);
		}

		public void Flip (bool up)
		{
			if (up)
			{
				frontObject.localEulerAngles = new Vector3(90, 0, 0);
				backObject.localEulerAngles = new Vector3(-90, 180, 0);
			}
			else
			{
				frontObject.localEulerAngles = new Vector3(-90, 180, 0);
				backObject.localEulerAngles = new Vector3(90, 0, 0);
			}
		}

		public void SetupData (CardData data)
		{
			this.data = data;
			SetupData();
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
			BuildCardToObjectReferences();

			if (!GetComponent<LayeringHelper>()) gameObject.AddComponent<LayeringHelper>();
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

		public bool HasTag (string tag)
		{
			return tags.Contains(tag);
		}

		public void AddTag (string tag)
		{
			if (!HasTag(tag))
				tags.Add(tag);
			if (tagsToObjects.ContainsKey(tag))
			{
				List<GameObject> objList = tagsToObjects[tag];
				for (int i = 0; i < objList.Count; i++)
					objList[i].SetActive(true);
			}
			OnTagAdded(tag);
			onTagAdded.Invoke();
		}

		public void RemoveTag (string tag)
		{
			tags.Remove(tag);
			if (tagsToObjects.ContainsKey(tag))
			{
				List<GameObject> objList = tagsToObjects[tag];
				for (int i = 0; i < objList.Count; i++)
					objList[i].SetActive(false);
			}
			OnTagRemoved(tag);
			onTagRemoved.Invoke();
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

		public void AddRule (Rule rule)
		{
			rules.Add(rule);
		}

		public void Use ()
		{
			Match.Current.UseCard(this);
		}

		public override string ToString ()
		{
			return $"{name} : {data.cardDataID} , tags( {data.tags} )";
		}

		public string GetTagsFromRules ()
		{
			StringBuilder sb = new StringBuilder();
			if (rules != null)
			{
				for (int i = 0; i < rules.Count; i++)
				{
					sb.Append(rules[i].tags);
					if (i < rules.Count - 1)
						sb.Append(",");
				}
			}
			return sb.ToString();
		}

		public void SetCardFieldValue (string fieldName, string value)
		{
			if (GetFieldDataType(fieldName) != CardFieldDataType.Text)
				return;

			fields[fieldName].stringValue = value;
			List<Component> components = fieldToComponents[fieldName];
			for (int i = 0; i < components.Count; i++)
			{
				((TextMeshPro)components[i]).text = value;
			}
		}

		public void SetCardFieldValue (string fieldName, float value)
		{
			if (GetFieldDataType(fieldName) != CardFieldDataType.Number)
				return;

			fields[fieldName].numValue = value;
			CardField numField = fields[fieldName];
			if (numField.hideOption == CardFieldHideOption.ObjectActivation)
			{
				List<Component> components = fieldToComponents[fieldName];
				for (int i = 0; i < components.Count; i++)
				{
					components[i].gameObject.SetActive(value == 1);
				}
			}
			else
			{
				List<Component> components = fieldToComponents[fieldName];
				for (int i = 0; i < components.Count; i++)
				{
					if ((numField.hideOption == CardFieldHideOption.ShowIfDifferentFromZero && value != 0) || numField.hideOption == CardFieldHideOption.AlwaysShow)
						((TextMeshPro)components[i]).text = value.ToString();
					else if ((numField.hideOption == CardFieldHideOption.ShowIfDifferentFromZero && value == 0) || numField.hideOption == CardFieldHideOption.AlwaysHide)
						((TextMeshPro)components[i]).text = "";
				}
			}
		}

		public void SetCardFieldValue (string fieldName, Sprite value)
		{
			if (GetFieldDataType(fieldName) != CardFieldDataType.Image)
				return;
			fields[fieldName].imageValue = value;
			List<Component> components = fieldToComponents[fieldName];
			for (int i = 0; i < components.Count; i++)
			{
				((SpriteRenderer)components[i]).sprite = value;
			}
		}

		private List<Transform> FindObjectsWithName (Transform parent, string name)
		{
			List<Transform> objs = new List<Transform>();

			if (parent.name.StartsWith(name) && !objs.Contains(parent))
				objs.Add(parent);

			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				if (child.name.StartsWith(name) && !objs.Contains(child))
					objs.Add(child);

				if (child.childCount > 0)
					objs.AddRange(FindObjectsWithName(child, name));
			}
			return objs;
		}

		private void BuildCardToObjectReferences ()
		{
			fieldToComponents = new Dictionary<string, List<Component>>();
			foreach (KeyValuePair<string, CardField> item in fields)
			{
				List<Transform> fieldObjs = FindObjectsWithName(transform, "Field-" + item.Key);
				List<Component> components = new List<Component>();
				for (int i = 0; i < fieldObjs.Count; i++)
				{
					switch (item.Value.dataType)
					{
						case CardFieldDataType.Text:
							if (fieldObjs[i].TryGetComponent(out TextMeshPro textComponent1))
							{
								components.Add(textComponent1);
								textComponent1.text = item.Value.stringValue;
							}
							break;
						case CardFieldDataType.Number:
							if (item.Value.hideOption == CardFieldHideOption.ObjectActivation)
							{
								components.Add(fieldObjs[i]);
								fieldObjs[i].gameObject.SetActive(item.Value.numValue == 1);
							}
							else if (fieldObjs[i].TryGetComponent(out TextMeshPro textComponent2))
							{
								components.Add(textComponent2);
								switch (item.Value.hideOption)
								{
									case CardFieldHideOption.AlwaysHide:
										textComponent2.text = "";
										break;
									case CardFieldHideOption.ShowIfDifferentFromZero:
										if (item.Value.numValue != 0)
											textComponent2.text = item.Value.numValue.ToString();
										else
											textComponent2.text = "";
										break;
									case CardFieldHideOption.AlwaysShow:
										textComponent2.text = item.Value.numValue.ToString();
										break;
								}
							}
							break;
						case CardFieldDataType.Image:
							if (fieldObjs[i].TryGetComponent(out SpriteRenderer spriteComponent))
							{
								components.Add(spriteComponent);
								spriteComponent.sprite = item.Value.imageValue;
							}
							break;
					}
				}
				fieldToComponents.Add(item.Key, components);
			}

			tagsToObjects = new Dictionary<string, List<GameObject>>();
			List<Transform> objects = FindObjectsWithName(transform, "Tag-");
			for (int i = 0; i < objects.Count; i++)
			{
				Transform obj = objects[i];
				string[] nameSplit = obj.name.Split('-');
				string tag = nameSplit[1];
				if (tagsToObjects.ContainsKey(tag))
					tagsToObjects[tag].Add(obj.gameObject);
				else
				{
					List<GameObject> newList = new List<GameObject>();
					newList.Add(obj.gameObject);
					tagsToObjects.Add(tag, newList);
				}
			}
		}
	}
}