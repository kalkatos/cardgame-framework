﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardGameFramework;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MatchWatcher
{
	public CardGameData game;
	public Transform showMonsterObject;

	[Header("User Interface")]
	public TextMeshProUGUI lifePoints;
	public GameObject monsterButtons;
	public Button skipRoom;
	public TextMeshProUGUI mainMessage;

	public override IEnumerator TreatTrigger(string triggerTag, params object[] args)
	{
		switch (triggerTag)
		{

			case "OnMessageSent":
				string msg = (string)GetArgumentWithTag("message", args);
				if (msg == "CanSkip")
				{
					skipRoom.interactable = true;
				}
				else if (msg == "CannotSkip")
				{
					skipRoom.interactable = false;
				}
				else if (msg == "BattleSelection")
				{
					monsterButtons.SetActive(true);
				}
				else if (msg == "Victory")
				{
					mainMessage.text = "Victory!";
					yield return RestartGame();
				}
				else if (msg == "Defeat")
				{
					mainMessage.text = "Defeat . . .";
					yield return RestartGame();
				}
				break;

			case "OnModifierValueChanged":
				double newValue = (double)GetArgumentWithTag("newValue", args);
				lifePoints.text = "Life: " + newValue;
				break;

			case "OnCardEnteredZone":
				Card card = (Card)GetArgumentWithTag("card", args);
				Zone zone = (Zone)GetArgumentWithTag("zone", args);
				if (zone.zoneType == "Battle")
				{
					//showMonsterObject.position = card.transform.position;
					yield return CardMover.MoveToCoroutine(card, card.transform.position + Vector3.up, 0.1f);
				}
				break;
		}
		yield return null;
	}

	private void Start()
	{
		if (game)
		{
			//CGEngine.CreateCards(game.cardTemplate, game.allCardsData, Vector3.zero, new GameObject("CardContainer").transform);

			if (game.rules != null && game.rules.Count > 0)
				CGEngine.StartMatch(game.rules[0]);

			lifePoints.text = "Life: " + Match.Current.SelectModifiers("mod(%PlayerHP)")[0].numValue;
		}
	}

	public void SkipRoom ()
	{
		Match.Current.UseAction("SkipRoom");
	}

	public void GoBarehanded ()
	{
		Match.Current.UseAction("FaceMonsterBarehanded");
		monsterButtons.SetActive(false);
	}

	public void UseWeapon ()
	{
		Match.Current.UseAction("FaceMonsterWithWeapon");
		monsterButtons.SetActive(false);
	}

	IEnumerator RestartGame()
	{
		yield return new WaitForSeconds(5);
		SceneManager.LoadScene("SampleScene");
	}
}