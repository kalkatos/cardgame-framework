using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;
using UnityEngine.EventSystems;

public class HSCloneUIManager : MatchWatcher, OnPointerEnterEventWatcher, OnPointerExitEventWatcher, OnEndDragEventWatcher,
	OnBeginDragEventWatcher
{
	public float attackingTime = 0.15f;
	public float damageShowTime = 2f;
	public float maxDistanceForAttack = 2f;

	Transform enemyFace;
	float enemyDamageShowTimer;
	Zone play;
	Zone hand;
	Card draggedCard;
	bool currentCardCanBeUsed;
	Transform arrow;

	private void Start ()
	{
		enemyFace = GameObject.Find("P2Face").transform;
		InputManager.Register(this);
		play = GameObject.Find("Play").GetComponent<Zone>();
		hand = GameObject.Find("Hand").GetComponent<Zone>();
		arrow = GameObject.Find("AttackArrow").transform;
		arrow.gameObject.SetActive(false);
	}

	private void Update ()
	{
		if (enemyDamageShowTimer > 0)
		{
			enemyDamageShowTimer -= Time.deltaTime;
			if (enemyDamageShowTimer < 0)
			{
				enemyDamageShowTimer = 0;
			}
		}
	}

	private void OnDestroy ()
	{
		InputManager.Unregister(this);
	}

	public void AttackEnemyFace ()
	{
		Transform attacker = Match.Current.GetCardVariable("Attacker").transform;
		StartCoroutine(AttackMotion(attacker, enemyFace.position));
	}

	private IEnumerator AttackMotion (Transform obj, Vector3 point)
	{
		Vector3 origin = obj.position;
		float distance = Vector3.Distance(origin, point);
		float maxStep = Mathf.Max(distance - maxDistanceForAttack, 1f) / distance;
		float firstTime = Time.time;
		float startTime = Time.time;
		float elapsedTime = 0;
		float currentStep = 0;
		float state = 0;
		while (state <= 1)
		{
			obj.position = Vector3.Lerp(origin, point, state == 0 ? currentStep : maxStep - currentStep);
			elapsedTime = Time.time - startTime;
			currentStep = Mathf.Clamp01(elapsedTime / attackingTime);
			if (currentStep >= maxStep)
			{
				state++;
				currentStep = 0;
				startTime = Time.time;
				enemyDamageShowTimer = damageShowTime;
			}

			yield return null;
		}
		obj.position = origin;
	}

	

	public void OnPointerEnterEvent (PointerEventData eventData, InputObject inputObject)
	{
		if (draggedCard && currentCardCanBeUsed)
		{
			if (inputObject.zone == play)
				draggedCard.AddTag("ToBeCast");
			else if (inputObject.zone == hand)
				draggedCard.RemoveTag("ToBeCast");
		}
	}

	public void OnPointerExitEvent (PointerEventData eventData, InputObject inputObject)
	{
		
	}

	public void OnBeginDragEvent (PointerEventData eventData, InputObject inputObject)
	{
		draggedCard = inputObject.card;
		if (draggedCard)
		{
			currentCardCanBeUsed = draggedCard.HasTag("CanBeUsed");
			draggedCard.RemoveTag("CanBeUsed");

			if (draggedCard.zone == play)
			{
				arrow.GetComponent<Arrow>().attacker = draggedCard;
				arrow.gameObject.SetActive(true);
			}
		}
	}

	public void OnEndDragEvent (PointerEventData eventData, InputObject inputObject)
	{
		if (draggedCard)
		{
			draggedCard.RemoveTag("ToBeCast");
			if (currentCardCanBeUsed)
				draggedCard.AddTag("CanBeUsed");
		}
		draggedCard = null;
		arrow.gameObject.SetActive(false);
	}

	
}
