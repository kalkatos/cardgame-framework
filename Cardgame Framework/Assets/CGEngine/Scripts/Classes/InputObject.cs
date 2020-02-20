using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGameFramework
{
	/*
	IPointerEnterHandler - OnPointerEnter - Called when a pointer enters the object
	IPointerExitHandler - OnPointerExit - Called when a pointer exits the object
	IPointerDownHandler - OnPointerDown - Called when a pointer is pressed on the object
	IPointerUpHandler - OnPointerUp - Called when a pointer is released (called on the GameObject that the pointer is clicking)
	IPointerClickHandler - OnPointerClick - Called when a pointer is pressed and released on the same object
	IBeginDragHandler - OnBeginDrag - Called on the drag object when dragging is about to begin
	IDragHandler - OnDrag - Called on the drag object when a drag is happening
	IEndDragHandler - OnEndDrag - Called on the drag object when a drag finishes
	IDropHandler - OnDrop - Called on the object where a drag finishes
	IScrollHandler - OnScroll - Called when a mouse wheel scrolls

	IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, 
	IPointerClickHandler, IBeginDragHandler, IDragHandler,
	IEndDragHandler, IDropHandler, IScrollHandler
	*/

	public class InputObject : MonoBehaviour,
		IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
	IPointerClickHandler, IBeginDragHandler, IDragHandler,
	IEndDragHandler, IDropHandler, IScrollHandler
	{
		public InputPermissions inputPermissions = InputPermissions.Click | InputPermissions.Drag | InputPermissions.Hover | InputPermissions.DropInto;
		public Collider inputCollider { get; private set; }
		public PointerEventData lastEventData;

		private void Awake ()
		{
			inputCollider = GetComponent<Collider>();
			if (!inputCollider)
			{
				Debug.LogError("The InputObject component needs a collider to work properly. Please add one.");
				enabled = false;
			}
		}

		public void OnPointerClick (PointerEventData eventData)
		{
			if (!inputPermissions.HasFlag(InputPermissions.Click))
				return;
			lastEventData = eventData;
			InputManager.Instance.OnPointerClickEvent(eventData, this);
		}

		public void OnPointerDown (PointerEventData eventData)
		{
			lastEventData = eventData;
			InputManager.Instance.OnPointerDownEvent(eventData, this);
		}

		public void OnPointerUp (PointerEventData eventData)
		{
			lastEventData = eventData;
			InputManager.Instance.OnPointerUpEvent(eventData, this);
		}

		public void OnPointerEnter (PointerEventData eventData)
		{
			if (!inputPermissions.HasFlag(InputPermissions.Hover))
				return;
			lastEventData = eventData;
			InputManager.Instance.OnPointerEnterEvent(eventData, this);
		}

		public void OnPointerExit (PointerEventData eventData)
		{
			if (!inputPermissions.HasFlag(InputPermissions.Hover))
				return;
			lastEventData = eventData;
			InputManager.Instance.OnPointerExitEvent(eventData, this);
		}

		public void OnBeginDrag (PointerEventData eventData)
		{
			if (!inputPermissions.HasFlag(InputPermissions.Drag))
				return;
			lastEventData = eventData;
			inputCollider.enabled = false;
			InputManager.Instance.OnBeginDragEvent(eventData, this);
		}

		public void OnDrag (PointerEventData eventData)
		{
			if (!inputPermissions.HasFlag(InputPermissions.Drag))
				return;
			lastEventData = eventData;
			InputManager.Instance.OnDragEvent(eventData, this);
		}

		public void OnEndDrag (PointerEventData eventData)
		{
			if (!inputPermissions.HasFlag(InputPermissions.Drag))
				return;
			lastEventData = eventData;
			InputManager.Instance.OnEndDragEvent(eventData, this);
			inputCollider.enabled = true;
		}

		public void OnDrop (PointerEventData eventData)
		{
			if (!inputPermissions.HasFlag(InputPermissions.DropInto))
				return;
			lastEventData = eventData;
			InputManager.Instance.OnDropEvent(eventData, this);
		}

		public void OnScroll (PointerEventData eventData)
		{
			lastEventData = eventData;
			InputManager.Instance.OnScrollEvent(eventData, this);
		}

		
	}
}