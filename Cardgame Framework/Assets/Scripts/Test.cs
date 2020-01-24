using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;
using UnityEngine.SceneManagement;
using TMPro;

public class Test : MonoBehaviour
{
	public TMP_InputField sentence;
	public TMP_InputField tags;

	// Start is called before the first frame update
	void Start()
    {
		

		//BuildAndPrint("Foo>16&card=>c(@Madre|Padre)|(Clow>=3&Glec!=16)", true);



		//BuildAndPrint("(Foo|Bar)|(Clow&Glec|Makko)");
		//BuildAndPrint("((Foo|Bar)&(Clow&Glec|Makko))|Masti");
		//BuildAndPrint("Foo&(Glec|Makko))|Masti");
		//BuildAndPrint("Foo&(Glec|Makko|Masti(");
		//BuildAndPrint("(Foo&(Glec|Makko|Masti(");
		//BuildAndPrint("(Foo&(Glec|Makko|)");
	}

	void BuildAndPrint (string clause, bool isComparison)
	{
		NestedStrings strings = new NestedStrings(clause, isComparison);
		Debug.Log(clause + "  =>  " + strings);
	}

	void TestMessage (string message, params object[] args)
	{
		Debug.Log(message + "  =>  " + StringUtility.BuildMessage(message, args));
	}

	void Analyse (string clause, string tags)
	{
		NestedStrings strings = new NestedStrings(clause, false);
		string[] analysing = tags.Split(',');
		Debug.Log(StringUtility.PrintStringArray(analysing));
		strings.PrepareEvaluation(analysing);
		Debug.Log(strings.ToString());
		Debug.Log(strings.Evaluate());
	}

	private void Update ()
	{
		if (Input.GetKeyDown(KeyCode.F1))
			Analyse(sentence.text, tags.text);
	}
}
