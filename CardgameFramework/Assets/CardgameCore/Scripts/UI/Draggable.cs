using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardgameCore
{
    [RequireComponent(typeof(Collider))]
    public class Draggable : MonoBehaviour, IBeginDragHandler, IPointerDownHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private bool getDragPlaneFromZone = true;
        [SerializeField] private Transform dragPlaneObj;

        private Collider col;
        private Plane dragPlane;
        private Vector3 dragOffset;
		private CGComponent attachedComponent;

		private void Awake ()
		{
			col = GetComponent<Collider>();
			if (getDragPlaneFromZone)
			{
				if (TryGetComponent(out attachedComponent))
				{
					attachedComponent.OnEnteredZone += OnComponentEnteredZone;
					if (attachedComponent.Zone)
						dragPlane = attachedComponent.Zone.zonePlane;
				}
			}
			else if (dragPlaneObj)
				dragPlane = new Plane(dragPlaneObj.up, dragPlaneObj.position);
			else
				dragPlane = new Plane(Vector3.up, Vector3.zero);
		}

		private void OnDestroy ()
		{
			if (attachedComponent)
				attachedComponent.OnEnteredZone -= OnComponentEnteredZone;
		}

		private void OnComponentEnteredZone (Zone zone)
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

		public void OnPointerDown (PointerEventData eventData)
		{
			Vector3 cameraPos = eventData.pressEventCamera.transform.position;
			Ray objRay = new Ray(cameraPos, transform.position - cameraPos);
			if (dragPlane.Raycast(objRay, out float distance))
				dragOffset = GetEventWorldposition(eventData) - objRay.GetPoint(distance);
		}

		public void OnBeginDrag (PointerEventData eventData)
		{
			col.enabled = false;
		}

		public void OnDrag (PointerEventData eventData)
		{
			transform.position = GetEventWorldposition(eventData) - dragOffset;
		}

		public void OnEndDrag (PointerEventData eventData)
		{
			col.enabled = true;
		}
	}
}