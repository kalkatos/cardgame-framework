using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CGEngine;

public class GameSceneManager : MonoBehaviour, IMessageReceiver
{
	private void Start()
	{
		MessageBus.Register("ObjectClick", this);
	}

	public void TreatMessage(string type, params object[] info)
	{
		switch (type)
		{
			case "ObjectClick":
				Debug.Log("Object clicked");
				break;
		}
	}
}
