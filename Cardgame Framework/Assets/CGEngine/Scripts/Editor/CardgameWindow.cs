using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using System;
using System.Text;
using System.IO;

namespace CardGameFramework
{
	public class CardgameWindow : EditorWindow
	{
		List<CardGameData> gameDataList;
		CardGameData gameBeingEdited;
		bool showCardFieldDefinitionsFoldout;
		bool showRulesetsFoldout;
		bool showMatchModifiersFoldout;
		bool showCardDataListFoldout;
		CardData cardToCopyFields;
		CardGameData markedForDeletion;
		Vector2 windowScrollPos;
		Vector2 cardsScrollPos;
		GUISkin skin;
		float minHorizontalWidth = 300;
		float maxHorizontalWidth = 9999;
		float minWidthFields = 150;
		float maxWidthFields = 250;
		float buttonWidth = 25;
		bool copyingFields;
		bool importingNewGame;
		TextAsset gameBeingImported;
		bool importingAListOfCards;
		List<CardData> cardDataListBeingImported;
		bool listReadyToImport;
		string[] modTypes;

		Dictionary<object, bool> foldoutDictionary;

		void OnEnable()
		{
			// ---- Expand dictionary initialization ----
			if (foldoutDictionary == null)
				foldoutDictionary = new Dictionary<object, bool>();

			skin = (GUISkin)Resources.Load("CGEngineSkin");
			//gameBeingEdited = null;
			if (gameDataList == null)
			{
				gameDataList = new List<CardGameData>();
				string[] foundAssets = AssetDatabase.FindAssets("t:CardGameData");
				if (foundAssets != null)
				{
					foreach (string item in foundAssets)
					{
						CardGameData data = AssetDatabase.LoadAssetAtPath<CardGameData>(AssetDatabase.GUIDToAssetPath(item));
						if (!gameDataList.Contains(data))
							gameDataList.Add(data);
					}
				}
			}

			modTypes = Enum.GetNames(typeof(ModifierTypes));
		}

		[MenuItem("CGEngine/Cardgame Definitions", priority = 1)]
		public static void ShowWindow()
		{
			GetWindow<CardgameWindow>("Cardgame Definitions");
		}

		void CreateFolderInsideData(string folderName)
		{
			if (!AssetDatabase.IsValidFolder("Assets/Data"))
			{
				AssetDatabase.CreateFolder("Assets", "Data");
			}
			if (!AssetDatabase.IsValidFolder("Assets/Data/" + folderName))
			{
				AssetDatabase.CreateFolder("Assets/Data", folderName);
			}
		}

