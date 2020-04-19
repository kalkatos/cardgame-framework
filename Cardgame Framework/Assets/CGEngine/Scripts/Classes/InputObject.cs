using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGameFramework
{

	public class InputObject : MonoBehaviour,
		IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
	IPointerClickHandler, IBeginDragHandler, IDragHandler,
	IEndDragHandler, IDropHandler, IScrollHandler
	{
		public InputPermissions inputPermissions = 0;
		public Collider inputCollider { get; private set; }
		public Card card { get; private set; }
		private Zone _zone;
		public Zone zone { get { if (_zone == null && card) return card.zone; return _zone; } private set { _zone = value; } }
		//public PointerEventData lastEventData;

		private void Awake ()
		{
			inputCollider = GetComponent<Collider>();
			if (!inputCollider)
			{
				Debug.LogError("The InputObject component needs a collider to work properly. Please add one.");
				enabled = false;
			}
			card = GetComponent<Card>();
			zone = GetComponent<Zone>();
		}

		public void OnPointerClick (PointerEventData eventData)
		{
			//lastEventData = eventData;
			InputManager.instance.OnPointerClickEvent(eventData, this);
		}

		public void OnPointerDown (PointerEventData eventData)
		{
			//lastEventData = eventData;
			InputManager.instance.OnPointerDownEvent(eventData, this);
		}

		public void OnPointerUp (PointerEventData eventData)
		{
			//lastEventData = eventData;
			InputManager.instance.OnPointerUpEvent(eventData, this);
		}

		public void OnPointerEnter (PointerEventData eventData)
		{
			//lastEventData = eventData;
			InputManager.instance.OnPointerEnterEvent(eventData, this);
		}

		public void OnPointerExit (PointerEventData eventData)
		{
			//lastEventData = eventData;
			InputManager.instance.OnPointerExitEvent(eventData, this);
		}

		public void OnBeginDrag (PointerEventData eventData)
		{
			//lastEventData = eventData;
			if (inputPermissions.HasFlag(InputPermissions.Drag))
				inputCollider.enabled = false;
			InputManager.instance.OnBeginDragEvent(eventData, this);
		}

		public void OnDrag (PointerEventData eventData)
		{
			//lastEventData = eventData;
			InputManager.instance.OnDragEvent(eventData, this);
		}

		public void OnEndDrag (PointerEventData eventData)
		{
			//lastEventData = eventData;
			InputManager.instance.OnEndDragEvent(eventData, this);
			if (inputPermissions.HasFlag(InputPermissions.Drag))
				inputCollider.enabled = true;
		}

		public void OnDrop (PointerEventData eventData)
		{
			//lastEventData = eventData;
			InputManager.instance.OnDropEvent(eventData, this);
		}

		public void OnScroll (PointerEventData eventData)
		{
			//lastEventData = eventData;
			InputManager.instance.OnScrollEvent(eventData, this);
		}


	}
}