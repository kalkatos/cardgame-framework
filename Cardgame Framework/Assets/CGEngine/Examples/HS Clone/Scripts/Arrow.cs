using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class Arrow : MonoBehaviour
{
	Plane myPlane = new Plane(Vector3.up, Vector3.zero);
	Transform myArrow;

	private void Awake ()
	{
		myArrow = transform.GetChild(0);
	}

	private void LateUpdate ()
	{
		transform.position = InputManager.Instance.GetMouseWorldPositionInPlane(myPlane);
		InputObject dragging = InputManager.Instance.draggedObject;

		if (dragging)
		{
			Vector3 scale = myArrow.localScale;
			scale.z = Vector3.Distance(transform.position, dragging.transform.position);
			myArrow.localScale = scale;
		}
	}
}
