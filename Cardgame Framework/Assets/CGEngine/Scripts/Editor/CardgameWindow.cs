using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Object = UnityEngine.Object;
using System;
using System.IO;

namespace CardGameFramework
{
	public class CardGameWindow : EditorWindow
	{
		const float buttonWidth = 25;
		List<CardGameData> gameDataList;
		List<Cardset> cardsetList;
		CardGameData gameBeingEdited;
		CardGameData cardGameMarkedForDeletion;
		CardData cardToCopyFields;
		Cardset cardsetBeingEdited;
		Cardset cardsetMarkedForDeletion;
		Ruleset rulesetBeingEdited;
		ReorderableList currentRuleList;
		Vector2 windowScrollPos;
		Vector2 cardsScrollPos;
		int view;
		string[] viewNames = new string[] { "Card Game Editor", "Cardset Editor" };
		bool copyingFields;
		bool creatingNewGame;
		bool creatingNewCardset;
		string newGameName;
		string newCardsetName;
		bool isGoodNewGameName;
		bool importingNewGame;
		TextAsset gameImportingFile;
		bool importingAListOfCards;
		TextAsset cardImportingFile;
		double lastSaveTime;
		Dictionary<object, bool> foldoutDictionary;
		HashSet<string> nameFieldsWithError = new HashSet<string>();
		List<float> fieldColumnWidthList = new List<float>();
		int resizeIndex = -1;
		Color lightLineColor;
		GUIStyle errorStyle;
		GUIContent nameErrorContent = new GUIContent("Error!", "Name must contain only letters, numbers, or _ (underscore)");
		GUIContent variableDuplicateErrorContent = new GUIContent("Duplicate variable name", "This variable name is already in use");
		List<string> triggerTags;
		CommandSequence testCommand;
		CommandSequence testCondition;
		int reorderIndex = -1;
		Rect windowRect;

		void OnEnable ()
		{
			// ---- Expand dictionary initialization ----
			if (foldoutDictionary == null)
			{
				foldoutDictionary = new Dictionary<object, bool>();
			}

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
				if (gameDataList == null)
					gameDataList = new List<CardGameData>();
				foreach (string item in foundAssets)
				{
					CardGameData data = AssetDatabase.LoadAssetAtPath<CardGameData>(AssetDatabase.GUIDToAssetPath(item));
					if (!gameDataList.Contains(data))
						gameDataList.Add(data);
				}
			}

