using Harmony;

public static class MenuAccessibilityPatches
{
	public static void MainMenuTransitionPostfix(MainMenuMode.State newState)
	{
		ScreenReaderAccessibilityBehaviour.Instance?.AnnounceMainMenuState(newState);
	}

	public static void ConsoleItemHoverPostfix(ConsoleUIItem __instance)
	{
		ScreenReaderAccessibilityBehaviour.Instance?.AnnounceConsoleFocus(__instance);
	}

	public static void DifficultyRefreshPostfix(DifficultySelectUI __instance)
	{
		ScreenReaderAccessibilityBehaviour.Instance?.AnnounceDifficulty(__instance);
	}

	public static void SaveLoadSetupLoadPostfix()
	{
		ScreenReaderAccessibilityBehaviour.Instance?.AnnounceText("Load game menu.", true);
	}

	public static void SaveLoadSetupSavePostfix()
	{
		ScreenReaderAccessibilityBehaviour.Instance?.AnnounceText("Save game menu.", true);
	}

	public static void PickNameOnEnablePostfix(PickNameUI __instance)
	{
		ScreenReaderAccessibilityBehaviour.Instance?.AnnouncePickName(__instance);
	}

	public static void ChooseBannerShowPostfix()
	{
		ScreenReaderAccessibilityBehaviour.Instance?.AnnounceText("Banner picker opened.", true);
	}

	public static void ChooseBannerClosePostfix()
	{
		ScreenReaderAccessibilityBehaviour.Instance?.AnnounceText("Banner picker closed.", true);
	}

	public static void SettingsMenuEnabledPostfix(SettingsMenuUI __instance)
	{
		ScreenReaderAccessibilityBehaviour.Instance?.AnnounceSettingsMenu(__instance);
	}

	public static void SettingsPreferenceTabPostfix(bool active)
	{
	}

	public static void SettingsGraphicsTabPostfix(bool active)
	{
	}

	public static void SettingsKeyboardTabPostfix(bool active)
	{
	}

	public static void MainMenuStartGamePostfix()
	{
		ScreenReaderAccessibilityBehaviour.Instance?.ApplyPreferredGameplayCameraAngle();
	}

	public static void SetNewModePostfix(GameMode NewMode)
	{
		if (ScreenReaderAccessibilityBehaviour.Instance == null || NewMode == null)
		{
			return;
		}

		if (GameState.inst != null && NewMode == GameState.inst.playingMode)
		{
			ScreenReaderAccessibilityBehaviour.Instance.AnnounceText("Gameplay screen.", true);
		}
		else if (GameState.inst != null && NewMode == GameState.inst.mainMenuMode)
		{
			ScreenReaderAccessibilityBehaviour.Instance.AnnounceText("Main menu.", true);
		}
	}
}
