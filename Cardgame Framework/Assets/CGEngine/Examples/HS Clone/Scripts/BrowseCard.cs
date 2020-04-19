using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;
using UnityEngine.EventSystems;

public class BrowseCard : MonoBehaviour, OnPointerEnterEventWatcher, OnPointerExitEventWatcher
{
	public Vector2 maxPositions = new Vector2(12, 7);
	public float timeToBrowse = 0.5f;
	Plane myPlane = new Plane(Vector3.up, Vector3.up * 30);
	Vector3 mousePosition;
	Card infoCard;
	float enterTime = -1;
	Card mouseEnterCard;

	private void Start ()
	{
		infoCard = transform.GetChild(0).GetComponent<Card>();
		InputManager.Register(this);
	}

	private void OnDestroy ()
	{
		InputManager.Unregister(this);
	}

	private void LateUpdate ()
	{
		mousePosition = InputManager.instance.GetMouseWorldPositionInPlane(myPlane);
		mousePosition.x = Mathf.Clamp(mousePosition.x, -maxPositions.x, maxPositions.x);
		mousePosition.z = Mathf.Clamp(mousePosition.z, -maxPositions.y, maxPositions.y);
		transform.position = mousePosition;

		if (enterTime > 0 && Time.time - enterTime > timeToBrowse)
		{
			infoCard.gameObject.SetActive(true);
			enterTime = -1;
			infoCard.SetupData(mouseEnterCard.data);
		}
	}

	public void OnPointerEnterEvent (PointerEventData eventData, InputObject inputObject)
	{
		if (mouseEnterCard = inputObject.card) 
		{
			if (infoCard.gameObject.activeSelf)
				infoCard.SetupData(mouseEnterCard.data);
			enterTime = Time.time;
		}
	}

	public void OnPointerExitEvent (PointerEventData eventData, InputObject inputObject)
	{
		if (infoCard.gameObject.activeSelf)
			infoCard.gameObject.SetActive(false);
		enterTime = -1;
	}
}
