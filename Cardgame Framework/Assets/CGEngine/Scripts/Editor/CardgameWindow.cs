using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditorInternal;

namespace CardGameFramework
{
	public class CardgameWindow : EditorWindow
	{
		public float autoSaveTime = 120f;

		List<CardGameData> gameDataList;
		CardGameData gameBeingEdited;
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
		GUIStyle errorStyle;
		GUIContent nameErrorContent = new GUIContent("Error!", "Name must contain only letters, numbers, or _ (underscore)");
		GUIContent variableDuplicateErrorContent = new GUIContent("Duplicate variable name", "This variable name is already in use");
		List<string> triggerTags;
		//string[] systemVariables;
		//List<string> customVariables;
		//List<string> fieldNames;
		//List<string> zoneTags;
		//List<string> cardTags;
		//List<string> turnPhases;
		//List<string> actionNames;
		//List<string> messageNames;
		//List<string> bracketSymbols;
		//List<string> logicSymbols;
		//List<string> mathSymbols;
		//List<string> comparisonSymbols;
		//string[] symbols = new string[] { "(", ")", "&", "|", ",", ";", "+", "-", "*", "/", "%", "^", "=", "!=", ">", "<", ">=", "<=", "=>" };
		//Dictionary<RuleData, RuleDataEditorInfo> ruleifierInfoDict;

		void OnEnable ()
		{
			// ---- Expand dictionary initialization ----
			if (foldoutDictionary == null)
			{
				foldoutDictionary = new Dictionary<object, bool>();
			}

			customSkin = (GUISkin)Resources.Load("CGEngineSkin");
			errorStyle = new GUIStyle();
			errorStyle.normal.textColor = Color.red;
			lightLineColor = new Color(0.6f, 0.6f, 0.6f, 1f);
			triggerTags = new List<string>();
			triggerTags.AddRange(Enum.GetNames(typeof(TriggerLabel)));
			//systemVariables = (string[])CGEngine.systemVariableNames.Clone();
			//bracketSymbols = new List<string>();
			//bracketSymbols.Add("");
			//bracketSymbols.Add("(");
			//bracketSymbols.Add(")");
			//logicSymbols = new List<string>();
			//logicSymbols.Add("");
			//logicSymbols.AddRange(StringUtility.logicOperators);
			//mathSymbols = new List<string>();
			//mathSymbols.Add("");
			//mathSymbols.AddRange(StringUtility.mathOperators);
			//comparisonSymbols = new List<string>();
			//comparisonSymbols.Add("");
			//comparisonSymbols.AddRange(StringUtility.comparisonOperators);

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
			if (gameBeingEdited != null && EditorApplication.timeSinceStartup - lastSaveTime >= 60)
			{
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

		#region ======================================= ON GUI =======================================================

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
						//GetGameNamesForEditing();
						//ruleifierClones = GetRuleClones(gameBeingEdited);
						//ruleifierInfoDict = GetRuleInfo(gameBeingEdited);
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
				if (markedForDeletion == gameBeingEdited) gameBeingEdited = null;
				gameDataList.Remove(markedForDeletion);
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(markedForDeletion));
				markedForDeletion = null;
			}
			EditorGUILayout.EndScrollView();
		}

		#endregion

		#region ======================================= CARD GAMES =======================================================

		void DisplayCardGameData (CardGameData data)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.BeginVertical();

