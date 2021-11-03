using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CardgameCore
{
	[RequireComponent(typeof(Collider)), Obsolete("Class is deprecated. Please use Unity's Event Trigger component instead.")]
	public class InputHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler , IDragHandler, IEndDragHandler, IDropHandler, 
		IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
	{
		public static bool raycasterCheck;
		public static bool eventSystemCheck;
		public static event Action<InputHandler, PointerEventData> OnClickAction;
		public static event Action<InputHandler, PointerEventData> OnPointerDownAction;
		public static event Action<InputHandler, PointerEventData> OnPointerUpAction;
		public static event Action<InputHandler, PointerEventData> OnBeginDragAction;
		public static event Action<InputHandler, PointerEventData> OnDragAction;
		public static event Action<InputHandler, PointerEventData> OnEndDragAction;
		public static event Action<InputHandler, PointerEventData> OnDropAction;
		public static event Action<InputHandler, PointerEventData> OnPointerEnterAction;
		public static event Action<InputHandler, PointerEventData> OnPointerExitAction;

		[SerializeField] private UnityEvent OnClickEvent;
		[SerializeField] private UnityEvent OnPointerDownEvent;
		[SerializeField] private UnityEvent OnPointerUpEvent;
		[SerializeField] private UnityEvent OnBeginDragEvent;
		[SerializeField] private UnityEvent OnDragEvent;
		[SerializeField] private UnityEvent OnEndDragEvent;
		[SerializeField] private UnityEvent OnDropEvent;
		[SerializeField] private UnityEvent OnPointerEnterEvent;
		[SerializeField] private UnityEvent OnPointerExitEvent;
		[SerializeField] private Transform dragPlaneObj;
		[SerializeField] private bool getDragPlaneFromZone = true;

		public InputPermissions inputPermissions;

		private Collider col;
		private Plane dragPlane;
		private Card attachedCard = null;
		private Zone attachedZone = null;
		private Vector3 dragOffset;

		private void Awake()
		{
			if (!raycasterCheck)
			{
				raycasterCheck = true;
				if (!FindObjectOfType<PhysicsRaycaster>())
					CustomDebug.LogWarning("The InputHandler needs a PhysicsRaycaster in the scene to work properly!");
			}
			if (!eventSystemCheck)
			{
				eventSystemCheck = true;
				if (!FindObjectOfType<EventSystem>())
					CustomDebug.LogWarning("The InputHandler needs an EventSystem in the scene to work properly!");
			}
			col = GetComponent<Collider>();
			//Get drag plane from Zone
			if (TryGetComponent(out attachedCard))
			{
				attachedCard.OnEnteredZone += OnCardEnteredZone;
				if (getDragPlaneFromZone)
				{
					if (attachedCard.Zone)
						dragPlane = attachedCard.Zone.zonePlane;
				}
				else if (dragPlaneObj)
					dragPlane = new Plane(dragPlaneObj.up, dragPlaneObj.position);
				else
					dragPlane = new Plane(Vector3.up, Vector3.zero);
			}
			attachedZone = GetComponent<Zone>();
		}

		private void OnDestroy()
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

		public void OnPointerClick (PointerEventData eventData)
		{
			if (inputPermissions.HasFlag(InputPermissions.Click))
			{
				OnClickAction?.Invoke(this, eventData);
				if (!eventData.used)
					OnClickEvent.Invoke();
			}
		}

		public void OnPointerDown (PointerEventData eventData)
		{
			Vector3 cameraPos = eventData.pressEventCamera.transform.position;
			Ray objRay = new Ray(cameraPos, transform.position - cameraPos);
			if (dragPlane.Raycast(objRay, out float distance))
				dragOffset = GetEventWorldposition(eventData) - objRay.GetPoint(distance);
			OnPointerDownAction?.Invoke(this, eventData);
			if (!eventData.used)
				OnPointerDownEvent.Invoke();
		}

		public void OnPointerUp (PointerEventData eventData)
		{
			OnPointerUpAction?.Invoke(this, eventData);
			if (!eventData.used)
				OnPointerUpEvent.Invoke();
		}

		public void OnBeginDrag (PointerEventData eventData)
		{
			if (inputPermissions.HasFlag(InputPermissions.Drag))
			{
				OnBeginDragAction?.Invoke(this, eventData);
				if (!eventData.used)
				{
					if (col)
						col.enabled = false;
					OnBeginDragEvent.Invoke();
				}
			}
		}

		public void OnDrag (PointerEventData eventData)
		{
			if (inputPermissions.HasFlag(InputPermissions.Drag))
			{
				OnDragAction?.Invoke(this, eventData);
				if (!eventData.used)
				{
					transform.position = GetEventWorldposition(eventData) - dragOffset;
					OnDragEvent.Invoke();
				}
			}
		}

		public void OnEndDrag (PointerEventData eventData)
		{
			if (inputPermissions.HasFlag(InputPermissions.Drag))
			{
				OnEndDragAction?.Invoke(this, eventData);
				if (!eventData.used)
				{
					if (col)
						col.enabled = true;
					OnEndDragEvent.Invoke();
				}
			}
		}

		public void OnDrop (PointerEventData eventData)
		{
			if (inputPermissions.HasFlag(InputPermissions.DropInto))
			{
				OnDropAction?.Invoke(this, eventData);
				if (!eventData.used)
					OnDropEvent.Invoke();
			}
		}

		public void OnPointerEnter (PointerEventData eventData)
		{
			if (inputPermissions.HasFlag(InputPermissions.Hover))
			{
				OnPointerEnterAction?.Invoke(this, eventData);
				if (!eventData.used)
					OnPointerEnterEvent.Invoke();
			}
		}

		public void OnPointerExit (PointerEventData eventData)
		{
			if (inputPermissions.HasFlag(InputPermissions.Hover))
			{
				OnPointerExitAction?.Invoke(this, eventData);
				if (!eventData.used)
					OnPointerExitEvent.Invoke();
			}
		}

		public void UseOwnCard ()
		{
			if (attachedCard)
				attachedCard.Use();
		}

		public void UseOwnZone ()
		{
			if (attachedZone)
				attachedZone.Use();
		}

		public void UseOwnCardZone ()
		{
			if (attachedCard && attachedCard.Zone)
				attachedCard.Zone.Use();
		}

		public void OrganizeOwnCardZone ()
		{
			if (attachedCard && attachedCard.Zone)
				attachedCard.Zone.Organize();
		}
	}
}