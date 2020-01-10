using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using System;

namespace CGEngine
{
	public class CardgameWindow : EditorWindow
	{
		List<CardGameData> gameDataList;
		CardGameData gameBeingEdited;
		bool showCardFieldDefinitionsFoldout;
		bool showRulesetsFoldout;
		bool showMatchModifiersFoldout;
		bool showCardDataListFoldout;
		CardGameData markedForDeletion;
		Vector2 windowScrollPos;
		Vector2 cardsScrollPos;
		GUISkin skin;

		Dictionary<ModifierData, bool> expandModifierDictionary;
		Dictionary<CardData, bool> expandCardDictionary;

		void OnEnable()
		{
			skin = (GUISkin)Resources.Load("CGEngineSkin");
			gameBeingEdited = null;
			if (gameDataList == null)
				gameDataList = new List<CardGameData>();
			string[] foundAssets = AssetDatabase.FindAssets("t:CardGameData");
			if (foundAssets != null)
			{
				foreach (string item in foundAssets)
				{
					CardGameData data = AssetDatabase.LoadAssetAtPath<CardGameData>(AssetDatabase.GUIDToAssetPath(item));
					if (!gameDataList.Contains(data))
						gameDataList.Add(data);

					// ---- Expand dictionaries initialization ----
					expandModifierDictionary = new Dictionary<ModifierData, bool>();
					expandCardDictionary = new Dictionary<CardData, bool>();
				}
			}
		}

		[MenuItem("CGEngine/Cardgame Definitions", priority = 1)]
		public static void ShowWindow()
		{
			GetWindow<CardgameWindow>("Cardgame Definitions");
		}

		void OnGUI()
		{
			//Padding
			GUI.skin.textField.wordWrap = true;
			GUI.skin.button.clipping = TextClipping.Overflow;

			windowScrollPos = EditorGUILayout.BeginScrollView(windowScrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));
			// --- First Label ------
			GUILayout.Label("Card Game Definitions", EditorStyles.boldLabel);
			GUILayout.Space(15);

			// ---- Clear list if empty ----
			for (int i = gameDataList.Count - 1; i >= 0; i--)
			{
				if (gameDataList[i] == null)
					gameDataList.RemoveAt(i);
			}

			// ---- New game button ----
			if (GUILayout.Button("New Game", GUILayout.Width(100), GUILayout.Height(20)))
			{
				CardGameData gameData = CreateInstance<CardGameData>();
				gameData.cardgameID = "New Card Game";
				if (!AssetDatabase.IsValidFolder("Assets/Data"))
					AssetDatabase.CreateFolder("Assets", "Data");
				AssetDatabase.CreateAsset(gameData, "Assets/Data/CardGame-New Card Game.asset");
				gameDataList.Add(gameData);
				gameBeingEdited = gameData;
			}
			GUILayout.Space(15);

			// ---- Display other games and buttons for deleting or editing
			for (int i = 0; i < gameDataList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField((i + 1) + ".  ", GUILayout.MaxWidth(20));

				if (gameDataList[i] == gameBeingEdited)
				{
					// ---- Edit game ----
					EditorGUILayout.BeginVertical();
					EditorGUILayout.BeginHorizontal();
					// ---- Save button ----
					if (GUILayout.Button("Save", GUILayout.Width(50), GUILayout.Height(18)))
					{
						gameBeingEdited = null;
					}
					GUILayout.Space(15);
					// ---- Delete game button ----
					if (GUILayout.Button("Delete", GUILayout.Width(50), GUILayout.Height(18)))
					{
						markedForDeletion = gameBeingEdited;
						gameBeingEdited = null;
					}
					EditorGUILayout.EndHorizontal();
					// ---- Game being edited ----
					if (gameBeingEdited) DisplayCardGameData(gameBeingEdited);
					EditorGUILayout.EndVertical();
				}
				else
				{
					// ---- Edit game button ----
					if (GUILayout.Button("Edit", GUILayout.Width(50), GUILayout.Height(18)))
					{
						gameBeingEdited = gameDataList[i];
						//gameDataList.Remove(gameBeingEdited);
						break;
					}
					GUILayout.Space(15);
					// ---- Delete game button ----
					if (GUILayout.Button("Delete", GUILayout.Width(50), GUILayout.Height(18)))
					{
						markedForDeletion = gameDataList[i];
					}
					EditorGUILayout.LabelField(gameDataList[i].cardgameID, EditorStyles.boldLabel);
				}
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(15);
			}

