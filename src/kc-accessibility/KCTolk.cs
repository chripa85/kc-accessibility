using System;
using UnityEngine;

public static partial class KCTolk
{
	private const string LogPrefix = "[kc-accessibility] ";

	private static string modRootPath;

	private static Action<string> log = delegate(string message)
	{
		Debug.Log(message);
	};

	private static bool configured;
	private static bool initialized;
	private static bool useDirectNvda;

	public static void Configure(string rootPath, Action<string> logger)
	{
		modRootPath = rootPath;
		log = logger ?? delegate(string message)
		{
			Debug.Log(message);
		};
		configured = true;
	}

	public static bool Initialize()
	{
		if (initialized)
		{
			return true;
		}
		if (!configured)
		{
			InternalLog("Initialize called before Configure.");
			return false;
		}

		string nvdaPath = BuildNativePath("nvdaControllerClient64.dll");
		string tolkPath = BuildNativePath("Tolk.dll");
		InternalLog("Trying to load NVDA controller from " + nvdaPath);
		InternalLog("Trying to load Tolk from " + tolkPath);

		try
		{
			LoadNvdaController(nvdaPath);
			bool nvdaRunning = IsNvdaRunning();
			if (!LoadAndBindTolk(tolkPath))
			{
				return false;
			}

			tolkLoad();
			tolkTrySapi(true);
			initialized = true;

			string activeReader = GetActiveScreenReaderName();
			if (string.Equals(activeReader, "SAPI", StringComparison.OrdinalIgnoreCase) && nvdaRunning && nvdaControllerSpeakText != null)
			{
				useDirectNvda = true;
				InternalLog("Tolk fell back to SAPI while NVDA is running. Switching to direct NVDA output.");
			}
			InternalLog("Tolk initialized. Active reader: " + activeReader + ". Speech available: " + SafeHasSpeech() + ". NVDA running: " + nvdaRunning + ". Direct NVDA: " + useDirectNvda);
			return true;
		}
		catch (Exception ex)
		{
			InternalLog("Tolk initialization failed: " + ex.Message);
			Shutdown();
			return false;
		}
	}

	public static bool Speak(string text, bool interrupt)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (!initialized && !Initialize())
		{
			InternalLog("Speak skipped because Tolk is not initialized.");
			return false;
		}

		try
		{
			if (useDirectNvda && nvdaControllerSpeakText != null)
			{
				int nvdaResult = nvdaControllerSpeakText(text);
				if (nvdaResult == 0)
				{
					return true;
				}
				InternalLog("Direct NVDA speech returned code " + nvdaResult + ". Falling back to Tolk.");
				useDirectNvda = false;
			}

			bool result = tolkOutput != null && tolkOutput(text, interrupt);
			if (!result)
			{
				InternalLog("Tolk did not accept speech output: " + text);
			}
			return result;
		}
		catch (Exception ex)
		{
			InternalLog("Speak failed: " + ex.Message);
			return false;
		}
	}

	public static void Shutdown()
	{
		try
		{
			if (tolkUnload != null && initialized)
			{
				tolkUnload();
			}
		}
		catch (Exception ex)
		{
			InternalLog("Shutdown error: " + ex.Message);
		}
		finally
		{
			initialized = false;
			useDirectNvda = false;
			ResetNativeBindings();
			ReleaseNativeLibraries();
		}
	}

	private static void InternalLog(string message)
	{
		log(LogPrefix + message);
	}
}
