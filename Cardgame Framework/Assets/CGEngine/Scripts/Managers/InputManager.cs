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
		float distanceForMouseRay;
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
			xz.Raycast(mouseRay, out distanceForMouseRay);
			mouseWorldPosition = mouseRay.GetPoint(distanceForMouseRay);
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
		}

		public void OnMouseDown (InputObject inputObject)
		{
			clickTime = Time.time;
			clickStartPos = mouseWorldPosition;
			//lastDownObject = inputObject;
		}

		public void OnMouseUp (InputObject inputObject)
		{
			//if (inputObject == lastDownObject && Time.time - clickTime < 0.25f && Vector3.Distance(MatchManager.mouseWorldPosition, clickStartPos) < 0.5f)
			//{
			//	// OBJECT CLICKED
			//	MessageBus.Send(MessageType.ObjectClicked, new Message(inputObject.gameObject));
			//}
		}

		public void OnMouseEnter (InputObject inputObject)
		{
			Debug.Log("Mouse Enter");
		}

		public void OnMouseExit (InputObject inputObject)
		{

		}

		public void OnMouseOver(InputObject inputObject)
		{

		}

		public void OnMouseDrag(InputObject inputObject)
		{
			
		}
	}
}