			foundAssets = AssetDatabase.FindAssets("t:Cardset");
			if (foundAssets != null)
			{
				if (cardsetList == null)
					cardsetList = new List<Cardset>();
				foreach (string item in foundAssets)
				{
					Cardset data = AssetDatabase.LoadAssetAtPath<Cardset>(AssetDatabase.GUIDToAssetPath(item));
					if (!cardsetList.Contains(data))
						cardsetList.Add(data);
				}
			}

		}
		private void OnDestroy ()
		{
			if (gameBeingEdited)
				SaveGame(true);
			if (cardsetBeingEdited)
				SaveCardset(true);
		}
		[MenuItem("Window/Cardgame Editor")]
		public static void ShowWindow ()
		{
			GetWindow<CardGameWindow>("CGEngine Editor");
		}
		void OnGUI ()
		{
			windowScrollPos = EditorGUILayout.BeginScrollView(windowScrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));
			GUILayout.Space(20);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(EditorGUIUtility.currentViewWidth / 2f - 200);
			view = GUILayout.Toolbar(view, viewNames, GUILayout.Width(400), GUILayout.Height(25));
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(20);
			if (view == 0)
			{
				#region ========================================================= Card Games On GUI ================================================

				GUILayout.Label("Card Game Definitions", EditorStyles.boldLabel);

				// ---- Clear list if empty ----
				for (int i = gameDataList.Count - 1; i >= 0; i--)
				{
					if (gameDataList[i] == null)
						gameDataList.RemoveAt(i);
				}

				// ---- New game button ----
				EditorGUILayout.BeginHorizontal(GUILayout.Width(500));
				GUILayout.Space(EditorGUIUtility.currentViewWidth / 2f - 250);
				if (!creatingNewGame)
				{
					if (GUILayout.Button("Create New Game", GUILayout.Width(250)))
					{
						newGameName = "NewGameName";
						creatingNewGame = true;
					}
				}
				else
				{
					VerifiedDelayedTextField("$newGameName", ref newGameName, GUILayout.Width(150), GUILayout.ExpandWidth(false));

					if (GUILayout.Button("Create", GUILayout.Width(50)))
					{
						if (!nameFieldsWithError.Contains("$newGameName"))
						{
							CardGameData gameData = CreateInstance<CardGameData>();
							gameData.cardgameID = newGameName;
							CreateAsset(gameData, "Data/CardGames", newGameName);
							gameDataList.Add(gameData);
							gameBeingEdited = gameData;
							StringPopupBuilder.instance.contextGame = gameBeingEdited;
							creatingNewGame = false;
						}
					}
					if (GUILayout.Button("Cancel", GUILayout.Width(50)))
					{
						creatingNewGame = false;
					}
				}

				if (!importingNewGame)
				{
					if (GUILayout.Button("Import Game", GUILayout.Width(250)))
					{
						importingNewGame = true;
					}
				}
				else
				{
					gameImportingFile = (TextAsset)EditorGUILayout.ObjectField(gameImportingFile, typeof(TextAsset), false, GUILayout.Width(150));
					if (GUILayout.Button("Import", GUILayout.Width(50)))
					{
						CardGameData importedGame = CardGameSerializer.RecoverCardGameFromJson(File.ReadAllText(AssetDatabase.GetAssetPath(gameImportingFile)));
						gameDataList.Add(importedGame);
						CreateAsset(importedGame, "Data/CardGames", importedGame.cardgameID);
						importingNewGame = false;
						gameImportingFile = null;
					}
					if (GUILayout.Button("Cancel", GUILayout.Width(50)))
					{
						importingNewGame = false;
						gameImportingFile = null;
					}
				}

				EditorGUILayout.EndHorizontal();

				// ---- Display other games and buttons for deleting or editing
				for (int i = 0; i < gameDataList.Count; i++)
				{
					DrawBoldLine();
					EditorGUILayout.BeginHorizontal();

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
							StringPopupBuilder.instance.contextGame = null;
						}
						GUILayout.Space(15);
						// ---- Game Name Bold ----
						EditorGUILayout.LabelField(gameDataList[i].cardgameID, EditorStyles.boldLabel);
						GUILayout.Space(15);
						// ---- Delete game button ----
						if (GUILayout.Button("Delete", GUILayout.Width(80), GUILayout.Height(18)))
						{
							cardGameMarkedForDeletion = gameBeingEdited;
							gameBeingEdited = null;
							StringPopupBuilder.instance.contextGame = null;
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
							StringPopupBuilder.instance.contextGame = gameBeingEdited;
							//GetGameNamesForEditing();
							break;
						}
						GUILayout.Space(15);
						// ---- Game Name Bold ----
						EditorGUILayout.LabelField(gameDataList[i].cardgameID, EditorStyles.boldLabel);
						GUILayout.Space(15);
						// ---- Delete game button ----
						if (GUILayout.Button("Delete", GUILayout.Width(80), GUILayout.Height(18)))
						{
							cardGameMarkedForDeletion = gameDataList[i];
						}
					}
					EditorGUILayout.EndHorizontal();
				}

				DrawBoldLine();

				// ---- Delete if marked for deletion and clean everything up ----
				if (cardGameMarkedForDeletion)
				{
					if (cardGameMarkedForDeletion == gameBeingEdited) gameBeingEdited = null;
					gameDataList.Remove(cardGameMarkedForDeletion);
					AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(cardGameMarkedForDeletion));
					cardGameMarkedForDeletion = null;
				}
				#endregion
			}
			else
			{
				#region ========================================================= Cardsets On GUI ================================================
				GUILayout.Label("Cardset Definitions", EditorStyles.boldLabel);

				// ---- Clear list if empty ----
				for (int i = cardsetList.Count - 1; i >= 0; i--)
				{
					if (cardsetList[i] == null)
						cardsetList.RemoveAt(i);
				}

				// ---- New cardset button ----
				EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(500));
				GUILayout.Space(EditorGUIUtility.currentViewWidth / 2f - 250);
				if (!creatingNewCardset)
				{
					if (GUILayout.Button("Create New Cardset", GUILayout.Width(250)))
					{
						newCardsetName = "NewCardsetName";
						creatingNewCardset = true;
					}
				}
				else
				{
					VerifiedDelayedTextField("$newCardsetName", ref newCardsetName, GUILayout.Width(150));

					if (GUILayout.Button("Create", GUILayout.Width(50)))
					{
						if (!nameFieldsWithError.Contains("$newCardsetName"))
						{
							Cardset cardset = CreateInstance<Cardset>();
							cardset.cardsetID = newCardsetName;
							CreateAsset(cardset, "Data/Cards", newCardsetName);
							cardsetList.Add(cardset);
							cardsetBeingEdited = cardset;
							SetDefaultColumnSizes(cardsetBeingEdited.cardFieldDefinitions.Count);
							creatingNewCardset = false;
						}
					}
					if (GUILayout.Button("Cancel", GUILayout.Width(50)))
					{
						creatingNewCardset = false;
					}
				}

				// ---- Creation and Import buttons ----
				if (!importingAListOfCards)
				{
					if (GUILayout.Button("Import Cardset", GUILayout.Width(250)))
					{
						importingAListOfCards = true;
					}
				}
				else
				{
					cardImportingFile = (TextAsset)EditorGUILayout.ObjectField(cardImportingFile, typeof(TextAsset), false, GUILayout.Width(150));
					if (GUILayout.Button("Import", GUILayout.Width(50)))
					{
						string sourceImagesFolder = EditorUtility.OpenFolderPanel("Select source images folder for the imported cards", Application.dataPath, "");
						Cardset cardsetBeingImported = CardGameSerializer.RecoverCardsetFromJson(cardImportingFile.text, sourceImagesFolder);

						EditorUtility.DisplayProgressBar("Importing Cards", "Importing: " + cardsetBeingImported.cardsetID, 0);
						if (cardsetBeingImported.cardsData != null)
							for (int i = 0; i < cardsetBeingImported.cardsData.Count; i++)
							{
								CardData cardData = cardsetBeingImported.cardsData[i];
								EditorUtility.DisplayProgressBar("Importing Cards", "Importing: " + cardData.cardDataID, (float)i / cardsetBeingImported.cardsData.Count);
								CreateAsset(cardData, "Data/Cards", cardData.cardDataID);
							}
						CreateAsset(cardsetBeingImported, "Data/Cards", cardsetBeingImported.cardsetID);
						cardsetList.Add(cardsetBeingImported);
						EditorUtility.ClearProgressBar();

						importingAListOfCards = false;
					}
					if (GUILayout.Button("Cancel", GUILayout.Width(50)))
					{
						importingAListOfCards = false;
					}
				}

				EditorGUILayout.EndHorizontal();

				// ---- Display other cardsets and buttons for deleting or editing
				for (int i = 0; i < cardsetList.Count; i++)
				{
					DrawBoldLine();
					EditorGUILayout.BeginHorizontal();

					if (cardsetList[i] == cardsetBeingEdited)
					{
						Undo.RecordObject(cardsetBeingEdited, "CGEngine.Cardset Change");
						// ---- Edit cardset ----
						EditorGUILayout.BeginVertical();
						EditorGUILayout.BeginHorizontal();
						// ---- Save button ----
						if (GUILayout.Button("Save", GUILayout.Width(80), GUILayout.Height(18)))
						{
							SaveCardset(false);
							cardsetBeingEdited = null;
						}
						GUILayout.Space(15);
						// ---- Cardset Name Bold ----
						EditorGUILayout.LabelField(cardsetList[i].cardsetID, EditorStyles.boldLabel);
						GUILayout.Space(15);
						// ---- Delete cardset button ----
						if (GUILayout.Button("Delete", GUILayout.Width(80), GUILayout.Height(18)))
						{
							cardsetMarkedForDeletion = cardsetBeingEdited;
							cardsetBeingEdited = null;
						}

						EditorGUILayout.EndHorizontal();
						GUILayout.Space(15);
						// ---- Cardset being edited ----
						if (cardsetBeingEdited)
							DisplayCardset(cardsetBeingEdited);
						EditorGUILayout.EndVertical();
					}
					else
					{
						// ---- Edit cardset button ----
						if (GUILayout.Button("Edit", GUILayout.Width(80), GUILayout.Height(18)))
						{
							lastSaveTime = EditorApplication.timeSinceStartup;
							cardsetBeingEdited = cardsetList[i];
							SetDefaultColumnSizes(cardsetBeingEdited.cardFieldDefinitions.Count);
							break;
						}
						GUILayout.Space(15);
						// ---- Cardset Name Bold ----
						EditorGUILayout.LabelField(cardsetList[i].cardsetID, EditorStyles.boldLabel);
						GUILayout.Space(15);
						// ---- Delete cardset button ----
						if (GUILayout.Button("Delete", GUILayout.Width(80), GUILayout.Height(18)))
						{
							cardsetMarkedForDeletion = cardsetList[i];
						}
					}
					EditorGUILayout.EndHorizontal();
				}
				DrawBoldLine();

				// ---- Delete if marked for deletion and clean everything up ----
				if (cardsetMarkedForDeletion)
				{
					if (cardsetMarkedForDeletion == cardsetBeingEdited) cardsetBeingEdited = null;
					cardsetList.Remove(cardsetMarkedForDeletion);
					AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(cardsetMarkedForDeletion));
					cardsetMarkedForDeletion = null;
				}
				#endregion
			}
			/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////   DEBUG     ////////////////////////////////////////////////////////////
			if (gameBeingEdited == null)
			{
				if (StringPopupBuilder.instance.contextGame == null)
					StringPopupBuilder.instance.contextGame = gameDataList[0];
				if (testCommand == null)
				{
					testCommand = new CommandSequence(new CommandLabelPopup(),
						new CommandLabelPopup(1), new CommandLabelPopup(2), new CommandLabelPopup(3), new CommandLabelPopup(4), new CommandLabelPopup(5), new CommandLabelPopup(6), new CommandLabelPopup(7),
						new CommandLabelPopup(8), new CommandLabelPopup(9), new CommandLabelPopup(10), new CommandLabelPopup(11), new CommandLabelPopup(12), new CommandLabelPopup(13), new CommandLabelPopup(14));
				}
				testCommand.ShowSequence();
				if (GUILayout.Button("Codify", GUILayout.Width(100)))
				{
					string code = testCommand.CodifySequence();
					Debug.Log(code.Replace(";", Environment.NewLine));
				}
				if (testCondition == null)
				{
					testCondition = new CommandSequence(new FullAndOrPopup().SetPrevious(new ConditionPopupPiece(true)));
				}
				testCondition.ShowSequence();
				if (GUILayout.Button("Codify", GUILayout.Width(100)))
				{
					Debug.Log(testCondition.CodifySequence());
				}
			}

			EditorGUILayout.EndScrollView();
		}
		#region Display Methods ================================================================
		void DisplayCardGameData (CardGameData data)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.BeginVertical();

			//Game name
			if (VerifiedDelayedTextField("Game Name", ref data.cardgameID, GUILayout.MaxWidth(400)))
			{
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(data), data.cardgameID);
			}

			//Custom variables
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

			if (data.cardsets == null)
				data.cardsets = new List<Cardset>();
			int remove = -1;
			for (int i = 0; i < data.cardsets.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				data.cardsets[i] = (Cardset)EditorGUILayout.ObjectField("Cardset", data.cardsets[i], typeof(Cardset), false, GUILayout.Width(600));
				if (GUILayout.Button("X", GUILayout.Width(20)))
					remove = i;
				EditorGUILayout.EndHorizontal();
			}
			if (remove > -1)
				data.cardsets.RemoveAt(remove);
			if (GUILayout.Button("Add cardset", GUILayout.Width(100)))
			{
				data.cardsets.Add(null);
			}

			DrawLightLine();

			//Rulesets
			EditorGUILayout.LabelField("Rulesets");
			if (data.rulesets == null)
				data.rulesets = new List<Ruleset>();
			DisplayRulesets(data.rulesets);
			DrawLightLine();

			EditorGUILayout.EndVertical();
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(data);
		}
		void DisplayCardFieldDefinitions (Cardset cardset)
		{
			int toBeDeleted = -1;
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
					toBeDeleted = i;
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
				fieldColumnWidthList.Add(150f);
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

			if (toBeDeleted >= 0)
			{
				string fieldName = fields[toBeDeleted].fieldName;
				fields.RemoveAt(toBeDeleted);
				fieldColumnWidthList.RemoveAt(toBeDeleted);
				if (cardset.cardsData != null)
				{
					for (int i = 0; i < cardset.cardsData.Count; i++)
					{
						for (int j = 0; j < cardset.cardsData[i].fields.Count; j++)
						{
							if (cardset.cardsData[i].fields[j].fieldName == fieldName)
							{
								cardset.cardsData[i].fields.RemoveAt(j);
								j--;
							}
						}
					}
				}
			}
		}
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
				
				//DisplayHandleRect(rules, i);
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
				EditorGUILayout.TextField(str, GUILayout.Width(width));
			}
			EditorGUILayout.EndHorizontal();
		}
		void DisplayHandleRect (List<RuleData> ruleList, int index)
		{
			Event evt = Event.current;
			Rect handleArea = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.helpBox, GUILayout.ExpandHeight(true));
			GUI.Box(handleArea, "=");
			EditorGUIUtility.AddCursorRect(handleArea, MouseCursor.Pan);
			if (handleArea.Contains(evt.mousePosition))
			{
				if (evt.type == EventType.MouseDown)
					reorderIndex = index;
				else if (evt.type == EventType.MouseUp)
				{
					if (reorderIndex != -1)
					{
						RuleData rule = ruleList[reorderIndex];
						ruleList.Remove(rule);
						ruleList.Insert(index, rule);
						reorderIndex = -1;
						Repaint();
					}
				}
			}
		}
		void DisplayCardset (Cardset cardset)
		{
			EditorGUI.BeginChangeCheck();
			//Cardset name
			VerifiedDelayedTextField("Cardset Name", ref cardset.cardsetID);
			//Cardset description
			cardset.description = EditorGUILayout.TextField("Description", cardset.description);
			//Card template
			EditorGUILayout.BeginHorizontal();
			cardset.cardTemplate = (GameObject)EditorGUILayout.ObjectField("Card Template", cardset.cardTemplate, typeof(GameObject), false, GUILayout.MaxWidth(400));
			if (!cardset.cardTemplate)
			{
				EditorGUILayout.LabelField("A card prefab is missing", errorStyle);
				if (GUILayout.Button("Add a Default Card Prefab", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
				{
					CheckOrCreateFolder("Resources");
					if (AssetDatabase.CopyAsset("Assets/CGEngine/Resources/DefaultCardPrefab.prefab", "Assets/" + gameBeingEdited.cardgameID + "_CardPrefab.prefab"))
					{
						cardset.cardTemplate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + gameBeingEdited.cardgameID + "_CardPrefab.prefab");
					}
					else
					{
						Debug.LogError("The default card prefab at CGEngine/Resources coudn't be loaded. You can reimport the package to recover it.");
					}
				}
			}
			EditorGUILayout.EndHorizontal();
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
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(cardset);
		}
		void DisplayCardDataList (Cardset cardset)
		{
			if (cardset.cardFieldDefinitions != null && cardset.cardFieldDefinitions.Count > 0)
			{
				List<CardData> cards = cardset.cardsData;
				CardData toBeDeleted = null;

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
					newCard.cardDataID = "NewCard";
					CreateAsset(newCard, "Data/Cards", newCard.cardDataID);
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
				EditorGUILayout.BeginHorizontal(GUILayout.Width(GetFieldsSumWidth()));
				// ---- Expand button title ----
				EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
				EditorGUILayout.LabelField(" ", GUILayout.Width(buttonWidth));
				EditorGUILayout.EndVertical();
				// ---- Card data ID name title ----
				EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField("Data Name", GUILayout.Width(fieldColumnWidthList[0] - 8f));
				EditorGUILayout.EndVertical();
				DrawColumnResizingHandle(0);
				// ---- Delete button title ----
				EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));
				EditorGUILayout.LabelField("X  |", GUILayout.Width(buttonWidth));
				EditorGUILayout.EndVertical();
				// ---- Card Tags title  ----
				EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField("Tags", GUILayout.Width(fieldColumnWidthList[1] - 12f));
				EditorGUILayout.EndVertical();
				DrawColumnResizingHandle(1);
				for (int i = 0; i < cardset.cardFieldDefinitions.Count; i++)
				{
					EditorGUILayout.BeginVertical();
					EditorGUILayout.LabelField(cardset.cardFieldDefinitions[i].fieldName, GUILayout.Width(fieldColumnWidthList[i + 2] - 11f - i * 2f));
					EditorGUILayout.EndVertical();
					DrawColumnResizingHandle(i + 2);
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
						EditorGUILayout.BeginHorizontal(EditorStyles.textArea, GUILayout.Width(GetFieldsSumWidth()));
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
						EditorGUILayout.BeginHorizontal(GUILayout.Width(GetFieldsSumWidth()));
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
		#endregion
		#region Helper Methods =================================================================
		float GetFieldsSumWidth ()
		{
			float width = 0;
			for (int i = 0; i < fieldColumnWidthList.Count; i++)
			{
				width += fieldColumnWidthList[i];
			}
			return width;
		}
		void SetDefaultColumnSizes (int cardFieldCount)
		{
			fieldColumnWidthList.Clear();

			if (PlayerPrefs.HasKey(cardsetBeingEdited.cardsetID))
			{
				string[] values = PlayerPrefs.GetString(cardsetBeingEdited.cardsetID).Split(',');
				for (int i = 0; i < values.Length; i++)
				{
					fieldColumnWidthList.Add(Mathf.Clamp(float.Parse(values[i]), 50f, float.MaxValue));
				}
			}
			else
			{
				fieldColumnWidthList.Add(150f);
				fieldColumnWidthList.Add(150f);
				for (int i = 0; i < cardFieldCount; i++)
				{
					fieldColumnWidthList.Add(150f);
				}
			}
		}
		void DrawColumnResizingHandle (int index)
		{
			Event evt = Event.current;
			Rect handleArea = GUILayoutUtility.GetRect(10.0f, 20.0f, GUILayout.Width(10.0f));
			GUI.Box(handleArea, "|", EditorStyles.label);
			EditorGUIUtility.AddCursorRect(handleArea, MouseCursor.ResizeHorizontal);

			if (handleArea.Contains(evt.mousePosition))
			{
				if (evt.type == EventType.MouseDown)
					resizeIndex = index;

			}

			if (index == resizeIndex)
			{
				if (evt.type == EventType.MouseUp)
					resizeIndex = -1;
				else if (evt.type == EventType.MouseDrag)
				{
					fieldColumnWidthList[index] += evt.delta.x;
					fieldColumnWidthList[index] = Mathf.Clamp(fieldColumnWidthList[index], 50f, float.MaxValue);
					Repaint();
				}
			}
		}
		void DisplayCardImporterField (Cardset cardset)
		{
			// ---- Import a List of Cards ---- 
			Event evt = Event.current;
			Rect dropArea = GUILayoutUtility.GetRect(250.0f, 20.0f, GUILayout.MaxWidth(250.0f));
			string boxMessage = "Drop Card Datas To Be Included";

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
						CardData importedCard = null;
						foreach (Object draggedObject in DragAndDrop.objectReferences)
						{
							if (draggedObject.GetType() == typeof(CardData))
							{
								importedCard = (CardData)draggedObject;
								cardset.cardsData.Add(importedCard);
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
			EditorGUILayout.BeginVertical();
			if (editableFields) EditorGUILayout.LabelField("Card Data ID", GUILayout.Width(fieldColumnWidthList[0]));
			if (VerifiedDelayedTextField("$cardName" + card.GetInstanceID(), ref card.cardDataID, GUILayout.Width(fieldColumnWidthList[0])))
			{
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(card), card.cardDataID);
			}
			EditorGUILayout.EndVertical();
		}
		void ShowEditableTagsAndCardFields (CardData card, bool editableFields)
		{
			// ---- Card Tags  ----
			EditorGUILayout.BeginVertical();
			if (editableFields) EditorGUILayout.LabelField("Tags", GUILayout.Width(fieldColumnWidthList[1]));
			card.tags = EditorGUILayout.TextField(card.tags, GUILayout.Width(fieldColumnWidthList[1]));
			EditorGUILayout.EndVertical();
			int toBeRemoved = -1;
			for (int i = 0; i < card.fields.Count; i++)
			{
				EditorGUILayout.BeginVertical();
				if (editableFields)
				{
					if (GUILayout.Button("Remove", GUILayout.Width(fieldColumnWidthList[i + 2])))
					{
						toBeRemoved = i;
					}
					VerifiedDelayedTextField("$cardField" + i, ref card.fields[i].fieldName, GUILayout.Width(fieldColumnWidthList[i + 2]));
					card.fields[i].dataType = (CardFieldDataType)EditorGUILayout.EnumPopup(card.fields[i].dataType);
				}
				switch (card.fields[i].dataType)
				{
					case CardFieldDataType.Text:
						card.fields[i].stringValue = EditorGUILayout.TextField(card.fields[i].stringValue, GUILayout.Width(fieldColumnWidthList[i + 2]));
						break;
					case CardFieldDataType.Number:
						card.fields[i].numValue = EditorGUILayout.FloatField(card.fields[i].numValue, GUILayout.Width(fieldColumnWidthList[i + 2]));
						if (editableFields) card.fields[i].hideOption = (CardFieldHideOption)EditorGUILayout.EnumPopup(card.fields[i].hideOption);
						break;
					case CardFieldDataType.Image:
						card.fields[i].imageValue = (Sprite)EditorGUILayout.ObjectField(card.fields[i].imageValue, typeof(Sprite), false, GUILayout.Width(fieldColumnWidthList[i + 2]));
						break;
				}
				if (toBeRemoved >= 0)
					card.fields.RemoveAt(toBeRemoved);
				EditorGUILayout.EndVertical();
			}
		}
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
				File.WriteAllText("Assets/" + gameBeingEdited.cardgameID + ".json", CardGameSerializer.SerializeCardGame(gameBeingEdited));
		}
		void SaveCardset (bool auto)
		{
			EditorUtility.SetDirty(cardsetBeingEdited);
			if (!auto)
				File.WriteAllText("Assets/" + cardsetBeingEdited.cardsetID + ".json", CardGameSerializer.SerializeCardset(cardsetBeingEdited));

			string values = "";
			for (int i = 0; i < fieldColumnWidthList.Count; i++)
			{
				values = values + fieldColumnWidthList[i];
				if (i < fieldColumnWidthList.Count - 1)
					values = values + ",";
			}
			PlayerPrefs.SetString(cardsetBeingEdited.cardsetID, values);
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
		#endregion
	}
}
