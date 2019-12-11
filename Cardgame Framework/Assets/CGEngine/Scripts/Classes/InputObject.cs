using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	[RequireComponent(typeof(Collider))]
	public class InputObject : MonoBehaviour
	{
		//float clickTime;
		//Vector3 clickStartPos = Vector3.one * -999;

		private void OnMouseUpAsButton()
		{
			InputManager.Instance.OnMouseUpAsButton(this);
		}

		private void OnMouseDown()
		{
			InputManager.Instance.OnMouseDown(this);
			//clickTime = Time.time;
			//clickStartPos = MatchManager.mouseWorldPosition;
			//MatchManager.Instance.cardInteractionPack.mouseDownCard = this;
		}

		private void OnMouseDrag()
		{
			InputManager.Instance.OnMouseDrag(this);
		}

		private void OnMouseUp()
		{
			InputManager.Instance.OnMouseUp(this);
		}

		private void OnMouseEnter()
		{
			InputManager.Instance.OnMouseEnter(this);
		}

		private void OnMouseOver()
		{
			InputManager.Instance.OnMouseOver(this);
		}

		private void OnMouseExit()
		{
			InputManager.Instance.OnMouseExit(this);
		}
	}
}