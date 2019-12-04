using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CGEngine
{
	public class ActionUIPrompter : MatchWatcher
	{
		public static ActionUIPrompter Instance;
		
		public Button[] buttons;
		//List<CardUsingBox> activatedBoxes = new List<CardUsingBox>();

		void Awake ()
		{
			if (Instance == null)
				Instance = this;
			else if (Instance != this)
				Destroy(gameObject);
		}

		public override IEnumerator TreatTrigger(string triggerTag, params object[] args)
		{
			switch (triggerTag)
			{
				case "OnPhaseStarted":
					Player p = (Player)GetArgumentWithTag("activePlayer", args);
					if (p.userType == UserType.Local)
					{
						TurnPhase phase = (TurnPhase)GetArgumentWithTag("phaseObject", args);
						if (phase == null)
							break;
						if (phase.allowedActions != null)
						{
							for (int i = 0; i < phase.allowedActions.Length; i++)
							{
								string action = phase.allowedActions[i];
								buttons[i].gameObject.SetActive(true);
								buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = action;
								buttons[i].onClick.AddListener(delegate { Match.Current.UseAction(action); });
							}
						}
						//if (!string.IsNullOrEmpty(phase.usableCards))
						//{
						//	List<Card> selection = Match.Current.SelectCards(phase.usableCards);
						//	if (selection != null)
						//	{
						//		for (int i = 0; i < selection.Count; i++)
						//		{
						//			CardUsingBox cardBox = selection[i].GetComponentInChildren<CardUsingBox>();
						//			if (!cardBox)
						//			{
						//				cardBox = ((GameObject)Instantiate(Resources.Load("CardBox"))).GetComponent<CardUsingBox>();
						//				cardBox.AttachToCard(selection[i]);
						//			}
						//			else
						//			{
						//				cardBox.gameObject.SetActive(true);
						//			}
						//			activatedBoxes.Add(cardBox);
						//		}
						//	}
						//}
					}
					break;
				case "OnPhaseEnded":
					for (int i = 0; i < buttons.Length; i++)
					{
						buttons[i].onClick.RemoveAllListeners();
						buttons[i].gameObject.SetActive(false);
					}
					//for	(int i = 0; i < activatedBoxes.Count; i++)
					//{
					//	activatedBoxes[i].gameObject.SetActive(false);
					//}
					//activatedBoxes.Clear();
					break;
				default:
					break;
			}
			yield return null;
		}
		
		/*
		public Card mouseDown;
		public Card mouseUp;
		public Card mouseOver;
		public Card mouseEnter;
		public Card mouseExit;
		public Card mouseDrag;
		public Card mouseClick;

		float mouseDownTime;
		Vector3 mouseDownOffset;
		Camera mainCamera;
		Card dragCard;

		private void Start()
		{
			mainCamera = Camera.main;
		}

		void Update ()
		{
			if (mouseDown)
			{
				Debug.Log("Mouse down in " + mouseDown.name);
				//code here
				mouseDownTime = Time.time;
				mouseDownOffset = mainCamera.ScreenToWorldPoint(Input.mousePosition) - mouseDown.transform.position;
				mouseDownOffset.y = 0;
				mouseDown = null;
			}
			if (mouseDrag)
			{
				//mouseDown.SetCollider(false);
				//dragCard = mouseDrag;
				Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
				mousePosition.y = mouseDrag.transform.position.y;
				mouseDrag.transform.position = mousePosition - mouseDownOffset;
				mouseDrag = null;
			}
			if (dragCard)
			{
				Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
				mousePosition.y = mouseDrag.transform.position.y;
				mouseDrag.transform.position = mousePosition - mouseDownOffset;
			}
			if (mouseClick)
			{
				if (Time.time - mouseDownTime <= 0.2f)
				{
					Debug.Log("Clicked " + mouseClick.name);
					Match.Current.UseCard(mouseClick);
					mouseClick = null;
				}
			}
			if (mouseUp)
			{
				Debug.Log("Mouse up in " + mouseUp.name);
				//code here
				if (mouseDrag == mouseUp)
					mouseDrag = null;
				dragCard = null;
				mouseClick = null;
				mouseUp = null;
				mouseDownOffset = Vector3.zero;
			}
			if (mouseEnter)
			{
				Debug.Log("Mouse enter " + mouseEnter.name);
				//code here
				mouseEnter = null;
			}
			if (mouseExit)
			{
				Debug.Log("Mouse exit " + mouseExit.name);
				//code here
				if (mouseOver == mouseExit)
					mouseOver = null;
				mouseExit = null;
			}
		}
		*/
	}
}