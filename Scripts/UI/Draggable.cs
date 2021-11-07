using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace CardgameCore
{
    [RequireComponent(typeof(Collider))]
    public class Draggable : MonoBehaviour, IBeginDragHandler, IPointerDownHandler, IDragHandler, IEndDragHandler
    {
		public Card AttachedCard => attachedCard;

        [SerializeField] private bool getDragPlaneFromZone = true;
        [SerializeField] private Transform dragPlaneObj;

		private Collider col;
        private Plane dragPlane;
        private Vector3 dragOffset;
		private Card attachedCard;
		private Coroutine correctDraggableCoroutine;

		private void Awake ()
		{
			col = GetComponent<Collider>();
			if (getDragPlaneFromZone)
			{
				if (TryGetComponent(out attachedCard))
				{
					attachedCard.OnEnteredZone += OnCardEnteredZone;
					if (attachedCard.Zone)
						dragPlane = attachedCard.Zone.zonePlane;
				}
			}
			else if (dragPlaneObj)
				dragPlane = new Plane(dragPlaneObj.up, dragPlaneObj.position);
			else
			{
				DragPlane planeObj = FindObjectOfType<DragPlane>();
				if (planeObj)
					dragPlane = planeObj.Plane;
				else
					dragPlane = new Plane(Vector3.up, Vector3.zero);
			}
		}

		private void OnDestroy ()
		{
			if (attachedCard)
				attachedCard.OnEnteredZone -= OnCardEnteredZone;
		}

		private void OnCardEnteredZone (Zone zone)
		{
			if (getDragPlaneFromZone)
				dragPlane = zone.zonePlane;
		}

		private Vector3 GetEventWorldposition (PointerEventData eventData)
		{
			Ray eventRay = eventData.pressEventCamera.ScreenPointToRay(eventData.position);
			if (dragPlane.Raycast(eventRay, out float distance))
				return eventRay.GetPoint(distance);
			return Vector3.zero;
		}

		private IEnumerator CorrectDraggable ()
		{
			while (dragOffset.sqrMagnitude > 0.2f)
			{
				dragOffset = Vector3.Lerp(dragOffset, Vector3.zero, 0.1f);
				yield return null;
			}
		}

		public void OnPointerDown (PointerEventData eventData)
		{
			Vector3 cameraPos = eventData.pressEventCamera.transform.position;
			Ray objRay = new Ray(cameraPos, transform.position - cameraPos);
			if (dragPlane.Raycast(objRay, out float distance))
				dragOffset = GetEventWorldposition(eventData) - objRay.GetPoint(distance);
		}

		public void OnBeginDrag (PointerEventData eventData)
		{
			SetAsDragging(true);
			correctDraggableCoroutine = StartCoroutine(CorrectDraggable());
		}

		public void OnDrag (PointerEventData eventData)
		{
			transform.position = GetEventWorldposition(eventData) - dragOffset;
		}

		public void OnEndDrag (PointerEventData eventData)
		{
			SetAsDragging(false);
			if (correctDraggableCoroutine != null)
			{
				StopCoroutine(correctDraggableCoroutine);
				correctDraggableCoroutine = null;
			}
		}

		public void SetAsDragging (bool isDragging)
		{
			col.enabled = !isDragging;
		}
	}
}