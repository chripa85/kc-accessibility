using System;
using System.Runtime.InteropServices;

public static partial class KCTolk
{
	private static IntPtr tolkHandle = IntPtr.Zero;
	private static IntPtr nvdaHandle = IntPtr.Zero;

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr LoadLibraryW(string lpLibFileName);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool FreeLibrary(IntPtr hLibModule);

	[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
	private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void TolkLoadDelegate();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void TolkUnloadDelegate();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool TolkIsLoadedDelegate();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	private delegate bool TolkOutputDelegate([MarshalAs(UnmanagedType.LPWStr)] string text, bool interrupt);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	private delegate IntPtr TolkDetectScreenReaderDelegate();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool TolkHasSpeechDelegate();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void TolkTrySapiDelegate(bool trySapi);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int NvdaControllerTestIfRunningDelegate();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	private delegate int NvdaControllerSpeakTextDelegate([MarshalAs(UnmanagedType.LPWStr)] string text);

	private static TolkLoadDelegate tolkLoad;
	private static TolkUnloadDelegate tolkUnload;
	private static TolkIsLoadedDelegate tolkIsLoaded;
	private static TolkOutputDelegate tolkOutput;
	private static TolkDetectScreenReaderDelegate tolkDetectScreenReader;
	private static TolkHasSpeechDelegate tolkHasSpeech;
	private static TolkTrySapiDelegate tolkTrySapi;
	private static NvdaControllerTestIfRunningDelegate nvdaControllerTestIfRunning;
	private static NvdaControllerSpeakTextDelegate nvdaControllerSpeakText;

	private static void LoadNvdaController(string nvdaPath)
	{
		nvdaHandle = LoadLibraryW(nvdaPath);
		if (nvdaHandle == IntPtr.Zero)
		{
			InternalLog("LoadLibraryW failed for nvdaControllerClient64.dll. Win32 error: " + Marshal.GetLastWin32Error());
			return;
		}

		try
		{
			nvdaControllerTestIfRunning = GetFunction<NvdaControllerTestIfRunningDelegate>(nvdaHandle, "nvdaController_testIfRunning");
			nvdaControllerSpeakText = GetFunction<NvdaControllerSpeakTextDelegate>(nvdaHandle, "nvdaController_speakText");
			InternalLog("NVDA controller loaded successfully.");
		}
		catch (Exception ex)
		{
			InternalLog("Failed to bind NVDA controller functions: " + ex.Message);
		}
	}

	private static bool LoadAndBindTolk(string tolkPath)
	{
		tolkHandle = LoadLibraryW(tolkPath);
		if (tolkHandle == IntPtr.Zero)
		{
			InternalLog("LoadLibraryW failed for Tolk.dll. Win32 error: " + Marshal.GetLastWin32Error());
			return false;
		}

		tolkLoad = GetFunction<TolkLoadDelegate>("Tolk_Load");
		tolkUnload = GetFunction<TolkUnloadDelegate>("Tolk_Unload");
		tolkIsLoaded = GetFunction<TolkIsLoadedDelegate>("Tolk_IsLoaded");
		tolkOutput = GetFunction<TolkOutputDelegate>("Tolk_Output");
		tolkDetectScreenReader = GetFunction<TolkDetectScreenReaderDelegate>("Tolk_DetectScreenReader");
		tolkHasSpeech = GetFunction<TolkHasSpeechDelegate>("Tolk_HasSpeech");
		tolkTrySapi = GetFunction<TolkTrySapiDelegate>("Tolk_TrySAPI");
		return true;
	}

	private static void ResetNativeBindings()
	{
		tolkLoad = null;
		tolkUnload = null;
		tolkIsLoaded = null;
		tolkOutput = null;
		tolkDetectScreenReader = null;
		tolkHasSpeech = null;
		tolkTrySapi = null;
		nvdaControllerTestIfRunning = null;
		nvdaControllerSpeakText = null;
	}

	private static void ReleaseNativeLibraries()
	{
		if (tolkHandle != IntPtr.Zero)
		{
			FreeLibrary(tolkHandle);
			tolkHandle = IntPtr.Zero;
		}
		if (nvdaHandle != IntPtr.Zero)
		{
			FreeLibrary(nvdaHandle);
			nvdaHandle = IntPtr.Zero;
		}
	}

	private static string GetActiveScreenReaderName()
	{
		if (tolkDetectScreenReader == null)
		{
			return "unknown";
		}
		try
		{
			IntPtr ptr = tolkDetectScreenReader();
			if (ptr == IntPtr.Zero)
			{
				return "none";
			}
			string name = Marshal.PtrToStringUni(ptr);
			return string.IsNullOrEmpty(name) ? "unknown" : name;
		}
		catch
		{
			return "unknown";
		}
	}

	private static bool SafeHasSpeech()
	{
		try
		{
			return tolkHasSpeech != null && tolkHasSpeech();
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNvdaRunning()
	{
		try
		{
			if (nvdaControllerTestIfRunning == null)
			{
				return false;
			}
			int result = nvdaControllerTestIfRunning();
			InternalLog(result == 0 ? "Direct NVDA test: NVDA is running." : "Direct NVDA test returned code " + result + ".");
			return result == 0;
		}
		catch (Exception ex)
		{
			InternalLog("Direct NVDA test failed: " + ex.Message);
			return false;
		}
	}

	private static T GetFunction<T>(string functionName) where T : class
	{
		IntPtr procAddress = GetProcAddress(tolkHandle, functionName);
		if (procAddress == IntPtr.Zero)
		{
			throw new InvalidOperationException("Could not find native function " + functionName);
		}
		return Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T)) as T;
	}

	private static T GetFunction<T>(IntPtr libraryHandle, string functionName) where T : class
	{
		IntPtr procAddress = GetProcAddress(libraryHandle, functionName);
		if (procAddress == IntPtr.Zero)
		{
			throw new InvalidOperationException("Could not find native function " + functionName);
		}
		return Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T)) as T;
	}

	private static string BuildNativePath(string fileName)
	{
		if (string.IsNullOrEmpty(modRootPath))
		{
			return fileName;
		}
		if (modRootPath.EndsWith("/") || modRootPath.EndsWith("\\"))
		{
			return modRootPath + fileName;
		}
		return modRootPath + "/" + fileName;
	}
}
