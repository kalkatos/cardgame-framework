using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendMessageTest : MonoBehaviour
{
    private void OnTagAdded (string tag)
	{
		Debug.Log("Got message with tag: " + tag);
	}
}
