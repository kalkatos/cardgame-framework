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
					_instance = new GameObject("InputManager").AddComponent<InputManager>();
				}
				return _instance;
			}
		}

		public bool dragPlaneIsPositionPlane;

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
		private List<OnPointerClickEventWatcher> onPointerClickEventWatchers = new List<OnPointerClickEventWatcher>();
		private List<OnPointerEnterEventWatcher> onPointerEnterEventWatchers = new List<OnPointerEnterEventWatcher>();
		private List<OnPointerExitEventWatcher> onPointerExitEventWatchers = new List<OnPointerExitEventWatcher>();
		private List<OnPointerDownEventWatcher> onPointerDownEventWatchers = new List<OnPointerDownEventWatcher>();
		private List<OnPointerUpEventWatcher> onPointerUpEventWatchers = new List<OnPointerUpEventWatcher>();
		private List<OnBeginDragEventWatcher> onBeginDragEventWatchers = new List<OnBeginDragEventWatcher>();
		private List<OnDragEventWatcher> onDragEventWatchers = new List<OnDragEventWatcher>();
		private List<OnEndDragEventWatcher> onEndDragEventWatchers = new List<OnEndDragEventWatcher>();
		private List<OnDropEventWatcher> onDropEventWatchers = new List<OnDropEventWatcher>();
		private List<OnScrollEventWatcher> onScrollEventWatchers = new List<OnScrollEventWatcher>();

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

		public static void Register (InputWatcher watcher)
		{
			if (watcher is OnPointerClickEventWatcher)
				instance.onPointerClickEventWatchers.Add((OnPointerClickEventWatcher)watcher);
			if (watcher is OnPointerEnterEventWatcher)
				instance.onPointerEnterEventWatchers.Add((OnPointerEnterEventWatcher)watcher);
			if (watcher is OnPointerExitEventWatcher)
				instance.onPointerExitEventWatchers.Add((OnPointerExitEventWatcher)watcher);
			if (watcher is OnPointerDownEventWatcher)
				instance.onPointerDownEventWatchers.Add((OnPointerDownEventWatcher)watcher);
			if (watcher is OnPointerUpEventWatcher)
				instance.onPointerUpEventWatchers.Add((OnPointerUpEventWatcher)watcher);
			if (watcher is OnBeginDragEventWatcher)
				instance.onBeginDragEventWatchers.Add((OnBeginDragEventWatcher)watcher);
			if (watcher is OnDragEventWatcher)
				instance.onDragEventWatchers.Add((OnDragEventWatcher)watcher);
			if (watcher is OnEndDragEventWatcher)
				instance.onEndDragEventWatchers.Add((OnEndDragEventWatcher)watcher);
			if (watcher is OnDropEventWatcher)
				instance.onDropEventWatchers.Add((OnDropEventWatcher)watcher);
			if (watcher is OnScrollEventWatcher)
				instance.onScrollEventWatchers.Add((OnScrollEventWatcher)watcher);
		}

		public static void Unregister (InputWatcher watcher)
		{
			if (!_instance)
				return;
			if (watcher is OnPointerClickEventWatcher)
				instance.onPointerClickEventWatchers.Remove((OnPointerClickEventWatcher)watcher);
			if (watcher is OnPointerEnterEventWatcher)
				instance.onPointerEnterEventWatchers.Remove((OnPointerEnterEventWatcher)watcher);
			if (watcher is OnPointerExitEventWatcher)
				instance.onPointerExitEventWatchers.Remove((OnPointerExitEventWatcher)watcher);
			if (watcher is OnPointerDownEventWatcher)
				instance.onPointerDownEventWatchers.Remove((OnPointerDownEventWatcher)watcher);
			if (watcher is OnPointerUpEventWatcher)
				instance.onPointerUpEventWatchers.Remove((OnPointerUpEventWatcher)watcher);
			if (watcher is OnBeginDragEventWatcher)
				instance.onBeginDragEventWatchers.Remove((OnBeginDragEventWatcher)watcher);
			if (watcher is OnDragEventWatcher)
				instance.onDragEventWatchers.Remove((OnDragEventWatcher)watcher);
			if (watcher is OnEndDragEventWatcher)
				instance.onEndDragEventWatchers.Remove((OnEndDragEventWatcher)watcher);
			if (watcher is OnDropEventWatcher)
				instance.onDropEventWatchers.Remove((OnDropEventWatcher)watcher);
			if (watcher is OnScrollEventWatcher)
				instance.onScrollEventWatchers.Remove((OnScrollEventWatcher)watcher);
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

		public void OnPointerClickEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			for (int i = 0; i < onPointerClickEventWatchers.Count; i++)
			{
				onPointerClickEventWatchers[i].OnPointerClickEvent(eventData, inputObject);
			}
		}

		public void OnPointerEnterEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			for (int i = 0; i < onPointerEnterEventWatchers.Count; i++)
			{
				onPointerEnterEventWatchers[i].OnPointerEnterEvent(eventData, inputObject);
			}
		}

		public void OnPointerExitEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			for (int i = 0; i < onPointerExitEventWatchers.Count; i++)
			{
				onPointerExitEventWatchers[i].OnPointerExitEvent(eventData, inputObject);
			}
		}

		public void OnPointerDownEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			for (int i = 0; i < onPointerDownEventWatchers.Count; i++)
			{
				onPointerDownEventWatchers[i].OnPointerDownEvent(eventData, inputObject);
			}
		}

		public void OnPointerUpEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			for (int i = 0; i < onPointerUpEventWatchers.Count; i++)
			{
				onPointerUpEventWatchers[i].OnPointerUpEvent(eventData, inputObject);
			}
		}

		public void OnBeginDragEvent (PointerEventData eventData, InputObject inputObject)
		{
			Vector3 pos = inputObject.transform.position;
			Plane objectPlane = new Plane(-mainCamera.transform.forward, pos);
			if (!dragPlaneIsPositionPlane)
				dragPlane.SetNormalAndPosition(-mainCamera.transform.forward, pos);
			UpdateMousePosition();
			RegisterObjects(eventData, inputObject);
			if (inputObject.inputPermissions.HasFlag(InputPermissions.Drag))
			{
				draggedObject = inputObject;
				draggedObjectOffset = GetMouseWorldPositionInPlane(objectPlane) - draggedObject.transform.position;
			}
			for (int i = 0; i < onBeginDragEventWatchers.Count; i++)
			{
				onBeginDragEventWatchers[i].OnBeginDragEvent(eventData, inputObject);
			}
		}

		public void OnDragEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			if (draggedObject)
				draggedObject.transform.position = mouseWorldPosition - draggedObjectOffset;
			for (int i = 0; i < onDragEventWatchers.Count; i++)
			{
				onDragEventWatchers[i].OnDragEvent(eventData, inputObject);
			}
		}

		public void OnEndDragEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			draggedObject = null;
			for (int i = 0; i < onEndDragEventWatchers.Count; i++)
			{
				onEndDragEventWatchers[i].OnEndDragEvent(eventData, inputObject);
			}
		}

		public void OnDropEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);

			for (int i = 0; i < onDropEventWatchers.Count; i++)
			{
				onDropEventWatchers[i].OnDropEvent(eventData, inputObject);
			}
		}

		public void OnScrollEvent (PointerEventData eventData, InputObject inputObject)
		{
			RegisterObjects(eventData, inputObject);
			for (int i = 0; i < onScrollEventWatchers.Count; i++)
			{
				onScrollEventWatchers[i].OnScrollEvent(eventData, inputObject);
			}
		}

		void RegisterObjects (PointerEventData eventData, InputObject inputObject)
		{
			currentEventData = eventData;
			lastEventObject = currentEventObject;
			currentEventObject = inputObject;
		}

		#endregion
	}
}

