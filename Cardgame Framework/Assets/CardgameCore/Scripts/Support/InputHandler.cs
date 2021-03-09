using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CardgameCore
{
	[RequireComponent(typeof(Collider))]
	public class InputHandler : MonoBehaviour, IPointerClickHandler //TODO other input events
	{
		public static bool raycasterCheck;
		public static bool eventSystemCheck;
		public static Action<InputHandler, PointerEventData> OnClickAction;

		[SerializeField] private UnityEvent OnClickEvent;

		internal InputPermissions inputPermissions;

		private void Awake()
		{
			if (!raycasterCheck)
			{
				raycasterCheck = true;
				if (!FindObjectOfType<PhysicsRaycaster>())
					Debug.LogWarning("The InputHandler needs a PhysicsRaycaster in the scene to work properly!");
			}
			if (!eventSystemCheck)
			{
				eventSystemCheck = true;
				if (!FindObjectOfType<EventSystem>())
					Debug.LogWarning("The InputHandler needs an EventSystem in the scene to work properly!");
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (inputPermissions.HasFlag(InputPermissions.Click))
			{
				OnClickAction?.Invoke(this, eventData);
				if (!eventData.used)
					OnClickEvent.Invoke();
			}
		}
	}
}