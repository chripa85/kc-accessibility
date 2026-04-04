using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Code;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ScreenReaderAccessibilityBehaviour
{
	private static readonly FieldInfo GameUiCursorModeField = typeof(GameUI).GetField("currCursorMode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
	private static readonly FieldInfo GamepadControlIsControllerActiveField = typeof(GamepadControl).GetField("isControllerActive", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
	private static readonly FieldInfo GamepadControlControllerUpdateField = typeof(GamepadControl).GetField("controllerUpdate", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
	private const int SettingsSaveIndex = -2;

	private sealed class GameplayTileElement
	{
		public Building Building;
		public IMoveableUnit Unit;

		public bool IsBuilding => Building != null;
		public bool IsUnit => Unit != null;
	}

	private sealed class AccessiblePanelItem
	{
		public string Speech;
		public Action Activate;
	}

	// Helper methods for label extraction, focus descriptions, and per-screen component actions.
	private string DescribeConsoleItem(ConsoleUIItem item)
	{
		string label = ExtractPrimaryLabel(item.gameObject);
		string role = ExtractRoleAndState(item.gameObject);
		string tooltip = ExtractTooltip(item);
		return JoinParts(label, role, tooltip);
	}

	private string ExtractPrimaryLabel(GameObject obj)
	{
		TMP_InputField input = obj.GetComponent<TMP_InputField>() ?? obj.GetComponentInChildren<TMP_InputField>(includeInactive: false);
		if (input != null)
		{
			string currentValue = CleanText(input.text);
			return string.IsNullOrEmpty(currentValue) ? "Text field" : currentValue;
		}

		TextMeshProUGUI[] tmpTexts = obj.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: false);
		for (int i = 0; i < tmpTexts.Length; i++)
		{
			string value = CleanText(tmpTexts[i].text);
			if (!string.IsNullOrEmpty(value))
			{
				return value;
			}
		}

		Text[] legacyTexts = obj.GetComponentsInChildren<Text>(includeInactive: false);
		for (int j = 0; j < legacyTexts.Length; j++)
		{
			string value2 = CleanText(legacyTexts[j].text);
			if (!string.IsNullOrEmpty(value2))
			{
				return value2;
			}
		}

		Localize[] localizeComponents = obj.GetComponentsInChildren<Localize>(includeInactive: false);
		for (int k = 0; k < localizeComponents.Length; k++)
		{
			string term = localizeComponents[k].Term;
			if (!string.IsNullOrEmpty(term))
			{
				string translated = CleanText(LocalizationManager.GetTranslation(term));
				if (!string.IsNullOrEmpty(translated))
				{
					return translated;
				}
			}
		}

		return SplitIdentifier(obj.name);
	}

	private string ExtractRoleAndState(GameObject obj)
	{
		Toggle toggle = obj.GetComponent<Toggle>() ?? obj.GetComponentInChildren<Toggle>(includeInactive: false);
		if (toggle != null)
		{
			return toggle.isOn ? "toggle on" : "toggle off";
		}

		Slider slider = obj.GetComponent<Slider>() ?? obj.GetComponentInChildren<Slider>(includeInactive: false);
		if (slider != null)
		{
			float percent = Mathf.Approximately(slider.maxValue, slider.minValue) ? 0f : Mathf.InverseLerp(slider.minValue, slider.maxValue, slider.value) * 100f;
			return "slider " + Mathf.RoundToInt(percent) + " percent";
		}

		TMP_InputField input = obj.GetComponent<TMP_InputField>() ?? obj.GetComponentInChildren<TMP_InputField>(includeInactive: false);
		if (input != null)
		{
			string value = CleanText(input.text);
			return string.IsNullOrEmpty(value) ? "text field blank" : "text field";
		}

		Button button = obj.GetComponent<Button>() ?? obj.GetComponentInChildren<Button>(includeInactive: false);
		if (button != null)
		{
			return button.interactable ? "button" : "button unavailable";
		}

		return string.Empty;
	}

	private string ExtractTooltip(ConsoleUIItem item)
	{
		string tooltip = CleanText(item.GetTooltip());
		if (!string.IsNullOrEmpty(tooltip))
		{
			return tooltip;
		}

		TooltipHook hook = item.GetComponent<TooltipHook>() ?? item.GetComponentInChildren<TooltipHook>(includeInactive: false);
		if (hook != null)
		{
			return CleanText(hook.toolTipText);
		}

		return string.Empty;
	}

	private static string JoinParts(params string[] parts)
	{
		return AccessibilityTextUtilities.JoinParts(parts);
	}

	private static string CleanText(string value)
	{
		return AccessibilityTextUtilities.CleanText(value);
	}

	private static string SplitIdentifier(string value)
	{
		return AccessibilityTextUtilities.SplitIdentifier(value);
	}

	private void FocusItem(ConsoleUIItem item)
	{
		FocusItem(item, announce: true);
	}

	private void FocusItem(ConsoleUIItem item, bool announce)
	{
		if (item == null)
		{
			return;
		}
		ConsoleUIItem current = GetFocusedItem();
		if (current != null && current != item)
		{
			current.Dehover();
		}
		suppressedHoverAnnouncements++;
		try
		{
			item.Hover();
		}
		finally
		{
			suppressedHoverAnnouncements = Math.Max(0, suppressedHoverAnnouncements - 1);
		}
		if (announce)
		{
			AnnounceConsoleFocus(item);
		}
	}





	private ConsoleUIItem GetConsoleItemForComponent(Component component)
	{
		if (component == null)
		{
			return null;
		}
		return component.GetComponent<ConsoleUIItem>() ?? component.GetComponentInParent<ConsoleUIItem>();
	}

	private bool ShouldSuppressGenericHoverAnnouncements()
	{
		if (Time.realtimeSinceStartup < genericHoverAnnouncementsSuppressedUntil)
		{
			return true;
		}

		if (!keyboardNavigationEnabled)
		{
			return false;
		}

		if (IsPlayingModeActive())
		{
			return gameplayKeyboardNavigationActive && HasActiveAccessibleGameplayPanel();
		}

		if (!IsMainMenuActive())
		{
			return false;
		}
		MainMenuMode.State state = GameState.inst.mainMenuMode.GetState();
		return state == MainMenuMode.State.Menu
			|| state == MainMenuMode.State.ChooseMode
			|| state == MainMenuMode.State.ChooseDifficulty
			|| state == MainMenuMode.State.NameAndBanner
			|| state == MainMenuMode.State.NewMap
			|| state == MainMenuMode.State.SettingsMenu
			|| state == MainMenuMode.State.PauseMenu
			|| state == MainMenuMode.State.QuitConfirm
			|| state == MainMenuMode.State.ExitConfirm;
	}

	private bool HasActiveAccessibleGameplayPanel()
	{
		if (BuildUI.inst != null && BuildUI.inst.Visible)
		{
			return true;
		}

		if (AdvisorUI.inst != null && AdvisorUI.inst.IsVisible())
		{
			return true;
		}

		if (PersonUI.inst != null && PersonUI.inst.Visible)
		{
			return true;
		}

		if (DecreeUI.inst != null && DecreeUI.inst.Visible)
		{
			return true;
		}

		if (GameUI.inst == null)
		{
			return false;
		}

		if (GameUI.inst.workerUI != null && GameUI.inst.workerUI.Visible)
		{
			return true;
		}

		if (GameUI.inst.constructUI != null && GameUI.inst.constructUI.Visible)
		{
			return true;
		}

		return IsIslandInfoPanelVisible(GameUI.inst);
	}

	private void PrimeGamepadControlForKeyboardNavigation()
	{
		GamepadControl control = GamepadControl.inst;
		if (control == null)
		{
			return;
		}

		if (GamepadControlIsControllerActiveField != null)
		{
			GamepadControlIsControllerActiveField.SetValue(
				GamepadControlIsControllerActiveField.IsStatic ? null : (object)control,
				true);
		}

		if (GamepadControlControllerUpdateField != null)
		{
			Delegate controllerUpdate = GamepadControlControllerUpdateField.GetValue(
				GamepadControlControllerUpdateField.IsStatic ? null : (object)control) as Delegate;
			controllerUpdate?.DynamicInvoke(true);
		}

		control.PrepForGamepad();
	}

	private static bool IsIslandInfoPanelVisible(GameUI gameUi)
	{
		return gameUi != null
			&& ((gameUi.islandInfoUI != null && gameUi.islandInfoUI.isShowing)
				|| (gameUi.foreignIslandInfoUI != null && gameUi.foreignIslandInfoUI.isShowing));
	}

	private static ConsoleUIItem GetVisibleIslandInfoEntry(GameUI gameUi)
	{
		if (gameUi == null)
		{
			return null;
		}

		if (gameUi.islandInfoUI != null && gameUi.islandInfoUI.isShowing && gameUi.islandInfoUI.consoleItemEntry != null)
		{
			return gameUi.islandInfoUI.consoleItemEntry;
		}

		if (gameUi.foreignIslandInfoUI != null && gameUi.foreignIslandInfoUI.isShowing && gameUi.foreignIslandInfoUI.consoleItemEntry != null)
		{
			return gameUi.foreignIslandInfoUI.consoleItemEntry;
		}

		return null;
	}

	private void SuppressGenericHoverAnnouncementsFor(float seconds)
	{
		genericHoverAnnouncementsSuppressedUntil = Mathf.Max(genericHoverAnnouncementsSuppressedUntil, Time.realtimeSinceStartup + Mathf.Max(0f, seconds));
	}

	private void InvokeWithHoverAnnouncementSuppressed(Action action)
	{
		suppressedHoverAnnouncements++;
		SuppressGenericHoverAnnouncementsFor(GameplayPanelTransitionSuppressionSeconds);
		try
		{
			action?.Invoke();
		}
		finally
		{
			suppressedHoverAnnouncements = Math.Max(0, suppressedHoverAnnouncements - 1);
		}
	}

	private void PlayNavigationSound()
	{
		try
		{
			SfxSystem.PlayUiSelect();
		}
		catch (Exception ex)
		{
			log("Failed to play navigation sound: " + ex.Message);
		}
	}





	private static bool IsFocusedWithin(ConsoleUIItem current, ConsoleUIItem root)
	{
		if (current == null || root == null)
		{
			return false;
		}

		return current == root || current.transform.IsChildOf(root.transform) || root.transform.IsChildOf(current.transform);
	}

	private string GetActiveSettingsTabName(SettingsMenuUI settings)
	{
		if (settings != null)
		{
			if (settings.GraphicsTab != null && settings.GraphicsTab.isOn)
			{
				return "Graphics";
			}
			if (settings.KeyboardTab != null && settings.KeyboardTab.isOn)
			{
				return "Keyboard";
			}
		}
		return "Preferences";
	}

	private string GetSettingsComponentLabel(Component component)
	{
		SettingsMenuUI settings = GameState.inst != null && GameState.inst.mainMenuMode != null && GameState.inst.mainMenuMode.settingsUI != null
			? GameState.inst.mainMenuMode.settingsUI.GetComponent<SettingsMenuUI>()
			: null;
		if (settings == null)
		{
			return string.Empty;
		}
		if (component == settings.MusicVolume) return "Music volume";
		if (component == settings.SfxVolume) return "Sound volume";
		if (component == settings.InvertZoom) return "Invert zoom";
		if (component == settings.ShowClouds) return "Show clouds";
		if (component == settings.ShowSSAO) return "Ambient occlusion";
		if (component == settings.ShowPostEffects) return "Post effects";
		if (component == settings.Shadows) return "Shadows";
		if (component == settings.Birds) return "Birds";
		if (component == settings.Waves) return "Waves";
		if (component == settings.AntiAliasing) return "Anti aliasing";
		if (component == settings.EdgeScroll) return "Edge scroll";
		if (component == settings.AdvisorNotifications) return "Advisor notifications";
		if (component == settings.VSync) return "Vertical sync";
		if (component == settings.MaxPanningSpeed) return "Max panning speed";
		if (component == settings.Fog) return "Fog";
		if (component == settings.FogDistance) return "Fog distance";
		if (component == settings.UIScale) return "UI scale";
		if (component == settings.VikingDragonTimers) return "Viking and dragon timers";
		if (component == settings.StreamerMode) return "Streamer mode";
		if (component == settings.InstancedRendering) return "Instanced rendering";
		if (component == settings.LegacyMouseControls) return "Legacy mouse controls";
		if (component == settings.ConsoleControls) return "Console controls";
		if (component == settings.SendCrashes) return "Send crash reports";
		if (component == settings.SeedText) return "World seed";
		return string.Empty;
	}

}
