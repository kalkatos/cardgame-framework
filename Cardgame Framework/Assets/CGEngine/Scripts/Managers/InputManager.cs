using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class InputManager : MonoBehaviour
	{
		static InputManager _instance;
		public static InputManager Instance
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

		public float dragDistanceThreshold = 0.3f;
		public float clickTimeThreshold = 0.25f;
		public float clickDistanceThreshold = 0.05f;
		public Plane dragPlane = new Plane(Vector3.up, Vector3.zero);

		float clickTime;
		Vector3 clickStartPos = Vector3.one * -999;
		Vector3 offset;
		Ray mouseRay;
		float distanceForMouseRay;
		Camera _mainCamera;
		Camera mainCamera { get { if (_mainCamera == null) _mainCamera = Camera.main; return _mainCamera; } }
		Dictionary<InputType, List<IInputEventReceiver>> receivers;
		Dictionary<InputType, List<IInputEventReceiver>> Receivers { get { if (receivers == null) receivers = new Dictionary<InputType, List<IInputEventReceiver>>(); return receivers; } }
		Vector3 mouseWorldPosition;
		public static Vector3 MouseWorldPosition { get { return Instance.mouseWorldPosition; } }

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
		}

		private void Update ()
		{
			UpdateMousePosition();
		}

		public Vector3 GetMouseWorldPositionInPlane (Plane plane)
		{
			plane.Raycast(mouseRay, out float dist);
			return mouseRay.GetPoint(dist);
		}

		public static void Send (InputType type, InputObject inputObject = null)
		{
			if (Instance.Receivers.ContainsKey(InputType.All))
			{
				for (int i = 0; i < Instance.Receivers[InputType.All].Count; i++)
				{
					Instance.Receivers[InputType.All][i].TreatEvent(type, inputObject);
				}
			}

			if (Instance.Receivers.ContainsKey(type))
			{
				for (int i = 0; i < Instance.Receivers[type].Count; i++)
				{
					Instance.Receivers[type][i].TreatEvent(type, inputObject);
				}
			}
		}

		public static void Register (InputType type, IInputEventReceiver receiver)
		{
			if (Instance.Receivers.ContainsKey(type))
				Instance.Receivers[type].Add(receiver);
			else
			{
				List<IInputEventReceiver> list = new List<IInputEventReceiver>();
				list.Add(receiver);
				Instance.Receivers.Add(type, list);
			}
		}

		void UpdateMousePosition ()
		{
			mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
			dragPlane.Raycast(mouseRay, out distanceForMouseRay);
			mouseWorldPosition = mouseRay.GetPoint(distanceForMouseRay);
		}
		/*
		void OnDrawGizmosSelected ()
		{
			if (transform.hasChanged)
			{
				SetWirePoints();
				dragPlane.SetNormalAndPosition(transform.up, transform.position);
			}
			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(bottomLeftCorner, topLeftCorner);
			Gizmos.DrawLine(topLeftCorner, topRightCorner);
			Gizmos.DrawLine(topRightCorner, bottomRightCorner);
			Gizmos.DrawLine(bottomRightCorner, bottomLeftCorner);
		}

		void SetWirePoints ()
		{
			bottomLeftCorner = transform.TransformPoint(new Vector3(-4, 0, -3));
			bottomRightCorner = transform.TransformPoint(new Vector3(4, 0, -3));
			topLeftCorner = transform.TransformPoint(new Vector3(-4, 0, 3));
			topRightCorner = transform.TransformPoint(new Vector3(4, 0, 3));
		}

		public static Vector3 GetMouseWorldPosition(Plane plane)
		{
			float distance;
			plane.Raycast(Instance.mouseRay, out distance);
			return Instance.mouseRay.GetPoint(distance);
		}
		*/
		public void OnMouseUpAsButton (InputObject inputObject)
		{
			//Debug.Log(Vector3.SqrMagnitude(clickStartPos - mouseWorldPosition));
			if (Time.time - clickTime > clickTimeThreshold || Vector3.SqrMagnitude(clickStartPos - mouseWorldPosition) > clickDistanceThreshold)
				return;
			Send(InputType.ObjectClicked, inputObject);
		}

		public void OnMouseDown (InputObject inputObject)
		{
			clickTime = Time.time;
			Vector3 pos = inputObject.transform.position;
			dragPlane.SetNormalAndPosition(-mainCamera.transform.forward, pos);
			UpdateMousePosition();
			clickStartPos = mouseWorldPosition;
			offset = pos - clickStartPos;

			//lastDownObject = inputObject;
			Send(InputType.ObjectCursorDown, inputObject);
		}

		public void OnMouseUp (InputObject inputObject)
		{
			bool wasDragging = inputObject.dragging;
			Send(InputType.ObjectCursorUp, inputObject);
			if (wasDragging)
			{
				OnObjectDrop(inputObject);
				RaycastHit[] hits = Physics.RaycastAll(mouseRay);
				for (int i = 0; i < hits.Length; i++)
				{
					InputObject hitObject = hits[i].collider.GetComponent<InputObject>();
					if (hitObject == inputObject)
						continue;
					if (hitObject)
					{
						OnObjectDropInto(hitObject);
					}
				}


				//InputObject closest = null;
				//float minDistance = float.MaxValue;
				//Vector3 cameraPos = mainCamera.transform.position;
				//for (int i = 0; i < hits.Length; i++)
				//{
				//	InputObject hitObject = hits[i].collider.GetComponent<InputObject>();
				//	if (hitObject == inputObject)
				//		continue;
				//	if (hitObject)
				//	{
				//		float distance = Vector3.Distance(cameraPos, hits[i].point);
				//		if (distance < minDistance)
				//		{
				//			minDistance = distance;
				//			closest = hitObject;
				//		}
				//	}
				//}
				//if (closest != null)
				//{
				//	OnObjectDropInto(closest);
				//}
			}
		}

		public void OnMouseEnter (InputObject inputObject)
		{
			Send(InputType.ObjectCursorEnter, inputObject);
		}

		public void OnMouseExit (InputObject inputObject)
		{
			Send(InputType.ObjectCursorExit, inputObject);
		}

		public void OnMouseOver (InputObject inputObject)
		{
			Send(InputType.ObjectHover, inputObject);
		}

		public void OnMouseDrag (InputObject inputObject)
		{
			if (Vector3.SqrMagnitude(clickStartPos - mouseWorldPosition) > clickDistanceThreshold)
			{
				Send(InputType.ObjectDrag, inputObject);
				inputObject.transform.position = mouseWorldPosition + offset;
			}
		}

		void OnObjectDrop (InputObject inputObject)
		{
			Send(InputType.ObjectDrop, inputObject);
		}

		void OnObjectDropInto (InputObject inputObject)
		{
			if (inputObject.inputPermissions.HasFlag(InputPermissions.DropInto))
				Send(InputType.ObjectDropInto, inputObject);
		}
	}
}