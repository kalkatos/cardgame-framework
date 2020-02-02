﻿using UnityEngine;
using CardGameFramework;
using TMPro;
using System.Collections;

public class Test : MatchWatcher
{
	public TMP_InputField sentence;
	public TMP_InputField tags;
	public CardGameData game;
	public Getter getter;
	public Command command;
	NestedConditions cond = null;

	// Start is called before the first frame update
	void Start ()
	{
		if (PlayerPrefs.HasKey("sentence"))
			sentence.text = PlayerPrefs.GetString("sentence");
		if (PlayerPrefs.HasKey("tags"))
			tags.text = PlayerPrefs.GetString("tags");
		/*
		if (game)
			CGEngine.StartMatch(game, game.rules[0]);
		else
			Debug.Log("Game missing!");
		*/
		//cond = new NestedConditions("clickedCard=>c(@Play)");



	}

	void BuildAndPrint (string clause, bool isComparison)
	{
		NestedStrings strings = new NestedStrings(clause, isComparison);
		Debug.Log(clause + "  =>  " + strings);
	}

	void Analyse (string clause, string tags)
	{
		NestedStrings strings = new NestedStrings(clause, false);
		string[] analysing = tags.Split(',');
		Debug.Log(StringUtility.PrintStringArray(analysing));
		bool result = strings.Evaluate(analysing);
		Debug.Log(strings.ToString());
		Debug.Log(result);
	}

	public void DoMath ()
	{
		MathGetter math = new MathGetter(sentence.text);
		Debug.Log(sentence.text + "  =>  " + math.Get());
		PlayerPrefs.SetString("sentence", sentence.text);
		PlayerPrefs.SetString("tags", tags.text);
	}

	public void AnalyseSentenceWithTags ()
	{
		Analyse(sentence.text, tags.text);
		PlayerPrefs.SetString("sentence", sentence.text);
		PlayerPrefs.SetString("tags", tags.text);
	}

	public void PrepareASelectorWithSentence ()
	{
		getter = Getter.Build(sentence.text);
		PlayerPrefs.SetString("sentence", sentence.text);
		PlayerPrefs.SetString("tags", tags.text);
	}

	public void SelectWithSelectorPrepared ()
	{
		if (getter.GetType() == typeof(CardSelector))
		{
			Card[] selected = (Card[])getter.Get();
			for (int i = 0; i < selected.Length; i++)
			{
				Debug.Log("Card " + i + " = " + selected[i].ToString());
			}
		}
		else
		{
			Debug.Log(getter.Get());
		}
	}

	public void PrepareACommand ()
	{
		command = Match.Current.CreateCommand(sentence.text);
		PlayerPrefs.SetString("sentence", sentence.text);
		PlayerPrefs.SetString("tags", tags.text);
	}

	public void ExecuteCommandPrepared ()
	{
		PrepareACommand();
		StartCoroutine(command.Execute());
	}

	public override IEnumerator TreatTrigger (TriggerTag triggerTag, params object[] args)
	{
		if (triggerTag == TriggerTag.OnCardClicked)
		{
			Debug.Log(cond.Evaluate());
		}
		yield return null;
	}
}
