using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[RequireComponent(typeof(Collider))]
	public class InputObject : MonoBehaviour
	{
		public bool interactable = true;

		private void OnMouseUpAsButton ()
		{
			InputManager.Instance.OnMouseUpAsButton(this);
		}

		private void OnMouseDown ()
		{
			InputManager.Instance.OnMouseDown(this);
		}

		private void OnMouseDrag ()
		{
			InputManager.Instance.OnMouseDrag(this);
		}

		private void OnMouseUp ()
		{
			InputManager.Instance.OnMouseUp(this);
		}

		private void OnMouseEnter ()
		{
			InputManager.Instance.OnMouseEnter(this);
		}

		private void OnMouseOver ()
		{
			InputManager.Instance.OnMouseOver(this);
		}

		private void OnMouseExit ()
		{
			InputManager.Instance.OnMouseExit(this);
		}
	}
}