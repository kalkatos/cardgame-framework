using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class InputObject : MonoBehaviour
	{
		public InputPermissions inputPermissions = InputPermissions.Click | InputPermissions.Drag | InputPermissions.Hover | InputPermissions.DropInto;
		public bool dragging { get; private set; }
		public Collider inputCollider { get; private set; }

		private void Awake ()
		{
			inputCollider = GetComponent<Collider>();
			if (!inputCollider)
				Debug.LogError("The InputObject component needs a collider to work properly. Please add one.");
		}

		private void OnMouseUpAsButton ()
		{
			if (inputPermissions.HasFlag(InputPermissions.Click))
				InputManager.Instance.OnMouseUpAsButton(this);
		}

		private void OnMouseDown ()
		{
			InputManager.Instance.OnMouseDown(this);
		}

		private void OnMouseDrag ()
		{
			if (inputPermissions.HasFlag(InputPermissions.Drag))
			{
				InputManager.Instance.OnMouseDrag(this);
				dragging = true;
			}
		}

		private void OnMouseUp ()
		{
			InputManager.Instance.OnMouseUp(this);
			dragging = false;
		}

		private void OnMouseEnter ()
		{
			if (inputPermissions.HasFlag(InputPermissions.Hover))
				InputManager.Instance.OnMouseEnter(this);
		}

		private void OnMouseOver ()
		{
			if (inputPermissions.HasFlag(InputPermissions.Hover))
				InputManager.Instance.OnMouseOver(this);
		}

		private void OnMouseExit ()
		{
			if (inputPermissions.HasFlag(InputPermissions.Hover))
				InputManager.Instance.OnMouseExit(this);
			dragging = false;
		}
	}
}