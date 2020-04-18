using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CardGameFramework
{
	public class InputManager : MonoBehaviour
	{
		static InputManager _instance;
		public static InputManager instance
		{
			get
			{
				if (_instance == null)
				{
					GameObject go = new GameObject("InputManager");
					_instance = go.AddComponent<InputManager>();
				}
				return _instance;
			}
		}

		public bool dragPlaneIsPositionPlane;

		public UnityEvent onPointerClickEvent;
		public UnityEvent onPointerEnterEvent;
		public UnityEvent onPointerExitEvent;
		public UnityEvent onPointerDownEvent;
		public UnityEvent onPointerUpEvent;
		public UnityEvent onBeginDragEvent;
		public UnityEvent onDragEvent;
		public UnityEvent onEndDragEvent;
		public UnityEvent onDropEvent;
		public UnityEvent onScrollEvent;

		public Vector3 mouseWorldPosition { get; private set; }
		public PointerEventData currentEventData { get; private set; }
		public InputObject lastEventObject { get; private set; }
		public InputObject currentEventObject { get; private set; }
		public InputObject draggedObject { get; private set; }

		private float distanceForMouseRay;
		private Ray mouseRay;
		private Camera _mainCamera;
		private Camera mainCamera
		{
			get
			{
				if (_mainCamera == null)
					_mainCamera = Camera.main;
				return _mainCamera;
			}
		}
		private Plane dragPlane = new Plane(Vector3.up, Vector3.zero);
		private Vector3 draggedObjectOffset = Vector3.zero;

		private void Awake ()
		{
			if (_instance == null)
			{
				_instance = this;
			}
			else if (_instance != this)
			{
				DestroyImmediate(gameObject);
				return;
			}

			if (dragPlaneIsPositionPlane)
				dragPlane = new Plane(transform.up, transform.position);

			if (!mainCamera.GetComponent<PhysicsRaycaster>())
				Debug.LogWarning("[CGEngine] Warning: The camera needs a PhysicsRaycaster component for inputs to be registered.");

			if (!FindObjectOfType<EventSystem>())
				Debug.LogWarning("[CGEngine] Warning: An EventSystem object is needed for input events.");
		}

		protected virtual void Update ()
		{
			UpdateMousePosition();
		}

		private void OnDestroy ()
		{
			onPointerClickEvent.RemoveAllListeners();
			onPointerEnterEvent.RemoveAllListeners();
			onPointerExitEvent.RemoveAllListeners();
			onPointerDownEvent.RemoveAllListeners();
			onPointerUpEvent.RemoveAllListeners();
			onBeginDragEvent.RemoveAllListeners();
			onDragEvent.RemoveAllListeners();
			onEndDragEvent.RemoveAllListeners();
			onDropEvent.RemoveAllListeners();
			onScrollEvent.RemoveAllListeners();
		}

		public Vector3 GetMouseWorldPositionInPlane (Plane plane)
		{
			plane.Raycast(mouseRay, out float dist);
			return mouseRay.GetPoint(dist);
		}

		void UpdateMousePosition ()
		{
			mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
			dragPlane.Raycast(mouseRay, out distanceForMouseRay);
			mouseWorldPosition = mouseRay.GetPoint(distanceForMouseRay);
		}

		#region Input Methods ===========================================================================================================

		public virtual void OnPointerClickEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			onPointerClickEvent.Invoke();
		}

		public virtual void OnPointerDownEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			onPointerDownEvent.Invoke();
		}

		public virtual void OnPointerUpEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			onPointerUpEvent.Invoke();
		}

		public virtual void OnBeginDragEvent (PointerEventData eventData, InputObject inputObject)
		{
			Vector3 pos = inputObject.transform.position;
			if (!dragPlaneIsPositionPlane)
				dragPlane.SetNormalAndPosition(-mainCamera.transform.forward, pos);
			UpdateMousePosition();
			RegisterObjects(eventData, inputObject);
			if (inputObject.inputPermissions.HasFlag(InputPermissions.Drag))
			{
				draggedObject = inputObject;
				draggedObjectOffset = mouseWorldPosition - draggedObject.transform.position;
			}
			onBeginDragEvent.Invoke();
		}

		public virtual void OnDragEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			if (draggedObject)
				draggedObject.transform.position = mouseWorldPosition + draggedObjectOffset;
			onDragEvent.Invoke();
		}

		public virtual void OnEndDragEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			draggedObject = null;
			onEndDragEvent.Invoke();
		}

		public virtual void OnPointerEnterEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			onPointerEnterEvent.Invoke();
		}

		public virtual void OnPointerExitEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			onPointerExitEvent.Invoke();
		}

		public virtual void OnDropEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			onDropEvent.Invoke();
		}

		public virtual void OnScrollEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			onScrollEvent.Invoke();
		}

		#endregion

		void RegisterObjects (PointerEventData eventData, InputObject inputObject)
		{
			currentEventData = eventData;
			lastEventObject = currentEventObject;
			currentEventObject = inputObject;
		}

		// HELPER METHODS

		public void UseDraggingCard ()
		{
			if (currentEventData.dragging && currentEventData.pointerDrag.TryGetComponent(out Card card))
				card.Use();
		}

		public void UseObjectIfCard ()
		{
			if (currentEventObject.TryGetComponent(out Card card))
				card.Use();
		}

		public void UseObjectIfZone ()
		{
			if (currentEventObject.TryGetComponent(out Zone zone))
				zone.Use();
		}

		public void UseObjectParentIfZone ()
		{
			if (currentEventObject.transform.parent != null && currentEventObject.transform.parent.TryGetComponent(out Zone zone))
				zone.Use();
		}

	}
}
