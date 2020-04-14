using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class EnterPlay : MonoBehaviour
{

	Card card;
	bool active;

	private void Awake ()
	{
		card = GetComponent<Card>();
	}

	private void Update ()
	{
		if (card.zone)
		{
			if (card.zone.zoneTags == "Hand")
			{
				if (transform.position.z > -15)
				{
					if (!active)
					{
						active = true;
						GetComponent<Animator>().SetBool("ToBePlayed", true);
					}
				}
				else
				{
					if (active)
					{
						active = false;
						GetComponent<Animator>().SetBool("ToBePlayed", false);
					}
				}
			}
			else
				GetComponent<Animator>().SetBool("ToBePlayed", false);
		}
		else
		{
			GetComponent<Animator>().SetBool("ToBePlayed", false);
		}
	}

	public void Hover (bool b)
	{
		GetComponent<Animator>().SetBool("Hover", true);
	}
}
