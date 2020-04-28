using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class Arrow : MonoBehaviour
{
	Plane myPlane = new Plane(Vector3.up, Vector3.up * 0.1f);
	public Card attacker;
	Transform mask;

	private void Awake ()
	{
		mask = transform.GetChild(0).Find("Mask");
	}

	private void LateUpdate ()
	{
		if (attacker)
		{
			transform.position = InputManager.instance.GetMouseWorldPositionInPlane(myPlane);
			Vector3 distance = transform.position - attacker.transform.position;
			transform.forward = distance;
			float distanceMag = distance.magnitude / 2;
			mask.position = attacker.transform.position + transform.forward * (distanceMag - 2f);
			mask.localScale = Vector3.one + Vector3.up * Mathf.Min(distanceMag - 1, 12f);
		}
	}
}