			//Game name
			//EditorGUILayout.LabelField(data.cardgameID, EditorStyles.boldLabel);
			if (VerifiedDelayedTextField("Game Name", ref data.cardgameID, GUILayout.MaxWidth(400)))
			{
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(data), data.cardgameID);
			}

			
				//custom variables
				EditorGUILayout.LabelField("Game Custom Variables");
				if (data.gameVariableNames == null)
				{
					data.gameVariableNames = new List<string>();
					data.gameVariableValues = new List<string>();
				}
				int varFieldToDelete = -1;
				for (int j = 0; j < data.gameVariableNames.Count; j++)
				{
					EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(430));
					string label = "$customGameVariable" + j;
					string tempString = data.gameVariableNames[j];
					if (VerifiedDelayedTextField(label, ref tempString, GUILayout.MaxWidth(200)))
					{
						data.gameVariableNames[j] = tempString;
					}
					data.gameVariableValues[j] = EditorGUILayout.DelayedTextField(data.gameVariableValues[j], GUILayout.MaxWidth(200));

					if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(18)))
					{
						varFieldToDelete = j;
					}
					EditorGUILayout.EndHorizontal();
				}
				if (varFieldToDelete >= 0)
				{
					nameFieldsWithError.Remove("$customGameVariable" + varFieldToDelete);
					data.gameVariableNames.RemoveAt(varFieldToDelete);
					data.gameVariableValues.RemoveAt(varFieldToDelete);
				}

				if (GUILayout.Button("Create New Variable", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
				{
					data.gameVariableNames.Add("");
					data.gameVariableValues.Add("");
				}
				DrawLightLine();

				//Rulesets
				EditorGUILayout.LabelField("Rulesets");
				if (data.rulesets == null)
					data.rulesets = new List<Ruleset>();
				DisplayRulesets(data.rulesets);
				DrawLightLine();

			//Cardsets
			EditorGUILayout.LabelField("Cardsets");
			if (data.cardsets == null)
				data.cardsets = new List<Cardset>();
			DisplayCardsets(data.cardsets);
			
			EditorGUILayout.EndVertical();
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(data);
		}

		#endregion

		#region ======================================= CARD FIELDS =======================================================

		void DisplayCardFieldDefinitions (Cardset cardset)
		{
			CardField toBeDeleted = null;
			List<CardField> fields = cardset.cardFieldDefinitions;

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
					if (cardset.cardsData != null)
					{
						for (int k = 0; k < cardset.cardsData.Count; k++)
						{
							for (int j = 0; j < cardset.cardsData[k].fields.Count; j++)
							{
								if (cardset.cardsData[k].fields[j].fieldName == oldName)
								{
									cardset.cardsData[k].fields[j].fieldName = fields[i].fieldName;
								}
							}
						}
					}
				}
				CardFieldDataType newDataType = (CardFieldDataType)EditorGUILayout.EnumPopup(fields[i].dataType);
				if (newDataType != fields[i].dataType)
				{
					fields[i].dataType = newDataType;
					if (cardset.cardsData != null)
					{
						for (int k = 0; k < cardset.cardsData.Count; k++)
						{
							for (int j = 0; j < cardset.cardsData[k].fields.Count; j++)
							{
								if (cardset.cardsData[k].fields[j].fieldName == fields[i].fieldName)
								{
									cardset.cardsData[k].fields[j].dataType = newDataType;
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
						if (cardset.cardsData != null)
						{
							for (int k = 0; k < cardset.cardsData.Count; k++)
							{
								for (int j = 0; j < cardset.cardsData[k].fields.Count; j++)
								{
									if (cardset.cardsData[k].fields[j].fieldName == fields[i].fieldName)
									{
										cardset.cardsData[k].fields[j].hideOption = newHideOption;
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
				if (cardset.cardsData != null)
				{
					for (int k = 0; k < cardset.cardsData.Count; k++)
					{
						cardset.cardsData[k].fields.Add(new CardField());
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
							cardset.cardFieldDefinitions = new List<CardField>();
							for (int i = 0; i < cardToCopyFields.fields.Count; i++)
							{
								CardField newField = new CardField();
								newField.fieldName = cardToCopyFields.fields[i].fieldName;
								newField.dataType = cardToCopyFields.fields[i].dataType;
								newField.hideOption = cardToCopyFields.fields[i].hideOption;
								cardset.cardFieldDefinitions.Add(newField);
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
				if (cardset.cardsData != null)
				{
					for (int i = 0; i < cardset.cardsData.Count; i++)
					{
						for (int j = 0; j < cardset.cardsData[i].fields.Count; j++)
						{
							if (cardset.cardsData[i].fields[j].fieldName == toBeDeleted.fieldName)
							{
								cardset.cardsData[i].fields.RemoveAt(j);
								j--;
							}
						}
					}
				}
			}
		}

		#endregion

		#region ======================================= RULESETS =======================================================

		void DisplayRulesets (List<Ruleset> rulesets)
		{
			Ruleset toBeDeleted = null;

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
				EditorGUILayout.LabelField("Ruleset Custom Variables");
				if (rulesets[i].rulesetVariableNames == null)
				{
					rulesets[i].rulesetVariableNames = new List<string>();
					rulesets[i].rulesetVariableValues = new List<string>();
				}

				GUILayout.BeginHorizontal();
				GUILayout.Space(20);
				GUILayout.BeginVertical();
				List<string> varNames = rulesets[i].rulesetVariableNames;
				List<string> varValues = rulesets[i].rulesetVariableValues;
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
					rulesets[i].rulesetVariableNames.Add("");
					rulesets[i].rulesetVariableValues.Add("");
				}
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				DrawLightLine();
				//Match Rules
				EditorGUILayout.LabelField("Match Rules");
				GUILayout.BeginHorizontal();
				GUILayout.Space(20);
				GUILayout.BeginVertical();
				if (rulesets[i].matchRules == null)
					rulesets[i].matchRules = new List<RuleData>();
				DisplayRules(rulesets[i].matchRules, "Rule");
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();

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
		}

		#endregion

		#region ======================================= RULES =======================================================

		void DisplayRules (List<RuleData> rules, string prefix)
		{
			RuleData toBeDeleted = null;
			RuleData moveUp = null;
			RuleData moveDown = null;

			for (int i = 0; i < rules.Count; i++)
			{
				if (rules[i] == null)
				{
					rules.RemoveAt(i);
					i--;
					continue;
				}

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));

				//if (!foldoutDictionary.ContainsKey(ruleifiers[i]))
				//	foldoutDictionary.Add(ruleifiers[i], false);

				//if (foldoutDictionary[ruleifiers[i]])
				//{
				//if (GUILayout.Button(" ▼", GUILayout.Width(20), GUILayout.Height(20)))
				//{
				//	foldoutDictionary[ruleifiers[i]] = false;
				//}
				EditorGUILayout.BeginVertical(GUILayout.Width(20));
				if (GUILayout.Button(" ↑ ", GUILayout.Width(20), GUILayout.Height(20)))
				{
					//Move Up
					moveUp = rules[i];
				}
				if (GUILayout.Button(" ↓ ", GUILayout.Width(20), GUILayout.Height(20)))
				{
					//Move Down
					moveDown = rules[i];
				}
				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical();
				// ---- Rule Name ----
				VerifiedDelayedTextField("Rule Name", ref rules[i].ruleID);
				// ---- Tags
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Tags");
				rules[i].tags = EditorGUILayout.TextArea(rules[i].tags);
				EditorGUILayout.EndHorizontal();

				// ----- Triggers
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Trigger");
				TriggerEnums(ref rules[i].trigger);
				EditorGUILayout.EndHorizontal();
				// ---- Condition
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Condition");
				rules[i].condition = EditorGUILayout.TextArea(rules[i].condition);
				EditorGUILayout.EndHorizontal();
				// ---- Commands
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Commands");
				rules[i].commands = EditorGUILayout.TextArea(rules[i].commands);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();

				if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
				{
					toBeDeleted = rules[i];
				}

				EditorGUILayout.EndHorizontal();

				GUILayout.Space(5);
				if (i < rules.Count - 1)
				{
					DrawLightLine();
					GUILayout.Space(5);
				}
			}


			if (GUILayout.Button("Create New " + prefix, GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
			{
				RuleData newRule = new RuleData();
				rules.Add(newRule);
				newRule.ruleID = "New" + prefix;
			}
			GUILayout.Space(15);

			if (toBeDeleted != null)
			{
				rules.Remove(toBeDeleted);
				//foldoutDictionary.Remove(toBeDeleted);
			}

			if (moveUp != null)
			{
				int index = rules.IndexOf(moveUp);
				if (index > 0)
				{
					rules.Remove(moveUp);
					index--;
					rules.Insert(index, moveUp);
				}
			}

			if (moveDown != null)
			{
				int index = rules.IndexOf(moveDown);
				if (index < rules.Count - 1)
				{
					rules.Remove(moveDown);
					index++;
					rules.Insert(index, moveDown);
				}
			}
		}

		void DisplayEditableStringArray (ref string[] strArray)
		{
			EditorGUILayout.BeginHorizontal();
			for (int i = 0; i < strArray.Length; i++)
			{
				string str = strArray[i];
				float width = GUI.skin.label.CalcSize(new GUIContent(str)).x + 5;
				//float width = str.Length * 7 + (str.Length * 7 - 0.75f * str.Length * str.Length / 2);
				EditorGUILayout.TextField(str, GUILayout.Width(width));
			}
			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region ======================================= CARDSETS =======================================================

		void DisplayCardsets (List<Cardset> cardsets)
		{
			Cardset toBeDeleted = null;

			for (int i = 0; i < cardsets.Count; i++)
			{
				Cardset cardset = cardsets[i];

				if (cardset == null)
				{
					cardsets.RemoveAt(i);
					i--;
					continue;
				}

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField((i + 1) + ".", GUILayout.MaxWidth(20));
				EditorGUILayout.BeginVertical();
				//Cardset name
				VerifiedDelayedTextField("Cardset Name", ref cardsets[i].cardsetID);
				//Cardset description
				cardsets[i].description = EditorGUILayout.TextField("Description", cardsets[i].description);
				//Card template
				cardset.cardTemplate = (GameObject)EditorGUILayout.ObjectField("Card Template", cardset.cardTemplate, typeof(GameObject), false, GUILayout.MaxWidth(400));
				if (cardset.cardTemplate)
				{
					DrawLightLine();
					//Card field definitions
					EditorGUILayout.LabelField("Card Fields Definition");
					if (cardset.cardFieldDefinitions == null)
						cardset.cardFieldDefinitions = new List<CardField>();
					DisplayCardFieldDefinitions(cardset);
					DrawLightLine();
					//Cards data
					EditorGUILayout.LabelField("Cards Data");
					if (cardset.cardsData == null)
						cardset.cardsData = new List<CardData>();
					DisplayCardDataList(cardset);
					DrawLightLine();
				}
				else
				{
					if (GUILayout.Button("Create Basic Card Template", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
					{
						CheckOrCreateFolder("Resources");
						if (AssetDatabase.CopyAsset("Assets/CGEngine/Resources/BasicCardTemplate.prefab", "Assets/" + gameBeingEdited.cardgameID + "CardTemplate.prefab"))
						{
							cardset.cardTemplate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + gameBeingEdited.cardgameID + "CardTemplate.prefab");
						}
						else
						{
							Debug.LogError("The Basic Card Template at CGEngine/Resources coudn't be loaded. You can reimport the package to recover it.");
						}
					}
					DrawLightLine();
				}
				
				EditorGUILayout.EndVertical();
				if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
				{
					toBeDeleted = cardsets[i];
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Create New Cardset", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
			{
				Cardset newCardset = new Cardset();
				cardsets.Add(newCardset);
				newCardset.cardsetID = "New Cardset";
			}

			if (toBeDeleted != null)
			{
				cardsets.Remove(toBeDeleted);
			}
		}

		void DisplayCardDataList (Cardset cardset)
		{
			if (cardset.cardFieldDefinitions != null && cardset.cardFieldDefinitions.Count > 0)
			{
				List<CardData> cards = cardset.cardsData;
				CardData toBeDeleted = null;

				minHorizontalWidth = buttonWidth * 2 + minWidthFields * 2 + minWidthFields * cardset.cardFieldDefinitions.Count;
				maxHorizontalWidth = buttonWidth * 2 + maxWidthFields * 2 + maxWidthFields * cardset.cardFieldDefinitions.Count;

				GUILayout.Space(10);
				//BUTTONS
				EditorGUILayout.BeginHorizontal();
				// ---- Create new Card ---- 
				if (GUILayout.Button("Create New Card", GUILayout.MaxWidth(150), GUILayout.MaxHeight(18)))
				{
					CardData newCard = CreateInstance<CardData>();
					newCard.fields = new List<CardField>();
					for (int i = 0; i < cardset.cardFieldDefinitions.Count; i++)
					{
						newCard.fields.Add(new CardField(cardset.cardFieldDefinitions[i]));
					}
					newCard.cardRules = new List<RuleData>();
					cards.Add(newCard);
					newCard.cardDataID = "New Card";
				}

				DisplayCardImporterField(cardset);

				if (GUILayout.Button("Instantiate Cards in Scene", GUILayout.MaxWidth(170), GUILayout.MaxHeight(18)))
				{
					CGEngine.CreateCards(cardset.cardTemplate, cards, Vector3.zero);
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
				for (int i = 0; i < cardset.cardFieldDefinitions.Count; i++)
				{
					EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidthFields), GUILayout.MaxWidth(maxWidthFields));
					EditorGUILayout.LabelField("     " + cardset.cardFieldDefinitions[i].fieldName);
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
					if (!CardHasUniformFields(cardset.cardFieldDefinitions, cards[i]))
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
							ConformCardFieldsWithDefinitions(cardset.cardFieldDefinitions, cards[i]);
						}
						if (GUILayout.Button("Set All Field Definitions to This", GUILayout.Width(200)))
						{
							OverwriteFieldDefinitionsFromCard(cardset, cards[i]);
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

						//Card Rules
						if (foldoutDictionary[cards[i]])
						{
							EditorGUILayout.BeginVertical();
							DisplayRules(cards[i].cardRules, "CardRule");
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
				DisplayCardImporterField(cardset);
			}
		}

		/// <summary>
		/// Shows a field for drag and drop of cards to be imported.
		/// </summary>
		void DisplayCardImporterField (Cardset cardset)
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
								cardset.cardsData.Add(importedCard);
							}
							else if (draggedObject.GetType() == typeof(TextAsset))
							{
								sourceImagesFolder = EditorUtility.OpenFolderPanel("Select source images folder for the imported cards", Application.dataPath, "");
								cardDataListBeingImported = CardGameSerializer.RecoverListOfCardsFromJson((TextAsset)draggedObject, sourceImagesFolder);

								for (int i = 0; i < cardDataListBeingImported.Count; i++)
								{
									EditorUtility.DisplayProgressBar("Importing Cards", "Importing: " + cardDataListBeingImported[i].cardDataID, (float)i / cardDataListBeingImported.Count);
									CreateAsset(cardDataListBeingImported[i], "Data/Cards", cardDataListBeingImported[i].cardDataID);
									cardset.cardsData.Add(cardDataListBeingImported[i]);
								}
								EditorUtility.ClearProgressBar();
								importedCard = cardDataListBeingImported[0];
							}
						}
						if (importedCard != null && (cardset.cardFieldDefinitions == null || cardset.cardFieldDefinitions.Count == 0))
							OverwriteFieldDefinitionsFromCard(cardset, importedCard);

						evt.Use();
					}
					break;
			}

		}

		void ConformCardFieldsWithDefinitions (List<CardField> cardFieldDefinitions, CardData card)
		{
			if (CardHasUniformFields(cardFieldDefinitions, card))
				return;

			List<CardField> tempList = new List<CardField>();
			for (int j = 0; j < cardFieldDefinitions.Count; j++)
			{
				tempList.Add(new CardField(cardFieldDefinitions[j]));
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

		void OverwriteFieldDefinitionsFromCard (Cardset cardset, CardData card)
		{
			cardset.cardFieldDefinitions = new List<CardField>();
			for (int i = 0; i < card.fields.Count; i++)
			{
				cardset.cardFieldDefinitions.Add(new CardField(card.fields[i]));
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

		#endregion

		#region ======================================= HELPER METHODS =======================================================

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
			EditorUtility.SetDirty(gameBeingEdited);
			if (!auto)
				File.WriteAllText("Assets/" + gameBeingEdited.cardgameID + ".json", CardGameSerializer.SaveToJson(gameBeingEdited));
		}

		bool CardHasUniformFields (List<CardField> cardFieldDefinitions, CardData data)
		{
			if (data == null || data.fields == null || data.fields.Count != cardFieldDefinitions.Count)
				return false;

			for (int i = 0; i < data.fields.Count; i++)
			{
				if (data.fields[i].fieldName != cardFieldDefinitions[i].fieldName ||
					data.fields[i].dataType != cardFieldDefinitions[i].dataType ||
					data.fields[i].hideOption != cardFieldDefinitions[i].hideOption)
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
		}

		void DrawLightLine ()
		{
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
			//ruleifiers[i].trigger = EditorGUILayout.TextArea(ruleifiers[i].trigger);
			EditorGUILayout.BeginHorizontal();
			bool changed = false;
			string[] tags = field.Split(';');
			for (int i = 0; i < tags.Length; i++)
			{
				int oldSelected = triggerTags.IndexOf(tags[i]);
				int newSelected = EditorGUILayout.Popup(oldSelected, triggerTags.ToArray(), GUILayout.MaxWidth(150));
				if (GUILayout.Button("X", GUILayout.Width(16), GUILayout.Height(15)))
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

			if (GUILayout.Button(" +", GUILayout.Width(18), GUILayout.Height(15)))
			{
				field = field + ";" + triggerTags[0];
			}
			EditorGUILayout.EndHorizontal();
		}
		/*
		void GetGameNamesForEditing ()
		{
			if (!gameBeingEdited)
				return;

			//custom variables
			customVariables = new List<string>();
			AddUnique(customVariables, gameBeingEdited.customVariableNames);
			for (int i = 0; i < gameBeingEdited.rules.Count; i++)
			{
				AddUnique(customVariables, gameBeingEdited.rules[i].customVariableNames);
			}

			//field names
			fieldNames = new List<string>();
			for (int i = 0; i < gameBeingEdited.cardFieldDefinitions.Count; i++)
			{
				fieldNames.Add(gameBeingEdited.cardFieldDefinitions[i].fieldName);
			}

			//Zone tags
			zoneTags = new List<string>();
			Zone[] zones = FindObjectsOfType<Zone>();
			if (zones != null)
			{
				for (int i = 0; i < zones.Length; i++)
				{
					AddUnique(zoneTags, zones[i].zoneTags.Split(','));
				}
			}

			//Card tags
			cardTags = new List<string>();
			for (int i = 0; i < gameBeingEdited.allCardsData.Count; i++)
			{
				AddUnique(cardTags, gameBeingEdited.allCardsData[i].tags.Split(','));
			}

			//Turn phases
			turnPhases = new List<string>();
			for (int i = 0; i < gameBeingEdited.rules.Count; i++)
			{
				AddUnique(turnPhases, gameBeingEdited.rules[i].turnStructure.Split(','));
			}

			//Action names
			actionNames = new List<string>();
			//message names
			messageNames = new List<string>();

			for (int i = 0; i < gameBeingEdited.rules.Count; i++)
			{
				Ruleset ruleset = gameBeingEdited.rules[i];
				for (int j = 0; j < ruleset.matchRules.Count; j++)
				{
					RuleData rule = ruleset.matchRules[j];
					AddUniqueNamesFromCondition(rule.condition);
					AddUniqueNamesFromCommand(rule.commands);
				}
			}

			Debug.Log(StringUtility.PrintStringList(triggerTags));
			Debug.Log(StringUtility.PrintStringArray(systemVariables));
			Debug.Log(StringUtility.PrintStringList(customVariables));
			Debug.Log(StringUtility.PrintStringList(fieldNames));
			Debug.Log(StringUtility.PrintStringList(zoneTags));
			Debug.Log(StringUtility.PrintStringList(cardTags));
			Debug.Log(StringUtility.PrintStringList(turnPhases));
			Debug.Log(StringUtility.PrintStringList(actionNames));
			Debug.Log(StringUtility.PrintStringList(messageNames));
		}

		void AddUnique (List<string> list, string name)
		{
			if (!list.Contains(name))
				list.Add(name);
		}

		void AddUnique (List<string> list, List<string> names)
		{
			for (int i = 0; i < names.Count; i++)
			{
				string newName = names[i];
				if (!list.Contains(newName))
					list.Add(newName);
			}
		}

		void AddUnique (List<string> list, string[] names)
		{
			for (int i = 0; i < names.Length; i++)
			{
				string newName = names[i];
				if (!list.Contains(newName))
					list.Add(newName);
			}
		}

		void AddUniqueNamesFromCondition (string condition)
		{
			AddNameAfterLabel(actionNames, condition, "actionName", '&', '|');
			AddNameAfterLabel(messageNames, condition, "message", '&', '|');
		}

		void AddUniqueNamesFromCommand (string command)
		{
			AddNameAfterLabel(turnPhases, command, "StartSubphaseLoop", ')');
			AddNameAfterLabel(actionNames, command, "UseAction", ')');
			AddNameAfterLabel(messageNames, command, "SendMessage", ')');
		}

		void AddNameAfterLabel (List<string> list, string str, string label, params char[] endChars)
		{
			if (str.Contains(label))
			{
				str = StringUtility.GetCleanStringForInstructions(str);
				double emergencyBreakTimer = EditorApplication.timeSinceStartup;
				while (EditorApplication.timeSinceStartup - emergencyBreakTimer < 2)
				{
					int index = str.IndexOf(label);
					if (index == -1)
						break;
					int startIndex = index + label.Length + 1;
					if (str[startIndex] == '=' || str[startIndex] == '>')
						startIndex++;
					int endIndex = str.IndexOfAny(endChars, startIndex);
					if (endIndex == -1)
						endIndex = str.Length;
					string sub = str.Substring(startIndex, endIndex - startIndex);
					string[] subSplit = sub.Split(',');
					for (int i = 0; i < subSplit.Length; i++)
					{
						AddUnique(list, subSplit[i]);
					}
					str = str.Substring(startIndex);
				}
			}
		}

		Dictionary<RuleData, RuleDataEditorInfo> GetRuleInfo (CardGameData game)
		{
			if (!game)
				return null;

			Dictionary<RuleData, RuleDataEditorInfo> ruleifierInfos = new Dictionary<RuleData, RuleDataEditorInfo>();
			for (int i = 0; i < gameBeingEdited.rules.Count; i++)
			{
				Ruleset ruleset = gameBeingEdited.rules[i];
				for (int j = 0; j < ruleset.matchRules.Count; j++)
				{
					RuleData rule = ruleset.matchRules[j];
					//RuleData newRule = new RuleData();
					//newRule.ruleID = rule.ruleifierID;
					//newRule.tags = rule.tags;
					//newRule.trigger = rule.trigger;
					//newRule.condition = rule.condition;//.Replace("&", "&" + Environment.NewLine).Replace("|", "|" + Environment.NewLine).Replace("&"+ Environment.NewLine + "(", "&" + Environment.NewLine + "(    ").Replace("|" + Environment.NewLine + "(", "|" + Environment.NewLine + "(    ");
					//newRule.commands = rule.commands;//.Replace(";", ";"+Environment.NewLine);
					ruleifierInfos.Add(rule, new RuleDataEditorInfo(rule));
				}
			}
			return ruleifierInfos;
		}
		*/

		#endregion



	}

	class RuleDataEditorInfo
	{
		public string[] conditionInfo;
		public string[] commandsInfo;

		public RuleDataEditorInfo (RuleData rule)
		{
			conditionInfo = StringUtility.GetSplitStringArray(rule.condition);
			commandsInfo = StringUtility.GetSplitStringArray(rule.commands);
		}
	}
}
