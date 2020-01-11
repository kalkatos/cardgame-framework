using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class GameManager : MonoBehaviour
{
	public CardGameData game;

	private void Start()
	{
		if (game)
		{
			CGEngine.CreateCards(game.cardTemplate, game.allCardsData, Vector3.zero, new GameObject("CardContainer").transform);

			if (game.rules != null && game.rules.Count > 0)
				CGEngine.StartMatch(game.rules[0]);
		}
	}
}
