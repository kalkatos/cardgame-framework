using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageBus : MonoBehaviour
{
	static MessageBus instance;
	public static MessageBus Instance
	{
		get { if (instance == null) instance = new GameObject("MessageBus").AddComponent<MessageBus>(); return instance; }
	}

	Dictionary<string, List<IMessageReceiver>> receivers;
	Dictionary<string, List<IMessageReceiver>> Receivers { get { if (receivers == null) receivers = new Dictionary<string, List<IMessageReceiver>>(); return receivers; } }

	private void Awake()
	{
		if (instance == null)
			instance = this;
		else if (instance != this)
			DestroyImmediate(gameObject);
	}

	public static void Send (string type, params object[] info)
	{
		if (Instance.Receivers.ContainsKey(type))
		{
			for (int i = 0; i < Instance.Receivers[type].Count; i++)
			{
				Instance.Receivers[type][i].TreatMessage(type, info);
			}
		}
	}

	public static void Register (string type, IMessageReceiver receiver)
	{
		if (Instance.Receivers.ContainsKey(type))
			Instance.Receivers[type].Add(receiver);
		else
		{
			List<IMessageReceiver> list = new List<IMessageReceiver>();
			list.Add(receiver);
			Instance.Receivers.Add(type, list);
		}
	}
}
