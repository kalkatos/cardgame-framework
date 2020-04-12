using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class CardTransitionToPlay : CardMover
{

	public override IEnumerator OnCardEnteredZone (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters)
	{
		StartCoroutine(PlaySeamlessly(card, newZone, oldZone, additionalParamenters));
		yield return null;
	}

	public override IEnumerator OnCardLeftZone (Card card, Zone oldZone)
	{
		yield return new WaitForSeconds(0.1f);
		yield return base.OnCardLeftZone(card, oldZone);
	}

	IEnumerator PlaySeamlessly (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters)
	{
		if (newZone.zoneTags.Contains("Play"))
		{
			Vector3 toPosition = newZone.transform.position + Vector3.up * 9 + Vector3.right * (Mathf.Clamp(newZone.Content.Count - 1, 0, 999) * newZone.distanceBetweenCards.x / 2);
			yield return SimpleMove(card.transform, toPosition, 0.1f);
			card.GetComponent<Animator>().SetTrigger("ToPlay");
		}
		else if (newZone.zoneTags.Contains("Hand"))
		{
			card.GetComponent<Animator>().SetTrigger("ToHand");
		}
		yield return new WaitForSeconds(0.5f);
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
		Debug.Log("Ended");
		obj.position = toPosition;
	}
}
