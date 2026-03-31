using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ScreenReaderAccessibilityBehaviour
{
	private bool IsSubmitActionPressed()
	{
		return IsSubmitPressed() || IsSubmitAlternatePressed();
	}

	private bool TryHandleTopLevelMainMenuNavigation()
	{
		if (GameState.inst.mainMenuMode.GetState() != MainMenuMode.State.Menu)
		{
			return false;
		}

		if (!(IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateNextPressed()
			|| IsSubmitActionPressed()))
		{
			return false;
		}

		List<Button> items = GetTopLevelMainMenuButtons();
		if (items.Count == 0)
		{
			log("Top-level main menu item discovery returned no items.");
			return false;
		}

		ConsoleUIItem focusedItem = GetFocusedItem();
		if (focusedItem != null)
		{
			int focusedIndex = FindButtonIndexForConsoleItem(items, focusedItem);
			if (focusedIndex >= 0)
			{
				topLevelMenuIndex = focusedIndex;
			}
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			topLevelMenuIndex = topLevelMenuIndex < 0 ? 0 : Mathf.Min(topLevelMenuIndex + 1, items.Count - 1);
			FocusTopLevelMainMenuButton(items[topLevelMenuIndex]);
			return true;
		}

		if (IsNavigateUpPressed())
		{
			topLevelMenuIndex = topLevelMenuIndex < 0 ? items.Count - 1 : Mathf.Max(topLevelMenuIndex - 1, 0);
			FocusTopLevelMainMenuButton(items[topLevelMenuIndex]);
			return true;
		}

		if (topLevelMenuIndex >= 0 && IsSubmitActionPressed())
		{
			items[topLevelMenuIndex].onClick.Invoke();
			return true;
		}

		return false;
	}

	private int FindButtonIndexForConsoleItem(List<Button> items, ConsoleUIItem focusedItem)
	{
		if (items == null || focusedItem == null)
		{
			return -1;
		}

		for (int i = 0; i < items.Count; i++)
		{
			Button button = items[i];
			if (button == null)
			{
				continue;
			}

			ConsoleUIItem buttonItem = GetConsoleItemForComponent(button);
			if (buttonItem == focusedItem)
			{
				return i;
			}
		}

		return -1;
	}

	private bool TryHandleChooseModeNavigation()
	{
		if (GameState.inst.mainMenuMode.GetState() != MainMenuMode.State.ChooseMode)
		{
			return false;
		}

		if (!(IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateLeftPressed()
			|| IsNavigateRightPressed()
			|| IsNavigateNextPressed()
			|| IsSubmitActionPressed()
			|| IsCancelPressed()))
		{
			return false;
		}

		List<Button> modeButtons = GetChooseModeButtons();
		Button backButton = GetChooseModeBackButton();
		if (modeButtons.Count == 0)
		{
			return false;
		}

		if (IsCancelPressed())
		{
			if (backButton != null)
			{
				backButton.onClick.Invoke();
				return true;
			}
		}

		if (IsNavigateLeftPressed())
		{
			chooseModeIndex = chooseModeIndex < 0 ? 0 : Mathf.Max(chooseModeIndex - 1, 0);
			FocusChooseModeItem(modeButtons, backButton, chooseModeIndex);
			return true;
		}

		if (IsNavigateRightPressed())
		{
			chooseModeIndex = chooseModeIndex < 0 ? 0 : Mathf.Min(chooseModeIndex + 1, modeButtons.Count - 1);
			FocusChooseModeItem(modeButtons, backButton, chooseModeIndex);
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			if (chooseModeIndex < 0)
			{
				chooseModeIndex = 0;
			}
			else if (chooseModeIndex < modeButtons.Count - 1 && !IsNavigateNextPressed())
			{
				chooseModeIndex++;
			}
			else
			{
				chooseModeIndex = modeButtons.Count;
			}
			FocusChooseModeItem(modeButtons, backButton, chooseModeIndex);
			return true;
		}

		if (IsNavigateUpPressed())
		{
			if (chooseModeIndex < 0)
			{
				chooseModeIndex = modeButtons.Count;
			}
			else if (chooseModeIndex > 0 && chooseModeIndex <= modeButtons.Count - 1)
			{
				chooseModeIndex--;
			}
			else
			{
				chooseModeIndex = modeButtons.Count - 1;
			}
			FocusChooseModeItem(modeButtons, backButton, chooseModeIndex);
			return true;
		}

		if (IsSubmitActionPressed())
		{
			if (chooseModeIndex == modeButtons.Count)
			{
				if (backButton != null)
				{
					backButton.onClick.Invoke();
				}
			}
			else
			{
				int resolvedIndex = chooseModeIndex < 0 ? 0 : Mathf.Clamp(chooseModeIndex, 0, modeButtons.Count - 1);
				modeButtons[resolvedIndex].onClick.Invoke();
			}
			return true;
		}

		return false;
	}

	private bool TryHandleChooseDifficultyNavigation()
	{
		if (GameState.inst.mainMenuMode.GetState() != MainMenuMode.State.ChooseDifficulty)
		{
			return false;
		}

		if (!(IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateLeftPressed()
			|| IsNavigateRightPressed()
			|| IsNavigateNextPressed()
			|| IsSubmitActionPressed()
			|| IsCancelPressed()))
		{
			return false;
		}

		DifficultySelectUI difficultyUi = GameState.inst.mainMenuMode.chooseDifficultyUI != null
			? GameState.inst.mainMenuMode.chooseDifficultyUI.GetComponent<DifficultySelectUI>()
			: null;
		Button acceptButton = GetChooseDifficultyButton("Accept");
		Button backButton = GetChooseDifficultyButton("Back");
		if (difficultyUi == null)
		{
			return false;
		}

		if (IsNavigateLeftPressed())
		{
			if (chooseDifficultyIndex <= 0)
			{
				chooseDifficultyIndex = -1;
				difficultyUi.OnPrevClicked();
			}
			else
			{
				chooseDifficultyIndex = 0;
				FocusChooseDifficultyItem(difficultyUi, acceptButton, backButton, chooseDifficultyIndex);
			}
			return true;
		}

		if (IsNavigateRightPressed())
		{
			if (chooseDifficultyIndex <= 0)
			{
				chooseDifficultyIndex = -1;
				difficultyUi.OnNextClicked();
			}
			else
			{
				chooseDifficultyIndex = 1;
				FocusChooseDifficultyItem(difficultyUi, acceptButton, backButton, chooseDifficultyIndex);
			}
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			chooseDifficultyIndex = chooseDifficultyIndex < 0 ? 0 : Mathf.Min(chooseDifficultyIndex + 1, 1);
			FocusChooseDifficultyItem(difficultyUi, acceptButton, backButton, chooseDifficultyIndex);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			chooseDifficultyIndex = chooseDifficultyIndex <= 0 ? -1 : 0;
			FocusChooseDifficultyItem(difficultyUi, acceptButton, backButton, chooseDifficultyIndex);
			return true;
		}

		if (IsCancelPressed())
		{
			if (backButton != null)
			{
				backButton.onClick.Invoke();
				return true;
			}
		}

		if (IsSubmitActionPressed())
		{
			if (chooseDifficultyIndex < 0 || chooseDifficultyIndex == 0)
			{
				if (acceptButton != null)
				{
					acceptButton.onClick.Invoke();
				}
			}
			else if (backButton != null)
			{
				backButton.onClick.Invoke();
			}
			return true;
		}

		return false;
	}

	private bool TryHandleNameAndBannerNavigation()
	{
		if (GameState.inst.mainMenuMode.GetState() != MainMenuMode.State.NameAndBanner)
		{
			return false;
		}

		PickNameUI pickNameUi = GameState.inst.mainMenuMode.nameBannerUI != null
			? GameState.inst.mainMenuMode.nameBannerUI.GetComponent<PickNameUI>()
			: null;
		if (pickNameUi == null || (pickNameUi.bannerUI != null && pickNameUi.bannerUI.gameObject.activeInHierarchy))
		{
			return false;
		}

		if (HandleActiveTextFieldEditing(pickNameUi.cityNameInput))
		{
			return true;
		}

		if (!(IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateLeftPressed()
			|| IsNavigateRightPressed()
			|| IsNavigateNextPressed()
			|| IsSubmitActionPressed()
			|| IsCancelPressed()))
		{
			return false;
		}

		List<Component> items = GetNameAndBannerItems(pickNameUi);
		if (items.Count == 0)
		{
			return false;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			nameBannerIndex = nameBannerIndex < 0 ? 0 : Mathf.Min(nameBannerIndex + 1, items.Count - 1);
			FocusNameAndBannerItem(items[nameBannerIndex]);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			nameBannerIndex = nameBannerIndex < 0 ? items.Count - 1 : Mathf.Max(nameBannerIndex - 1, 0);
			FocusNameAndBannerItem(items[nameBannerIndex]);
			return true;
		}

		if (nameBannerIndex >= 0)
		{
			Component current = items[nameBannerIndex];
			if (IsSubmitActionPressed())
			{
				ActivateNameAndBannerItem(pickNameUi, current);
				return true;
			}
			if (IsCancelPressed())
			{
				Button backButton = GetNameAndBannerButton(pickNameUi, "Back");
				if (backButton != null)
				{
					backButton.onClick.Invoke();
					return true;
				}
			}
		}

		return false;
	}

	private bool TryHandleNewMapNavigation()
	{
		if (GameState.inst.mainMenuMode.GetState() != MainMenuMode.State.NewMap)
		{
			return false;
		}

		NewMapUI newMapUi = GameState.inst.mainMenuMode.newMapUI != null
			? GameState.inst.mainMenuMode.newMapUI.GetComponent<NewMapUI>()
			: null;
		if (newMapUi == null)
		{
			return false;
		}

		TMP_InputField seedField = SeedButtonUI.inst != null ? SeedButtonUI.inst.inputField : null;
		if (HandleActiveTextFieldEditing(seedField))
		{
			return true;
		}

		if (!(IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateLeftPressed()
			|| IsNavigateRightPressed()
			|| IsNavigateNextPressed()
			|| IsSubmitActionPressed()
			|| IsCancelPressed()))
		{
			return false;
		}

		List<Component> items = GetNewMapItems(newMapUi);
		if (items.Count == 0)
		{
			return false;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			newMapIndex = newMapIndex < 0 ? 0 : Mathf.Min(newMapIndex + 1, items.Count - 1);
			FocusNewMapItem(items[newMapIndex]);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			newMapIndex = newMapIndex < 0 ? items.Count - 1 : Mathf.Max(newMapIndex - 1, 0);
			FocusNewMapItem(items[newMapIndex]);
			return true;
		}

		if (newMapIndex >= 0)
		{
			Component current = items[newMapIndex];
			if (IsNavigateLeftPressed())
			{
				AdjustNewMapItem(current, moveRight: false);
				return true;
			}
			if (IsNavigateRightPressed())
			{
				AdjustNewMapItem(current, moveRight: true);
				return true;
			}
			if (IsSubmitActionPressed())
			{
				ActivateNewMapItem(current);
				return true;
			}
			if (IsCancelPressed())
			{
				TriggerNewMapBack(newMapUi);
				return true;
			}
		}

		return false;
	}

	private bool TryHandleSettingsMenuNavigation()
	{
		if (GameState.inst.mainMenuMode.GetState() != MainMenuMode.State.SettingsMenu)
		{
			return false;
		}

		if (!(IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateNextPressed()
			|| IsSubmitActionPressed()
			|| IsNavigateLeftPressed()
			|| IsNavigateRightPressed()
			|| IsCancelPressed()))
		{
			return false;
		}

		SettingsMenuUI settings = GameState.inst.mainMenuMode.settingsUI != null ? GameState.inst.mainMenuMode.settingsUI.GetComponent<SettingsMenuUI>() : null;
		if (settings == null)
		{
			return false;
		}
		if (IsNavigateNextPressed())
		{
			SwitchSettingsTab(settings, moveForward: !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift));
			return true;
		}

		List<Component> items = GetSettingsMenuItems(settings);
		if (items.Count == 0)
		{
			log("Settings menu item discovery returned no items.");
			return false;
		}

		if (HandleActiveTextFieldEditing(settings.SeedText))
		{
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			if (IsSettingsSaveFocused())
			{
				settingsMenuIndex = items.Count > 0 ? 0 : -1;
				if (settingsMenuIndex >= 0)
				{
					FocusSettingsComponent(items[settingsMenuIndex], announce: true, playSound: true);
				}
				return true;
			}
			settingsMenuIndex = settingsMenuIndex < 0 ? 0 : Mathf.Min(settingsMenuIndex + 1, items.Count - 1);
			FocusSettingsComponent(items[settingsMenuIndex], announce: true, playSound: true);
			return true;
		}

		if (IsNavigateUpPressed())
		{
			if (IsSettingsSaveFocused())
			{
				settingsMenuIndex = items.Count > 0 ? items.Count - 1 : -1;
				if (settingsMenuIndex >= 0)
				{
					FocusSettingsComponent(items[settingsMenuIndex], announce: true, playSound: true);
				}
				return true;
			}
			settingsMenuIndex = settingsMenuIndex < 0 ? 0 : Mathf.Max(settingsMenuIndex - 1, 0);
			FocusSettingsComponent(items[settingsMenuIndex], announce: true, playSound: true);
			return true;
		}

		if (IsSettingsSaveFocused())
		{
			if (IsSubmitActionPressed())
			{
				settings.SaveChanges();
				return true;
			}
			return IsNavigateLeftPressed() || IsNavigateRightPressed();
		}

		if (settingsMenuIndex >= 0)
		{
			Component currentComponent = items[settingsMenuIndex];
			if (IsSubmitActionPressed())
			{
				ActivateSettingsComponent(currentComponent);
				return true;
			}
			if (IsNavigateLeftPressed())
			{
				AdjustSettingsComponent(currentComponent, moveRight: false);
				return true;
			}
			if (IsNavigateRightPressed())
			{
				AdjustSettingsComponent(currentComponent, moveRight: true);
				return true;
			}
			if (IsCancelPressed())
			{
				ConsoleUIItem item = GetConsoleItemForComponent(currentComponent);
				if (item != null)
				{
					item.Cancel();
				}
				return true;
			}
		}

		return false;
	}

	private bool TryHandlePauseMenuNavigation()
	{
		if (GameState.inst.mainMenuMode.GetState() != MainMenuMode.State.PauseMenu)
		{
			return false;
		}

		if (!(IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateNextPressed()
			|| IsSubmitActionPressed()
			|| IsCancelPressed()))
		{
			return false;
		}

		List<Button> items = GetPauseMenuButtons();
		if (items.Count == 0)
		{
			return false;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			int nextIndex = pauseMenuIndex < 0 ? 0 : Mathf.Min(pauseMenuIndex + 1, items.Count - 1);
			if (nextIndex == pauseMenuIndex)
			{
				return true;
			}
			pauseMenuIndex = nextIndex;
			FocusPauseMenuButton(items[pauseMenuIndex]);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			int nextIndex = pauseMenuIndex < 0 ? items.Count - 1 : Mathf.Max(pauseMenuIndex - 1, 0);
			if (nextIndex == pauseMenuIndex)
			{
				return true;
			}
			pauseMenuIndex = nextIndex;
			FocusPauseMenuButton(items[pauseMenuIndex]);
			return true;
		}

		if (pauseMenuIndex >= 0 && IsSubmitActionPressed())
		{
			items[pauseMenuIndex].onClick.Invoke();
			return true;
		}

		if (IsCancelPressed())
		{
			Button backToGame = FindVisibleButtonByLabel(GameState.inst.mainMenuMode.pauseMenuUI, "Back to Game");
			if (backToGame != null)
			{
				backToGame.onClick.Invoke();
				return true;
			}
		}

		return false;
	}

	private bool TryHandleConfirmationDialogNavigation()
	{
		MainMenuMode.State state = GameState.inst.mainMenuMode.GetState();
		if (state != MainMenuMode.State.ExitConfirm && state != MainMenuMode.State.QuitConfirm)
		{
			return false;
		}

		if (!(IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateLeftPressed()
			|| IsNavigateRightPressed()
			|| IsNavigateNextPressed()
			|| IsSubmitActionPressed()
			|| IsCancelPressed()))
		{
			return false;
		}

		List<Button> items = GetConfirmationDialogButtons();
		if (items.Count == 0)
		{
			return false;
		}

		if (IsNavigateLeftPressed()
			|| IsNavigateUpPressed()
			|| IsNavigatePreviousPressed())
		{
			int nextIndex = confirmationDialogIndex < 0 ? items.Count - 1 : Mathf.Max(confirmationDialogIndex - 1, 0);
			if (nextIndex == confirmationDialogIndex)
			{
				return true;
			}
			confirmationDialogIndex = nextIndex;
			FocusConfirmationDialogButton(items[confirmationDialogIndex]);
			return true;
		}

		if (IsNavigateRightPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateNextPressed())
		{
			int nextIndex = confirmationDialogIndex < 0 ? 0 : Mathf.Min(confirmationDialogIndex + 1, items.Count - 1);
			if (nextIndex == confirmationDialogIndex)
			{
				return true;
			}
			confirmationDialogIndex = nextIndex;
			FocusConfirmationDialogButton(items[confirmationDialogIndex]);
			return true;
		}

		if (confirmationDialogIndex >= 0 && IsSubmitActionPressed())
		{
			items[confirmationDialogIndex].onClick.Invoke();
			return true;
		}

		if (IsCancelPressed())
		{
			Button backButton = GetConfirmationDialogBackButton(items);
			if (backButton != null)
			{
				backButton.onClick.Invoke();
				return true;
			}
		}

		return false;
	}
}
