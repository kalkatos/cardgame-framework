using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class ZoomCard : MonoBehaviour
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
		InputManager.Instance.onPointerEnterEvent.AddListener(MouseEnterOnCard);
		InputManager.Instance.onPointerExitEvent.AddListener(MouseExitOnCard);
	}

	private void LateUpdate ()
	{
		mousePosition = InputManager.Instance.GetMouseWorldPositionInPlane(myPlane);
		mousePosition.x = Mathf.Clamp(mousePosition.x, -maxPositions.x, maxPositions.x);
		mousePosition.z = Mathf.Clamp(mousePosition.z, -maxPositions.y, maxPositions.y);
		transform.position = mousePosition;

		if (enterTime > 0 && Time.time - enterTime > timeToBrowse && InputManager.Instance.currentEventObject.TryGetComponent(out Card currentCard) && currentCard == mouseEnterCard)
		{
			infoCard.gameObject.SetActive(true);
			enterTime = -1;
			infoCard.SetupData(currentCard.data);
		}
	}

	public void MouseEnterOnCard ()
	{
		if (InputManager.Instance.currentEventObject.TryGetComponent(out mouseEnterCard))
		{
			if (infoCard.gameObject.activeSelf)
				infoCard.SetupData(mouseEnterCard.data);
			enterTime = Time.time;
		}
	}

	public void MouseExitOnCard ()
	{
		if (infoCard.gameObject.activeSelf)
			infoCard.gameObject.SetActive(false);
		enterTime = -1;
	}
}
