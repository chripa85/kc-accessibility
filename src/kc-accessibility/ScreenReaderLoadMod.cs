using UnityEngine;

public class ScreenReaderLoadMod
{
	private const string InitialAnnouncement = "Kingdoms and Castles loaded.";
	private const string BuildStamp = "0.2.3";

	private static KCModHelper helper;

	private static GameObject hostObject;

	private static bool initialAnnouncementSent;

	private void Preload(KCModHelper modHelper)
	{
		helper = modHelper;
		Log("Preload started. Build " + BuildStamp + ". Game version " + Application.version + ". Unity " + Application.unityVersion + ".");
		KCTolk.Configure(modHelper.modPath, Log);
		bool initialized = KCTolk.Initialize();
		Log("Preload finished. Tolk initialized: " + initialized);
	}

	private void SceneLoaded(KCModHelper modHelper)
	{
		helper = modHelper;
		Log("SceneLoaded received.");
		EnsureHost();
		if (!initialAnnouncementSent)
		{
			initialAnnouncementSent = true;
			KCTolk.Speak(InitialAnnouncement, true);
			Log("Initial load announcement requested.");
		}
	}

	private static void EnsureHost()
	{
		if (hostObject != null)
		{
			return;
		}
		hostObject = new GameObject("kc-accessibility-host");
		UnityEngine.Object.DontDestroyOnLoad(hostObject);
		ScreenReaderLoadBehaviour behaviour = hostObject.AddComponent<ScreenReaderLoadBehaviour>();
		behaviour.Initialize(Log);
		ScreenReaderAccessibilityBehaviour accessibility = hostObject.AddComponent<ScreenReaderAccessibilityBehaviour>();
		accessibility.Initialize(Log);
		Log("Persistent load listener created.");
	}

	private static void Log(string message)
	{
		string fullMessage = "[kc-accessibility] " + message;
		if (helper != null)
		{
			helper.Log(fullMessage);
		}
		else
		{
			Debug.Log(fullMessage);
		}
	}
}
