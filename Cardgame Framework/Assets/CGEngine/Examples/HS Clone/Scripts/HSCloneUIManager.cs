using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class HSCloneUIManager : MonoBehaviour
{
	public float attackingTime = 0.15f;
	public float damageShowTime = 2f;
	public float maxDistanceForAttack = 2f;

	Transform enemyFace;
	GameObject enemyDamageTakenObject;
	float enemyDamageShowTimer;

	private void Start ()
	{
		enemyFace = GameObject.Find("EnemyFace").transform;
		enemyDamageTakenObject = GameObject.Find("EnemyDamageTaken");
		enemyDamageTakenObject.SetActive(false);
	}

	private void Update ()
	{
		if (enemyDamageShowTimer > 0)
		{
			enemyDamageShowTimer -= Time.deltaTime;
			if (enemyDamageShowTimer < 0)
			{
				enemyDamageShowTimer = 0;
				enemyDamageTakenObject.SetActive(false);
			}
		}
	}

	public void AttackEnemyFace ()
	{
		Transform attacker = Match.Current.GetCardVariable("usedCard").transform;
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
				enemyDamageTakenObject.SetActive(true);
			}

			yield return null;
		}
		obj.position = origin;
	}
}
