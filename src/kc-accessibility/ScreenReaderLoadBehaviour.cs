using System;
using UnityEngine;

public class ScreenReaderLoadBehaviour : MonoBehaviour
{
	private EventHandler<OnLoadedEvent> onLoadedHandler;

	private Action<string> log = delegate(string message)
	{
		Debug.Log(message);
	};

	private bool subscribed;

	public void Initialize(Action<string> logger)
	{
		log = logger ?? delegate(string message)
		{
			Debug.Log(message);
		};
		SubscribeIfNeeded();
	}

	private void SubscribeIfNeeded()
	{
		if (subscribed)
		{
			return;
		}
		onLoadedHandler = Broadcast.OnLoadedEvent.Listen(OnSaveLoaded);
		subscribed = true;
		log("Subscribed to Broadcast.OnLoadedEvent.");
	}

	private void OnSaveLoaded(object sender, OnLoadedEvent data)
	{
		KCTolk.Speak("Saved game loaded.", true);
		log("Save-load announcement requested.");
	}

	private void OnApplicationQuit()
	{
		log("OnApplicationQuit received.");
	}

	private void OnDestroy()
	{
		if (subscribed && onLoadedHandler != null)
		{
			Broadcast.OnLoadedEvent.Unlisten(onLoadedHandler);
			subscribed = false;
		}
	}
}
