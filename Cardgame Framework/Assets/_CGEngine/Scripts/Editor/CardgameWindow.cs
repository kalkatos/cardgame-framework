using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;

namespace CGEngine
{
	public class CardgameWindow : EditorWindow
	{
		CardGameData gameData;
		bool showRulesets;
		bool showCardField;
		List<bool> showRulesetPlayerRoles = new List<bool>();
		List<bool> showRulesetPlayerTeams = new List<bool>();
		List<bool> showPlayerRules = new List<bool>();
		GUISkin skin;

		void OnEnable()
		{
			if (showRulesetPlayerRoles.Count == 0)
				showRulesetPlayerRoles.Add(false);
			if (showRulesetPlayerTeams.Count == 0)
				showRulesetPlayerTeams.Add(false);
			if (showPlayerRules.Count == 0)
				showPlayerRules.Add(false);

			skin = (GUISkin)Resources.Load("CGEngineSkin");

			//Search for all CardGameData
			if (!gameData)
			{
				string[] foundAssets = AssetDatabase.FindAssets("t:CardGameData");

				if (foundAssets.Length == 0)
				{
					gameData = CreateInstance<CardGameData>();
					AssetDatabase.CreateAsset(gameData, "Assets/New Card Game Data.asset");
				}
				else
					foreach (string item in foundAssets)
					{
						gameData = AssetDatabase.LoadAssetAtPath<CardGameData>(AssetDatabase.GUIDToAssetPath(item));
					}

				showRulesetPlayerRoles.Clear();
				showRulesetPlayerTeams.Clear();
				showPlayerRules.Clear();
				foreach (Ruleset item in gameData.rules)
				{
					showRulesetPlayerRoles.Add(false);
					showRulesetPlayerTeams.Add(false);
					showPlayerRules.Add(false);
				}
			}
		}

		void OnGUI()
		{
			//GUI.skin = skin;
			GUI.skin.textField.wordWrap = true;
			//GUI.skin.button.padding.left = GUI.skin.button.padding.right = GUI.skin.button.padding.top = GUI.skin.button.padding.bottom = 3;
			GUI.skin.button.clipping = TextClipping.Overflow;

			// --- First Label ------
			GUILayout.Label("Card Game Definitions", EditorStyles.boldLabel);
			GUILayout.Space(15);

			// --- Game Name ------
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Game Name", GUILayout.Width(80));
			gameData.name = GUILayout.TextField(gameData.name);
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(15);

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
						*/
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
					newRuleset.id = "ruleset" + (gameData.rules.Count < 10 ? "0" : "") + (gameData.rules.Count+1);
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
			
			if (GUI.changed)
			{
				EditorUtility.SetDirty(gameData);
				//EditorSceneManager.MarkSceneDirty(gameData);
			}
		}

		[MenuItem("CGEngine/Cardgame Definitions", priority = 1)]
		public static void ShowWindow()
		{
			// Get existing open window or if none, make a new one:
			var window = GetWindow<CardgameWindow>();
			window.titleContent = new GUIContent("Cardgame Definitions");
			window.Show();
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
		}
	}
}