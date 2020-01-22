using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		TestMessage(" x x x @ x x @ x x END", 1, 2);
		TestMessage(" x x x @ x x x x x END", 1, 2);
		TestMessage(" x x x @ x x @ x x END", 1, 2, 3);
		TestMessage("@ x x x @ x x @ x x END @", 1, 2, 3, 4);
		//BuildAndPrint("Foo&Bar|(Clow&Glec)");
		//BuildAndPrint("Foo|Bar&(Clow&Makko)");
		//BuildAndPrint("(Foo|Bar)|(Clow&Glec|Makko)");
		//BuildAndPrint("((Foo|Bar)&(Clow&Glec|Makko))|Masti");
		//BuildAndPrint("Foo&(Glec|Makko))|Masti");
		//BuildAndPrint("Foo&(Glec|Makko|Masti(");
		//BuildAndPrint("(Foo&(Glec|Makko|Masti(");
		//BuildAndPrint("(Foo&(Glec|Makko|)");
	}

	void BuildAndPrint (string clause)
	{
		NestedStrings strings = new NestedStrings();
		strings.Build(clause);
		Debug.Log(clause + "  =>  " + strings);
	}

	void TestMessage (string message, params object[] args)
	{
		Debug.Log(message + "  =>  " + StringUtility.BuildMessage(message, args));
	}
}
