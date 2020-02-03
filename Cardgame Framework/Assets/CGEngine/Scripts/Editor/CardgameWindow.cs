using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CardGameFramework
{
	public class CardgameWindow : EditorWindow
	{
		public float autoSaveTime = 120f;

		List<CardGameData> gameDataList;
		CardGameData gameBeingEdited;
		//bool showCardFieldDefinitionsFoldout;
		//bool showRulesetsFoldout;
		//bool showMatchModifiersFoldout;
		//bool showCardDataListFoldout;
		CardData cardToCopyFields;
		CardGameData markedForDeletion;
		Vector2 windowScrollPos;
		Vector2 cardsScrollPos;
		GUISkin customSkin;
		float minHorizontalWidth = 300;
		float maxHorizontalWidth = 9999;
		float minWidthFields = 150;
		float maxWidthFields = 250;
		float buttonWidth = 25;
		bool copyingFields;
		bool creatingNewGame;
		string newGameName;
		bool goodNewGameName;
		bool importingNewGame;
		TextAsset gameImportedFile;
		bool importingAListOfCards;
		List<CardData> cardDataListBeingImported;
		bool listReadyToImport;
		double lastSaveTime;
		Dictionary<object, bool> foldoutDictionary;
		HashSet<string> nameFieldsWithError = new HashSet<string>();
		Color lightLineColor;
		Color boldLineColor;
		GUIStyle errorStyle;
		GUIContent nameErrorContent = new GUIContent("Error!", "Name must contain only letters, numbers, or _ (underscore)");
		GUIContent variableDuplicateErrorContent = new GUIContent("Duplicate variable name", "This variable name is already in use");
		List<string> triggerTags;

		void OnEnable ()
		{
			// ---- Expand dictionary initialization ----
			if (foldoutDictionary == null)
			{
				foldoutDictionary = new Dictionary<object, bool>();
				//	foldoutDictionary.Add("ShowRulesetVariables", false);
				//	foldoutDictionary.Add("ShowGameVariables", false);
				//	foldoutDictionary.Add("ShowCardFieldDefinitions", false);
				//	foldoutDictionary.Add("ShowRulesets", false);
				//	foldoutDictionary.Add("ShowMatchModifiers", false);
				//	foldoutDictionary.Add("ShowCardDataList", false);
			}

			customSkin = (GUISkin)Resources.Load("CGEngineSkin");
			errorStyle = new GUIStyle();
			errorStyle.normal.textColor = Color.red;
			lightLineColor = new Color(0.6f, 0.6f, 0.6f, 1f);
			boldLineColor = new Color(0.3f, 0.3f, 0.3f, 1f);
			triggerTags = new List<string>();
			triggerTags.AddRange(Enum.GetNames(typeof(TriggerTag)));

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
		}

		private void Update ()
		{
			if (gameBeingEdited != null && EditorApplication.timeSinceStartup - lastSaveTime >= 120)
			{
				Debug.Log("Saving game");
				SaveGame(true);
				lastSaveTime = EditorApplication.timeSinceStartup;
			}
		}

		private void OnDestroy ()
		{
			if (gameBeingEdited)
				SaveGame(true);
		}

		[MenuItem("Window/Cardgame Editor")]
		public static void ShowWindow ()
		{
			GetWindow<CardgameWindow>("Cardgame Definitions");
		}

		// ======================================= ON GUI =======================================================
		void OnGUI ()
		{
			windowScrollPos = EditorGUILayout.BeginScrollView(windowScrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));
			// --- First Label ------
			GUILayout.Label("Card Game Definitions", EditorStyles.boldLabel);

			// ---- Clear list if empty ----
			for (int i = gameDataList.Count - 1; i >= 0; i--)
			{
				if (gameDataList[i] == null)
					gameDataList.RemoveAt(i);
			}

			// ---- New game button ----
			EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(500));
			if (!creatingNewGame)
			{
				if (GUILayout.Button("New Game", GUILayout.Width(250), GUILayout.Height(25)))
				{
					newGameName = "NewGameName";
					creatingNewGame = true;
				}
			}
			else
			{
				VerifiedDelayedTextField("$newGameName", ref newGameName, GUILayout.Width(150), GUILayout.Height(25));
				//newGameName = EditorGUILayout.TextField(newGameName, GUILayout.Width(150), GUILayout.Height(20));

				if (GUILayout.Button("Create", GUILayout.Width(50), GUILayout.Height(25)))
				{
					if (!nameFieldsWithError.Contains("$newGameName"))
					{
						CardGameData gameData = CreateInstance<CardGameData>();
						gameData.cardgameID = newGameName;
						CreateAsset(gameData, "Data/CardGames", newGameName);
						gameDataList.Add(gameData);
						gameBeingEdited = gameData;
						creatingNewGame = false;
					}
				}
				if (GUILayout.Button("Cancel", GUILayout.Width(50), GUILayout.Height(25)))
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
				gameImportedFile = (TextAsset)EditorGUILayout.ObjectField(gameImportedFile, typeof(TextAsset), false, GUILayout.Width(150), GUILayout.Height(25));
				if (GUILayout.Button("Import", GUILayout.Width(50), GUILayout.Height(25)))
				{
					string sourceImagesFolder = EditorUtility.OpenFolderPanel("Select source images folder for the imported cards", Application.dataPath, "");
					CardGameData importedGame = CardGameSerializer.RecoverFromJson(File.ReadAllText(AssetDatabase.GetAssetPath(gameImportedFile)), sourceImagesFolder);
					gameDataList.Add(importedGame);
					CreateAsset(importedGame, "Data/CardGames", importedGame.cardgameID);
					importingNewGame = false;
					gameImportedFile = null;
				}
				if (GUILayout.Button("Cancel", GUILayout.Width(50), GUILayout.Height(25)))
				{
					importingNewGame = false;
					gameImportedFile = null;
				}
			}
			EditorGUILayout.EndHorizontal();

			// ---- Display other games and buttons for deleting or editing
			for (int i = 0; i < gameDataList.Count; i++)
			{
				DrawBoldLine();
				EditorGUILayout.BeginHorizontal();
				//EditorGUILayout.LabelField((i + 1) + ".  ", GUILayout.MaxWidth(20));

				if (gameDataList[i] == gameBeingEdited)
				{
					Undo.RecordObject(gameBeingEdited, "CGEngine.CardGame Change");
					// ---- Edit game ----
					EditorGUILayout.BeginVertical();
					EditorGUILayout.BeginHorizontal();
					// ---- Save button ----
					if (GUILayout.Button("Save", GUILayout.Width(80), GUILayout.Height(18)))
					{
						SaveGame(false);
						gameBeingEdited = null;
					}
					GUILayout.Space(15);
					// ---- Game Name Bold ----
					EditorGUILayout.LabelField(gameDataList[i].cardgameID, EditorStyles.boldLabel);
					GUILayout.Space(15);
					// ---- Delete game button ----
					if (GUILayout.Button("Delete", GUILayout.Width(80), GUILayout.Height(18)))
					{
						markedForDeletion = gameBeingEdited;
						gameBeingEdited = null;
					}

					EditorGUILayout.EndHorizontal();
					GUILayout.Space(15);
					// ---- Game being edited ----
					if (gameBeingEdited) DisplayCardGameData(gameBeingEdited);
					EditorGUILayout.EndVertical();
				}
				else
				{
					// ---- Edit game button ----
					if (GUILayout.Button("Edit", GUILayout.Width(80), GUILayout.Height(18)))
					{
						lastSaveTime = EditorApplication.timeSinceStartup;
						gameBeingEdited = gameDataList[i];
						break;
					}
					GUILayout.Space(15);
					// ---- Game Name Bold ----
					EditorGUILayout.LabelField(gameDataList[i].cardgameID, EditorStyles.boldLabel);
					GUILayout.Space(15);
					// ---- Delete game button ----
					if (GUILayout.Button("Delete", GUILayout.Width(80), GUILayout.Height(18)))
					{
						markedForDeletion = gameDataList[i];
					}
				}
				EditorGUILayout.EndHorizontal();
			}

			DrawBoldLine();
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
							//for (int j = markedForDeletion.rules[i].matchModifiers.Count - 1; j >= 0; j--)
							//{
							//	foldoutDictionary.Remove(markedForDeletion.rules[i].matchModifiers[j]);
							//}
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
							//for (int j = markedForDeletion.allCardsData[i].cardModifiers.Count - 1; j >= 0; j--)
							//{
							//	foldoutDictionary.Remove(markedForDeletion.allCardsData[i].cardModifiers[j]);
							//}
							markedForDeletion.allCardsData[i].cardModifiers.Clear();
						}
					}
					markedForDeletion.allCardsData.Clear();
				}

				if (markedForDeletion == gameBeingEdited) gameBeingEdited = null;
				//foreach (var item in foldoutDictionary)
				//{
				//	foldoutDictionary[item.Key] = false;
				//}
				gameDataList.Remove(markedForDeletion);
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(markedForDeletion));
				markedForDeletion = null;
			}
			EditorGUILayout.EndScrollView();
		}

		// ======================================= CARD GAMES =======================================================
		void DisplayCardGameData (CardGameData data)
		{
			EditorGUILayout.BeginVertical();

			//Game name
			//EditorGUILayout.LabelField(data.cardgameID, EditorStyles.boldLabel);
			if (VerifiedDelayedTextField("Game Name", ref data.cardgameID, GUILayout.MaxWidth(400)))
			{
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(data), data.cardgameID);
			}

			//Card template
			data.cardTemplate = (GameObject)EditorGUILayout.ObjectField("Card Template", data.cardTemplate, typeof(GameObject), false, GUILayout.MaxWidth(400));
			DrawLightLine();
			if (data.cardTemplate)
			{

				if (data.cardFieldDefinitions == null)
					data.cardFieldDefinitions = new List<CardField>();
				DisplayCardFieldDefinitions(data.cardFieldDefinitions);
				DrawLightLine();
				//custom variables
				//if (GUILayout.Button("Custom Game Variables", EditorStyles.foldout))
				//	foldoutDictionary["ShowGameVariables"] = !foldoutDictionary["ShowGameVariables"];
				//if (foldoutDictionary["ShowGameVariables"])
				//{
				if (data.customVariableNames == null)
				{
					data.customVariableNames = new List<string>();
					data.customVariableValues = new List<string>();
				}

				List<string> varNames = data.customVariableNames;
				List<string> varValues = data.customVariableValues;
				int varFieldToDelete = -1;
				for (int j = 0; j < varNames.Count; j++)
				{
					EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(430));
					string label = "$customGameVariable" + j;
					string tempString = varNames[j];
					if (VerifiedDelayedTextField(label, ref tempString, GUILayout.MaxWidth(200)))
					{
						varNames[j] = tempString;
					}
					varValues[j] = EditorGUILayout.DelayedTextField(varValues[j], GUILayout.MaxWidth(200));

					if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(18)))
					{
						varFieldToDelete = j;
					}
					EditorGUILayout.EndHorizontal();
				}
				if (varFieldToDelete >= 0)
				{
					nameFieldsWithError.Remove("$customGameVariable" + varFieldToDelete);
					varNames.RemoveAt(varFieldToDelete);
					varValues.RemoveAt(varFieldToDelete);
				}

				if (GUILayout.Button("Create New Variable", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
				{
					data.customVariableNames.Add("");
					data.customVariableValues.Add("");
				}
				//}

				DrawLightLine();

				//Rulesets
				if (data.rules == null)
					data.rules = new List<Ruleset>();
				DisplayRulesets(data.rules);

				DrawLightLine();
				//Cards
				if (data.allCardsData == null)
					data.allCardsData = new List<CardData>();
				DisplayCardDataList(data.allCardsData);
			}
			else
			{
				if (GUILayout.Button("Create Basic Card Template", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
				{
					CheckOrCreateFolder("Resources");
					if (AssetDatabase.CopyAsset("Assets/CGEngine/Resources/BasicCardTemplate.prefab", "Assets/" + gameBeingEdited.cardgameID + "CardTemplate.prefab"))
					{
						gameBeingEdited.cardTemplate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + gameBeingEdited.cardgameID + "CardTemplate.prefab");
					}
					else
					{
						Debug.LogError("The Basic Card Template at CGEngine/Resources coudn't be loaded. You can reimport the package to recover it.");
					}
				}
				//EditorGUILayout.LabelField("  - - - Please add a Card Template to continue - - -");
			}
			EditorGUILayout.EndVertical();
		}

		// ======================================= CARD FIELDS =======================================================
		void DisplayCardFieldDefinitions (List<CardField> fields)
		{
			CardField toBeDeleted = null;

			//if (GUILayout.Button("Card Field Definitions", EditorStyles.foldout))
			//	foldoutDictionary["ShowCardFieldDefinitions"] = !foldoutDictionary["ShowCardFieldDefinitions"];
			//if (foldoutDictionary["ShowCardFieldDefinitions"])
			//{
			for (int i = 0; i < fields.Count; i++)
			{
				bool nextLine = i % 4 == 0;
				bool endLine = (i + 1) % 4 == 0 || i == fields.Count - 1;

				if (nextLine)
					EditorGUILayout.BeginHorizontal();
				//if ((i+1) % 3 == 0)
				EditorGUILayout.BeginHorizontal(GUILayout.Width(230));
				//Display editable content
				EditorGUILayout.BeginVertical();
				string oldName = fields[i].fieldName;
				if (VerifiedDelayedTextField("$fieldName" + i, ref fields[i].fieldName))
				{
					if (gameBeingEdited.allCardsData != null)
					{
						for (int k = 0; k < gameBeingEdited.allCardsData.Count; k++)
						{
							for (int j = 0; j < gameBeingEdited.allCardsData[k].fields.Count; j++)
							{
								if (gameBeingEdited.allCardsData[k].fields[j].fieldName == oldName)
								{
									gameBeingEdited.allCardsData[k].fields[j].fieldName = fields[i].fieldName;
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
				if (endLine)
				{
					if (i == fields.Count - 1 && fields.Count % 4 != 0)
					{
						for (int j = 0; j < 4 - fields.Count % 4; j++)
						{
							EditorGUILayout.LabelField("", GUILayout.Width(230));
						}
					}
					EditorGUILayout.EndHorizontal();
				}
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
			//}
		}

		// ======================================= RULESETS =======================================================
		void DisplayRulesets (List<Ruleset> rulesets)
		{
			Ruleset toBeDeleted = null;

			//if (GUILayout.Button("Rulesets", EditorStyles.foldout))
			//	foldoutDictionary["ShowRulesets"] = !foldoutDictionary["ShowRulesets"];
			//if (foldoutDictionary["ShowRulesets"])
			//{

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
				//Ruleset name
				VerifiedDelayedTextField("Ruleset Name", ref rulesets[i].rulesetID);
				//Ruleset description
				rulesets[i].description = EditorGUILayout.TextField("Description", rulesets[i].description);
				//Ruleset turn structure
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Turn Structure");
				rulesets[i].turnStructure = EditorGUILayout.TextArea(rulesets[i].turnStructure);
				EditorGUILayout.EndHorizontal();
				DrawLightLine();
				//Ruleset variables

				//if (GUILayout.Button("Custom Ruleset Variables", EditorStyles.foldout))
				//	foldoutDictionary["ShowRulesetVariables"] = !foldoutDictionary["ShowRulesetVariables"];
				//if (foldoutDictionary["ShowRulesetVariables"])
				//{
				if (rulesets[i].customVariableNames == null)
				{
					rulesets[i].customVariableNames = new List<string>();
					rulesets[i].customVariableValues = new List<string>();
				}

				GUILayout.BeginHorizontal();
				GUILayout.Space(20);
				GUILayout.BeginVertical();
				List<string> varNames = rulesets[i].customVariableNames;
				List<string> varValues = rulesets[i].customVariableValues;
				int varFieldToDelete = -1;
				for (int j = 0; j < varNames.Count; j++)
				{
					EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(430));
					string label = "$customVariable" + j;
					string tempString = varNames[j];
					if (VerifiedDelayedTextField(label, ref tempString, GUILayout.MaxWidth(200)))
					{
						varNames[j] = tempString;
					}
					varValues[j] = EditorGUILayout.DelayedTextField(varValues[j], GUILayout.MaxWidth(200));

					if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(18)))
					{
						varFieldToDelete = j;
					}
					EditorGUILayout.EndHorizontal();
				}
				if (varFieldToDelete >= 0)
				{
					nameFieldsWithError.Remove("$customVariable" + varFieldToDelete);
					varNames.RemoveAt(varFieldToDelete);
					varValues.RemoveAt(varFieldToDelete);
				}

				if (GUILayout.Button("Create New Variable", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
				{
					rulesets[i].customVariableNames.Add("");
					rulesets[i].customVariableValues.Add("");
				}
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				//}
				DrawLightLine();
				//Match Modifiers

				//if (GUILayout.Button("Match Modifiers", EditorStyles.foldout))
				//	foldoutDictionary["ShowMatchModifiers"] = !foldoutDictionary["ShowMatchModifiers"];
				//if (foldoutDictionary["ShowMatchModifiers"])
				//{
				GUILayout.BeginHorizontal();
				GUILayout.Space(20);
				GUILayout.BeginVertical();
				if (rulesets[i].matchModifiers == null)
					rulesets[i].matchModifiers = new List<ModifierData>();
				DisplayModifiers(rulesets[i].matchModifiers, "Modifier");
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				//}
				DrawLightLine();
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
			//}
		}

		// ======================================= MODIFIERS =======================================================
		void DisplayModifiers (List<ModifierData> modifiers, string prefix)
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

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));

				//if (!foldoutDictionary.ContainsKey(modifiers[i]))
				//	foldoutDictionary.Add(modifiers[i], false);

				//if (foldoutDictionary[modifiers[i]])
				//{
				//if (GUILayout.Button(" ▼", GUILayout.Width(20), GUILayout.Height(20)))
				//{
				//	foldoutDictionary[modifiers[i]] = false;
				//}
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
				VerifiedDelayedTextField("Modifier Name", ref modifiers[i].modifierID);
				// ---- Tags
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Tags");
				modifiers[i].tags = EditorGUILayout.TextArea(modifiers[i].tags);
				EditorGUILayout.EndHorizontal();

				// ----- Triggers
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Trigger");
				//modifiers[i].trigger = EditorGUILayout.TextArea(modifiers[i].trigger);



				TriggerEnums(ref modifiers[i].trigger);






				EditorGUILayout.EndHorizontal();
				// ---- Condition
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Condition");
				modifiers[i].condition = EditorGUILayout.TextArea(modifiers[i].condition);
				EditorGUILayout.EndHorizontal();
				// ---- Affected
				//EditorGUILayout.BeginHorizontal();
				//EditorGUILayout.PrefixLabel("Affected");
				//modifiers[i].affected = EditorGUILayout.TextArea(modifiers[i].affected);
				//EditorGUILayout.EndHorizontal();
				// ---- True effect
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Commands");
				modifiers[i].commands = EditorGUILayout.TextArea(modifiers[i].commands);
				EditorGUILayout.EndHorizontal();
				// ---- False effect
				//EditorGUILayout.BeginHorizontal();
				//EditorGUILayout.PrefixLabel("False Effect");
				//modifiers[i].falseEffect = EditorGUILayout.TextArea(modifiers[i].falseEffect);
				//EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();

				//}
				//else
				//{
				//	if (GUILayout.Button(" ►", GUILayout.Width(20), GUILayout.Height(20)))
				//	{
				//		foldoutDictionary[modifiers[i]] = true;
				//	}
				//	EditorGUILayout.BeginVertical(GUILayout.Width(20));
				//	if (GUILayout.Button(" ↑ ", GUILayout.Width(20), GUILayout.Height(20)))
				//	{
				//		//Move Up
				//		moveUp = modifiers[i];
				//	}
				//	if (GUILayout.Button(" ↓ ", GUILayout.Width(20), GUILayout.Height(20)))
				//	{
				//		//Move Down
				//		moveDown = modifiers[i];
				//	}
				//	EditorGUILayout.EndVertical();

				//	EditorGUILayout.BeginVertical(GUILayout.MaxWidth(400));
				//	EditorGUILayout.LabelField(modifiers[i].modifierID);
				//	EditorGUILayout.LabelField("    " + modifiers[i].trigger);
				//	EditorGUILayout.EndVertical();
				//}


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
				//foldoutDictionary.Add(newMod, true);
				newMod.modifierID = "New" + prefix;
			}
			GUILayout.Space(15);

			if (toBeDeleted != null)
			{
				modifiers.Remove(toBeDeleted);
				//foldoutDictionary.Remove(toBeDeleted);
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
		void DisplayCardDataList (List<CardData> cards)
		{
			//if (GUILayout.Button("All Cards", EditorStyles.foldout))
			//	foldoutDictionary["ShowCardDataList"] = !foldoutDictionary["ShowCardDataList"];
			//if (foldoutDictionary["ShowCardDataList"])
			//{
			if (gameBeingEdited.cardFieldDefinitions != null && gameBeingEdited.cardFieldDefinitions.Count > 0)
			{
				CardData toBeDeleted = null;

				minHorizontalWidth = buttonWidth * 2 + minWidthFields * 2 + minWidthFields * gameBeingEdited.cardFieldDefinitions.Count;
				maxHorizontalWidth = buttonWidth * 2 + maxWidthFields * 2 + maxWidthFields * gameBeingEdited.cardFieldDefinitions.Count;

				GUILayout.Space(10);
				//BUTTONS
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

				DisplayCardImporterField();

				if (GUILayout.Button("Instantiate Cards in Scene", GUILayout.MaxWidth(170), GUILayout.MaxHeight(18)))
				{
					CGEngine.CreateCards(gameBeingEdited.cardTemplate, cards, Vector3.zero);
				}
				if (GUILayout.Button("Clear All Cards", GUILayout.MaxWidth(170), GUILayout.MaxHeight(18)))
				{
					cards.Clear();
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(10);

				// TITLE ROW
				EditorGUILayout.BeginHorizontal(GUILayout.MinWidth(minHorizontalWidth), GUILayout.MaxWidth(maxHorizontalWidth));
				// ---- Expand button title ----
				EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
				EditorGUILayout.LabelField(" ", GUILayout.Width(buttonWidth));
				EditorGUILayout.EndVertical();
				// ---- Card data ID name title ----
				EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
				EditorGUILayout.LabelField("     Data Name");
				EditorGUILayout.EndVertical();
				// ---- Delete button title ----
				EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
				EditorGUILayout.LabelField("X", GUILayout.Width(buttonWidth));
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

					// ---- CARD FIELDS ARE NOT COMPATIBLE WITH GAME DEFINITIONS ----
					if (!CardHasUniformFields(cards[i]))
					{
						EditorGUILayout.BeginHorizontal(EditorStyles.textArea, GUILayout.MinWidth(minHorizontalWidth), GUILayout.MaxWidth(maxHorizontalWidth));
						EditorGUILayout.BeginVertical();
						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(25);
						EditorGUILayout.LabelField(cards[i].cardDataID, GUILayout.Width(100));
						EditorGUILayout.LabelField(" --- This card fields are not compatible with the game fields defined! ---- ");
						EditorGUILayout.EndHorizontal();

						//Edit Fields Data
						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(25);
						ShowEditableCardName(cards[i], true);
						GUILayout.Space(25);
						ShowEditableTagsAndCardFields(cards[i], true);
						GUILayout.Space(25);
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(25);
						if (GUILayout.Button("Delete", GUILayout.Width(70)))
						{
							toBeDeleted = cards[i];
						}
						if (GUILayout.Button("Conform Undefined Fields", GUILayout.Width(200)))
						{
							ConformCardFieldsWithDefinitions(cards[i]);
						}
						if (GUILayout.Button("Set All Field Definitions to This", GUILayout.Width(200)))
						{
							OverwriteFieldDefinitionsFromCard(cards[i]);
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();
					}
					else
					{
						// ---- CARD FIELDS ARE OK ! ----
						EditorGUILayout.BeginHorizontal(GUILayout.MinWidth(minHorizontalWidth), GUILayout.MaxWidth(maxHorizontalWidth));
						EditorGUILayout.BeginVertical();
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
							if (GUILayout.Button(" ▼", GUI.skin.label, GUILayout.Width(buttonWidth)))
							{
								foldoutDictionary[cards[i]] = false;
							}
						}
						EditorGUILayout.EndVertical();

						ShowEditableCardName(cards[i], false);
						// ---- Delete button ----
						EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
						if (GUILayout.Button("X", GUILayout.Width(buttonWidth)))
						{
							toBeDeleted = cards[i];
						}
						EditorGUILayout.EndVertical();
						ShowEditableTagsAndCardFields(cards[i], false);


						EditorGUILayout.EndHorizontal();

						//Card Modifiers
						if (foldoutDictionary[cards[i]])
						{
							EditorGUILayout.BeginVertical();
							DisplayModifiers(cards[i].cardModifiers, "CardModifier");
							EditorGUILayout.EndVertical();
						}
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();
					}

					if (EditorGUI.EndChangeCheck())
						EditorUtility.SetDirty(cards[i]);
				}
				EditorGUILayout.LabelField("      " + cards.Count + " card" + (cards.Count > 1 ? "s" : ""));

				if (toBeDeleted)
				{
					cards.Remove(toBeDeleted);
				}
			}
			else
			{
				DisplayCardImporterField();

				//EditorGUILayout.LabelField("- - - Define the card fields above before creating any card - - -");
			}
			//}
		}

		/// <summary>
		/// Shows a field for drag and drop of cards to be imported.
		/// </summary>
		void DisplayCardImporterField ()
		{
			// ---- Import a List of Cards ---- 
			Event evt = Event.current;
			Rect dropArea = GUILayoutUtility.GetRect(250.0f, 20.0f, GUILayout.MaxWidth(250.0f));
			string boxMessage = "Drop Cards Here To Be Imported";
			string sourceImagesFolder = "";

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
						cardDataListBeingImported = new List<CardData>();
						CardData importedCard = null;
						foreach (Object draggedObject in DragAndDrop.objectReferences)
						{
							if (draggedObject.GetType() == typeof(CardData))
							{
								importedCard = (CardData)draggedObject;
								gameBeingEdited.allCardsData.Add(importedCard);
							}
							else if (draggedObject.GetType() == typeof(TextAsset))
							{
								sourceImagesFolder = EditorUtility.OpenFolderPanel("Select source images folder for the imported cards", Application.dataPath, "");
								cardDataListBeingImported = CardGameSerializer.RecoverListOfCardsFromJson((TextAsset)draggedObject, sourceImagesFolder);

								for (int i = 0; i < cardDataListBeingImported.Count; i++)
								{
									EditorUtility.DisplayProgressBar("Importing Cards", "Importing: " + cardDataListBeingImported[i].cardDataID, (float)i / cardDataListBeingImported.Count);
									CreateAsset(cardDataListBeingImported[i], "Data/Cards", cardDataListBeingImported[i].cardDataID);
									gameBeingEdited.allCardsData.Add(cardDataListBeingImported[i]);
								}
								EditorUtility.ClearProgressBar();
								importedCard = cardDataListBeingImported[0];
							}
						}
						if (importedCard != null && (gameBeingEdited.cardFieldDefinitions == null || gameBeingEdited.cardFieldDefinitions.Count == 0))
							OverwriteFieldDefinitionsFromCard(importedCard);
					}
					break;
			}

		}

		void ConformCardFieldsWithDefinitions (CardData card)
		{
			if (CardHasUniformFields(card))
				return;

			List<CardField> tempList = new List<CardField>();
			for (int j = 0; j < gameBeingEdited.cardFieldDefinitions.Count; j++)
			{
				tempList.Add(new CardField(gameBeingEdited.cardFieldDefinitions[j]));
			}

			for (int j = 0; j < card.fields.Count; j++)
			{
				for (int k = 0; k < tempList.Count; k++)
				{
					if (card.fields[j].fieldName == tempList[k].fieldName && card.fields[j].dataType == tempList[k].dataType)
					{
						tempList[k].stringValue = card.fields[j].stringValue;
						tempList[k].imageValue = card.fields[j].imageValue;
						tempList[k].numValue = card.fields[j].numValue;
						tempList[k].hideOption = card.fields[j].hideOption;
					}
				}
			}
			card.fields = tempList;
		}

		void OverwriteFieldDefinitionsFromCard (CardData card)
		{
			gameBeingEdited.cardFieldDefinitions = new List<CardField>();
			for (int i = 0; i < card.fields.Count; i++)
			{
				gameBeingEdited.cardFieldDefinitions.Add(new CardField(card.fields[i]));
			}
		}

		void ShowEditableCardName (CardData card, bool editableFields)
		{
			// ---- Card data ID name ----
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
			if (editableFields) EditorGUILayout.LabelField("Card Data ID", GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
			if (VerifiedDelayedTextField("$cardName" + card.GetInstanceID(), ref card.cardDataID, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields)))
			{
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(card), card.cardDataID);
			}
			EditorGUILayout.EndVertical();
		}

		void ShowEditableTagsAndCardFields (CardData card, bool editableFields)
		{
			// ---- Card Tags  ----
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
			if (editableFields) EditorGUILayout.LabelField("Tags", GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
			card.tags = EditorGUILayout.TextField(card.tags, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
			EditorGUILayout.EndVertical();
			int toBeRemoved = -1;
			for (int i = 0; i < card.fields.Count; i++)
			{
				EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
				if (editableFields)
				{
					if (GUILayout.Button("Remove", GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields)))
					{
						toBeRemoved = i;
					}
					VerifiedDelayedTextField("$cardField" + i, ref card.fields[i].fieldName, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
					card.fields[i].dataType = (CardFieldDataType)EditorGUILayout.EnumPopup(card.fields[i].dataType);
				}
				switch (card.fields[i].dataType)
				{
					case CardFieldDataType.Text:
						card.fields[i].stringValue = EditorGUILayout.TextField(card.fields[i].stringValue, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
						break;
					case CardFieldDataType.Number:
						card.fields[i].numValue = EditorGUILayout.FloatField(card.fields[i].numValue, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
						if (editableFields) card.fields[i].hideOption = (CardFieldHideOption)EditorGUILayout.EnumPopup(card.fields[i].hideOption);
						break;
					case CardFieldDataType.Image:
						card.fields[i].imageValue = (Sprite)EditorGUILayout.ObjectField(card.fields[i].imageValue, typeof(Sprite), false, GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
						break;
				}
				if (toBeRemoved >= 0)
					card.fields.RemoveAt(toBeRemoved);
				EditorGUILayout.EndVertical();
			}
		}

		// ======================================= HELPER METHODS =======================================================

		void CreateAsset (Object asset, string folder, string assetName)
		{
			string path = "Assets/" + folder + "/" + assetName + ".asset";
			if (AssetDatabase.LoadAssetAtPath<Object>(path))
				return;
			CheckOrCreateFolder(folder);
			AssetDatabase.CreateAsset(asset, path);
		}

		void CheckOrCreateFolder (string folderName)
		{
			string[] folders = folderName.Split('/');

			for (int i = 0; i < folders.Length; i++)
			{
				if (folders[i] == "Assets" || string.IsNullOrEmpty(folders[i]))
					continue;

				string parentFolders = "Assets";

				for (int j = 0; j < i; j++)
				{
					if (folders[j] == "Assets" || string.IsNullOrEmpty(folders[i]))
						continue;
					parentFolders = parentFolders + "/" + folders[j];
				}

				if (!AssetDatabase.IsValidFolder(parentFolders + "/" + folders[i]))
					AssetDatabase.CreateFolder(parentFolders, folders[i]);
			}

		}

		void SaveGame (bool auto)
		{
			File.WriteAllText("Assets/" + gameBeingEdited.cardgameID + (auto ? "(autosave)" : "") + ".json", CardGameSerializer.SaveToJson(gameBeingEdited));
		}

		bool CardHasUniformFields (CardData data)
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

		bool IsNameOk (string name)
		{
			for (int i = 0; i < name.Length; i++)
			{
				char c = name[i];
				if (!(char.IsLetterOrDigit(c) || c == '_'))
					return false;
			}
			return true;
		}

		void DrawBoldLine ()
		{
			GUILayout.Space(13);
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Space(13);
			//Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(30));
			//r.height = 4;
			//r.y += 13;
			//r.x -= 2;
			//r.width += 6;
			//EditorGUI.DrawRect(r, boldLineColor);
		}

		void DrawLightLine ()
		{
			//GUILayout.Space(5);
			//EditorGUILayout.LabelField("", GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(1));
			Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(12));
			r.height = 2;
			r.y += 5;
			r.x -= 2;
			r.width += 6;
			EditorGUI.DrawRect(r, lightLineColor);
		}

		bool VerifiedDelayedTextField (string label, ref string fieldVariable, params GUILayoutOption[] options)
		{
			bool changed = false;
			EditorGUI.BeginChangeCheck();
			GUILayout.BeginHorizontal();
			if (label[0] == '$')
				fieldVariable = EditorGUILayout.DelayedTextField(fieldVariable, options);
			else
				fieldVariable = EditorGUILayout.DelayedTextField(label, fieldVariable, options);
			if (EditorGUI.EndChangeCheck())
			{
				changed = true;
				if (!IsNameOk(fieldVariable))
				{
					if (!nameFieldsWithError.Contains(label))
						nameFieldsWithError.Add(label);
				}
				else
				{
					nameFieldsWithError.Remove(label);
				}
			}
			if (nameFieldsWithError.Contains(label))
			{
				EditorGUILayout.LabelField(nameErrorContent, errorStyle, GUILayout.MaxWidth(30));
			}
			GUILayout.EndHorizontal();
			return changed;
		}

		void TriggerEnums (ref string field)
		{
			if (string.IsNullOrEmpty(field)) field = "";
			//modifiers[i].trigger = EditorGUILayout.TextArea(modifiers[i].trigger);
			EditorGUILayout.BeginHorizontal();
			bool changed = false;
			string[] tags = field.Split(';');
			for (int i = 0; i < tags.Length; i++)
			{
				int oldSelected = triggerTags.IndexOf(tags[i]);
				int newSelected = EditorGUILayout.Popup(oldSelected, triggerTags.ToArray(), GUILayout.MaxWidth(150));
				if (GUILayout.Button("", customSkin.button, GUILayout.Width(15), GUILayout.Height(15)))
				{
					changed = true;
					tags[i] = "";
				}
				if (newSelected != oldSelected)
				{
					changed = true;
					tags[i] = triggerTags[newSelected];
				}
			}
			if (changed)
			{
				field = StringUtility.Concatenate(tags, ";");
			}

			if (GUILayout.Button("+", GUILayout.Width(15), GUILayout.Height(15)))
			{
				field = field + ";" + triggerTags[0];
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}
