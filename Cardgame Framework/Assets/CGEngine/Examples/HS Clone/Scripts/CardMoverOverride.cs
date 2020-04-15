using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class CardMoverOverride : CardMover
{

	public override IEnumerator OnCardEnteredZone (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters)
	{
		if (newZone.zoneTags.Contains("Play"))
		{
			StartCoroutine(PlaySeamlessly(card, newZone, oldZone, additionalParamenters));
		}
		else
			yield return base.OnCardEnteredZone(card, newZone, oldZone, additionalParamenters);
	}

	public override IEnumerator OnCardLeftZone (Card card, Zone oldZone)
	{
		yield return new WaitForSeconds(0.1f);
		yield return base.OnCardLeftZone(card, oldZone);
	}

	IEnumerator PlaySeamlessly (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters)
	{
		//movementLockedCards.Add(card);
		//if (newZone.zoneTags.Contains("Play"))
		//{
		//	Vector3 toPosition = newZone.transform.position + Vector3.up * 15 + Vector3.right * (Mathf.Clamp(newZone.Content.Count - 1, 0, 999) * newZone.distanceBetweenCards.x / 2);
		//	yield return SimpleMove(card.transform, toPosition, 0.1f);
		//	card.GetComponent<Animator>().SetTrigger("ToPlay");
		//	yield return new WaitForSeconds(1.25f);
		//}
		
		if (newZone.zoneTags.Contains("Play"))
		{
			//movementLockedCards.Add(card);
			card.GetComponent<Animator>().SetTrigger("ToPlay");

		}
		yield return base.OnCardEnteredZone(card, newZone, oldZone, additionalParamenters);
		//movementLockedCards.Remove(card);

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
