using System;
using System.Reflection;
using UnityEngine;

public class ScreenReaderLoadMod
{
	private const string InitialAnnouncement = "Kingdoms and Castles loaded.";
	private const string BuildStamp = "0.2.4";

	private static KCModHelper helper;

	private static GameObject hostObject;

	private static bool initialAnnouncementSent;

	private void Preload(KCModHelper modHelper)
	{
		helper = modHelper;
		Log("Preload started. Build " + BuildStamp + ". Game version " + Application.version + ". Unity " + Application.unityVersion + ". Mod path " + modHelper.modPath + ". Language " + GetCurrentLanguageName() + ".");
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

	private static string GetCurrentLanguageName()
	{
		try
		{
			Type localizationManagerType = Type.GetType("I2.Loc.LocalizationManager, Assembly-CSharp-firstpass")
				?? Type.GetType("I2.Loc.LocalizationManager, Assembly-CSharp");
			if (localizationManagerType == null)
			{
				return "unknown";
			}

			PropertyInfo currentLanguageProperty = localizationManagerType.GetProperty("CurrentLanguage", BindingFlags.Public | BindingFlags.Static);
			if (currentLanguageProperty != null)
			{
				object value = currentLanguageProperty.GetValue(null, null);
				string language = value as string;
				if (!string.IsNullOrEmpty(language))
				{
					return language;
				}
			}

			FieldInfo currentLanguageField = localizationManagerType.GetField("CurrentLanguage", BindingFlags.Public | BindingFlags.Static);
			if (currentLanguageField != null)
			{
				object value = currentLanguageField.GetValue(null);
				string language = value as string;
				if (!string.IsNullOrEmpty(language))
				{
					return language;
				}
			}
		}
		catch
		{
		}

		return "unknown";
	}
}
