using System;
using System.Collections.Generic;

public partial class ScreenReaderAccessibilityBehaviour
{
	private sealed class MainMenuNavigationRoute
	{
		public readonly string HandlerName;
		public readonly Func<bool> TryHandle;

		public MainMenuNavigationRoute(string handlerName, Func<bool> tryHandle)
		{
			HandlerName = handlerName;
			TryHandle = tryHandle;
		}
	}

	private sealed class GameplayPanelNavigationRoute
	{
		public readonly string HandlerName;
		public readonly Func<ConsoleUIItem, bool> TryHandle;

		public GameplayPanelNavigationRoute(string handlerName, Func<ConsoleUIItem, bool> tryHandle)
		{
			HandlerName = handlerName;
			TryHandle = tryHandle;
		}
	}

	private sealed class HarmonyPostfixRoute
	{
		public readonly Type TargetType;
		public readonly string TargetMethodName;
		public readonly Type PatchType;
		public readonly string PatchMethodName;
		public readonly Type[] ParameterTypes;

		public HarmonyPostfixRoute(Type targetType, string targetMethodName, Type patchType, string patchMethodName, Type[] parameterTypes)
		{
			TargetType = targetType;
			TargetMethodName = targetMethodName;
			PatchType = patchType;
			PatchMethodName = patchMethodName;
			ParameterTypes = parameterTypes;
		}
	}

	private Dictionary<MainMenuMode.State, MainMenuNavigationRoute> mainMenuNavigationRoutes;
	private List<GameplayPanelNavigationRoute> gameplayPanelNavigationRoutes;

	private void EnsureNavigationRoutesInitialized()
	{
		if (mainMenuNavigationRoutes == null)
		{
			mainMenuNavigationRoutes = new Dictionary<MainMenuMode.State, MainMenuNavigationRoute>
			{
				{ MainMenuMode.State.Menu, new MainMenuNavigationRoute("Top-level main menu", TryHandleTopLevelMainMenuNavigation) },
				{ MainMenuMode.State.ChooseMode, new MainMenuNavigationRoute("Choose mode", TryHandleChooseModeNavigation) },
				{ MainMenuMode.State.ChooseDifficulty, new MainMenuNavigationRoute("Choose difficulty", TryHandleChooseDifficultyNavigation) },
				{ MainMenuMode.State.NameAndBanner, new MainMenuNavigationRoute("Name and banner", TryHandleNameAndBannerNavigation) },
				{ MainMenuMode.State.NewMap, new MainMenuNavigationRoute("New map", TryHandleNewMapNavigation) },
				{ MainMenuMode.State.SettingsMenu, new MainMenuNavigationRoute("Settings menu", TryHandleSettingsMenuNavigation) },
				{ MainMenuMode.State.PauseMenu, new MainMenuNavigationRoute("Pause menu", TryHandlePauseMenuNavigation) },
				{ MainMenuMode.State.QuitConfirm, new MainMenuNavigationRoute("Confirmation dialog", TryHandleConfirmationDialogNavigation) },
				{ MainMenuMode.State.ExitConfirm, new MainMenuNavigationRoute("Confirmation dialog", TryHandleConfirmationDialogNavigation) }
			};
		}

		if (gameplayPanelNavigationRoutes == null)
		{
			gameplayPanelNavigationRoutes = new List<GameplayPanelNavigationRoute>
			{
				new GameplayPanelNavigationRoute("Advisor panel", TryHandleAccessibleAdvisorNavigation),
				new GameplayPanelNavigationRoute("Person panel", delegate(ConsoleUIItem _) { return TryHandleAccessiblePersonNavigation(); }),
				new GameplayPanelNavigationRoute("Worker panel", TryHandleAccessibleWorkerNavigation),
				new GameplayPanelNavigationRoute("Construct panel", TryHandleAccessibleConstructNavigation),
				new GameplayPanelNavigationRoute("Decree panel", TryHandleAccessibleDecreeNavigation),
				new GameplayPanelNavigationRoute("Island info panel", TryHandleAccessibleIslandInfoNavigation),
				new GameplayPanelNavigationRoute("Foreign island info panel", TryHandleAccessibleForeignIslandInfoNavigation)
			};
		}
	}

