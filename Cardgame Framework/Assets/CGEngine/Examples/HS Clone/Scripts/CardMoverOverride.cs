using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class CardMoverOverride : CardMover
{
	public float showCardTime = 1f;
	Transform showCardPosition;

	private void Start ()
	{
		showCardPosition = GameObject.Find("ShowCardWhenDraw").transform;
	}

	public override IEnumerator OnCardEnteredZone (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters)
	{
		if (newZone.zoneTags == "Play,P1")
		{
			StartCoroutine(PlaySeamlessly(card, newZone, oldZone, additionalParamenters));
		}
		else if (oldZone.zoneTags == "Deck,P1" && newZone.zoneTags == "Hand,P1" && Match.Current.turnNumber > 1)
		{
			yield return ShowCardDrawn(card, newZone, oldZone, additionalParamenters);
		}
		else
			yield return base.OnCardEnteredZone(card, newZone, oldZone, additionalParamenters);
	}

	IEnumerator PlaySeamlessly (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters)
	{
		card.GetComponentInChildren<Animator>().SetTrigger("ToPlay");
		yield return base.OnCardEnteredZone(card, newZone, oldZone, additionalParamenters);
	}

	IEnumerator ShowCardDrawn (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters)
	{
		yield return MoveCard(card, showCardPosition.position, showCardPosition.rotation, moveTime);
		Vector3 littleToLeft = showCardPosition.position + Vector3.left;
		yield return MoveCard(card, littleToLeft, showCardPosition.rotation, showCardTime);
		yield return base.OnCardEnteredZone(card, newZone, oldZone, additionalParamenters);
	}

	private IEnumerator SimpleMove (Transform obj, Vector3 toPosition, float time)
	{
		Vector3 startPosition = obj.position;
		float delta = Time.deltaTime;
		float lerpSteps = time / delta;
		float lerpAmount = 1f / lerpSteps;
		for (float step = 0; step < 1; step += lerpAmount)
		{
			obj.position = Vector3.Lerp(startPosition, toPosition, step);
			yield return new WaitForSeconds(delta);
		}
		obj.position = toPosition;
	}
}