		// ======================================= ON GUI =======================================================
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
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("New Game", GUILayout.Width(150), GUILayout.Height(20)))
			{
				CardGameData gameData = CreateInstance<CardGameData>();
				gameData.cardgameID = "New Card Game";
				CreateFolderInsideData("CardGames");
				AssetDatabase.CreateAsset(gameData, "Assets/Data/CardGames/CardGame-New Card Game.asset");
				gameDataList.Add(gameData);
				gameBeingEdited = gameData;
			}
			if (!importingNewGame)
			{
				if (GUILayout.Button("Import Game", GUILayout.Width(150), GUILayout.Height(20)))
				{
					importingNewGame = true;
				}
			}
			else
			{
				gameBeingImported = (TextAsset)EditorGUILayout.ObjectField(gameBeingImported, typeof(TextAsset), false, GUILayout.Width(150));
				if (GUILayout.Button("Import", GUILayout.Width(50), GUILayout.Height(20)))
				{
					JsonUtility.FromJson(File.ReadAllText(AssetDatabase.GetAssetPath(gameBeingImported)), typeof(CardGameData));
					importingNewGame = false;
					gameBeingImported = null;
				}
				if (GUILayout.Button("Cancel", GUILayout.Width(50), GUILayout.Height(20)))
				{
					importingNewGame = false;
					gameBeingImported = null;
				}
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(15);
			// ---- Display other games and buttons for deleting or editing
			for (int i = 0; i < gameDataList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField((i + 1) + ".  ", GUILayout.MaxWidth(20));

				if (gameDataList[i] == gameBeingEdited)
				{
					Undo.RecordObject(gameBeingEdited, "CGEngine.CardGame Change");
					// ---- Edit game ----
					EditorGUILayout.BeginVertical();
					EditorGUILayout.BeginHorizontal();
					// ---- Save button ----
					if (GUILayout.Button("Save", GUILayout.Width(50), GUILayout.Height(18)))
					{
						SaveGameToFile(gameBeingEdited);
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
				//GUILayout.Space(15);
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
								foldoutDictionary.Remove(markedForDeletion.rules[i].matchModifiers[j]);
								MoveAssetToUnused(markedForDeletion.rules[i].matchModifiers[j]);
							}
							markedForDeletion.rules[i].matchModifiers.Clear();
						}
						MoveAssetToUnused(markedForDeletion.rules[i]);
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
								MoveAssetToUnused(markedForDeletion.allCardsData[i].cardModifiers[j]);
							}
							markedForDeletion.allCardsData[i].cardModifiers.Clear();
						}
						MoveAssetToUnused(markedForDeletion.allCardsData[i]);
					}
					markedForDeletion.allCardsData.Clear();
				}

				if (markedForDeletion == gameBeingEdited) gameBeingEdited = null;
				showCardFieldDefinitionsFoldout = false;
				showRulesetsFoldout = false;
				showMatchModifiersFoldout = false;
				showCardDataListFoldout = false;
				gameDataList.Remove(markedForDeletion);
				MoveAssetToUnused(markedForDeletion);
				markedForDeletion = null;
			}
			EditorGUILayout.EndScrollView();
		}

		// ======================================= CARD GAMES =======================================================
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

			if (data.cardTemplate)
			{
				if (data.cardFieldDefinitions == null)
					data.cardFieldDefinitions = new List<CardField>();
				DisplayCardFieldDefinitions(data.cardFieldDefinitions);

				if (data.rules == null)
					data.rules = new List<Ruleset>();
				DisplayRulesets(data.rules);

				if (data.allCardsData == null)
					data.allCardsData = new List<CardData>();
				DisplayCardDataList(data.allCardsData);
			}
			EditorGUILayout.EndVertical();
		}

		// ======================================= CARD FIELDS =======================================================
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
				EditorGUILayout.BeginHorizontal();
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
				if (!copyingFields)
				{
					if (GUILayout.Button("Copy Fields From Card", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
					{
						copyingFields = true;
					}
				}
				else
				{
					cardToCopyFields = (CardData)EditorGUILayout.ObjectField(cardToCopyFields, typeof(CardData), false, GUILayout.MaxWidth(150), GUILayout.MaxHeight(18));
					if (GUILayout.Button("Copy", GUILayout.MaxWidth(50), GUILayout.MaxHeight(18)))
					{
						if (cardToCopyFields != null)
						{
							if (cardToCopyFields.fields != null)
							{
								gameBeingEdited.cardFieldDefinitions = new List<CardField>();
								for (int i = 0; i < cardToCopyFields.fields.Count; i++)
								{
									CardField newField = new CardField();
									newField.name = cardToCopyFields.fields[i].name;
									newField.dataType = cardToCopyFields.fields[i].dataType;
									newField.hideOption = cardToCopyFields.fields[i].hideOption;
									gameBeingEdited.cardFieldDefinitions.Add(newField);
								}
							}
							copyingFields = false;
							cardToCopyFields = null;
						}
					}
					if (GUILayout.Button("Cancel", GUILayout.MaxWidth(50), GUILayout.MaxHeight(18)))
					{
						copyingFields = false;
						cardToCopyFields = null;
					}
				}
				EditorGUILayout.EndHorizontal();
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
									gameBeingEdited.allCardsData[i].fields.RemoveAt(j);
									j--;
								}
							}
						}
					}
				}
			}
		}

		// ======================================= RULESETS =======================================================
		void DisplayRulesets(List<Ruleset> rulesets)
		{
			Ruleset toBeDeleted = null;

			if (showRulesetsFoldout = EditorGUILayout.Foldout(showRulesetsFoldout, "Rulesets"))
			{

				for (int i = 0; i < rulesets.Count; i++)
				{
					if (rulesets[i] == null)
					{
						rulesets.RemoveAt(i);
						i--;
						continue;
					}
					Undo.RecordObject(rulesets[i], "CGEngine.Ruleset Change");

					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));
					EditorGUILayout.BeginVertical(GUILayout.Width(800));
					//Ruleset name
					string newName = EditorGUILayout.TextField("Ruleset Name", rulesets[i].rulesetID);
					if (newName != rulesets[i].rulesetID)
					{
						rulesets[i].rulesetID = newName;
						AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(rulesets[i]), "Ruleset-" + newName);
					}
					//Ruleset description
					rulesets[i].description = EditorGUILayout.TextField("Description", rulesets[i].description, GUILayout.Height(42));

					//Ruleset turn structure
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel("Turn Structure");
					rulesets[i].turnStructure = EditorGUILayout.TextArea(rulesets[i].turnStructure);
					EditorGUILayout.EndHorizontal();

					//Match Modifiers
					if (showMatchModifiersFoldout = EditorGUILayout.Foldout(showMatchModifiersFoldout, "Match Modifiers"))
					{
						if (rulesets[i].matchModifiers == null)
							rulesets[i].matchModifiers = new List<ModifierData>();
						DisplayModifiers(rulesets[i].matchModifiers, "Modifier");
					}
					EditorGUILayout.EndVertical();
					if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
					{
						toBeDeleted = rulesets[i];
					}
					EditorGUILayout.EndHorizontal();
				}

				if (GUILayout.Button("Create New Ruleset", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
				{
					Ruleset newRuleset = CreateInstance<Ruleset>();
					rulesets.Add(newRuleset);
					newRuleset.rulesetID = "New Ruleset";
					CreateFolderInsideData("Rulesets");
					AssetDatabase.CreateAsset(newRuleset, "Assets/Data/Rulesets/Ruleset-New Ruleset.asset");
				}

				if (toBeDeleted)
				{
					rulesets.Remove(toBeDeleted);
					MoveAssetToUnused(toBeDeleted);
				}
			}
		}

		// ======================================= MODIFIERS =======================================================
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
				Undo.RecordObject(modifiers[i], "CGEngine.Modifier Change");

				EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(800));
				EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));

				if (!foldoutDictionary.ContainsKey(modifiers[i]))
					foldoutDictionary.Add(modifiers[i], false);

				if (foldoutDictionary[modifiers[i]])
				{
					if (GUILayout.Button(" ▼", GUILayout.Width(20), GUILayout.Height(20)))
					{
						foldoutDictionary[modifiers[i]] = false;
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
					//modifiers[i].tags = EditorGUILayout.TextField("Tags", modifiers[i].tags);
					// ---- Tags
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel("Tags");
					modifiers[i].tags = EditorGUILayout.TextArea(modifiers[i].tags);
					EditorGUILayout.EndHorizontal();
					// ---- Type of modifier
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel("Modifier Type");
					modifiers[i].modType = GUILayout.SelectionGrid(modifiers[i].modType, modTypes, modTypes.Length, GUILayout.MaxWidth(300));
					EditorGUILayout.EndHorizontal();
					if (modifiers[i].modType == (int)ModifierTypes.Number)
					{
						// ---- Num Value
						modifiers[i].startingNumValue = EditorGUILayout.DoubleField("Value", modifiers[i].startingNumValue, GUILayout.MaxWidth(300));
						modifiers[i].minValue = EditorGUILayout.DoubleField("Min", modifiers[i].minValue, GUILayout.MaxWidth(300));
						modifiers[i].maxValue = EditorGUILayout.DoubleField("Max", modifiers[i].maxValue, GUILayout.MaxWidth(300));
					}
					else
					{
						// ----- Triggers
						//modifiers[i].trigger = EditorGUILayout.TextField("Trigger", modifiers[i].trigger);
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Triggers");
						modifiers[i].trigger = EditorGUILayout.TextArea(modifiers[i].trigger);
						EditorGUILayout.EndHorizontal();
						// ---- Condition
						//modifiers[i].condition = EditorGUILayout.TextField("Condition", modifiers[i].condition);
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Condition");
						modifiers[i].condition = EditorGUILayout.TextArea(modifiers[i].condition);
						EditorGUILayout.EndHorizontal();
						// ---- Affected
						//modifiers[i].affected = EditorGUILayout.TextField("Affected", modifiers[i].affected);
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Affected");
						modifiers[i].affected = EditorGUILayout.TextArea(modifiers[i].affected);
						EditorGUILayout.EndHorizontal();
						// ---- True effect
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("True Effect");
						modifiers[i].trueEffect = EditorGUILayout.TextArea(modifiers[i].trueEffect);
						EditorGUILayout.EndHorizontal();
						// ---- False effect
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("False Effect");
						modifiers[i].falseEffect = EditorGUILayout.TextArea(modifiers[i].falseEffect);
						EditorGUILayout.EndHorizontal();
					}
					EditorGUILayout.EndVertical();

				}
				else
				{
					if (GUILayout.Button(" ►", GUILayout.Width(20), GUILayout.Height(20)))
					{
						foldoutDictionary[modifiers[i]] = true;
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

					EditorGUILayout.BeginVertical(GUILayout.MaxWidth(400));
					EditorGUILayout.LabelField(modifiers[i].modifierID);
					if (modifiers[i].modType == (int)ModifierTypes.Number)
						EditorGUILayout.LabelField("    " + modifiers[i].startingNumValue.ToString());
					else
						EditorGUILayout.LabelField("    " + modifiers[i].trigger);
					EditorGUILayout.EndVertical();
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


			if (GUILayout.Button("Create New " + prefix, GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
			{
				ModifierData newMod = CreateInstance<ModifierData>();
				modifiers.Add(newMod);
				foldoutDictionary.Add(newMod, true);
				newMod.modifierID = "New " + prefix;
				CreateFolderInsideData("Modifiers");
				AssetDatabase.CreateAsset(newMod, "Assets/Data/Modifiers/" + prefix + "-New " + prefix + ".asset");
			}
			GUILayout.Space(15);

			if (toBeDeleted)
			{
				modifiers.Remove(toBeDeleted);
				foldoutDictionary.Remove(toBeDeleted);
				MoveAssetToUnused(toBeDeleted);
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

		// ======================================= CARD LIST =======================================================
		void DisplayCardDataList(List<CardData> cards)
		{
			if (showCardDataListFoldout = EditorGUILayout.Foldout(showCardDataListFoldout, "All Cards"))
			{
				if (gameBeingEdited.cardFieldDefinitions != null && gameBeingEdited.cardFieldDefinitions.Count > 0)
				{
					CardData toBeDeleted = null;

					//minWidthFields = EditorGUILayout.FloatField("Min Width Fields", minWidthFields);
					//maxWidthFields = EditorGUILayout.FloatField("Max Width Fields", maxWidthFields);
					//buttonWidth = EditorGUILayout.FloatField("Buttons Width", buttonWidth);
					minHorizontalWidth = buttonWidth * 2 + minWidthFields * 2 + minWidthFields * gameBeingEdited.cardFieldDefinitions.Count;
					maxHorizontalWidth = buttonWidth * 2 + maxWidthFields * 2 + maxWidthFields * gameBeingEdited.cardFieldDefinitions.Count;

					EditorGUILayout.BeginHorizontal();

					// ---- Create new Card ---- 
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
						CreateFolderInsideData("Cards");
						AssetDatabase.CreateAsset(newCard, "Assets/Data/Cards/Card-New Card.asset");
					}

					// ---- Import a List of Cards ---- 
					if (!importingAListOfCards)
					{
						if (GUILayout.Button("Import Cards", GUILayout.MaxWidth(150), GUILayout.MaxHeight(18)))
						{
							importingAListOfCards = true;
							cardDataListBeingImported = new List<CardData>();
						}
					}
					else
					{
						bool cardsReady = cardDataListBeingImported != null && cardDataListBeingImported.Count > 0;
						Event evt = Event.current;
						Rect dropArea;
						if (cardsReady)
							dropArea = GUILayoutUtility.GetRect(200.0f, 25.0f, GUILayout.Width(200));
						else
							dropArea = GUILayoutUtility.GetRect(250.0f, 25.0f, GUILayout.Width(250));
						int cardsCount = cardDataListBeingImported.Count;
						string boxMessage = cardsReady ? cardsCount + " card" + (cardsCount > 1 ? "s" : "") + " ready! Hit Import" : "Drop Card Datas Here";
						GUI.Box(dropArea, boxMessage);

						switch (evt.type)
						{
							case EventType.DragUpdated:
							case EventType.DragPerform:
								if (!dropArea.Contains(evt.mousePosition))
									return;

								DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

								if (evt.type == EventType.DragPerform)
								{
									DragAndDrop.AcceptDrag();

									foreach (Object draggedObject in DragAndDrop.objectReferences)
									{
										// Do On Drag Stuff here
										if (draggedObject.GetType() == typeof(CardData) && !cardDataListBeingImported.Contains((CardData)draggedObject))
											cardDataListBeingImported.Add((CardData)draggedObject);
									}
								}
								break;
						}
						if (cardsReady)
						{
							if (GUILayout.Button("Import", GUILayout.MaxWidth(50), GUILayout.MaxHeight(18)))
							{
								importingAListOfCards = false;
								if (cardDataListBeingImported != null && cardDataListBeingImported.Count > 0) cards.AddRange(cardDataListBeingImported);
								cardDataListBeingImported = null;
							}
						}
						if (GUILayout.Button("Cancel", GUILayout.MaxWidth(50), GUILayout.MaxHeight(18)))
						{
							importingAListOfCards = false;
							cardDataListBeingImported = null;
						}
					}

					if (GUILayout.Button("Instantiate Cards in Scene", GUILayout.MaxWidth(170), GUILayout.MaxHeight(18)))
					{
						CGEngine.CreateCards(gameBeingEdited.cardTemplate, cards, Vector3.zero);
					}
					EditorGUILayout.EndHorizontal();

					// TITLE ROW
					EditorGUILayout.BeginHorizontal(EditorStyles.miniButtonMid, GUILayout.MinWidth(minHorizontalWidth), GUILayout.MaxWidth(maxHorizontalWidth));
					// ---- Expand button title ----
					//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
					EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
					EditorGUILayout.LabelField("►", GUILayout.Width(buttonWidth));
					EditorGUILayout.EndVertical();
					// ---- Card data ID name title ----
					//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
					EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
					EditorGUILayout.LabelField("     Data Name");
					EditorGUILayout.EndVertical();
					// ---- Delete button title ----
					//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
					EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
					EditorGUILayout.LabelField("X", GUILayout.Width(buttonWidth));
					EditorGUILayout.EndVertical();
					// ---- Card Tags title  ----
					//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
					EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
					EditorGUILayout.LabelField("     Tags");
					EditorGUILayout.EndVertical();
					for (int i = 0; i < gameBeingEdited.cardFieldDefinitions.Count; i++)
					{
						//EditorGUILayout.BeginVertical(EditorStyles.miniButtonMid);
						EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
						EditorGUILayout.LabelField("     " + gameBeingEdited.cardFieldDefinitions[i].name);
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

						Undo.RecordObject(cards[i], "CGEngine.Card Change");

						if (!foldoutDictionary.ContainsKey(cards[i]))
							foldoutDictionary.Add(cards[i], false);

						EditorGUILayout.BeginHorizontal(EditorStyles.textField, GUILayout.MinWidth(minHorizontalWidth), GUILayout.MaxWidth(maxHorizontalWidth));
						EditorGUILayout.BeginVertical();

						// ---- CARD FIELDS ARE NOT COMPATIBLE WITH GAME DEFINITIONS ----
						if (!CardHasUniformFields(cards[i]))
						{
							EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
							GUILayout.Space(25);
							EditorGUILayout.LabelField(cards[i].cardDataID, GUILayout.Width(100));
							EditorGUILayout.LabelField(" --- This card fields are not compatible with the game fields defined! ---- ");
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.BeginHorizontal();
							GUILayout.Space(25);
							if (GUILayout.Button("Remove", GUILayout.Width(70)))
							{
								toBeDeleted = cards[i];
							}
							if (GUILayout.Button("Conform", GUILayout.Width(70)))
							{
								List<CardField> tempList = new List<CardField>();
								for (int j = 0; j < gameBeingEdited.cardFieldDefinitions.Count; j++)
								{
									tempList.Add(new CardField(gameBeingEdited.cardFieldDefinitions[j]));
								}

								for (int j = 0; j < cards[i].fields.Count; j++)
								{
									for (int k = 0; k < tempList.Count; k++)
									{
										if (cards[i].fields[j].name == tempList[k].name && cards[i].fields[j].dataType == tempList[k].dataType)
										{
											tempList[k].stringValue = cards[i].fields[j].stringValue;
											tempList[k].imageValue = cards[i].fields[j].imageValue;
											tempList[k].numValue = cards[i].fields[j].numValue;
											tempList[k].hideOption = cards[i].fields[j].hideOption;
										}
									}
								}
								cards[i].fields = tempList;
							}
							EditorGUILayout.LabelField("Note that hitting 'Conform' may result in data loss for fields that are not defined!");
							EditorGUILayout.EndHorizontal();
						}
						else
						{
							// ---- CARD FIELDS ARE OK ! ----

							EditorGUILayout.BeginHorizontal();
							// ---- Expand button ----
							EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
							if (!foldoutDictionary[cards[i]])
							{
								if (GUILayout.Button(" ►", GUILayout.Width(buttonWidth)))
								{
									foldoutDictionary[cards[i]] = true;
								}
							}
							else
							{
								if (GUILayout.Button(" ▼", GUILayout.Width(buttonWidth)))
								{
									foldoutDictionary[cards[i]] = false;
								}
							}
							EditorGUILayout.EndVertical();
							// ---- Card data ID name ----
							EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
							string newName = EditorGUILayout.TextField(cards[i].cardDataID, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
							if (newName != cards[i].cardDataID)
							{
								cards[i].cardDataID = newName;
								AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(cards[i]), "Card-" + newName);
							}
							EditorGUILayout.EndVertical();
							// ---- Delete button ----
							EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
							if (GUILayout.Button("X", GUILayout.Width(buttonWidth)))
							{
								toBeDeleted = cards[i];
							}
							EditorGUILayout.EndVertical();
							// ---- Card Tags  ----
							EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
							cards[i].tags = EditorGUILayout.TextField(cards[i].tags, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
							EditorGUILayout.EndVertical();

							for (int j = 0; j < cards[i].fields.Count; j++)
							{
								EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
								switch (cards[i].fields[j].dataType)
								{
									case CardFieldDataType.Text:
										cards[i].fields[j].stringValue = EditorGUILayout.TextField(cards[i].fields[j].stringValue, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
										break;
									case CardFieldDataType.Number:
										cards[i].fields[j].numValue = EditorGUILayout.DoubleField(cards[i].fields[j].numValue, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
										break;
									case CardFieldDataType.Image:
										cards[i].fields[j].imageValue = (Sprite)EditorGUILayout.ObjectField(cards[i].fields[j].imageValue, typeof(Sprite), false, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
										break;
								}
								EditorGUILayout.EndVertical();
							}
							EditorGUILayout.EndHorizontal();

							//Card Modifiers
							if (foldoutDictionary[cards[i]])
							{
								EditorGUILayout.BeginVertical();
								DisplayModifiers(cards[i].cardModifiers, "CardModifier");
								EditorGUILayout.EndVertical();
							}
						}
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();

					}

					if (toBeDeleted)
					{
						cards.Remove(toBeDeleted);
						//MoveAssetToUnused(toBeDeleted);
					}
				}
				else
				{
					EditorGUILayout.LabelField("- - - Define the card fields above before creating any card - - -");
				}
			}
		}

		// ======================================= HELPER METHODS =======================================================


		string PrintStringArray(string[] str)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < str.Length; i++)
			{
				sb.Append(i + "{ ");
				sb.Append(str[i]);
				sb.Append(" }  ");
			}
			return sb.ToString();
		}

		string[] ArgumentsBreakdown(string clause, bool onlyParenthesis = false)
		{
			clause = clause.Replace(" ", "");
			char[] clauseChar = clause.ToCharArray();
			List<string> result = new List<string>();
			string sub = "";
			int lastSubStartIndex = 0;
			int parCounter = 0;
			for (int i = 0; i < clauseChar.Length; i++)
			{
				switch (clauseChar[i])
				{
					case '(':
						if (parCounter == 0)
						{
							sub = clause.Substring(lastSubStartIndex, i - lastSubStartIndex);
							if (!string.IsNullOrEmpty(sub)) result.Add(sub);
							lastSubStartIndex = i + 1;
						}
						parCounter++;
						break;
					case ',':
						if (parCounter == 1 && !onlyParenthesis)
						{
							sub = clause.Substring(lastSubStartIndex, i - lastSubStartIndex);
							if (!string.IsNullOrEmpty(sub)) result.Add(sub);
							lastSubStartIndex = i + 1;
						}
						break;
					case ')':
						parCounter--;
						if (parCounter == 0)
						{
							sub = clause.Substring(lastSubStartIndex, i - lastSubStartIndex);
							if (!string.IsNullOrEmpty(sub)) result.Add(sub);
							lastSubStartIndex = i + 1;
						}
						break;
					default:
						if (i == clauseChar.Length - 1)
						{
							sub = clause.Substring(lastSubStartIndex, i - lastSubStartIndex + 1);
							if (!string.IsNullOrEmpty(sub)) result.Add(sub);
						}
						continue;
				}
			}
			string[] resultArray = result.ToArray();
			return resultArray;
		}

		void MoveAssetToUnused(Object asset)
		{
			CreateFolderInsideData("Unused");
			AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(asset), "Assets/Data/Unused/" + asset.name + ".asset");
		}

		bool CardHasUniformFields(CardData data)
		{
			if (data == null || data.fields == null || data.fields.Count != gameBeingEdited.cardFieldDefinitions.Count)
				return false;

			for (int i = 0; i < data.fields.Count; i++)
			{
				if (data.fields[i].name != gameBeingEdited.cardFieldDefinitions[i].name ||
					data.fields[i].dataType != gameBeingEdited.cardFieldDefinitions[i].dataType ||
					data.fields[i].hideOption != gameBeingEdited.cardFieldDefinitions[i].hideOption)
					return false;
			}

			return true;
		}

		void SaveGameToFile(CardGameData game)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{\"cardgameID\":\"" + game.cardgameID + "\",");
			sb.Append("\"allCardsData\":[");
			if (game.allCardsData != null && game.allCardsData.Count > 0)
			{
				for (int i = 0; i < game.allCardsData.Count; i++)
				{
					sb.Append(JsonUtility.ToJson(game.allCardsData[i]));

					if (i < game.allCardsData.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			sb.Append("\"cardTemplate\":{\"instanceID\":" + game.cardTemplate.GetInstanceID() + "},");
			sb.Append("\"cardFieldDefinitions\":[");
			if (game.cardFieldDefinitions != null && game.cardFieldDefinitions.Count > 0)
			{
				for (int i = 0; i < game.cardFieldDefinitions.Count; i++)
				{
					sb.Append(JsonUtility.ToJson(game.cardFieldDefinitions[i]));

					if (i < game.cardFieldDefinitions.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			sb.Append("\"rules\":[");
			if (game.rules != null && game.rules.Count > 0)
			{
				for (int i = 0; i < game.rules.Count; i++)
				{
					sb.Append("{\"rulesetID\":\"" + game.rules[i].rulesetID + "\",");
					sb.Append("\"description\":\"" + game.rules[i].description + "\",");
					sb.Append("\"turnStructure\":\"" + game.rules[i].turnStructure + "\",");
					sb.Append("\"matchModifiers\":[");
					if (game.rules[i].matchModifiers != null && game.rules[i].matchModifiers.Count > 0)
					{
						for (int j = 0; j < game.rules[i].matchModifiers.Count; j++)
						{
							sb.Append(JsonUtility.ToJson(game.rules[i].matchModifiers[j]));

							if (j < game.rules[i].matchModifiers.Count - 1)
								sb.Append(",");
						}
					}
					sb.Append("]}");

					if (i < game.rules.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("]}");
			
			File.WriteAllText("Assets/" + game.cardgameID + ".json", sb.ToString().Replace("\n", ""));
			File.WriteAllText("Assets/" + game.cardgameID + "-JSON.json", JsonUtility.ToJson(game, true));
		}


		//void SaveFoldoutDictionary()
		//{
		//	PlayerPrefs.SetInt("showCardFieldDefinitionsFoldout", showCardFieldDefinitionsFoldout ? 1 : 0);
		//	PlayerPrefs.SetInt("showRulesetsFoldout", showRulesetsFoldout ? 1 : 0);
		//	PlayerPrefs.SetInt("showMatchModifiersFoldout", showMatchModifiersFoldout ? 1 : 0);
		//	PlayerPrefs.SetInt("showCardDataListFoldout", showCardDataListFoldout ? 1 : 0);
		//	for (int i = 0; i < gameDataList.Count; i++)
		//	{
		//		if (foldoutDictionary.ContainsKey(gameDataList[i]))
		//			PlayerPrefs.SetInt(gameDataList[i].cardgameID, foldoutDictionary[gameDataList[i]] ? 1 : 0);
		//		for (int j = 0; j < gameDataList[i].rules.Count; j++)
		//		{
		//			if (foldoutDictionary.ContainsKey(gameDataList[i].rules[j]))
		//				PlayerPrefs.SetInt(gameDataList[i].rules[j].rulesetID, foldoutDictionary[gameDataList[i].rules[j]] ? 1 : 0);
		//			for (int k = 0; k < gameDataList[i].rules[j].matchModifiers.Count; k++)
		//			{
		//				if (foldoutDictionary.ContainsKey(gameDataList[i].rules[j].matchModifiers[k]))
		//					PlayerPrefs.SetInt(gameDataList[i].rules[j].matchModifiers[k].modifierID, foldoutDictionary[gameDataList[i].rules[j].matchModifiers[k]] ? 1 : 0);
		//			}
		//		}
		//	}
		//}

		//void RecoverFoldoutDictionary ()
		//{
		//	showCardFieldDefinitionsFoldout = PlayerPrefs.GetInt("showCardFieldDefinitionsFoldout") == 1;
		//	showRulesetsFoldout = PlayerPrefs.GetInt("showRulesetsFoldout") == 1;
		//	showMatchModifiersFoldout = PlayerPrefs.GetInt("showMatchModifiersFoldout") == 1;
		//	showCardDataListFoldout = PlayerPrefs.GetInt("showCardDataListFoldout") == 1;
		//}
	}
}
