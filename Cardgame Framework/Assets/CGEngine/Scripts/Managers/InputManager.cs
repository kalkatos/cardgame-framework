using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
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

		float clickTime;
		Vector3 clickStartPos = Vector3.one * -999;
		Plane xz = new Plane(Vector3.up, Vector3.zero);
		Ray mouseRay;
		Camera _mainCamera;
		Camera mainCamera { get { if (_mainCamera == null) _mainCamera = Camera.main; return _mainCamera; } }
		Vector3 mouseWorldPosition;
		InputObject lastDownObject;

		private void Awake()
		{
			if (_instance == null)
				_instance = this;
			else if (_instance != this)
				DestroyImmediate(gameObject);
		}
				
		private void Update()
		{
			mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
			float distanceForMouseRay;
			xz.Raycast(mouseRay, out distanceForMouseRay);
			mouseWorldPosition = mouseRay.GetPoint(distanceForMouseRay);
			//mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
		}
		/*
		public static Vector3 GetMouseWorldPosition(Plane plane)
		{
			float distance;
			plane.Raycast(Instance.mouseRay, out distance);
			return Instance.mouseRay.GetPoint(distance);
		}
		*/

		public void OnMouseUpAsButton (InputObject inputObject)
		{
			Debug.Log(Vector3.SqrMagnitude(clickStartPos - mouseWorldPosition));
			if (Time.time - clickTime > clickTimeThreshold || Vector3.SqrMagnitude(clickStartPos - mouseWorldPosition) > clickDistanceThreshold)
				return;
			Debug.Log("Up As Button "+ inputObject.name + " OOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO");
		}

		public void OnMouseDown (InputObject inputObject)
		{
			Debug.Log("Down "+ inputObject.name + " vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv");
			clickTime = Time.time;
			clickStartPos = mouseWorldPosition;
			//lastDownObject = inputObject;
		}

		public void OnMouseUp (InputObject inputObject)
		{
			Debug.Log("Up " + inputObject.name + " ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
			//if (inputObject == lastDownObject && Time.time - clickTime < 0.25f && Vector3.Distance(MatchManager.mouseWorldPosition, clickStartPos) < 0.5f)
			//{
			//	// OBJECT CLICKED
			//	MessageBus.Send(MessageType.ObjectClicked, new Message(inputObject.gameObject));
			//}
		}

		public void OnMouseEnter (InputObject inputObject)
		{
			Debug.Log("Mouse Enter " + inputObject.name + " <<<<<<<<<<<<<<<");
		}

		public void OnMouseExit (InputObject inputObject)
		{
			Debug.Log("Mouse Exit " + inputObject.name + " >>>>>>>>>>>>>>>");
		}

		public void OnMouseOver(InputObject inputObject)
		{
			//Debug.Log("Mouse Over");
		}

		public void OnMouseDrag(InputObject inputObject)
		{
			Debug.Log("Mouse Drag " + inputObject.name + " /\\/\\/\\/\\/\\");
			
		}
	}
}