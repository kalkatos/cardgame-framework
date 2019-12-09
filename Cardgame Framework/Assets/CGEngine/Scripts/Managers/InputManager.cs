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

		float clickTime;
		Vector3 clickStartPos = Vector3.one * -999;
		Plane xz = new Plane(Vector3.up, Vector3.zero);
		Ray mouseRay;
		Camera mainCamera;
		Vector3 mouseWorldPosition;
		InputObject lastDownObject;

		private void Awake()
		{
			if (_instance == null)
				_instance = this;
			else if (_instance != this)
				DestroyImmediate(gameObject);
		}

		public void Initialize()
		{
			mainCamera = Camera.main;
		}

		private void Update()
		{
			mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
			float distanceForMouseRay;
			xz.Raycast(mouseRay, out distanceForMouseRay);
			mouseWorldPosition = mouseRay.GetPoint(distanceForMouseRay);
			//mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
		}

		public static Vector3 GetMouseWorldPosition(Plane plane)
		{
			float distance;
			plane.Raycast(Instance.mouseRay, out distance);
			return Instance.mouseRay.GetPoint(distance);
		}

		public void OnMouseDown (InputObject inputObject)
		{
			clickTime = Time.time;
			clickStartPos = mouseWorldPosition;
			lastDownObject = inputObject;
		}

		public void OnMouseUp (InputObject inputObject)
		{
			if (inputObject == lastDownObject && Time.time - clickTime < 0.25f && Vector3.Distance(MatchManager.mouseWorldPosition, clickStartPos) < 0.5f)
			{
				// OBJECT CLICKED
				MessageBus.Send(MessageType.ObjectClicked, new Message());
			}
		}

		public void OnMouseEnter (InputObject inputObject)
		{

		}

		public void OnMouseExit (InputObject inputObject)
		{

		}

		
	}
}