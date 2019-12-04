using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageBus : MonoBehaviour
{
	static MessageBus _instance;
	public static MessageBus instance
	{
		get
		{
			if (_instance == null)
			{
				GameObject go = new GameObject("MessageBus");
				_instance = go.AddComponent<MessageBus>();
			}
			return _instance;
		}
	}

	Dictionary<MessageType, List<IMessageReceiver>> _receivers;
	Dictionary<MessageType, List<IMessageReceiver>> receivers
	{
		get
		{
			if (_receivers == null)
				_receivers = new Dictionary<MessageType, List<IMessageReceiver>>();
			return _receivers;
		}
	}

	private void Awake()
	{
		if (_instance == null)
			_instance = this;
		else if (_instance != this)
			DestroyImmediate(gameObject);
	}

	public static void Send (MessageType type, Message msg)
	{
		if (!instance.receivers.ContainsKey(type))
		{
			//Debug.LogWarning("There is no receiver for message of type " + type);
			return;
		}

		if (instance.receivers[type] == null)
			instance.receivers[type] = new List<IMessageReceiver>();

		for (int i = 0; i < instance.receivers[type].Count; i++)
		{
			instance.receivers[type][i].TreatMessage(type, msg);
		}
	}
	
	public static void Register (MessageType type, IMessageReceiver receiver)
	{
		if (receiver == null || type == MessageType.None)
			return;

		if (type.HasFlag(MessageType.GameStart))
		{
			if (!instance.receivers.ContainsKey(MessageType.GameStart))
				instance.receivers.Add(MessageType.GameStart, new List<IMessageReceiver>());
			instance.receivers[MessageType.GameStart].Add(receiver);
		}

		if (type.HasFlag(MessageType.MatchStart))
		{
			if (!instance.receivers.ContainsKey(MessageType.MatchStart))
				instance.receivers.Add(MessageType.MatchStart, new List<IMessageReceiver>());
			instance.receivers[MessageType.MatchStart].Add(receiver);
		}

		if (type.HasFlag(MessageType.MatchEnd))
		{
			if (!instance.receivers.ContainsKey(MessageType.MatchEnd))
				instance.receivers.Add(MessageType.MatchEnd, new List<IMessageReceiver>());
			instance.receivers[MessageType.MatchEnd].Add(receiver);
		}

		if (type.HasFlag(MessageType.GameEnd))
		{
			if (!instance.receivers.ContainsKey(MessageType.GameEnd))
				instance.receivers.Add(MessageType.GameEnd, new List<IMessageReceiver>());
			instance.receivers[MessageType.GameEnd].Add(receiver);
		}

		if (type.HasFlag(MessageType.CardEnterZone))
		{
			if (!instance.receivers.ContainsKey(MessageType.CardEnterZone))
				instance.receivers.Add(MessageType.CardEnterZone, new List<IMessageReceiver>());
			instance.receivers[MessageType.CardEnterZone].Add(receiver);
		}

		if (type.HasFlag(MessageType.CardLeaveZone))
		{
			if (!instance.receivers.ContainsKey(MessageType.CardLeaveZone))
				instance.receivers.Add(MessageType.CardLeaveZone, new List<IMessageReceiver>());
			instance.receivers[MessageType.CardLeaveZone].Add(receiver);
		}

		if (type.HasFlag(MessageType.CardUsed))
		{
			if (!instance.receivers.ContainsKey(MessageType.CardUsed))
				instance.receivers.Add(MessageType.CardUsed, new List<IMessageReceiver>());
			instance.receivers[MessageType.CardUsed].Add(receiver);
		}
	}

	public static void Unregister (IMessageReceiver receiver)
	{
		if (receiver == null)
			return;

		foreach (List<IMessageReceiver> item in instance.receivers.Values)
		{
			//if (instance.receivers[item].Contains(receiver))
				item.Remove(receiver);
		}
	}
}