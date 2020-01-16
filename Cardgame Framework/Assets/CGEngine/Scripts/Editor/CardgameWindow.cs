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
		public float autoSaveTime = 120f;

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
		bool creatingNewGame;
		string newGameName;
		bool importingNewGame;
		TextAsset gameImportedFile;
		bool importingAListOfCards;
		//List<CardData> cardDataListBeingImported;
		bool listReadyToImport;
		string[] modTypes;
		double lastSaveTime;

		Dictionary<object, bool> foldoutDictionary;

		void OnEnable()
		{
			//Debug.Log("CardgameWindow Enable");

			// ---- Expand dictionary initialization ----
			if (foldoutDictionary == null)
				foldoutDictionary = new Dictionary<object, bool>();

			skin = (GUISkin)Resources.Load("CGEngineSkin");

			string[] foundAssets = AssetDatabase.FindAssets("t:CardGameData");
			if (foundAssets != null)
			{
				if (gameDataList == null) gameDataList = new List<CardGameData>();
				foreach (string item in foundAssets)
				{
					CardGameData data = AssetDatabase.LoadAssetAtPath<CardGameData>(AssetDatabase.GUIDToAssetPath(item));
					if (!gameDataList.Contains(data))
						gameDataList.Add(data);
				}
			}

			modTypes = Enum.GetNames(typeof(ModifierTypes));
		}

		private void Update()
		{
			if (gameBeingEdited != null && EditorApplication.timeSinceStartup - lastSaveTime >= 120)
			{
				Debug.Log("Saving game");
				File.WriteAllText("Assets/" + gameBeingEdited.cardgameID + " (autosave).json", CardGameSerializer.SaveToJson(gameBeingEdited));
				lastSaveTime = EditorApplication.timeSinceStartup;
			}
		}

		[MenuItem("CGEngine/Cardgame Definitions", priority = 1)]
		public static void ShowWindow()
		{
			GetWindow<CardgameWindow>("Cardgame Definitions");
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
			if (!creatingNewGame)
			{
				if (GUILayout.Button("New Game", GUILayout.Width(250), GUILayout.Height(25)))
				{
					newGameName = "New Game Name";
					creatingNewGame = true;
				}
			}
			else
			{
				newGameName = EditorGUILayout.TextField(newGameName, GUILayout.Width(150), GUILayout.Height(20));

				if (GUILayout.Button("Create", GUILayout.Width(50)))
				{
					if (string.IsNullOrEmpty(newGameName)) newGameName = "NewGame";
					else newGameName = newGameName.Replace(" ", "").Replace("/", "").Replace("\\", "").Replace(":", "").Replace("*", "").Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "");

					CardGameData gameData = CreateInstance<CardGameData>();
					gameData.cardgameID = newGameName;
					CheckOrCreateFolder("Resources");
					CheckOrCreateFolder("Resources/CardGames");
					AssetDatabase.CreateAsset(gameData, "Assets/Resources/CardGames/" + newGameName + ".asset");
					gameDataList.Add(gameData);
					gameBeingEdited = gameData;
					creatingNewGame = false;
				}
				if (GUILayout.Button("Cancel", GUILayout.Width(50)))
				{
					creatingNewGame = false;
				}
			}

			if (!importingNewGame)
			{
				if (GUILayout.Button("Import Game", GUILayout.Width(250), GUILayout.Height(25)))
				{
					importingNewGame = true;
				}
			}
			else
			{
				gameImportedFile = (TextAsset)EditorGUILayout.ObjectField(gameImportedFile, typeof(TextAsset), false, GUILayout.Width(150));
				if (GUILayout.Button("Import", GUILayout.Width(50), GUILayout.Height(20)))
				{
					CardGameData importedGame = CardGameSerializer.RecoverFromJson(File.ReadAllText(AssetDatabase.GetAssetPath(gameImportedFile)));
					gameDataList.Add(importedGame);
					CheckOrCreateFolder("Resources");
					CheckOrCreateFolder("Resources/CardGames");
					AssetDatabase.CreateAsset(importedGame, "Assets/Resources/CardGames/" + importedGame.cardgameID + ".asset");
					importingNewGame = false;
					gameImportedFile = null;
				}
				if (GUILayout.Button("Cancel", GUILayout.Width(50), GUILayout.Height(20)))
				{
					importingNewGame = false;
					gameImportedFile = null;
				}
			}
			EditorGUILayout.EndHorizontal();
			// ---- Display other games and buttons for deleting or editing
			for (int i = 0; i < gameDataList.Count; i++)
			{
				GUILayout.Space(15);
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
						File.WriteAllText("Assets/" + gameBeingEdited.cardgameID + ".json", CardGameSerializer.SaveToJson(gameBeingEdited));
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
						lastSaveTime = EditorApplication.timeSinceStartup;
						gameBeingEdited = gameDataList[i];
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
							}
							markedForDeletion.rules[i].matchModifiers.Clear();
						}
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
								foldoutDictionary.Remove(markedForDeletion.allCardsData[i].cardModifiers[j]);
							}
							markedForDeletion.allCardsData[i].cardModifiers.Clear();
						}
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

		// ======================================= CARD GAMES =======================================================
		void DisplayCardGameData(CardGameData data)
		{
			EditorGUILayout.BeginVertical();

			EditorGUILayout.LabelField(data.cardgameID, EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			data.cardgameID = EditorGUILayout.TextField("Game Name", data.cardgameID, GUILayout.MaxWidth(400));
			if (EditorGUI.EndChangeCheck())
			{
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
			else
			{
				EditorGUILayout.LabelField("  - - - Please add a Card Template to continue - - -");
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

					//Display editable content
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));
					EditorGUILayout.BeginVertical(GUILayout.MaxWidth(200));
					string newFieldName = GUILayout.TextField(fields[i].fieldName);
					if (newFieldName != fields[i].fieldName)
					{
						string oldName = fields[i].fieldName;
						fields[i].fieldName = newFieldName;
						if (gameBeingEdited.allCardsData != null)
						{
							for (int k = 0; k < gameBeingEdited.allCardsData.Count; k++)
							{
								for (int j = 0; j < gameBeingEdited.allCardsData[k].fields.Count; j++)
								{
									if (gameBeingEdited.allCardsData[k].fields[j].fieldName == oldName)
									{
										gameBeingEdited.allCardsData[k].fields[j].fieldName = newFieldName;
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
									if (gameBeingEdited.allCardsData[k].fields[j].fieldName == fields[i].fieldName)
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
										if (gameBeingEdited.allCardsData[k].fields[j].fieldName == fields[i].fieldName)
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
									newField.fieldName = cardToCopyFields.fields[i].fieldName;
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
								if (gameBeingEdited.allCardsData[i].fields[j].fieldName == toBeDeleted.fieldName)
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

					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));
					EditorGUILayout.BeginVertical(GUILayout.Width(800));
					//Ruleset name
					string newName = EditorGUILayout.TextField("Ruleset Name", rulesets[i].rulesetID);
					if (newName != rulesets[i].rulesetID)
					{
						rulesets[i].rulesetID = newName;
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
					Ruleset newRuleset = new Ruleset();
					rulesets.Add(newRuleset);
					newRuleset.rulesetID = "New Ruleset";
				}

				if (toBeDeleted != null)
				{
					rulesets.Remove(toBeDeleted);
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
					}
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
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Triggers");
						modifiers[i].trigger = EditorGUILayout.TextArea(modifiers[i].trigger);
						EditorGUILayout.EndHorizontal();
						// ---- Condition
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Condition");
						modifiers[i].condition = EditorGUILayout.TextArea(modifiers[i].condition);
						EditorGUILayout.EndHorizontal();
						// ---- Affected
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
				ModifierData newMod = new ModifierData();
				modifiers.Add(newMod);
				foldoutDictionary.Add(newMod, true);
				newMod.modifierID = "New " + prefix;
			}
			GUILayout.Space(15);

			if (toBeDeleted != null)
			{
				modifiers.Remove(toBeDeleted);
				foldoutDictionary.Remove(toBeDeleted);
			}

			if (moveUp != null)
			{
				int index = modifiers.IndexOf(moveUp);
				if (index > 0)
				{
					modifiers.Remove(moveUp);
					index--;
					modifiers.Insert(index, moveUp);
				}
			}

			if (moveDown != null)
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
					}

					// ---- Import a List of Cards ---- 
					Event evt = Event.current;
					Rect dropArea = GUILayoutUtility.GetRect(250.0f, 20.0f, GUILayout.Width(250));
					string boxMessage = "Drop Cards Here To Be Imported";
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
									if (draggedObject.GetType() == typeof(CardData))
										gameBeingEdited.allCardsData.Add((CardData)draggedObject);
									else if (draggedObject.GetType() == typeof(TextAsset))
									{
										List<CardData> listOfCards = CardGameSerializer.RecoverListOfCardsFromJson((TextAsset)draggedObject);
										CheckOrCreateFolder("Resources/Cards");
										for (int i = 0; i < listOfCards.Count; i++)
										{
											AssetDatabase.CreateAsset(listOfCards[i], "Assets/Resources/Cards/Card-" + listOfCards[i].cardDataID + ".asset");
										}
										cards.AddRange(listOfCards);
									}
								}
							}
							break;
					}

					if (GUILayout.Button("Instantiate Cards in Scene", GUILayout.MaxWidth(170), GUILayout.MaxHeight(18)))
					{
						CGEngine.CreateCards(gameBeingEdited.cardTemplate, cards, Vector3.zero);
					}
					if (GUILayout.Button("Clear All Cards", GUILayout.MaxWidth(170), GUILayout.MaxHeight(18)))
					{
						cards.Clear();
					}
					EditorGUILayout.EndHorizontal();

					// TITLE ROW
					EditorGUILayout.BeginHorizontal(EditorStyles.miniButtonMid, GUILayout.MinWidth(minHorizontalWidth), GUILayout.MaxWidth(maxHorizontalWidth));
					// ---- Expand button title ----
					EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
					EditorGUILayout.LabelField("►", GUILayout.Width(buttonWidth));
					EditorGUILayout.EndVertical();
					// ---- Card data ID name title ----
					EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
					EditorGUILayout.LabelField("     Data Name");
					EditorGUILayout.EndVertical();
					// ---- Card Tags title  ----
					EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
					EditorGUILayout.LabelField("     Tags");
					EditorGUILayout.EndVertical();
					for (int i = 0; i < gameBeingEdited.cardFieldDefinitions.Count; i++)
					{
						EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
						EditorGUILayout.LabelField("     " + gameBeingEdited.cardFieldDefinitions[i].fieldName);
						EditorGUILayout.EndVertical();
					}
					// ---- Delete button title ----
					EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
					EditorGUILayout.LabelField("X", GUILayout.Width(buttonWidth));
					EditorGUILayout.EndVertical();
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
						EditorGUI.BeginChangeCheck();

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

							//Edit Fields Data
							EditorGUILayout.BeginHorizontal();
							GUILayout.Space(25);
							ShowCardFieldData(cards[i], true);
							GUILayout.Space(25);
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
										if (cards[i].fields[j].fieldName == tempList[k].fieldName && cards[i].fields[j].dataType == tempList[k].dataType)
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

							ShowCardFieldData(cards[i], false);

							// ---- Delete button ----
							EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
							if (GUILayout.Button("X", GUILayout.Width(buttonWidth)))
							{
								toBeDeleted = cards[i];
							}
							EditorGUILayout.EndVertical();
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

						if (EditorGUI.EndChangeCheck())
							EditorUtility.SetDirty(cards[i]);
					}
					EditorGUILayout.LabelField("      " + cards.Count + " card" +(cards.Count > 1 ? "s" : ""));

					if (toBeDeleted)
					{
						cards.Remove(toBeDeleted);
					}
				}
				else
				{
					EditorGUILayout.LabelField("- - - Define the card fields above before creating any card - - -");
				}
			}
		}

		void ShowCardFieldData (CardData card, bool editableFields)
		{
			// ---- Card data ID name ----
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
			string newName = EditorGUILayout.TextField(card.cardDataID, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
			if (newName != card.cardDataID)
			{
				card.cardDataID = newName;
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(card), "Card-" + newName);
			}
			EditorGUILayout.EndVertical();
			
			// ---- Card Tags  ----
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
			card.tags = EditorGUILayout.TextField(card.tags, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
			EditorGUILayout.EndVertical();

			for (int j = 0; j < card.fields.Count; j++)
			{
				EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
				if (editableFields)
				{
					card.fields[j].fieldName = EditorGUILayout.TextField(card.fields[j].fieldName, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
					card.fields[j].dataType = (CardFieldDataType)EditorGUILayout.EnumPopup(card.fields[j].dataType);
				}
				switch (card.fields[j].dataType)
				{
					case CardFieldDataType.Text:
						card.fields[j].stringValue = EditorGUILayout.TextField(card.fields[j].stringValue, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
						break;
					case CardFieldDataType.Number:
						card.fields[j].numValue = EditorGUILayout.DoubleField(card.fields[j].numValue, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
						if (editableFields) card.fields[j].hideOption = (CardFieldHideOption)EditorGUILayout.EnumPopup(card.fields[j].hideOption);
						break;
					case CardFieldDataType.Image:
						card.fields[j].imageValue = (Sprite)EditorGUILayout.ObjectField(card.fields[j].imageValue, typeof(Sprite), false, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
						break;
				}
				EditorGUILayout.EndVertical();
			}
		}

		// ======================================= HELPER METHODS =======================================================

		void CheckOrCreateFolder(string folderName)
		{
			int startIndex = 0;
			int slashIndex = folderName.IndexOf("/");
			if (slashIndex == -1)
			{
				if (!AssetDatabase.IsValidFolder("Assets/" + folderName))
				{
					AssetDatabase.CreateFolder("Assets", folderName);
				}
			}
			else
			{
				while (slashIndex != -1)
				{
					string parentFolder = folderName.Substring(startIndex, slashIndex);
					if (!AssetDatabase.IsValidFolder("Assets/" + parentFolder))
					{
						AssetDatabase.CreateFolder("Assets", parentFolder);
					}
					slashIndex++;
					if (slashIndex >= folderName.Length)
						break;
					startIndex = slashIndex;
					slashIndex = folderName.IndexOf("/", startIndex);
				}
			}
			
		}

		bool CardHasUniformFields(CardData data)
		{
			if (data == null || data.fields == null || data.fields.Count != gameBeingEdited.cardFieldDefinitions.Count)
				return false;

			for (int i = 0; i < data.fields.Count; i++)
			{
				if (data.fields[i].fieldName != gameBeingEdited.cardFieldDefinitions[i].fieldName ||
					data.fields[i].dataType != gameBeingEdited.cardFieldDefinitions[i].dataType ||
					data.fields[i].hideOption != gameBeingEdited.cardFieldDefinitions[i].hideOption)
					return false;
			}

			return true;
		}
	}
}
