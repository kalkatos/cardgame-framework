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
		GUISkin skin;

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
								AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(markedForDeletion.rules[i].matchModifiers[j]));
							}
							markedForDeletion.rules[i].matchModifiers.Clear();
						}
						AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(markedForDeletion.rules[i]));
					}
					markedForDeletion.rules.Clear();
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

			#region OldCodeONGUI
			/*
			// --- Game Name ------
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Game Name", GUILayout.Width(80));
			gameData.name = GUILayout.TextField(gameData.name);
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(15);

			if (!string.IsNullOrEmpty(gameData.name))
			{
				// --- Ruleset Definitions -----
				if (gameData.rules == null) gameData.rules = new List<Ruleset>();
				EditorGUILayout.BeginVertical("Button");
				showRulesets = EditorGUILayout.Foldout(showRulesets, "Rulesets");
				if (showRulesets)
				{
					for (int i = 0; i < gameData.rules.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("" + i, GUILayout.Width(18));
						EditorGUILayout.BeginHorizontal("Button");
						EditorGUILayout.BeginVertical();

						//---- Identification --------
						EditorGUILayout.LabelField("ID");
						gameData.rules[i].id = GUILayout.TextField(gameData.rules[i].id);
						EditorGUILayout.LabelField("Name");
						gameData.rules[i].name = GUILayout.TextField(gameData.rules[i].name);
						EditorGUILayout.LabelField("Description");
						gameData.rules[i].description = GUILayout.TextField(gameData.rules[i].description, GUILayout.Height(30));
						GUILayout.Space(5);
						//---- Definition of ROLES --------
						showRulesetPlayerRoles[i] = EditorGUILayout.Foldout(showRulesetPlayerRoles[i], "Player Role Names");
						if (showRulesetPlayerRoles[i])
						{
							RenderStringArray(ref gameData.rules[i].playerRoles, "Add New Role Name");
							if (gameData.rules[i].starter == Starter.SpecificRole && gameData.rules[i].starterRoleIndex >= gameData.rules[i].playerRoles.Length)
								gameData.rules[i].starterRoleIndex = 0;
						}
						GUILayout.Space(5);
						//---- Definition of TEAMS --------
						gameData.rules[i].freeForAll = EditorGUILayout.Toggle("Free for All", gameData.rules[i].freeForAll);
						if (!gameData.rules[i].freeForAll)
						{
							showRulesetPlayerTeams[i] = EditorGUILayout.Foldout(showRulesetPlayerTeams[i], "Player Team Names");
							if (showRulesetPlayerTeams[i])
							{
								RenderStringArray(ref gameData.rules[i].playerTeams, "Add New Team Name");
								if (gameData.rules[i].starter == Starter.SpecificTeam && gameData.rules[i].starterTeamIndex >= gameData.rules[i].playerTeams.Length)
									gameData.rules[i].starterTeamIndex = 0;
							}
						}
						GUILayout.Space(5);
						//---- Definition of Starter --------
						gameData.rules[i].starter = (Starter)EditorGUILayout.EnumPopup("Starter Selection Method", gameData.rules[i].starter);
						switch (gameData.rules[i].starter)
						{
							case Starter.SpecificRole:
								gameData.rules[i].starterRoleIndex = EditorGUILayout.Popup("Starter Role", gameData.rules[i].starterRoleIndex, gameData.rules[i].playerRoles);
								break;
							case Starter.SpecificTeam:
								gameData.rules[i].starterTeamIndex = EditorGUILayout.Popup("Starter Team", gameData.rules[i].starterTeamIndex, gameData.rules[i].playerTeams);
								break;
						}
						GUILayout.Space(5);
						//---- Definition of Player Rules -------------
						showPlayerRules[i] = EditorGUILayout.Foldout(showPlayerRules[i], "Player Specific Rules");
						if (showPlayerRules[i])
						{
							/*
							for (int j = 0; j < gameData.rules[i].playerRules.Count; j++)
							{

								EditorGUILayout.BeginHorizontal();
								EditorGUILayout.LabelField("" + j, GUILayout.Width(18));
								EditorGUILayout.BeginVertical("Button");
								EditorGUILayout.BeginHorizontal();
								EditorGUILayout.BeginVertical();
								gameData.rules[i].playerRules[j].role = EditorGUILayout.Popup("Role", gameData.rules[i].playerRules[j].role, gameData.rules[i].playerRoles);
								gameData.rules[i].playerRules[j].roleDescription = EditorGUILayout.TextField("Description", gameData.rules[i].playerRules[j].roleDescription, GUILayout.Height(30));
								if (!gameData.rules[i].freeForAll)
									gameData.rules[i].playerRules[j].team = EditorGUILayout.Popup("Team", gameData.rules[i].playerRules[j].team, gameData.rules[i].playerTeams);
								gameData.rules[i].playerRules[j].quantityNeededOnMatch = EditorGUILayout.IntField("Quantity on Match", gameData.rules[i].playerRules[j].quantityNeededOnMatch);
								EditorGUILayout.EndVertical();
								if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
								{
									gameData.rules[i].playerRules.RemoveAt(j);
								}
								EditorGUILayout.EndHorizontal();
								EditorGUILayout.EndVertical();
								EditorGUILayout.EndHorizontal();
								GUILayout.Space(4);
							}
							*/ /*
						}

						EditorGUILayout.EndVertical();
						if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
						{
							gameData.rules.RemoveAt(i);
							showRulesetPlayerRoles.RemoveAt(i);
							showRulesetPlayerTeams.RemoveAt(i);
						}
						EditorGUILayout.EndHorizontal();
						GUILayout.Space(5);
						EditorGUILayout.EndHorizontal();
					}

					if (GUILayout.Button("Add New Ruleset", GUILayout.Height(30)))
					{
						Ruleset newRuleset = new Ruleset();
						newRuleset.id = "ruleset" + (gameData.rules.Count < 10 ? "0" : "") + (gameData.rules.Count + 1);
						gameData.rules.Add(newRuleset);
						showRulesetPlayerRoles.Add(false);
						showRulesetPlayerTeams.Add(false);
					}
				}
				GUILayout.EndVertical();
				GUILayout.Space(15);

				// --- Card Field Definitions -----
				EditorGUILayout.BeginVertical("Button");
				showCardField = EditorGUILayout.Foldout(showCardField, "Card Field Definitions");
				if (showCardField)
				{
					for (int i = 0; i < gameData.cardFieldDefinitions.Count; i++)
					{

						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("" + i, GUILayout.Width(18));
						EditorGUILayout.BeginHorizontal("Button");
						EditorGUILayout.BeginVertical();
						GUILayout.Space(4);
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("Name", GUILayout.Width(50));
						gameData.cardFieldDefinitions[i].name = GUILayout.TextField(gameData.cardFieldDefinitions[i].name);
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("Type", GUILayout.Width(50));
						gameData.cardFieldDefinitions[i].dataType = (CardFieldDataType)EditorGUILayout.EnumPopup(gameData.cardFieldDefinitions[i].dataType);
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.EndVertical();
						GUILayout.Space(4);
						if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
						{
							gameData.cardFieldDefinitions.RemoveAt(i);
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.EndHorizontal();
						GUILayout.Space(4);
					}

					if (GUILayout.Button("New Card Field", GUILayout.Height(30)))
					{
						gameData.cardFieldDefinitions.Add(new CardField());
					}
				}
				GUILayout.EndVertical();
			}
			
			if (GUI.changed)
			{
				EditorUtility.SetDirty(gameData);
				//EditorSceneManager.MarkSceneDirty(gameData);
			}
		}

		

		void RenderStringArray(ref string[] array, string addNewElementButtonLabel)
		{
			if (array == null) array = new string[0];
			EditorGUILayout.BeginVertical();
			int indexToBeRemoved = -1;
			for (int j = 0; j < array.Length; j++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(" ", GUILayout.Width(10));
				array[j] = GUILayout.TextField(array[j]);
				if (GUILayout.Button("X", GUILayout.Height(15), GUILayout.Width(15)))
					indexToBeRemoved = j;
				EditorGUILayout.EndHorizontal();
			}
			if (indexToBeRemoved >= 0)
			{
				string[] newTeams = new string[array.Length - 1];
				if (newTeams.Length > 0)
				{
					for (int j = 0; j < array.Length; j++)
					{
						if (j == indexToBeRemoved)
							continue;
						else if (j > indexToBeRemoved)
							newTeams[j - 1] = array[j];
						else
							newTeams[j] = array[j];
					}
				}
				array = newTeams;
				//if (gameData.rules[i].starterRoleIndex >= indexToBeRemoved) gameData.rules[i].starterRoleIndex = 0;
			}
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(" ", GUILayout.Width(10));
			if (GUILayout.Button(addNewElementButtonLabel))
			{
				string[] newTeams = new string[array.Length + 1];
				for (int j = 0; j < array.Length - 1; j++)
					newTeams[j] = array[j];
				newTeams[newTeams.Length - 1] = "";
				array = newTeams;
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			*/
			#endregion
		}

		void DisplayCardGameData(CardGameData data)
		{
			EditorGUILayout.BeginVertical();

			EditorGUILayout.LabelField(data.cardgameID, EditorStyles.boldLabel);

			string newName = EditorGUILayout.TextField("Game Name", data.cardgameID, GUILayout.MaxWidth(400));
			if (newName != data.cardgameID)
			{
				data.cardgameID = newName;
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(data), "CardGame-" + newName);
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
					if (fields[i] == null)
					{
						fields.RemoveAt(i);
						i--;
						continue;
					}

					//Display editable content
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField((i + 1) + ".  ", GUILayout.MaxWidth(20)); 
					if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
					{
						toBeDeleted = fields[i];
					}
					EditorGUILayout.BeginVertical();
					fields[i].name = GUILayout.TextField(fields[i].name, GUILayout.MaxWidth(200));
					fields[i].dataType = (CardFieldDataType)EditorGUILayout.EnumPopup(fields[i].dataType, GUILayout.MaxWidth(200));
					if (fields[i].dataType == CardFieldDataType.Number)
						fields[i].hideOption = (CardFieldHideOption)EditorGUILayout.EnumPopup(fields[i].hideOption, GUILayout.MaxWidth(200));
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
				}
				if (GUILayout.Button("Create New Field", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
				{
					fields.Add(new CardField());
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
				if (toBeDeleted != null)
					fields.Remove(toBeDeleted);
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
					EditorGUILayout.LabelField((i + 1) + ".  ", GUILayout.MaxWidth(20));
					if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
					{
						toBeDeleted = rulesets[i];
					}
					EditorGUILayout.BeginVertical();
					//Ruleset name
					string newName = EditorGUILayout.TextField("Ruleset Name", rulesets[i].rulesetID, GUILayout.MaxWidth(400));
					if (newName != rulesets[i].rulesetID)
					{
						rulesets[i].rulesetID = newName;
						AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(rulesets[i]), "Ruleset-" + newName);
					}
					//Ruleset description
					rulesets[i].description = EditorGUILayout.TextField("Description", rulesets[i].description, GUILayout.Height(42), GUILayout.MaxWidth(400));
					//Match Modifiers
					if (showMatchModifiersFoldout = EditorGUILayout.Foldout(showMatchModifiersFoldout, "Match Modifiers"))
					{
						if (rulesets[i].matchModifiers == null)
							rulesets[i].matchModifiers = new List<ModifierData>();
						DisplayModifiers(rulesets[i].matchModifiers);
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

		void DisplayModifiers (List<ModifierData> modifiers)
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
				if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
				{
					toBeDeleted = modifiers[i];
				}
				EditorGUILayout.BeginVertical(GUILayout.Width(20));
				if (GUILayout.Button(" ▲", GUILayout.Width(20), GUILayout.Height(20)))
				{
					//Move Up
					moveUp = modifiers[i];
				}
				if (GUILayout.Button(" ▼", GUILayout.Width(20), GUILayout.Height(20)))
				{
					//Move Down
					moveDown = modifiers[i];
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical();
				//mod fields
				string newName = EditorGUILayout.TextField("Modifier Name", modifiers[i].modifierID, GUILayout.MaxWidth(400));
				if (newName != modifiers[i].modifierID)
				{
					modifiers[i].modifierID = newName;
					AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(modifiers[i]), "Modifier-" + newName);
				}

				//TODO fields

				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Create New Modifier", GUILayout.MaxWidth(250), GUILayout.MaxHeight(18)))
			{
				ModifierData newMod = CreateInstance<ModifierData>();
				modifiers.Add(newMod);
				newMod.modifierID = "New Modifier";
				if (!AssetDatabase.IsValidFolder("Assets/Data"))
					AssetDatabase.CreateFolder("Assets", "Data");
				AssetDatabase.CreateAsset(newMod, "Assets/Data/Modifier-New Modifier.asset");
			}
			GUILayout.Space(15);

			if (toBeDeleted)
			{
				modifiers.Remove(toBeDeleted);
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

		void DisplayCardDataList(List<CardData> cards)
		{
			if (showCardDataListFoldout = EditorGUILayout.Foldout(showCardDataListFoldout, "All Cards"))
			{
				//TODO Card List
			}
		}
	}
}