	private HarmonyPostfixRoute[] GetHarmonyPostfixRoutes()
	{
		return new HarmonyPostfixRoute[]
		{
			new HarmonyPostfixRoute(typeof(MainMenuMode), "TransitionTo", typeof(MenuAccessibilityPatches), "MainMenuTransitionPostfix", new Type[] { typeof(MainMenuMode.State) }),
			new HarmonyPostfixRoute(typeof(ConsoleUIItem), "Hover", typeof(MenuAccessibilityPatches), "ConsoleItemHoverPostfix", Type.EmptyTypes),
			new HarmonyPostfixRoute(typeof(DifficultySelectUI), "RefreshDisplay", typeof(MenuAccessibilityPatches), "DifficultyRefreshPostfix", Type.EmptyTypes),
			new HarmonyPostfixRoute(typeof(Assets.Code.UI.SaveLoadUI), "SetupLoadUI", typeof(MenuAccessibilityPatches), "SaveLoadSetupLoadPostfix", Type.EmptyTypes),
			new HarmonyPostfixRoute(typeof(Assets.Code.UI.SaveLoadUI), "SetupSaveUI", typeof(MenuAccessibilityPatches), "SaveLoadSetupSavePostfix", Type.EmptyTypes),
			new HarmonyPostfixRoute(typeof(PickNameUI), "OnEnable", typeof(MenuAccessibilityPatches), "PickNameOnEnablePostfix", Type.EmptyTypes),
			new HarmonyPostfixRoute(typeof(ChooseBannerUI), "Show", typeof(MenuAccessibilityPatches), "ChooseBannerShowPostfix", new Type[] { typeof(ConsoleUIItem) }),
			new HarmonyPostfixRoute(typeof(ChooseBannerUI), "Close", typeof(MenuAccessibilityPatches), "ChooseBannerClosePostfix", Type.EmptyTypes),
			new HarmonyPostfixRoute(typeof(SettingsMenuUI), "OnEnable", typeof(MenuAccessibilityPatches), "SettingsMenuEnabledPostfix", Type.EmptyTypes),
			new HarmonyPostfixRoute(typeof(SettingsMenuUI), "ChangePreferenceGroup", typeof(MenuAccessibilityPatches), "SettingsPreferenceTabPostfix", new Type[] { typeof(bool) }),
			new HarmonyPostfixRoute(typeof(SettingsMenuUI), "ChangeGraphicsGroup", typeof(MenuAccessibilityPatches), "SettingsGraphicsTabPostfix", new Type[] { typeof(bool) }),
			new HarmonyPostfixRoute(typeof(SettingsMenuUI), "ChangeKeyboardGroup", typeof(MenuAccessibilityPatches), "SettingsKeyboardTabPostfix", new Type[] { typeof(bool) }),
			new HarmonyPostfixRoute(typeof(GameState), "SetNewMode", typeof(MenuAccessibilityPatches), "SetNewModePostfix", new Type[] { typeof(GameMode) }),
			new HarmonyPostfixRoute(typeof(MainMenuMode), "StartGame", typeof(MenuAccessibilityPatches), "MainMenuStartGamePostfix", Type.EmptyTypes)
		};
	}

	private void ResetGameplayNavigationStateIfNeeded()
	{
		AccessibilityStateSnapshot snapshot = CaptureAccessibilityStateSnapshot();
		if (!snapshot.IsPlayingModeActive && gameplayKeyboardNavigationActive)
		{
			gameplayKeyboardNavigationActive = false;
			gameplayMapCursorCell = null;
			gameplayTileElementIndex = -1;
		}
	}

	private bool TryHandleMainMenuNavigation(out string handledBy)
	{
		handledBy = string.Empty;
		AccessibilityStateSnapshot snapshot = CaptureAccessibilityStateSnapshot();

		if (!snapshot.IsMainMenuActive)
		{
			return false;
		}

		if (!HasKeyboardNavigationInput())
		{
			return false;
		}

		EnsureKeyboardNavigationEnabled();
		MainMenuMode.State state = snapshot.MainMenuState ?? MainMenuMode.State.Uninitialized;
		if (TryHandleMainMenuNavigationRouted(state, out handledBy))
		{
			return true;
		}

		return TryHandleGenericMainMenuNavigation(out handledBy);
	}

	private bool TryHandleMainMenuNavigationRouted(MainMenuMode.State state, out string handledBy)
	{
		handledBy = string.Empty;
		EnsureNavigationRoutesInitialized();

		MainMenuNavigationRoute route;
		if (!mainMenuNavigationRoutes.TryGetValue(state, out route))
		{
			return false;
		}

		if (!route.TryHandle())
		{
			return false;
		}

		handledBy = route.HandlerName;
		return true;
	}

	private bool TryHandleGenericMainMenuNavigation(out string handledBy)
	{
		handledBy = string.Empty;
		ConsoleUIItem current = GetFocusedItem();
		if (current == null)
		{
			current = GetRootItemForCurrentMenu();
			if (current != null)
			{
				log("Keyboard navigation root item: " + current.name);
				current.Hover();
				ConsoleUIItem hoveredItem = GetFocusedItem();
				if (hoveredItem != null)
				{
					current = hoveredItem;
				}
				else
				{
					log("No focused child after hovering root item; using root directly.");
					AnnounceConsoleFocus(current);
				}
			}
		}

		if (current == null)
		{
			log("Keyboard navigation could not resolve a focusable item for state " + GameState.inst.mainMenuMode.GetState() + ".");
			return false;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			current.NavigateUp();
			handledBy = "Generic menu NavigateUp";
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			current.NavigateDown();
			handledBy = "Generic menu NavigateDown";
			return true;
		}

		if (IsNavigateLeftPressed())
		{
			current.NavigateLeft();
			handledBy = "Generic menu NavigateLeft";
			return true;
		}

		if (IsNavigateRightPressed())
		{
			current.NavigateRight();
			handledBy = "Generic menu NavigateRight";
			return true;
		}

		if (IsSubmitPressed() || IsSubmitAlternatePressed())
		{
			current.Submit();
			handledBy = "Generic menu Submit";
			return true;
		}

		if (IsCancelPressed())
		{
			current.Cancel();
			handledBy = "Generic menu Cancel";
			return true;
		}

		return false;
	}

	private bool TryHandleAccessiblePlayingPanelNavigationRouted(ConsoleUIItem current, out string handledBy)
	{
		handledBy = string.Empty;
		EnsureNavigationRoutesInitialized();

		for (int i = 0; i < gameplayPanelNavigationRoutes.Count; i++)
		{
			GameplayPanelNavigationRoute route = gameplayPanelNavigationRoutes[i];
			if (route.TryHandle(current))
			{
				handledBy = route.HandlerName;
				return true;
			}
		}

		gameplayPanelIndex = -1;
		return false;
	}
}