			// ---- Delete if marked for deletion and clean everything up ----
			if (markedForDeletion)
			{
				if (markedForDeletion.cardFieldDefinitions != null)
					markedForDeletion.cardFieldDefinitions.Clear();

				if (markedForDeletion.rules != null)
				{
					for (int i = markedForDeletion.rules.Count - 1; i >= 0; i--)
					{
						if (markedForDeletion.rules[i].matchModifiers != null)
						{
							for (int j = markedForDeletion.rules[i].matchModifiers.Count - 1; j >= 0; j--)
							{
								expandModifierDictionary.Remove(markedForDeletion.rules[i].matchModifiers[j]);
								AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(markedForDeletion.rules[i].matchModifiers[j]));
							}
							markedForDeletion.rules[i].matchModifiers.Clear();
						}
						AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(markedForDeletion.rules[i]));
					}
					markedForDeletion.rules.Clear();
				}
				if (markedForDeletion.allCardsData != null)
				{
					for (int i = markedForDeletion.allCardsData.Count - 1; i >= 0; i--)
					{
						if (markedForDeletion.allCardsData[i].cardModifiers != null)
						{
							for (int j = markedForDeletion.allCardsData[i].cardModifiers.Count - 1; j >= 0; j--)
							{
								AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(markedForDeletion.allCardsData[i].cardModifiers[j]));
							}
							markedForDeletion.allCardsData[i].cardModifiers.Clear();
						}
						AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(markedForDeletion.allCardsData[i]));
					}
					markedForDeletion.allCardsData.Clear();
				}

				if (markedForDeletion == gameBeingEdited) gameBeingEdited = null;
				showCardFieldDefinitionsFoldout = false;
				showRulesetsFoldout = false;
				showMatchModifiersFoldout = false;
				showCardDataListFoldout = false;
				gameDataList.Remove(markedForDeletion);
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(markedForDeletion));
				markedForDeletion = null;
			}
			EditorGUILayout.EndScrollView();
		}

		void DisplayCardGameData(CardGameData data)
		{
			EditorGUILayout.BeginVertical();

			EditorGUILayout.LabelField(data.cardgameID, EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			data.cardgameID = EditorGUILayout.TextField("Game Name", data.cardgameID, GUILayout.MaxWidth(400));
			if (EditorGUI.EndChangeCheck())
			{
				//data.cardgameID = newName;
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(data), "CardGame-" + data.cardgameID);
			}

			data.cardTemplate = (GameObject)EditorGUILayout.ObjectField("Card Template", data.cardTemplate, typeof(GameObject), false, GUILayout.MaxWidth(400));

			if (data.cardFieldDefinitions == null)
				data.cardFieldDefinitions = new List<CardField>();
			DisplayCardFieldDefinitions(data.cardFieldDefinitions);

			if (data.rules == null)
				data.rules = new List<Ruleset>();
			DisplayRulesets(data.rules);

			if (data.allCardsData == null)
				data.allCardsData = new List<CardData>();
			DisplayCardDataList(data.allCardsData);

			EditorGUILayout.EndVertical();
		}

		void DisplayCardFieldDefinitions(List<CardField> fields)
		{
			CardField toBeDeleted = null;

			if (showCardFieldDefinitionsFoldout = EditorGUILayout.Foldout(showCardFieldDefinitionsFoldout, "Card Field Definitions"))
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(30);
				EditorGUILayout.BeginVertical();
				for (int i = 0; i < fields.Count; i++)
				{
					//if (fields[i] == null)
					//{
					//	fields.RemoveAt(i);
					//	i--;
					//	continue;
					//}

					//Display editable content
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));
					EditorGUILayout.BeginVertical(GUILayout.MaxWidth(200));
					string newFieldName = GUILayout.TextField(fields[i].name);
					if (newFieldName != fields[i].name)
					{
						string oldName = fields[i].name;
						fields[i].name = newFieldName;
						if (gameBeingEdited.allCardsData != null)
						{
							for (int k = 0; k < gameBeingEdited.allCardsData.Count; k++)
							{
								for (int j = 0; j < gameBeingEdited.allCardsData[k].fields.Count; j++)
								{
									if (gameBeingEdited.allCardsData[k].fields[j].name == oldName)
									{
										gameBeingEdited.allCardsData[k].fields[j].name = newFieldName;
									}
								}
							}
						}
					}
					CardFieldDataType newDataType = (CardFieldDataType)EditorGUILayout.EnumPopup(fields[i].dataType);
					if (newDataType != fields[i].dataType)
					{
						fields[i].dataType = newDataType;
						if (gameBeingEdited.allCardsData != null)
						{
							for (int k = 0; k < gameBeingEdited.allCardsData.Count; k++)
							{
								for (int j = 0; j < gameBeingEdited.allCardsData[k].fields.Count; j++)
								{
									if (gameBeingEdited.allCardsData[k].fields[j].name == fields[i].name)
									{
										gameBeingEdited.allCardsData[k].fields[j].dataType = newDataType;
									}
								}
							}
						}
					}

					if (fields[i].dataType == CardFieldDataType.Number)
					{
						CardFieldHideOption newHideOption = (CardFieldHideOption)EditorGUILayout.EnumPopup(fields[i].hideOption);
						if (newHideOption != fields[i].hideOption)
						{
							fields[i].hideOption = newHideOption;
							if (gameBeingEdited.allCardsData != null)
							{
								for (int k = 0; k < gameBeingEdited.allCardsData.Count; k++)
								{
									for (int j = 0; j < gameBeingEdited.allCardsData[k].fields.Count; j++)
									{
										if (gameBeingEdited.allCardsData[k].fields[j].name == fields[i].name)
										{
											gameBeingEdited.allCardsData[k].fields[j].hideOption = newHideOption;
										}
									}
								}
							}
						}
					}

					EditorGUILayout.EndVertical();
					if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
					{
						toBeDeleted = fields[i];
					}
					EditorGUILayout.EndHorizontal();
				}
				if (GUILayout.Button("Create New Field", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
				{
					fields.Add(new CardField());
					if (gameBeingEdited.allCardsData != null)
					{
						for (int k = 0; k < gameBeingEdited.allCardsData.Count; k++)
						{
							gameBeingEdited.allCardsData[k].fields.Add(new CardField());
						}
					}
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				if (toBeDeleted != null)
				{
					fields.Remove(toBeDeleted);
					if (gameBeingEdited.allCardsData != null)
					{
						for (int i = 0; i < gameBeingEdited.allCardsData.Count; i++)
						{
							for (int j = 0; j < gameBeingEdited.allCardsData[i].fields.Count; j++)
							{
								if (gameBeingEdited.allCardsData[i].fields[j].name == toBeDeleted.name)
								{
									gameBeingEdited.allCardsData[i].fields.RemoveAt(i);
									i--;
								}
							}
						}
					}
				}
			}
		}

		void DisplayRulesets(List<Ruleset> rulesets)
		{
			Ruleset toBeDeleted = null;

			if (showRulesetsFoldout = EditorGUILayout.Foldout(showRulesetsFoldout, "Rulesets"))
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(30);
				EditorGUILayout.BeginVertical();
				for (int i = 0; i < rulesets.Count; i++)
				{
					if (rulesets[i] == null)
					{
						rulesets.RemoveAt(i);
						i--;
						continue;
					}

					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));
					EditorGUILayout.BeginVertical();
					EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(420));
					EditorGUILayout.BeginVertical();
					//Ruleset name
					string newName = EditorGUILayout.TextField("Ruleset Name", rulesets[i].rulesetID);
					if (newName != rulesets[i].rulesetID)
					{
						rulesets[i].rulesetID = newName;
						AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(rulesets[i]), "Ruleset-" + newName);
					}
					//Ruleset description
					rulesets[i].description = EditorGUILayout.TextField("Description", rulesets[i].description, GUILayout.Height(42));
					EditorGUILayout.EndVertical();
					if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
					{
						toBeDeleted = rulesets[i];
					}
					EditorGUILayout.EndHorizontal();

					//Match Modifiers
					if (showMatchModifiersFoldout = EditorGUILayout.Foldout(showMatchModifiersFoldout, "Match Modifiers"))
					{
						if (rulesets[i].matchModifiers == null)
							rulesets[i].matchModifiers = new List<ModifierData>();
						DisplayModifiers(rulesets[i].matchModifiers, "Modifier");
					}
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
				}

				if (GUILayout.Button("Create New Ruleset", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
				{
					Ruleset newRuleset = CreateInstance<Ruleset>();
					rulesets.Add(newRuleset);
					newRuleset.rulesetID = "New Ruleset";
					if (!AssetDatabase.IsValidFolder("Assets/Data"))
						AssetDatabase.CreateFolder("Assets", "Data");
					AssetDatabase.CreateAsset(newRuleset, "Assets/Data/Ruleset-New Ruleset.asset");
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				if (toBeDeleted)
				{
					rulesets.Remove(toBeDeleted);
					AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(toBeDeleted));
				}
			}
		}

		void DisplayModifiers(List<ModifierData> modifiers, string prefix)
		{
			ModifierData toBeDeleted = null;
			ModifierData moveUp = null;
			ModifierData moveDown = null;

			
			for (int i = 0; i < modifiers.Count; i++)
			{
				if (modifiers[i] == null)
				{
					modifiers.RemoveAt(i);
					i--;
					continue;
				}

				EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(800));
				EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));

				if (!expandModifierDictionary.ContainsKey(modifiers[i]))
					expandModifierDictionary.Add(modifiers[i], false);

				if (expandModifierDictionary[modifiers[i]])
				{
					if (GUILayout.Button(" ▼", GUILayout.Width(20), GUILayout.Height(20)))
					{
						expandModifierDictionary[modifiers[i]] = false;
					}
					EditorGUILayout.BeginVertical(GUILayout.Width(20));
					if (GUILayout.Button(" ↑ ", GUILayout.Width(20), GUILayout.Height(20)))
					{
						//Move Up
						moveUp = modifiers[i];
					}
					if (GUILayout.Button(" ↓ ", GUILayout.Width(20), GUILayout.Height(20)))
					{
						//Move Down
						moveDown = modifiers[i];
					}
					EditorGUILayout.EndVertical();

					EditorGUILayout.BeginVertical();
					// ---- Modifier Fields ----
					string newName = EditorGUILayout.TextField("Modifier Name", modifiers[i].modifierID);
					if (newName != modifiers[i].modifierID)
					{
						modifiers[i].modifierID = newName;
						AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(modifiers[i]), prefix + "-" + newName);
					}
					modifiers[i].tags = EditorGUILayout.TextField("Tags", modifiers[i].tags);
					modifiers[i].startingNumValue = EditorGUILayout.DoubleField("Starting Num Value", modifiers[i].startingNumValue);
					modifiers[i].trigger = EditorGUILayout.TextField("Trigger", modifiers[i].trigger);
					modifiers[i].condition = EditorGUILayout.TextField("Condition", modifiers[i].condition);
					modifiers[i].affected = EditorGUILayout.TextField("Affected", modifiers[i].affected);
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel("True Effect");
					modifiers[i].trueEffect = EditorGUILayout.TextArea(modifiers[i].trueEffect);
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel("False Effect");
					modifiers[i].falseEffect = EditorGUILayout.TextArea(modifiers[i].falseEffect);
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();

				}
				else
				{
					if (GUILayout.Button(" ►", GUILayout.Width(20), GUILayout.Height(20)))
					{
						expandModifierDictionary[modifiers[i]] = true;
					}
					EditorGUILayout.BeginVertical(GUILayout.Width(20));
					if (GUILayout.Button(" ↑ ", GUILayout.Width(20), GUILayout.Height(20)))
					{
						//Move Up
						moveUp = modifiers[i];
					}
					if (GUILayout.Button(" ↓ ", GUILayout.Width(20), GUILayout.Height(20)))
					{
						//Move Down
						moveDown = modifiers[i];
					}
					EditorGUILayout.EndVertical();

					EditorGUILayout.LabelField(modifiers[i].modifierID, GUILayout.MaxWidth(200));
				}


				if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
				{
					toBeDeleted = modifiers[i];
				}

				EditorGUILayout.EndHorizontal();

				if (i == modifiers.Count - 1)
					GUILayout.Space(5);
				else
					GUILayout.Space(15);
			}
			

			if (GUILayout.Button("Create New "+ prefix, GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
			{
				ModifierData newMod = CreateInstance<ModifierData>();
				modifiers.Add(newMod);
				expandModifierDictionary.Add(newMod, true);
				newMod.modifierID = "New "+ prefix;
				if (!AssetDatabase.IsValidFolder("Assets/Data"))
					AssetDatabase.CreateFolder("Assets", "Data");
				AssetDatabase.CreateAsset(newMod, "Assets/Data/" + prefix + "-New " + prefix + ".asset");
			}
			GUILayout.Space(15);

			if (toBeDeleted)
			{
				modifiers.Remove(toBeDeleted);
				expandModifierDictionary.Remove(toBeDeleted);
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(toBeDeleted));
			}

			if (moveUp)
			{
				int index = modifiers.IndexOf(moveUp);
				if (index > 0)
				{
					modifiers.Remove(moveUp);
					index--;
					modifiers.Insert(index, moveUp);
				}
			}

			if (moveDown)
			{
				int index = modifiers.IndexOf(moveDown);
				if (index < modifiers.Count - 1)
				{
					modifiers.Remove(moveDown);
					index++;
					modifiers.Insert(index, moveDown);
				}
			}
		}

		float minWidthTitle = 180;
		float minWidthFields = 190;
		float buttonTitleWidth = 15;
		float buttonWidth = 25;

		void DisplayCardDataList(List<CardData> cards)
		{
			if (showCardDataListFoldout = EditorGUILayout.Foldout(showCardDataListFoldout, "All Cards"))
			{
				if (gameBeingEdited.cardFieldDefinitions != null && gameBeingEdited.cardFieldDefinitions.Count > 0)
				{
					CardData toBeDeleted = null;

					//minWidthTitle = EditorGUILayout.FloatField(minWidthTitle);
					//minWidthFields = EditorGUILayout.FloatField(minWidthFields);
					//buttonTitleWidth = EditorGUILayout.FloatField(buttonTitleWidth);
					//buttonWidth = EditorGUILayout.FloatField(buttonWidth);

					if (GUILayout.Button("Create New Card", GUILayout.MaxWidth(150), GUILayout.MaxHeight(18)))
					{
						CardData newCard = CreateInstance<CardData>();
						newCard.fields = new List<CardField>();
						for (int i = 0; i < gameBeingEdited.cardFieldDefinitions.Count; i++)
						{
							newCard.fields.Add(new CardField(gameBeingEdited.cardFieldDefinitions[i]));
						}
						newCard.cardModifiers = new List<ModifierData>();
						cards.Add(newCard);
						newCard.cardDataID = "New Card";
						if (!AssetDatabase.IsValidFolder("Assets/Data"))
							AssetDatabase.CreateFolder("Assets", "Data");
						AssetDatabase.CreateAsset(newCard, "Assets/Data/Card-New Card.asset");
					}
					// TITLE ROW
					EditorGUILayout.BeginHorizontal();
					// ---- Expand button title ----
					//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
					EditorGUILayout.BeginVertical();
					EditorGUILayout.LabelField("►", GUILayout.Width(buttonTitleWidth));
					EditorGUILayout.EndVertical();
					// ---- Card data ID name title ----
					//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
					EditorGUILayout.BeginVertical();
					EditorGUILayout.LabelField("Data Name", GUILayout.Width(minWidthTitle));
					EditorGUILayout.EndVertical();
					// ---- Delete button title ----
					//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
					EditorGUILayout.BeginVertical();
					EditorGUILayout.LabelField("X", GUILayout.Width(buttonTitleWidth));
					EditorGUILayout.EndVertical();
					// ---- Card Tags title  ----
					//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
					EditorGUILayout.BeginVertical();
					EditorGUILayout.LabelField("Tags", GUILayout.Width(minWidthTitle));
					EditorGUILayout.EndVertical();
					for (int i = 0; i < gameBeingEdited.cardFieldDefinitions.Count; i++)
					{
						//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
						EditorGUILayout.BeginVertical();
						EditorGUILayout.LabelField(gameBeingEdited.cardFieldDefinitions[i].name, GUILayout.Width(minWidthTitle));
						EditorGUILayout.EndVertical();
					}
					EditorGUILayout.EndHorizontal();

					//CARD ROWS
					for (int i = 0; i < cards.Count; i++)
					{
						if (cards[i] == null)
						{
							cards.RemoveAt(i);
							i--;
							continue;
						}

						if (!expandCardDictionary.ContainsKey(cards[i]))
							expandCardDictionary.Add(cards[i], false);

						EditorGUILayout.BeginHorizontal(EditorStyles.textField);
						EditorGUILayout.BeginVertical();
						EditorGUILayout.BeginHorizontal();
						// ---- Expand button ----
						EditorGUILayout.BeginVertical();
						if (!expandCardDictionary[cards[i]])
						{
							if (GUILayout.Button(" ►", GUILayout.Width(buttonWidth)))
							{
								expandCardDictionary[cards[i]] = true;
							}
						}
						else
						{
							if (GUILayout.Button(" ▼", GUILayout.Width(buttonWidth)))
							{
								expandCardDictionary[cards[i]] = false;
							}
						}
						EditorGUILayout.EndVertical();
						// ---- Card data ID name ----
						EditorGUILayout.BeginVertical();
						string newName = EditorGUILayout.TextField(cards[i].cardDataID, GUILayout.Width(minWidthFields));
						if (newName != cards[i].cardDataID)
						{
							cards[i].cardDataID = newName;
							AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(cards[i]), "Card-" + newName);
						}
						EditorGUILayout.EndVertical();
						// ---- Delete button ----
						EditorGUILayout.BeginVertical();
						if (GUILayout.Button("X", GUILayout.Width(buttonWidth)))
						{
							toBeDeleted = cards[i];
						}
						EditorGUILayout.EndVertical();
						// ---- Card Tags  ----
						EditorGUILayout.BeginVertical();
						cards[i].tags = EditorGUILayout.TextField(cards[i].tags, GUILayout.Width(minWidthFields));
						EditorGUILayout.EndVertical();

						for (int j = 0; j < cards[i].fields.Count; j++)
						{
							EditorGUILayout.BeginVertical();
							switch (cards[i].fields[j].dataType)
							{
								case CardFieldDataType.Text:
									cards[i].fields[j].stringValue = EditorGUILayout.TextField(cards[i].fields[j].stringValue, GUILayout.Width(minWidthFields));
									break;
								case CardFieldDataType.Number:
									cards[i].fields[j].numValue = EditorGUILayout.DoubleField(cards[i].fields[j].numValue, GUILayout.Width(minWidthFields));
									break;
								case CardFieldDataType.Image:
									cards[i].fields[j].imageValue = (Sprite)EditorGUILayout.ObjectField(cards[i].fields[j].imageValue, typeof(Sprite), false, GUILayout.Width(minWidthFields));
									break;
							}
							EditorGUILayout.EndVertical();
						}
						EditorGUILayout.EndHorizontal();

						//Card Modifiers
						if (expandCardDictionary[cards[i]])
						{
							EditorGUILayout.BeginVertical();
							DisplayModifiers(cards[i].cardModifiers, "CardModifier");
							EditorGUILayout.EndVertical();
						}
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();
					}

					if (toBeDeleted)
					{
						cards.Remove(toBeDeleted);
						AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(toBeDeleted));
					}
				}
				else
				{
					EditorGUILayout.LabelField("- - - Define the card fields above before creating any card - - -");
				}
			}
		}
	}
}