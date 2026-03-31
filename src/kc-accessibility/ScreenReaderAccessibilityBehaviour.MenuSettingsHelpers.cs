using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ScreenReaderAccessibilityBehaviour
{
	private List<Button> GetTopLevelMainMenuButtons()
	{
		List<Button> items = new List<Button>();
		GameObject root = GameState.inst.mainMenuMode.topLevelUI;
		if (root == null)
		{
			return items;
		}

		GameObject preferredRoot = FindTopLevelMainMenuButtonContainer(root) ?? root;
		Button[] buttons = preferredRoot.GetComponentsInChildren<Button>(includeInactive: false);
		for (int i = 0; i < buttons.Length; i++)
		{
			Button button = buttons[i];
			if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
			{
				continue;
			}

			items.Add(button);
		}

		items.Sort(CompareVerticalMenuButtons);
		LogTopLevelMainMenuButtons(preferredRoot == root ? "dynamic-root" : "dynamic-button-container", items);
		if (items.Count > 0)
		{
			return items;
		}

		string[] labels = new string[]
		{
			"New",
			"Load",
			"Kingdom Share",
			"Settings",
			"Credits",
			"Mods",
			"Quit"
		};

		for (int i = 0; i < labels.Length; i++)
		{
			Button item = FindVisibleButtonByLabel(root, labels[i]);
			if (item != null && !items.Contains(item))
			{
				items.Add(item);
			}
		}

		LogTopLevelMainMenuButtons("fallback", items);
		return items;
	}

	private static GameObject FindTopLevelMainMenuButtonContainer(GameObject root)
	{
		if (root == null)
		{
			return null;
		}

		Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: false);
		for (int i = 0; i < transforms.Length; i++)
		{
			Transform candidate = transforms[i];
			if (candidate == null || candidate.gameObject == null)
			{
				continue;
			}

			if (string.Equals(candidate.name, "ButtonContainer", StringComparison.OrdinalIgnoreCase))
			{
				return candidate.gameObject;
			}
		}

		return null;
	}

	private void LogTopLevelMainMenuButtons(string source, List<Button> items)
	{
		if (items == null)
		{
			log("Top-level main menu discovery source=" + source + " returned null.");
			return;
		}

		log("Top-level main menu discovery source=" + source + " count=" + items.Count + ".");
		for (int i = 0; i < items.Count; i++)
		{
			log("Top-level main menu item " + i + ": " + DescribeTopLevelMainMenuButtonForLog(items[i]));
		}
	}

	private string DescribeTopLevelMainMenuButtonForLog(Button button)
	{
		if (button == null)
		{
			return "null";
		}

		ConsoleUIItem consoleItem = GetConsoleItemForComponent(button);
		Transform transform = button.transform;
		Vector3 position = transform.position;
		string label = GetComponentLabel(button);
		string objectName = button.gameObject.name;
		string parentName = transform.parent != null ? transform.parent.name : "none";
		string consoleItemName = consoleItem != null ? consoleItem.gameObject.name : "none";
		string consoleItemType = consoleItem != null ? consoleItem.GetType().Name : "none";
		string localizeTerm = GetPrimaryLocalizationTerm(button.gameObject);
		return "label=" + SafeLogValue(label)
			+ ", object=" + SafeLogValue(objectName)
			+ ", parent=" + SafeLogValue(parentName)
			+ ", type=" + button.GetType().Name
			+ ", consoleItem=" + SafeLogValue(consoleItemName)
			+ ", consoleItemType=" + SafeLogValue(consoleItemType)
			+ ", localize=" + SafeLogValue(localizeTerm)
			+ ", pos=(" + position.x.ToString("F1") + "," + position.y.ToString("F1") + "," + position.z.ToString("F1") + ")";
	}

	private static string SafeLogValue(string value)
	{
		return string.IsNullOrEmpty(value) ? "<empty>" : value;
	}

	private static string GetPrimaryLocalizationTerm(GameObject obj)
	{
		if (obj == null)
		{
			return string.Empty;
		}

		I2.Loc.Localize localize = obj.GetComponent<I2.Loc.Localize>() ?? obj.GetComponentInChildren<I2.Loc.Localize>(includeInactive: false);
		return localize != null ? CleanText(localize.Term) : string.Empty;
	}

	private static int CompareVerticalMenuButtons(Button left, Button right)
	{
		if (left == null && right == null)
		{
			return 0;
		}

		if (left == null)
		{
			return 1;
		}

		if (right == null)
		{
			return -1;
		}

		Vector3 leftPosition = left.transform.position;
		Vector3 rightPosition = right.transform.position;
		float yDelta = Mathf.Abs(leftPosition.y - rightPosition.y);
		if (yDelta > 0.5f)
		{
			return rightPosition.y.CompareTo(leftPosition.y);
		}

		float xDelta = Mathf.Abs(leftPosition.x - rightPosition.x);
		if (xDelta > 0.5f)
		{
			return leftPosition.x.CompareTo(rightPosition.x);
		}

		return string.Compare(GetStableMenuButtonName(left), GetStableMenuButtonName(right), StringComparison.OrdinalIgnoreCase);
	}

	private static string GetStableMenuButtonName(Button button)
	{
		return button != null ? button.gameObject.name : string.Empty;
	}

	private List<Button> GetChooseModeButtons()
	{
		List<Button> buttons = new List<Button>();
		GameObject root = GameState.inst.mainMenuMode.chooseModeUI;
		if (root == null)
		{
			return buttons;
		}

		Button[] allButtons = root.GetComponentsInChildren<Button>(includeInactive: false);
		for (int i = 0; i < allButtons.Length; i++)
		{
			Button button = allButtons[i];
			if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
			{
				continue;
			}

			string label = GetComponentLabel(button);
			if (string.Equals(label, "Accept", StringComparison.OrdinalIgnoreCase))
			{
				buttons.Add(button);
			}
		}

		buttons.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
		return buttons;
	}

	private Button GetChooseModeBackButton()
	{
		GameObject root = GameState.inst.mainMenuMode.chooseModeUI;
		if (root == null)
		{
			return null;
		}

		Button[] buttons = root.GetComponentsInChildren<Button>(includeInactive: false);
		for (int i = 0; i < buttons.Length; i++)
		{
			Button button = buttons[i];
			if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
			{
				continue;
			}

			string label = GetComponentLabel(button);
			if (string.Equals(label, "Back", StringComparison.OrdinalIgnoreCase))
			{
				return button;
			}
		}

		return null;
	}

	private Button GetChooseDifficultyButton(string label)
	{
		GameObject root = GameState.inst.mainMenuMode.chooseDifficultyUI;
		if (root == null)
		{
			return null;
		}

		Button[] buttons = root.GetComponentsInChildren<Button>(includeInactive: false);
		for (int i = 0; i < buttons.Length; i++)
		{
			Button button = buttons[i];
			if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
			{
				continue;
			}

			string candidate = GetComponentLabel(button);
			if (string.Equals(candidate, label, StringComparison.OrdinalIgnoreCase))
			{
				return button;
			}
		}

		return null;
	}

	private List<Component> GetNameAndBannerItems(PickNameUI pickNameUi)
	{
		List<Component> items = new List<Component>();
		if (pickNameUi == null)
		{
			return items;
		}

		AddActiveUniqueComponent(items, pickNameUi.cityNameInput);
		Button bannerButton = pickNameUi.bigBanner != null ? pickNameUi.bigBanner.GetComponentInParent<Button>() : null;
		AddActiveUniqueComponent(items, bannerButton);
		AddActiveUniqueComponent(items, GetNameAndBannerButton(pickNameUi, "Accept"));
		AddActiveUniqueComponent(items, GetNameAndBannerButton(pickNameUi, "Back"));
		return items;
	}

	private List<Component> GetNewMapItems(NewMapUI newMapUi)
	{
		List<Component> items = new List<Component>();
		if (newMapUi == null)
		{
			return items;
		}

		if (newMapUi.InEditMode)
		{
			GameObject editRoot = newMapUi.MapEditContainer;
			AddActiveUniqueComponent(items, FindVisibleButtonByLabel(editRoot, "Done"));
			AddActiveUniqueComponent(items, FindVisibleButtonByLabel(editRoot, "Back"));
			return items;
		}

		AddActiveUniqueComponent(items, SeedButtonUI.inst != null ? SeedButtonUI.inst.inputField : null);
		AddActiveUniqueComponent(items, MapSettingsUI.inst != null ? MapSettingsUI.inst.mapSizeDropdown : null);
		AddActiveUniqueComponent(items, MapSettingsUI.inst != null ? MapSettingsUI.inst.mapBiasDropdown : null);
		AddActiveUniqueComponent(items, MapSettingsUI.inst != null ? MapSettingsUI.inst.mapRiverLakeDropdown : null);
		AddActiveUniqueComponent(items, FindVisibleButtonByLabel(newMapUi.NewMapSettingsContainer, "New Map"));
		AddActiveUniqueComponent(items, FindVisibleButtonByLabel(newMapUi.NewMapSettingsContainer, "Add AI"));
		AddActiveUniqueComponent(items, FindVisibleButtonByLabel(newMapUi.confirmButtonContainer != null && newMapUi.confirmButtonContainer.activeInHierarchy ? newMapUi.confirmButtonContainer : newMapUi.NewMapSettingsContainer, "Accept"));
		AddActiveUniqueComponent(items, FindVisibleButtonByLabel(newMapUi.confirmButtonContainer != null && newMapUi.confirmButtonContainer.activeInHierarchy ? newMapUi.confirmButtonContainer : newMapUi.NewMapSettingsContainer, "Back"));
		return items;
	}

	private Button GetNameAndBannerButton(PickNameUI pickNameUi, string label)
	{
		GameObject root = pickNameUi != null ? pickNameUi.gameObject : null;
		if (root == null)
		{
			return null;
		}

		Button[] buttons = root.GetComponentsInChildren<Button>(includeInactive: false);
		for (int i = 0; i < buttons.Length; i++)
		{
			Button button = buttons[i];
			if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
			{
				continue;
			}

			if (string.Equals(GetComponentLabel(button), label, StringComparison.OrdinalIgnoreCase))
			{
				return button;
			}
		}

		return null;
	}

	private List<Component> GetSettingsMenuItems(SettingsMenuUI settings)
	{
		List<Component> items = new List<Component>();
		if (settings == null)
		{
			return items;
		}

		AddActiveUniqueComponent(items, settings.MusicVolume);
		AddActiveUniqueComponent(items, settings.SfxVolume);
		AddActiveUniqueComponent(items, settings.InvertZoom);
		AddActiveUniqueComponent(items, settings.EdgeScroll);
		AddActiveUniqueComponent(items, settings.AdvisorNotifications);
		AddActiveUniqueComponent(items, settings.MaxPanningSpeed);
		AddActiveUniqueComponent(items, settings.VikingDragonTimers);
		AddActiveUniqueComponent(items, settings.StreamerMode);
		AddActiveUniqueComponent(items, settings.LegacyMouseControls);
		AddActiveUniqueComponent(items, settings.ConsoleControls);
		AddActiveUniqueComponent(items, settings.SendCrashes);
		AddActiveUniqueComponent(items, settings.ShowClouds);
		AddActiveUniqueComponent(items, settings.ShowSSAO);
		AddActiveUniqueComponent(items, settings.ShowPostEffects);
		AddActiveUniqueComponent(items, settings.Shadows);
		AddActiveUniqueComponent(items, settings.Birds);
		AddActiveUniqueComponent(items, settings.Waves);
		AddActiveUniqueComponent(items, settings.AntiAliasing);
		AddActiveUniqueComponent(items, settings.VSync);
		AddActiveUniqueComponent(items, settings.Fog);
		AddActiveUniqueComponent(items, settings.FogDistance);
		AddActiveUniqueComponent(items, settings.UIScale);
		AddActiveUniqueComponent(items, settings.InstancedRendering);
		AddActiveUniqueComponent(items, settings.SeedText);
		return items;
	}

	private List<Button> GetPauseMenuButtons()
	{
		List<Button> items = new List<Button>();
		GameObject root = GameState.inst != null && GameState.inst.mainMenuMode != null ? GameState.inst.mainMenuMode.pauseMenuUI : null;
		if (root == null)
		{
			return items;
		}

		string[] labels = new string[]
		{
			"Back to Game",
			"Save",
			"Load",
			"Settings",
			"Quit?"
		};

		for (int i = 0; i < labels.Length; i++)
		{
			Button button = FindVisibleButtonByLabel(root, labels[i]);
			if (button != null && !items.Contains(button))
			{
				items.Add(button);
			}
		}

		if (items.Count == 0)
		{
			Button[] buttons = root.GetComponentsInChildren<Button>(includeInactive: false);
			for (int i = 0; i < buttons.Length; i++)
			{
				Button button = buttons[i];
				if (button != null && button.gameObject.activeInHierarchy && button.interactable && !items.Contains(button))
				{
					items.Add(button);
				}
			}
		}

		return items;
	}

	private void FocusPauseMenuButton(Button button)
	{
		if (button == null)
		{
			return;
		}

		FocusAccessibilityComponent(button);
		PlayNavigationSound();
		AnnounceText(DescribePauseMenuButton(button), false);
	}

	private string DescribePauseMenuButton(Button button)
	{
		if (button == null)
		{
			return string.Empty;
		}

		return JoinParts(GetComponentLabel(button), "button");
	}

	private List<Button> GetConfirmationDialogButtons()
	{
		List<Button> items = new List<Button>();
		GameObject root = GameState.inst != null && GameState.inst.mainMenuMode != null && GameState.inst.mainMenuMode.QuitConfirmation != null
			? GameState.inst.mainMenuMode.QuitConfirmation.gameObject
			: null;
		if (root == null)
		{
			return items;
		}

		string[] labels = new string[]
		{
			"Back to Game",
			"Cancel",
			"Save & Quit",
			"Save and Quit",
			"Quit",
			"Yes",
			"No"
		};

		for (int i = 0; i < labels.Length; i++)
		{
			Button button = FindVisibleButtonByLabel(root, labels[i]);
			if (button != null && !items.Contains(button))
			{
				items.Add(button);
			}
		}

		if (items.Count == 0)
		{
			Button[] buttons = root.GetComponentsInChildren<Button>(includeInactive: false);
			for (int i = 0; i < buttons.Length; i++)
			{
				Button button = buttons[i];
				if (button != null && button.gameObject.activeInHierarchy && button.interactable && !items.Contains(button))
				{
					items.Add(button);
				}
			}
		}

		return items;
	}

	private Button GetConfirmationDialogBackButton(List<Button> items)
	{
		for (int i = 0; i < items.Count; i++)
		{
			string label = CleanText(GetComponentLabel(items[i]));
			if (string.Equals(label, "Back to Game", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(label, "Cancel", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(label, "No", StringComparison.OrdinalIgnoreCase))
			{
				return items[i];
			}
		}

		return items.Count > 0 ? items[0] : null;
	}

	private void FocusConfirmationDialogButton(Button button)
	{
		if (button == null)
		{
			return;
		}

		FocusAccessibilityComponent(button);
		PlayNavigationSound();
		AnnounceText(JoinParts(GetComponentLabel(button), "button"), false);
	}

	private string DescribeConfirmationDialogButton(Button button)
	{
		return JoinParts(GetConfirmationDialogPrompt(), GetComponentLabel(button), "button");
	}

	private string GetConfirmationDialogPrompt()
	{
		GameObject root = GameState.inst != null && GameState.inst.mainMenuMode != null && GameState.inst.mainMenuMode.QuitConfirmation != null
			? GameState.inst.mainMenuMode.QuitConfirmation.gameObject
			: null;
		if (root == null)
		{
			return string.Empty;
		}

		TextMeshProUGUI[] tmpTexts = root.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: false);
		for (int i = 0; i < tmpTexts.Length; i++)
		{
			string value = CleanText(tmpTexts[i].text);
			if (IsConfirmationPromptText(value))
			{
				return value;
			}
		}

		Text[] legacyTexts = root.GetComponentsInChildren<Text>(includeInactive: false);
		for (int i = 0; i < legacyTexts.Length; i++)
		{
			string value = CleanText(legacyTexts[i].text);
			if (IsConfirmationPromptText(value))
			{
				return value;
			}
		}

		MainMenuMode.State state = GameState.inst.mainMenuMode.GetState();
		string fallback;
		if (!MainMenuStateNames.TryGetValue(state, out fallback))
		{
			fallback = SplitIdentifier(state.ToString());
		}

		return fallback;
	}

	private static bool IsConfirmationPromptText(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}

		string normalized = value.Trim();
		return normalized.IndexOf("quit", StringComparison.OrdinalIgnoreCase) >= 0
			|| normalized.IndexOf("return to main menu", StringComparison.OrdinalIgnoreCase) >= 0
			|| normalized.IndexOf("lose all unsaved progress", StringComparison.OrdinalIgnoreCase) >= 0
			|| normalized.IndexOf("unsaved", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private void AddActiveUniqueComponent(List<Component> items, Component component)
	{
		if (component == null)
		{
			return;
		}

		if (!component.gameObject.activeInHierarchy)
		{
			return;
		}

		if (!items.Contains(component))
		{
			items.Add(component);
		}
	}

	private Button FindVisibleButtonByLabel(GameObject root, string label)
	{
		if (root == null)
		{
			return null;
		}

		Button[] buttons = root.GetComponentsInChildren<Button>(includeInactive: false);
		for (int i = 0; i < buttons.Length; i++)
		{
			Button button = buttons[i];
			if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
			{
				continue;
			}

			if (!string.Equals(GetComponentLabel(button), label, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			return button;
		}

		return null;
	}

	private void FocusTopLevelMainMenuButton(Button button)
	{
		if (button == null)
		{
			return;
		}

		FocusAccessibilityComponent(button);
		PlayNavigationSound();
		AnnounceTopLevelMainMenuButton(button);
	}

	private void FocusChooseModeItem(List<Button> modeButtons, Button backButton, int index)
	{
		if (index >= 0 && index < modeButtons.Count)
		{
			Button button = modeButtons[index];
			ConsoleUIItem item = GetConsoleItemForComponent(button);
			if (item != null)
			{
				FocusItem(item, announce: false);
			}

			button.Select();
			PlayNavigationSound();
			AnnounceText(DescribeChooseModeButton(button), false);
			return;
		}

		if (index == modeButtons.Count && backButton != null)
		{
			ConsoleUIItem item = GetConsoleItemForComponent(backButton);
			if (item != null)
			{
				FocusItem(item, announce: false);
			}

			backButton.Select();
			PlayNavigationSound();
			AnnounceText("Back. button.", false);
		}
	}

	private string DescribeChooseModeButton(Button button)
	{
		if (button == null)
		{
			return string.Empty;
		}

		Transform panel = button.transform.parent;
		string title = string.Empty;
		string description = string.Empty;
		if (panel != null)
		{
			TextMeshProUGUI[] texts = panel.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: false);
			for (int i = 0; i < texts.Length; i++)
			{
				string value = CleanText(texts[i].text);
				if (string.IsNullOrEmpty(value) || string.Equals(value, "Accept", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (string.IsNullOrEmpty(title))
				{
					title = value;
				}
				else if (string.IsNullOrEmpty(description))
				{
					description = value;
					break;
				}
			}
		}

		return JoinParts(title, description, "Accept button");
	}

	private void FocusChooseDifficultyItem(DifficultySelectUI difficultyUi, Button acceptButton, Button backButton, int index)
	{
		if (index < 0)
		{
			ConsoleUIItem item = GetConsoleItemForComponent(GameState.inst.mainMenuMode.chooseDifficultyUI.GetComponent<ConsoleUIItem>());
			if (item != null)
			{
				FocusItem(item, announce: false);
			}

			PlayNavigationSound();
			AnnounceDifficulty(difficultyUi);
			return;
		}

		Button button = index == 0 ? acceptButton : backButton;
		if (button == null)
		{
			return;
		}

		ConsoleUIItem buttonItem = GetConsoleItemForComponent(button);
		if (buttonItem != null)
		{
			FocusItem(buttonItem, announce: false);
		}

		button.Select();
		PlayNavigationSound();
		AnnounceText(GetComponentLabel(button) + ". button.", false);
	}

	private void FocusNameAndBannerItem(Component component)
	{
		if (component == null)
		{
			return;
		}

		FocusAccessibilityComponent(component);
		PlayNavigationSound();
		AnnounceText(DescribeNameAndBannerItem(component), false);
	}

	private void FocusNewMapItem(Component component)
	{
		if (component == null)
		{
			return;
		}

		FocusAccessibilityComponent(component);
		PlayNavigationSound();
		AnnounceText(DescribeNewMapItem(component), false);
	}

	private void ActivateNameAndBannerItem(PickNameUI pickNameUi, Component component)
	{
		TMP_InputField inputField = component as TMP_InputField;
		if (inputField != null)
		{
			BeginTextFieldEditing(inputField, "City name");
			return;
		}

		Button button = component as Button;
		if (button != null)
		{
			button.onClick.Invoke();
		}
	}

	private string DescribeNameAndBannerItem(Component component)
	{
		TMP_InputField inputField = component as TMP_InputField;
		if (inputField != null)
		{
			return DescribeTextField("City name", inputField);
		}

		Button button = component as Button;
		if (button != null)
		{
			PickNameUI pickNameUi = GameState.inst.mainMenuMode.nameBannerUI != null ? GameState.inst.mainMenuMode.nameBannerUI.GetComponent<PickNameUI>() : null;
			Button bannerButton = pickNameUi != null && pickNameUi.bigBanner != null ? pickNameUi.bigBanner.GetComponentInParent<Button>() : null;
			if (button == bannerButton)
			{
				return "Choose banner. button.";
			}

			return GetComponentLabel(button) + ". button.";
		}

		return DescribeSettingsComponent(component);
	}

	private void BeginTextFieldEditing(TMP_InputField inputField, string label)
	{
		if (inputField == null)
		{
			return;
		}

		activeTextField = inputField;
		activeTextFieldLabel = CleanText(label);
		inputField.Select();
		inputField.ActivateInputField();
		AnnounceText("Editing " + activeTextFieldLabel + ". " + DescribeTextField(activeTextFieldLabel, inputField), true);
	}

	private bool HandleActiveTextFieldEditing(TMP_InputField expectedField)
	{
		if (activeTextField == null || activeTextField != expectedField)
		{
			return false;
		}

		if (IsSubmitPressed() || Input.GetKeyDown(KeyCode.Escape))
		{
			activeTextField.DeactivateInputField();
			AnnounceText(DescribeTextField(activeTextFieldLabel, activeTextField), true);
			activeTextField = null;
			activeTextFieldLabel = string.Empty;
			return true;
		}

		if (IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateLeftPressed()
			|| IsNavigateRightPressed()
			|| IsNavigateNextPressed()
			|| Input.GetKeyDown(KeyCode.Backspace)
			|| IsSubmitAlternatePressed())
		{
			return true;
		}

		return false;
	}

	private string DescribeTextField(string label, TMP_InputField inputField)
	{
		string text = inputField != null ? CleanText(inputField.text) : string.Empty;
		return string.IsNullOrEmpty(text) ? label + ". text field blank." : label + ". text field. " + text + ".";
	}

	private void ActivateNewMapItem(Component component)
	{
		TMP_InputField inputField = component as TMP_InputField;
		if (inputField != null)
		{
			BeginTextFieldEditing(inputField, GetNewMapComponentLabel(inputField));
			return;
		}

		TMP_Dropdown dropdown = component as TMP_Dropdown;
		if (dropdown != null)
		{
			AnnounceText(DescribeNewMapItem(dropdown), true);
			return;
		}

		Button button = component as Button;
		if (button != null)
		{
			button.onClick.Invoke();
			return;
		}

		Toggle toggle = component as Toggle;
		if (toggle != null)
		{
			toggle.isOn = !toggle.isOn;
			AnnounceText(DescribeNewMapItem(toggle), true);
		}
	}

	private void AdjustNewMapItem(Component component, bool moveRight)
	{
		TMP_Dropdown dropdown = component as TMP_Dropdown;
		if (dropdown != null)
		{
			int count = dropdown.options != null ? dropdown.options.Count : 0;
			if (count > 0)
			{
				int delta = moveRight ? 1 : -1;
				dropdown.value = (dropdown.value + delta + count) % count;
				MapSettingsUI.inst?.OnMapSizeChanged();
			}

			AnnounceText(DescribeNewMapItem(dropdown), true);
			return;
		}

		Toggle toggle = component as Toggle;
		if (toggle != null)
		{
			bool target = moveRight;
			if (toggle.isOn != target)
			{
				toggle.isOn = target;
			}

			AnnounceText(DescribeNewMapItem(toggle), true);
		}
	}

	private void TriggerNewMapBack(NewMapUI newMapUi)
	{
		if (newMapUi != null && newMapUi.InEditMode && newMapUi.doneButton != null && newMapUi.doneButton.gameObject.activeInHierarchy)
		{
			newMapUi.doneButton.onClick.Invoke();
			return;
		}

		Button backButton = FindVisibleButtonByLabel(newMapUi != null && newMapUi.confirmButtonContainer != null && newMapUi.confirmButtonContainer.activeInHierarchy ? newMapUi.confirmButtonContainer : newMapUi != null ? newMapUi.NewMapSettingsContainer : null, "Back");
		if (backButton != null)
		{
			backButton.onClick.Invoke();
		}
	}

	private string DescribeNewMapItem(Component component)
	{
		if (component == null)
		{
			return string.Empty;
		}

		string label = GetNewMapComponentLabel(component);
		TMP_InputField inputField = component as TMP_InputField;
		if (inputField != null)
		{
			return DescribeTextField(label, inputField);
		}

		TMP_Dropdown dropdown = component as TMP_Dropdown;
		if (dropdown != null)
		{
			string option = dropdown.options != null && dropdown.value >= 0 && dropdown.value < dropdown.options.Count ? CleanText(dropdown.options[dropdown.value].text) : string.Empty;
			return JoinParts(label, option);
		}

		Toggle toggle = component as Toggle;
		if (toggle != null)
		{
			return JoinParts(label, toggle.isOn ? "toggle on" : "toggle off");
		}

		Button button = component as Button;
		if (button != null)
		{
			return JoinParts(label, "button");
		}

		return JoinParts(label, SplitIdentifier(component.GetType().Name));
	}

	private string GetNewMapComponentLabel(Component component)
	{
		if (SeedButtonUI.inst != null && component == SeedButtonUI.inst.inputField)
		{
			return "Terrain generation seed";
		}

		if (MapSettingsUI.inst != null)
		{
			if (component == MapSettingsUI.inst.mapSizeDropdown) return "Size";
			if (component == MapSettingsUI.inst.mapBiasDropdown) return "Type";
			if (component == MapSettingsUI.inst.mapRiverLakeDropdown) return "Rivers";
		}

		string label = GetComponentLabel(component);
		if (!string.IsNullOrEmpty(label) && !label.StartsWith("IMN", StringComparison.OrdinalIgnoreCase) && !label.StartsWith("TMP", StringComparison.OrdinalIgnoreCase))
		{
			if (string.Equals(label, "New Map", StringComparison.OrdinalIgnoreCase))
			{
				return "Regenerate map";
			}

			if (string.Equals(label, "Accept", StringComparison.OrdinalIgnoreCase))
			{
				return "Start game";
			}

			if (string.Equals(label, "Back", StringComparison.OrdinalIgnoreCase))
			{
				return "Back to name and banner";
			}

			if (string.Equals(label, "Add AI", StringComparison.OrdinalIgnoreCase))
			{
				return "AI kingdoms";
			}

			return label;
		}

		GameObject obj = component.gameObject;
		if (obj.name.IndexOf("size", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "Map size";
		}

		if (obj.name.IndexOf("random", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "Random";
		}

		return SplitIdentifier(obj.name);
	}

	private void AnnounceTopLevelMainMenuButton(Button button)
	{
		string label = GetComponentLabel(button);
		if (string.IsNullOrEmpty(label))
		{
			label = "Button";
		}

		AnnounceText(label + ". button.", false);
	}

	private void FocusSettingsComponent(Component component, bool announce, bool playSound)
	{
		if (component == null)
		{
			return;
		}

		FocusAccessibilityComponent(component);
		if (playSound)
		{
			PlayNavigationSound();
		}

		if (announce)
		{
			AnnounceText(DescribeSettingsComponent(component), false);
		}
	}

	private void FocusAccessibilityComponent(Component component)
	{
		if (component == null)
		{
			return;
		}

		ConsoleUIItem item = GetConsoleItemForComponent(component);
		if (item != null)
		{
			FocusItem(item, announce: false);
		}

		Selectable selectable = component as Selectable;
		if (selectable != null)
		{
			selectable.Select();
		}
	}

	private void ActivateSettingsComponent(Component component)
	{
		if (component == null)
		{
			return;
		}

		Toggle toggle = component as Toggle;
		if (toggle != null)
		{
			toggle.isOn = !toggle.isOn;
			AnnounceText(DescribeSettingsComponent(toggle), true);
			return;
		}

		Slider slider = component as Slider;
		if (slider != null)
		{
			AnnounceText(DescribeSettingsComponent(slider), false);
			return;
		}

		TMP_InputField inputField = component as TMP_InputField;
		if (inputField != null)
		{
			BeginTextFieldEditing(inputField, GetComponentLabel(inputField));
		}
	}

	private void AdjustSettingsComponent(Component component, bool moveRight)
	{
		if (component == null)
		{
			return;
		}

		Toggle toggle = component as Toggle;
		if (toggle != null)
		{
			bool target = moveRight;
			if (toggle.isOn != target)
			{
				toggle.isOn = target;
			}

			AnnounceText(DescribeSettingsComponent(toggle), true);
			return;
		}

		Slider slider = component as Slider;
		if (slider != null)
		{
			float step = slider.wholeNumbers ? 1f : Mathf.Max((slider.maxValue - slider.minValue) / 20f, 0.05f);
			slider.value = Mathf.Clamp(slider.value + (moveRight ? step : -step), slider.minValue, slider.maxValue);
			AnnounceText(DescribeSettingsComponent(slider), true);
		}
	}

	private string DescribeSettingsComponent(Component component)
	{
		if (component == null)
		{
			return string.Empty;
		}

		string label = GetComponentLabel(component);
		Toggle toggle = component as Toggle;
		if (toggle != null)
		{
			return JoinParts(label, toggle.isOn ? "toggle on" : "toggle off");
		}

		Slider slider = component as Slider;
		if (slider != null)
		{
			float percent = Mathf.Approximately(slider.maxValue, slider.minValue) ? 0f : Mathf.InverseLerp(slider.minValue, slider.maxValue, slider.value) * 100f;
			return JoinParts(label, "slider " + Mathf.RoundToInt(percent) + " percent");
		}

		TMP_InputField inputField = component as TMP_InputField;
		if (inputField != null)
		{
			string text = CleanText(inputField.text);
			return string.IsNullOrEmpty(text) ? JoinParts(label, "text field blank") : JoinParts(label, "text field", text);
		}

		return JoinParts(label, SplitIdentifier(component.GetType().Name));
	}

	private string GetComponentLabel(Component component)
	{
		if (component == null)
		{
			return string.Empty;
		}

		string settingsLabel = GetSettingsComponentLabel(component);
		if (!string.IsNullOrEmpty(settingsLabel))
		{
			return settingsLabel;
		}

		GameObject obj = component.gameObject;
		string label = CleanText(ExtractPrimaryLabel(obj));
		if (label.StartsWith("IMS", StringComparison.OrdinalIgnoreCase) || label.StartsWith("IMN", StringComparison.OrdinalIgnoreCase))
		{
			label = string.Empty;
		}

		if (string.Equals(label, "Toggle", StringComparison.OrdinalIgnoreCase))
		{
			label = string.Empty;
		}

		if (string.IsNullOrEmpty(label))
		{
			label = SplitIdentifier(obj.name);
		}

		return CleanText(label);
	}

	private bool IsSettingsSaveFocused()
	{
		return settingsMenuIndex == SettingsSaveIndex;
	}

	private void FocusSettingsSave(bool announce, bool playSound)
	{
		ConsoleUIItem current = GetFocusedItem();
		if (current != null)
		{
			current.Dehover();
		}

		if (playSound)
		{
			PlayNavigationSound();
		}

		settingsMenuIndex = SettingsSaveIndex;
		if (announce)
		{
			AnnounceText("Save button", false);
		}
	}

	private void SwitchSettingsTab(SettingsMenuUI settings, bool moveForward)
	{
		List<Toggle> tabs = GetAvailableSettingsTabs(settings);
		if (tabs.Count == 0)
		{
			return;
		}

		if (IsSettingsSaveFocused())
		{
			int targetIndex = moveForward ? 0 : tabs.Count - 1;
			ActivateSettingsTabAndFocusFirstItem(settings, tabs[targetIndex]);
			return;
		}

		int currentIndex = 0;
		for (int i = 0; i < tabs.Count; i++)
		{
			if (tabs[i].isOn)
			{
				currentIndex = i;
				break;
			}
		}

		if (moveForward && currentIndex == tabs.Count - 1)
		{
			FocusSettingsSave(announce: false, playSound: true);
			AnnounceText("Save button", true);
			return;
		}

		if (!moveForward && currentIndex == 0)
		{
			FocusSettingsSave(announce: false, playSound: true);
			AnnounceText("Save button", true);
			return;
		}

		int nextIndex = moveForward ? currentIndex + 1 : currentIndex - 1;
		ActivateSettingsTabAndFocusFirstItem(settings, tabs[nextIndex]);
	}

	private void ActivateSettingsTabAndFocusFirstItem(SettingsMenuUI settings, Toggle tab)
	{
		if (settings == null || tab == null)
		{
			return;
		}

		tab.isOn = true;
		List<Component> items = GetSettingsMenuItems(settings);
		settingsMenuIndex = items.Count > 0 ? 0 : -1;
		if (settingsMenuIndex >= 0)
		{
			FocusSettingsComponent(items[settingsMenuIndex], announce: false, playSound: false);
		}

		PlayNavigationSound();
		AnnounceText(JoinParts(GetActiveSettingsTabName(settings) + " tab", settingsMenuIndex >= 0 ? DescribeSettingsComponent(items[settingsMenuIndex]) : string.Empty), true);
	}

	private List<Toggle> GetAvailableSettingsTabs(SettingsMenuUI settings)
	{
		List<Toggle> tabs = new List<Toggle>();
		AddSettingsTab(tabs, settings.PreferenceTabs);
		AddSettingsTab(tabs, settings.GraphicsTab);
		AddSettingsTab(tabs, settings.KeyboardTab);
		return tabs;
	}

	private void AddSettingsTab(List<Toggle> tabs, Toggle tab)
	{
		if (tab != null && tab.gameObject.activeInHierarchy)
		{
			tabs.Add(tab);
		}
	}
}
