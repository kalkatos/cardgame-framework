using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
    public class DragPlane : MonoBehaviour
    {
        public Plane Plane => new Plane(transform.up, transform.position);

		private void OnDrawGizmosSelected ()
		{
			Color oldColor = Gizmos.color;
			Gizmos.color = Color.magenta;
			Vector3 scale = transform.localScale;
			Vector3 upLeft = transform.TransformPoint(-10 * scale.x, 0, 10 * scale.z);
			Vector3 upRight = transform.TransformPoint(10 * scale.x, 0, 10 * scale.z);
			Vector3 downRight = transform.TransformPoint(10 * scale.x, 0, -10 * scale.z);
			Vector3 downLeft = transform.TransformPoint(-10 * scale.x, 0, -10 * scale.z);
			Gizmos.DrawLine(upLeft, upRight);
			Gizmos.DrawLine(upRight, downRight);
			Gizmos.DrawLine(downRight, downLeft);
			Gizmos.DrawLine(downLeft, upLeft);
			Gizmos.color = oldColor;
		}
	}
}
