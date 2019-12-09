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
			
		}

		private void OnMouseUp()
		{
			
		}

		private void OnMouseEnter()
		{
			
		}

		private void OnMouseOver()
		{
			
		}

		private void OnMouseExit()
		{
			
		}
	}
}