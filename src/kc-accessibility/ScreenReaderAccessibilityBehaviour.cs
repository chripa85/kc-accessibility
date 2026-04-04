using System;
using System.Collections.Generic;
using System.Reflection;
using Assets;
using Harmony;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ScreenReaderAccessibilityBehaviour : MonoBehaviour
{
	private const string HarmonyId = "kc.accessibility.menu";
	private const float RepeatSuppressionSeconds = 0.2f;
	private const float GameplayPanelTransitionSuppressionSeconds = 0.35f;
	private const float RepeatedCommandWindowSeconds = 1f;
	private const float PendingGameplayCommandTimeoutSeconds = 3f;
	private const float PreferredGameplayCameraTheta = 90f;
	private const float PreferredGameplayCameraPhi = 50f;
	private static readonly bool InputDebugLogging = true;
	private static readonly string[] PrimaryBuildCategoryTitles = new[]
	{
		"Castle",
		"Town",
		"AdvTown",
		"Food",
		"Industry",
		"Maritime"
	};
	private static readonly KeyCode[] DigitKeys = new[]
	{
		KeyCode.Alpha1,
		KeyCode.Alpha2,
		KeyCode.Alpha3,
		KeyCode.Alpha4,
		KeyCode.Alpha5,
		KeyCode.Alpha6,
		KeyCode.Alpha7,
		KeyCode.Alpha8,
		KeyCode.Alpha9,
		KeyCode.Alpha0
	};
	private static readonly KeyCode[] KeypadDigitKeys = new[]
	{
		KeyCode.Keypad1,
		KeyCode.Keypad2,
		KeyCode.Keypad3,
		KeyCode.Keypad4,
		KeyCode.Keypad5,
		KeyCode.Keypad6,
		KeyCode.Keypad7,
		KeyCode.Keypad8,
		KeyCode.Keypad9,
		KeyCode.Keypad0
	};

	private static readonly Dictionary<MainMenuMode.State, string> MainMenuStateNames = new Dictionary<MainMenuMode.State, string>
	{
		{ MainMenuMode.State.Menu, "Main menu." },
		{ MainMenuMode.State.ChooseMode, "Choose mode." },
		{ MainMenuMode.State.ChooseDifficulty, "Choose difficulty." },
		{ MainMenuMode.State.NewMap, "New map." },
		{ MainMenuMode.State.NameAndBanner, "Name and banner." },
		{ MainMenuMode.State.PauseMenu, "Pause menu." },
		{ MainMenuMode.State.SettingsMenu, "Settings menu." },
		{ MainMenuMode.State.Save, "Save game menu." },
		{ MainMenuMode.State.Load, "Load game menu." },
		{ MainMenuMode.State.QuitConfirm, "Return to main menu confirmation." },
		{ MainMenuMode.State.ExitConfirm, "Exit game confirmation." },
		{ MainMenuMode.State.LoadError, "Load error." },
		{ MainMenuMode.State.SendSave, "Send save screen." },
		{ MainMenuMode.State.Credits, "Credits." },
		{ MainMenuMode.State.Failure, "Failure screen." },
		{ MainMenuMode.State.KeepDestroyed, "Keep destroyed screen." },
		{ MainMenuMode.State.BannerSelect, "Banner select." },
		{ MainMenuMode.State.GameWorkshopUI, "Workshop." },
		{ MainMenuMode.State.RivalChoiceUI, "Rival settings." },
		{ MainMenuMode.State.KingdomShareFromMenu, "Kingdom share menu." },
		{ MainMenuMode.State.KingdomShareFromGame, "Kingdom share game menu." }
	};

	private HarmonyInstance harmony;
	private ModKeyBindings modKeyBindings;
	private KeyChord[] originalKeyboardSettings;
	private bool nativeKeyboardBindingsDisabled;
	private Action<string> log = delegate(string _)
	{
	};
	private bool keyboardNavigationEnabled;

	public static ScreenReaderAccessibilityBehaviour Instance { get; private set; }

	public void Initialize(Action<string> logger)
	{
		log = logger ?? delegate(string _)
		{
		};
		Instance = this;
		EnsureNavigationRoutesInitialized();
		modKeyBindings = ModKeyBindings.LoadOrCreate(log);
		TryDisableNativeKeyboardBindings();
		InstallHarmony();
	}

	private void TryDisableNativeKeyboardBindings()
	{
		if (nativeKeyboardBindingsDisabled || Settings.inst == null || Settings.inst.KeyboardSettings == null)
		{
			return;
		}

		KeyChord[] current = Settings.inst.KeyboardSettings;
		originalKeyboardSettings = new KeyChord[current.Length];
		KeyChord.CopyTo(current, originalKeyboardSettings);

		for (int i = 0; i < current.Length; i++)
		{
			current[i] = KeyChord.None;
		}

		nativeKeyboardBindingsDisabled = true;
		log("Disabled native keyboard bindings for accessibility mod session.");
	}

	private void RestoreNativeKeyboardBindingsIfNeeded()
	{
		if (!nativeKeyboardBindingsDisabled || originalKeyboardSettings == null || Settings.inst == null)
		{
			return;
		}

		if (Settings.inst.KeyboardSettings == null || Settings.inst.KeyboardSettings.Length != originalKeyboardSettings.Length)
		{
			Settings.inst.KeyboardSettings = new KeyChord[originalKeyboardSettings.Length];
		}

		KeyChord.CopyTo(originalKeyboardSettings, Settings.inst.KeyboardSettings);
		nativeKeyboardBindingsDisabled = false;
		log("Restored native keyboard bindings.");
	}

	private void InstallHarmony()
	{
		if (harmony != null)
		{
			return;
		}
		try
		{
			harmony = HarmonyInstance.Create(HarmonyId);
			HarmonyPostfixRoute[] routes = GetHarmonyPostfixRoutes();
			for (int i = 0; i < routes.Length; i++)
			{
				HarmonyPostfixRoute route = routes[i];
				PatchPostfix(route.TargetType, route.TargetMethodName, route.PatchType, route.PatchMethodName, route.ParameterTypes);
			}
			log("Installed menu accessibility patches.");
		}
		catch (Exception ex)
		{
			log("Failed to install menu accessibility patches: " + ex.Message);
		}
	}

	public void AnnounceMainMenuState(MainMenuMode.State state)
	{
		bool stateChanged = !lastAnnouncedState.HasValue || lastAnnouncedState.Value != state;
		if (stateChanged)
		{
			topLevelMenuIndex = -1;
			chooseModeIndex = -1;
			chooseDifficultyIndex = -1;
			nameBannerIndex = -1;
			newMapIndex = -1;
			settingsMenuIndex = -1;
			pauseMenuIndex = -1;
			confirmationDialogIndex = -1;
			activeTextField = null;
			activeTextFieldLabel = string.Empty;
			lastAnnouncedState = state;
		}
		if (state == MainMenuMode.State.SettingsMenu || state == MainMenuMode.State.NameAndBanner)
		{
			return;
		}
		if (state == MainMenuMode.State.PauseMenu)
		{
			if (!stateChanged)
			{
				return;
			}
			List<Button> pauseButtons = GetPauseMenuButtons();
			pauseMenuIndex = pauseButtons.Count > 0 ? 0 : -1;
			AnnounceText(JoinParts("Pause menu", pauseButtons.Count > 0 ? DescribePauseMenuButton(pauseButtons[0]) : string.Empty), true);
			return;
		}
		if (state == MainMenuMode.State.QuitConfirm || state == MainMenuMode.State.ExitConfirm)
		{
			if (!stateChanged)
			{
				return;
			}
			List<Button> confirmationButtons = GetConfirmationDialogButtons();
			Button backButton = GetConfirmationDialogBackButton(confirmationButtons);
			confirmationDialogIndex = backButton != null ? confirmationButtons.IndexOf(backButton) : (confirmationButtons.Count > 0 ? 0 : -1);
			AnnounceText(JoinParts(GetConfirmationDialogPrompt(), backButton != null ? JoinParts(GetComponentLabel(backButton), "button") : string.Empty), true);
			return;
		}
		string message;
		if (!MainMenuStateNames.TryGetValue(state, out message))
		{
			message = SplitIdentifier(state.ToString()) + ".";
		}
		AnnounceText(message, true);
	}

	public void AnnounceConsoleFocus(ConsoleUIItem item)
	{
		if (item == null || !item.gameObject.activeInHierarchy)
		{
			return;
		}
		if (IsPlayingModeActive() && !gameplayKeyboardNavigationActive)
		{
			return;
		}
		if (suppressedHoverAnnouncements > 0 || ShouldSuppressGenericHoverAnnouncements())
		{
			return;
		}
		string message = DescribeConsoleItem(item);
		AnnounceText(message, false);
	}

	public void AnnounceDifficulty(DifficultySelectUI difficultyUi)
	{
		if (difficultyUi == null || !difficultyUi.gameObject.activeInHierarchy)
		{
			return;
		}
		string title = CleanText(difficultyUi.title != null ? difficultyUi.title.text : string.Empty);
		string description = CleanText(difficultyUi.desc != null ? difficultyUi.desc.text : string.Empty);
		AnnounceText(JoinParts(title, description), true);
	}

	public void AnnouncePickName(PickNameUI pickNameUi)
	{
		if (pickNameUi == null || !pickNameUi.gameObject.activeInHierarchy)
		{
			return;
		}
		string cityName = CleanText(pickNameUi.cityNameInput != null ? pickNameUi.cityNameInput.text : string.Empty);
		string message = string.IsNullOrEmpty(cityName) ? "Name and banner. City name field is blank." : "Name and banner. City name: " + cityName + ".";
		AnnounceText(message, true);
	}

	public void AnnounceSettingsMenu(SettingsMenuUI settingsMenuUi)
	{
		if (settingsMenuUi == null || !settingsMenuUi.gameObject.activeInHierarchy)
		{
			return;
		}
		List<Component> items = GetSettingsMenuItems(settingsMenuUi);
		settingsMenuIndex = items.Count > 0 ? 0 : -1;
		if (settingsMenuIndex >= 0)
		{
			FocusSettingsComponent(items[settingsMenuIndex], announce: false, playSound: false);
		}
		AnnounceText(JoinParts("Settings menu", GetActiveSettingsTabName(settingsMenuUi) + " tab", settingsMenuIndex >= 0 ? DescribeSettingsComponent(items[settingsMenuIndex]) : string.Empty), true);
	}

	public void AnnounceText(string message, bool interrupt)
	{
		message = CleanText(message);
		if (string.IsNullOrEmpty(message))
		{
			return;
		}
		float now = Time.realtimeSinceStartup;
		if (message == lastAnnouncement && now - lastAnnouncementTime < RepeatSuppressionSeconds)
		{
			return;
		}
		lastAnnouncement = message;
		lastAnnouncementTime = now;
		KCTolk.Speak(message, interrupt);
		log("Accessibility announcement: " + message);
	}

	public void ApplyPreferredGameplayCameraAngle()
	{
		if (Cam.inst == null)
		{
			return;
		}

		Cam.inst.desiredTheta = PreferredGameplayCameraTheta;
		Cam.inst.desiredPhi = PreferredGameplayCameraPhi;
		log("Applied preferred gameplay camera angle. theta=" + PreferredGameplayCameraTheta + " phi=" + PreferredGameplayCameraPhi);
	}

	private void Update()
	{
		LogNavigationKeyPressesIfAny();
		ResetGameplayNavigationStateIfNeeded();
		if (IsPlayingModeActive() && TryHandlePlayingModeNavigation())
		{
			LogInputHandled("Playing mode navigation");
			return;
		}
		string handledBy;
		if (TryHandleMainMenuNavigation(out handledBy))
		{
			LogInputHandled(handledBy);
		}
	}

	private void PatchPostfix(Type targetType, string targetMethodName, Type patchType, string patchMethodName, Type[] parameterTypes)
	{
		MethodInfo target = AccessTools.Method(targetType, targetMethodName, parameterTypes);
		MethodInfo postfix = AccessTools.Method(patchType, patchMethodName);
		if (target == null)
		{
			throw new MissingMethodException(targetType.FullName, targetMethodName);
		}
		if (postfix == null)
		{
			throw new MissingMethodException(patchType.FullName, patchMethodName);
		}
		harmony.Patch(target, null, new HarmonyMethod(postfix), null);
		log("Patched " + targetType.Name + "." + targetMethodName + " with " + patchMethodName + ".");
	}

	private void EnsureKeyboardNavigationEnabled()
	{
		if (keyboardNavigationEnabled)
		{
			return;
		}
		keyboardNavigationEnabled = true;
		try
		{
			suppressedHoverAnnouncements++;
			PrimeGamepadControlForKeyboardNavigation();
			log("Enabled keyboard-driven accessibility navigation.");
		}
		catch (Exception ex)
		{
			log("Failed to enable keyboard-driven accessibility navigation: " + ex.Message);
		}
		finally
		{
			suppressedHoverAnnouncements = Math.Max(0, suppressedHoverAnnouncements - 1);
		}
	}

	private bool IsNavigateUpPressed()
	{
		return IsActionPressed(AccessibilityAction.NavigateUp, KeyCode.UpArrow);
	}

	private bool IsNavigateDownPressed()
	{
		return IsActionPressed(AccessibilityAction.NavigateDown, KeyCode.DownArrow);
	}

	private bool IsNavigateLeftPressed()
	{
		return IsActionPressed(AccessibilityAction.NavigateLeft, KeyCode.LeftArrow);
	}

	private bool IsNavigateRightPressed()
	{
		return IsActionPressed(AccessibilityAction.NavigateRight, KeyCode.RightArrow);
	}

	private bool IsNavigateNextPressed()
	{
		if (IsActionPressed(AccessibilityAction.NavigateNext))
		{
			return true;
		}
		return Input.GetKeyDown(KeyCode.Tab) && !IsShiftHeld();
	}

	private bool IsNavigatePreviousPressed()
	{
		if (IsActionPressed(AccessibilityAction.NavigatePrevious))
		{
			return true;
		}
		return Input.GetKeyDown(KeyCode.Tab) && IsShiftHeld();
	}

	private bool IsSubmitPressed()
	{
		return IsActionPressed(AccessibilityAction.Submit, KeyCode.Return, KeyCode.KeypadEnter);
	}

	private bool IsSubmitAlternatePressed()
	{
		return IsActionPressed(AccessibilityAction.SubmitAlternate, KeyCode.Space);
	}

	private bool IsCancelPressed()
	{
		return IsActionPressed(AccessibilityAction.Cancel, KeyCode.Escape, KeyCode.Backspace);
	}

	private bool IsOpenBuildMenuPressed()
	{
		return IsActionPressed(AccessibilityAction.OpenBuildMenu) && !IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsResourceMenuPressed()
	{
		return IsActionPressed(AccessibilityAction.ResourceMenu) && !IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsResourceValuePromptPressed()
	{
		return IsActionPressed(AccessibilityAction.ResourceMenu) && IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsKeepAnchorPressed()
	{
		return IsActionPressed(AccessibilityAction.KeepAnchor) && !IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsIslandInfoPressed()
	{
		return IsActionPressed(AccessibilityAction.IslandInfo) && !IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsCycleIslandsPressed()
	{
		return IsActionPressed(AccessibilityAction.IslandInfo) && IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsCycleMatchingTilesPressed()
	{
		return IsActionPressed(AccessibilityAction.CycleMatchingTiles) && !IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsMeshAwarenessPressed()
	{
		return IsActionPressed(AccessibilityAction.MeshAwareness) && !IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsDirectionalMeshPressed()
	{
		return IsActionPressed(AccessibilityAction.MeshAwareness) && IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsSearchPressed()
	{
		return Input.GetKeyDown(KeyCode.G) && IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsSetBookmarkPressed()
	{
		return IsCtrlHeld() && !IsAltHeld() && !IsShiftHeld() && TryGetPressedDigitSlot(out _);
	}

	private bool IsJumpBookmarkPressed()
	{
		return !IsCtrlHeld() && !IsAltHeld() && !IsShiftHeld() && TryGetPressedDigitSlot(out _);
	}

	private bool IsNamedBookmarkPressed()
	{
		return Input.GetKeyDown(KeyCode.M) && IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsDirectionalScanPressed()
	{
		return IsAltHeld() && !IsCtrlHeld() && (IsNavigateUpPressed() || IsNavigateDownPressed() || IsNavigateLeftPressed() || IsNavigateRightPressed());
	}

	private bool IsDirectionalSummaryPressed()
	{
		return IsCtrlHeld() && !IsAltHeld() && (IsNavigateUpPressed() || IsNavigateDownPressed() || IsNavigateLeftPressed() || IsNavigateRightPressed());
	}

	private bool IsIncreaseSpeedPressed()
	{
		return Input.GetKeyDown(KeyCode.Equals) && !IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsDecreaseSpeedPressed()
	{
		return Input.GetKeyDown(KeyCode.Minus) && !IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsChopShortcutPressed()
	{
		return IsActionPressed(AccessibilityAction.ChopShortcut) && !IsCtrlHeld() && !IsAltHeld();
	}

	private bool IsFastMoveModifierHeld()
	{
		if (modKeyBindings != null && modKeyBindings.IsHeld(AccessibilityAction.FastMoveModifier))
		{
			return true;
		}
		return IsShiftHeld();
	}

	private bool IsActionPressed(AccessibilityAction action, params KeyCode[] fallbackKeys)
	{
		bool allowCustomBindings = IsPlayingModeActive();
		if (allowCustomBindings && modKeyBindings != null && modKeyBindings.IsPressed(action))
		{
			return true;
		}
		if (fallbackKeys == null)
		{
			return false;
		}
		for (int i = 0; i < fallbackKeys.Length; i++)
		{
			if (Input.GetKeyDown(fallbackKeys[i]))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsActionPressed(AccessibilityAction action)
	{
		if (!IsPlayingModeActive())
		{
			return false;
		}
		return modKeyBindings != null && modKeyBindings.IsPressed(action);
	}

	private static bool IsShiftHeld()
	{
		return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
	}

	private static bool IsCtrlHeld()
	{
		return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
	}

	private static bool IsAltHeld()
	{
		return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
	}

	private bool HasKeyboardNavigationInput()
	{
		return IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateLeftPressed()
			|| IsNavigateRightPressed()
			|| IsNavigateNextPressed()
			|| IsNavigatePreviousPressed()
			|| IsSubmitPressed()
			|| IsSubmitAlternatePressed()
			|| IsCancelPressed()
			|| IsOpenBuildMenuPressed()
			|| IsResourceMenuPressed()
			|| IsResourceValuePromptPressed()
			|| IsChopShortcutPressed()
			|| IsIslandInfoPressed()
			|| IsCycleIslandsPressed()
			|| IsKeepAnchorPressed()
			|| IsCycleMatchingTilesPressed()
			|| IsMeshAwarenessPressed()
			|| IsDirectionalMeshPressed()
			|| IsDirectionalScanPressed()
			|| IsDirectionalSummaryPressed()
			|| IsSearchPressed()
			|| IsSetBookmarkPressed()
			|| IsNamedBookmarkPressed()
			|| IsJumpBookmarkPressed()
			|| IsIncreaseSpeedPressed()
			|| IsDecreaseSpeedPressed();
	}

	private static bool IsMainMenuActive()
	{
		return GameState.inst != null && GameState.inst.CurrMode == GameState.inst.mainMenuMode;
	}

	private static bool IsPlayingModeActive()
	{
		return GameState.inst != null && GameState.inst.CurrMode == GameState.inst.playingMode;
	}

	private static ConsoleUIItem GetFocusedItem()
	{
		ConsoleUIItem[] items = FindObjectsOfType<ConsoleUIItem>();
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i].isHovered && items[i].gameObject.activeInHierarchy)
			{
				return items[i];
			}
		}
		return null;
	}

	private static ConsoleUIItem GetRootItemForCurrentMenu()
	{
		if (!IsMainMenuActive())
		{
			return null;
		}
		MainMenuMode mainMenu = GameState.inst.mainMenuMode;
		switch (mainMenu.GetState())
		{
			case MainMenuMode.State.PauseMenu:
				return GetItem(mainMenu.pauseMenuUI);
			case MainMenuMode.State.QuitConfirm:
			case MainMenuMode.State.ExitConfirm:
				return mainMenu.QuitConfirmation != null ? mainMenu.QuitConfirmation.GetComponent<ConsoleUIItem>() : null;
			case MainMenuMode.State.Menu:
				return GetItem(mainMenu.topLevelUI);
			case MainMenuMode.State.ChooseMode:
				return GetItem(mainMenu.chooseModeUI);
			case MainMenuMode.State.ChooseDifficulty:
				return GetItem(mainMenu.chooseDifficultyUI);
			case MainMenuMode.State.GameWorkshopUI:
				return GetItem(mainMenu.gameWorkshopUI);
			case MainMenuMode.State.NewMap:
			{
				NewMapUI newMap = mainMenu.newMapUI != null ? mainMenu.newMapUI.GetComponent<NewMapUI>() : null;
				if (newMap == null)
				{
					return GetItem(mainMenu.newMapUI);
				}
				if (newMap.InEditMode)
				{
					return GetItem(newMap.MapEditContainer);
				}
				if (newMap.confirmButtonContainer != null && newMap.confirmButtonContainer.activeInHierarchy)
				{
					return GetItem(newMap.confirmButtonContainer);
				}
				return GetItem(newMap.NewMapSettingsContainer);
			}
			case MainMenuMode.State.NameAndBanner:
				if (mainMenu.chooseBannerUI != null && mainMenu.chooseBannerUI.activeInHierarchy)
				{
					return GetItem(mainMenu.chooseBannerUI);
				}
				return GetItem(mainMenu.nameBannerUI);
			case MainMenuMode.State.BannerSelect:
				return GetItem(mainMenu.chooseBannerUI);
			case MainMenuMode.State.SettingsMenu:
			{
				SettingsMenuUI settings = mainMenu.settingsUI != null ? mainMenu.settingsUI.GetComponent<SettingsMenuUI>() : null;
				return settings != null ? settings.entryItem : GetItem(mainMenu.settingsUI);
			}
			case MainMenuMode.State.Save:
			case MainMenuMode.State.Load:
				return mainMenu.saveLoadUI != null ? mainMenu.saveLoadUI.GetComponent<ConsoleUIItem>() : null;
			case MainMenuMode.State.Credits:
			{
				CreditsUI credits = mainMenu.creditsUI != null ? mainMenu.creditsUI.GetComponent<CreditsUI>() : null;
				return credits != null && credits.backButton != null ? credits.backButton.GetComponent<ConsoleUIItem>() : GetItem(mainMenu.creditsUI);
			}
			default:
				return null;
		}
	}

	private static ConsoleUIItem GetItem(GameObject obj)
	{
		return obj != null ? obj.GetComponent<ConsoleUIItem>() : null;
	}

	private void OnDestroy()
	{
		if (ReferenceEquals(Instance, this))
		{
			Instance = null;
		}
		RestoreNativeKeyboardBindingsIfNeeded();
		if (harmony != null)
		{
			try
			{
				harmony.UnpatchAll();
			}
			catch (Exception ex)
			{
				log("Failed to remove menu accessibility patches: " + ex.Message);
			}
			harmony = null;
		}
	}

	private void LogNavigationKeyPressesIfAny()
	{
		if (!InputDebugLogging)
		{
			return;
		}
		string keys = DescribePressedNavigationKeys();
		if (string.IsNullOrEmpty(keys))
		{
			return;
		}

		string mode = "Other";
		if (IsMainMenuActive())
		{
			mode = "MainMenu:" + GameState.inst.mainMenuMode.GetState();
		}
		else if (IsPlayingModeActive())
		{
			mode = "PlayingMode";
		}

		ConsoleUIItem focused = GetFocusedItem();
		string focusName = focused != null ? focused.name : "none";
		log("InputDebug keys=[" + keys + "] mode=" + mode + " playingNav=" + gameplayKeyboardNavigationActive + " focus=" + focusName + ".");
	}

	private void LogInputHandled(string handler)
	{
		if (!InputDebugLogging)
		{
			return;
		}
		log("InputDebug handled by " + handler + ".");
	}

	private string DescribePressedNavigationKeys()
	{
		List<string> keys = new List<string>(20);
		bool digitPressed = TryGetPressedDigitSlot(out _);
		AddPressedAction(keys, IsNavigateUpPressed(), "Up");
		AddPressedAction(keys, IsNavigateDownPressed(), "Down");
		AddPressedAction(keys, IsNavigateLeftPressed(), "Left");
		AddPressedAction(keys, IsNavigateRightPressed(), "Right");
		AddPressedAction(keys, IsNavigateNextPressed(), "Next");
		AddPressedAction(keys, IsNavigatePreviousPressed(), "Previous");
		AddPressedAction(keys, IsSubmitPressed(), "Submit");
		AddPressedAction(keys, IsSubmitAlternatePressed(), "SubmitAlt");
		AddPressedAction(keys, IsCancelPressed(), "Cancel");
		AddPressedAction(keys, IsOpenBuildMenuPressed(), "OpenBuildMenu");
		AddPressedAction(keys, IsResourceMenuPressed(), "ResourceMenu");
		AddPressedAction(keys, IsResourceValuePromptPressed(), "ResourceValue");
		AddPressedAction(keys, IsChopShortcutPressed(), "Chop");
		AddPressedAction(keys, IsIslandInfoPressed(), "IslandInfo");
		AddPressedAction(keys, IsCycleIslandsPressed(), "CycleIslands");
		AddPressedAction(keys, IsKeepAnchorPressed(), "KeepAnchor");
		AddPressedAction(keys, IsCycleMatchingTilesPressed(), IsShiftHeld() ? "CycleMatchPrevious" : "CycleMatchNext");
		AddPressedAction(keys, IsMeshAwarenessPressed(), "MeshAwareness");
		AddPressedAction(keys, IsDirectionalMeshPressed(), "DirectionalMesh");
		AddPressedAction(keys, IsDirectionalScanPressed(), "DirectionalScan");
		AddPressedAction(keys, IsDirectionalSummaryPressed(), "DirectionalSummary");
		AddPressedAction(keys, IsSearchPressed(), "Search");
		AddPressedAction(keys, IsSetBookmarkPressed() && pendingGameplayCommand == PendingGameplayCommand.None, "SetBookmark");
		AddPressedAction(keys, IsNamedBookmarkPressed(), "NamedBookmark");
		AddPressedAction(keys, pendingGameplayCommand == PendingGameplayCommand.ResourceValue && digitPressed, "ResourceValue");
		AddPressedAction(keys, IsJumpBookmarkPressed() && pendingGameplayCommand == PendingGameplayCommand.None, "JumpBookmark");
		AddPressedAction(keys, IsIncreaseSpeedPressed(), "IncreaseSpeed");
		AddPressedAction(keys, IsDecreaseSpeedPressed(), "DecreaseSpeed");
		return keys.Count == 0 ? string.Empty : string.Join(",", keys);
	}

	private static bool TryGetPressedDigitSlot(out int slot)
	{
		for (int i = 0; i < DigitKeys.Length; i++)
		{
			if (Input.GetKeyDown(DigitKeys[i]) || Input.GetKeyDown(KeypadDigitKeys[i]))
			{
				slot = i;
				return true;
			}
		}

		slot = -1;
		return false;
	}

	private static void AddPressedAction(List<string> keys, bool isPressed, string name)
	{
		if (isPressed)
		{
			keys.Add(name);
		}
	}
}
