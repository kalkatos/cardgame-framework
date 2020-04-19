using UnityEngine.EventSystems;

namespace CardGameFramework
{
	public interface InputWatcher { }

	public interface OnPointerClickEventWatcher : InputWatcher
	{
		void OnPointerClickEvent (PointerEventData eventData, InputObject inputObject);
	}
	public interface OnPointerDownEventWatcher : InputWatcher
	{
		void OnPointerDownEvent (PointerEventData eventData, InputObject inputObject);
	}
	public interface OnPointerUpEventWatcher : InputWatcher
	{
		void OnPointerUpEvent (PointerEventData eventData, InputObject inputObject);
	}
	public interface OnBeginDragEventWatcher : InputWatcher
	{
		void OnBeginDragEvent (PointerEventData eventData, InputObject inputObject);
	}
	public interface OnDragEventWatcher : InputWatcher
	{
		void OnDragEvent (PointerEventData eventData, InputObject inputObject);
	}
	public interface OnEndDragEventWatcher : InputWatcher
	{
		void OnEndDragEvent (PointerEventData eventData, InputObject inputObject);
	}
	public interface OnPointerEnterEventWatcher : InputWatcher
	{
		void OnPointerEnterEvent (PointerEventData eventData, InputObject inputObject);
	}
	public interface OnPointerExitEventWatcher : InputWatcher
	{
		void OnPointerExitEvent (PointerEventData eventData, InputObject inputObject);
	}
	public interface OnDropEventWatcher : InputWatcher
	{
		void OnDropEvent (PointerEventData eventData, InputObject inputObject);
	}
	public interface OnScrollEventWatcher : InputWatcher
	{
		void OnScrollEvent (PointerEventData eventData, InputObject inputObject);
	}

}