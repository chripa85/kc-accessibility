using System;
using System.Collections.Generic;
using Assets.Code;
using UnityEngine;
using UnityEngine.UI;

public partial class ScreenReaderAccessibilityBehaviour
{
	private bool TryHandlePlayingModeNavigation()
	{
		if (!HasPlayingModeNavigationInput())
		{
			return false;
		}

		EnsureKeyboardNavigationEnabled();
		if (TryHandleGameplayDiscoveryInput())
		{
			return true;
		}
		if (IsChopShortcutPressed())
		{
			return TryHandleAccessibleChopShortcut();
		}
		if (TryHandleAccessibleBuildMenuNavigation())
		{
			return true;
		}
		bool enableRequested = IsNavigateNextPressed()
			|| (!gameplayKeyboardNavigationActive && (IsNavigateUpPressed()
				|| IsNavigateDownPressed()
				|| IsNavigateLeftPressed()
				|| IsNavigateRightPressed()));
		if (enableRequested && !gameplayKeyboardNavigationActive)
		{
			gameplayKeyboardNavigationActive = true;
			ConsoleUIItem entry = GetPlayingModeEntryItem();
			if (entry != null && IsNavigateNextPressed())
			{
				FocusItem(entry);
			}
			else
			{
				bool enabledByTab = IsNavigateNextPressed();
				InitializeGameplayMapCursor(enabledByTab ? GameplayMapInitAnnouncement.Full : GameplayMapInitAnnouncement.None);
			}
			if (!IsNavigateNextPressed())
			{
				return TryHandleGameplayMapNavigation();
			}
			return true;
		}

		if (!gameplayKeyboardNavigationActive)
		{
			if (IsCancelPressed())
			{
				InitializeGameplayMapCursor(GameplayMapInitAnnouncement.EnabledOnly);
				gameplayKeyboardNavigationActive = true;
				return true;
			}
			return false;
		}

		if (IsPlacementModeActive())
		{
			ConsoleUIItem focusedPlacementItem = GetFocusedItem();
			if (focusedPlacementItem != null)
			{
				focusedPlacementItem.Dehover();
			}

			return TryHandleGameplayMapNavigation();
		}

		ConsoleUIItem current = GetFocusedItem();
		if (current == null)
		{
			if (IsNavigateNextPressed())
			{
				current = GetPlayingModeEntryItem();
				if (current != null)
				{
					FocusItem(current);
				}
			}
		}

		if (current == null)
		{
			if (TryHandleAccessiblePlayingPanelNavigation(null))
			{
				return true;
			}

			if (IsCancelPressed())
			{
				AnnounceCurrentGameplayMapCursor(interrupt: true);
				return true;
			}
			return TryHandleGameplayMapNavigation();
		}

		if (TryHandleAccessiblePlayingPanelNavigation(current))
		{
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			current.NavigateUp();
			return true;
		}
		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			current.NavigateDown();
			return true;
		}
		if (IsNavigateLeftPressed())
		{
			current.NavigateLeft();
			return true;
		}
		if (IsNavigateRightPressed())
		{
			current.NavigateRight();
			return true;
		}
		if (IsSubmitPressed())
		{
			current.Submit();
			return true;
		}
		if (IsCancelPressed())
		{
			current.Cancel();
			if (GetFocusedItem() == null)
			{
				InitializeGameplayMapCursor(GameplayMapInitAnnouncement.None);
				AnnounceCurrentGameplayMapCursor(interrupt: true);
			}
			return true;
		}

		return false;
	}

	private bool TryHandleAccessibleBuildMenuNavigation()
	{
		if (BuildUI.inst == null || !BuildUI.inst.Visible)
		{
			buildMenuItemIndex = -1;
			buildMenuDetailIndex = -1;
			return false;
		}

		BuildTab currentTab = BuildUI.inst.GetCurrentTab();
		if (currentTab == null)
		{
			return false;
		}

		int shortcutIndex = GetPressedPrimaryBuildCategoryShortcutIndex();
		if (shortcutIndex >= 0)
		{
			OpenAccessibleBuildCategory(PrimaryBuildCategoryTitles[shortcutIndex]);
			return true;
		}

		List<BuildingCostUpdater> items = GetAccessibleBuildMenuItems(currentTab);
		if (items.Count == 0)
		{
			buildMenuItemIndex = -1;
			buildMenuDetailIndex = -1;
		}
		else if (buildMenuItemIndex < 0 || buildMenuItemIndex >= items.Count)
		{
			buildMenuItemIndex = 0;
			buildMenuDetailIndex = -1;
			FocusAccessibleBuildMenuItem(currentTab, items, buildMenuItemIndex, interrupt: true, includeCategory: true);
			return true;
		}

		if (buildMenuDetailIndex >= 0 && items.Count > 0)
		{
			return TryHandleAccessibleBuildMenuDetailNavigation(currentTab, items);
		}

		if (IsNavigateLeftPressed())
		{
			CycleAccessibleBuildCategory(currentTab.title, -1);
			return true;
		}

		if (IsNavigateRightPressed())
		{
			CycleAccessibleBuildCategory(currentTab.title, 1);
			return true;
		}

		if (items.Count > 0 && (IsNavigateUpPressed() || IsNavigatePreviousPressed()))
		{
			buildMenuItemIndex = (buildMenuItemIndex - 1 + items.Count) % items.Count;
			buildMenuDetailIndex = -1;
			FocusAccessibleBuildMenuItem(currentTab, items, buildMenuItemIndex, interrupt: false, includeCategory: false);
			return true;
		}

		if (items.Count > 0 && (IsNavigateDownPressed() || IsNavigateNextPressed()))
		{
			buildMenuItemIndex = (buildMenuItemIndex + 1) % items.Count;
			buildMenuDetailIndex = -1;
			FocusAccessibleBuildMenuItem(currentTab, items, buildMenuItemIndex, interrupt: false, includeCategory: false);
			return true;
		}

		if (items.Count > 0 && IsSubmitPressed())
		{
			EnterAccessibleBuildMenuDetails(currentTab, items, buildMenuItemIndex);
			return true;
		}

		if (IsCancelPressed())
		{
			if (currentTab.isBackable && currentTab.back != null && currentTab.back.gameObject.activeInHierarchy)
			{
				currentTab.back.onClick.Invoke();
				BuildTab nextTab = BuildUI.inst.GetCurrentTab();
				buildMenuItemIndex = 0;
				buildMenuDetailIndex = -1;
				AnnounceAccessibleBuildCategory(nextTab ?? currentTab, interrupt: true);
			}
			else
			{
				BuildUI.inst.SetVisible(b: false);
				buildMenuItemIndex = -1;
				buildMenuDetailIndex = -1;
				AnnounceText("Build menu closed.", true);
			}
			return true;
		}

		return true;
	}

	private void CycleAccessibleBuildCategory(string currentTitle, int delta)
	{
		int index = GetPrimaryBuildCategoryIndex(currentTitle);
		index = (index + delta + PrimaryBuildCategoryTitles.Length) % PrimaryBuildCategoryTitles.Length;
		OpenAccessibleBuildCategory(PrimaryBuildCategoryTitles[index]);
	}

	private void OpenAccessibleBuildCategory(string title)
	{
		if (string.IsNullOrEmpty(title) || GameUI.inst == null)
		{
			return;
		}

		suppressedHoverAnnouncements++;
		try
		{
			GameUI.inst.OnShowBuildTabClicked(title);
			buildMenuItemIndex = 0;
			buildMenuDetailIndex = -1;
			BuildTab tab = BuildUI.inst != null ? BuildUI.inst.GetCurrentTab() : null;
			AnnounceAccessibleBuildCategory(tab, interrupt: true);
		}
		finally
		{
			suppressedHoverAnnouncements = Math.Max(0, suppressedHoverAnnouncements - 1);
		}
	}

	private void AnnounceAccessibleBuildCategory(BuildTab tab, bool interrupt)
	{
		if (tab == null)
		{
			return;
		}

		List<BuildingCostUpdater> items = GetAccessibleBuildMenuItems(tab);
		if (items.Count == 0)
		{
			FocusAccessibleBuildCategory(tab);
			AnnounceText(JoinParts(GetAccessibleBuildCategoryName(tab.title), IsAccessibleBuildCategoryLocked(tab) ? "Locked" : "No build options"), interrupt);
			return;
		}

		if (buildMenuItemIndex < 0 || buildMenuItemIndex >= items.Count)
		{
			buildMenuItemIndex = 0;
		}
		buildMenuDetailIndex = -1;

		FocusAccessibleBuildMenuItem(tab, items, buildMenuItemIndex, interrupt, includeCategory: true);
	}

	private List<BuildingCostUpdater> GetAccessibleBuildMenuItems(BuildTab tab)
	{
		List<BuildingCostUpdater> items = new List<BuildingCostUpdater>();
		if (tab == null)
		{
			return items;
		}

		for (int i = 0; i < tab.buttons.Count; i++)
		{
			BuildingCostUpdater item = tab.buttons[i];
			if (item != null && item.gameObject.activeInHierarchy)
			{
				items.Add(item);
			}
		}

		return items;
	}

	private void FocusAccessibleBuildMenuItem(BuildTab tab, List<BuildingCostUpdater> items, int index, bool interrupt, bool includeCategory)
	{
		if (items == null || index < 0 || index >= items.Count)
		{
			return;
		}

		BuildingCostUpdater item = items[index];
		ConsoleUIItem consoleItem = GetConsoleItemForComponent(item);
		if (consoleItem != null)
		{
			FocusItem(consoleItem, announce: false);
		}
		if (item.btn != null)
		{
			item.btn.Select();
		}

		PlayNavigationSound();
		AnnounceText(DescribeAccessibleBuildMenuItem(tab, item, index, includeCategory), interrupt);
	}

	private void FocusAccessibleBuildCategory(BuildTab tab)
	{
		if (tab == null)
		{
			return;
		}

		ConsoleUIItem consoleItem = GetConsoleItemForComponent(tab.buildButton);
		if (consoleItem != null)
		{
			FocusItem(consoleItem, announce: false);
		}

		Button categoryButton = tab.buildButton != null ? tab.buildButton.GetComponent<Button>() : null;
		if (categoryButton != null)
		{
			categoryButton.Select();
		}

		PlayNavigationSound();
	}

	private static bool IsAccessibleBuildCategoryLocked(BuildTab tab)
	{
		return tab != null && tab.buildButton != null && !tab.buildButton.unlocked;
	}

	private void ActivateAccessibleBuildMenuItem(BuildTab tab, BuildingCostUpdater item)
	{
		if (item == null)
		{
			return;
		}

		if (!item.IsEnabled())
		{
			AnnounceText(DescribeAccessibleBuildMenuItem(tab, item, buildMenuItemIndex, includeCategory: true), true);
			return;
		}

		Building prefab = item.prefab;
		if (prefab != null)
		{
			if (prefab.GetComponent<CemeteryBuildDummy>() != null)
			{
				GameUI.inst.OnShowBuildTabClicked("Cemetery");
			}
			else if (prefab.GetComponent<StatueBuildDummy>() != null)
			{
				GameUI.inst.OnShowBuildTabClicked("Statue");
			}
			else if (prefab.UniqueName == World.parkDummyName)
			{
				GameUI.inst.OnShowBuildTabClicked("Park");
			}
			else
			{
				GameUI.inst.OnBuild(item);
			}
		}
		BuildTab currentTab = BuildUI.inst != null ? BuildUI.inst.GetCurrentTab() : null;
		if (BuildUI.inst != null && BuildUI.inst.Visible && currentTab != null)
		{
			buildMenuItemIndex = 0;
			buildMenuDetailIndex = -1;
			AnnounceAccessibleBuildCategory(currentTab, interrupt: true);
			return;
		}

		if (IsPlacementModeActive())
		{
			ConsoleUIItem focusedPlacementItem = GetFocusedItem();
			if (focusedPlacementItem != null)
			{
				focusedPlacementItem.Dehover();
			}

			gameplayKeyboardNavigationActive = true;
			InitializeGameplayMapCursor(GameplayMapInitAnnouncement.None);
			Building hoverBuilding = GameUI.inst.CurrPlacementMode.GetHoverBuilding();
			string buildingName = hoverBuilding != null ? CleanText(hoverBuilding.FriendlyName) : string.Empty;
			if (string.IsNullOrEmpty(buildingName) && hoverBuilding != null)
			{
				buildingName = SplitIdentifier(hoverBuilding.UniqueName);
			}
			buildMenuDetailIndex = -1;
			AnnounceText(JoinParts(buildingName, "Placement mode", DescribeCurrentPlacementStatus()), true);
		}
	}

	private string DescribeAccessibleBuildMenuItem(BuildTab tab, BuildingCostUpdater item, int index, bool includeCategory)
	{
		if (item == null)
		{
			return string.Empty;
		}

		Building building = item.GetBuilding();
		string itemName = building != null ? CleanText(building.FriendlyName) : string.Empty;
		if (string.IsNullOrEmpty(itemName) && building != null)
		{
			itemName = SplitIdentifier(building.UniqueName);
		}

		string category = includeCategory && tab != null ? GetAccessibleBuildCategoryName(tab.title) : string.Empty;
		string position = (index + 1).ToString();
		string cost = DescribeBuildCost(building);
		string availability = item.IsEnabled() ? string.Empty : "Unavailable";
		return JoinParts(category, position, itemName, includeCategory ? cost : string.Empty, availability);
	}

	private bool TryHandleAccessibleBuildMenuDetailNavigation(BuildTab tab, List<BuildingCostUpdater> items)
	{
		if (items == null || buildMenuItemIndex < 0 || buildMenuItemIndex >= items.Count)
		{
			buildMenuDetailIndex = -1;
			return true;
		}

		List<string> detailLines = GetAccessibleBuildMenuDetailLines(tab, items[buildMenuItemIndex], buildMenuItemIndex);
		if (detailLines.Count == 0)
		{
			buildMenuDetailIndex = -1;
			FocusAccessibleBuildMenuItem(tab, items, buildMenuItemIndex, interrupt: true, includeCategory: false);
			return true;
		}

		if (buildMenuDetailIndex < 0 || buildMenuDetailIndex >= detailLines.Count)
		{
			buildMenuDetailIndex = 0;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			buildMenuDetailIndex = (buildMenuDetailIndex - 1 + detailLines.Count) % detailLines.Count;
			AnnounceText(detailLines[buildMenuDetailIndex], false);
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			buildMenuDetailIndex = (buildMenuDetailIndex + 1) % detailLines.Count;
			AnnounceText(detailLines[buildMenuDetailIndex], false);
			return true;
		}

		if (IsSubmitPressed())
		{
			if (buildMenuDetailIndex == 0)
			{
				ActivateAccessibleBuildMenuItem(tab, items[buildMenuItemIndex]);
			}
			else
			{
				AnnounceText(detailLines[buildMenuDetailIndex], true);
			}
			return true;
		}

		if (IsCancelPressed())
		{
			buildMenuDetailIndex = -1;
			FocusAccessibleBuildMenuItem(tab, items, buildMenuItemIndex, interrupt: true, includeCategory: false);
			return true;
		}

		return true;
	}

	private void EnterAccessibleBuildMenuDetails(BuildTab tab, List<BuildingCostUpdater> items, int index)
	{
		if (items == null || index < 0 || index >= items.Count)
		{
			return;
		}

		buildMenuDetailIndex = 0;
		List<string> detailLines = GetAccessibleBuildMenuDetailLines(tab, items[index], index);
		if (detailLines.Count == 0)
		{
			buildMenuDetailIndex = -1;
			FocusAccessibleBuildMenuItem(tab, items, index, interrupt: true, includeCategory: false);
			return;
		}

		AnnounceText(detailLines[0], true);
	}

	private List<string> GetAccessibleBuildMenuDetailLines(BuildTab tab, BuildingCostUpdater item, int index)
	{
		List<string> lines = new List<string>();
		if (item == null)
		{
			return lines;
		}

		Building building = item.GetBuilding();
		if (building == null)
		{
			return lines;
		}

		string itemName = CleanText(building.FriendlyName);
		if (string.IsNullOrEmpty(itemName))
		{
			itemName = SplitIdentifier(building.UniqueName);
		}

		string availability = item.IsEnabled() ? "Build " + itemName : JoinParts("Build " + itemName, "Unavailable", DescribeBuildLockedReason(building));
		lines.Add(availability);

		string cost = DescribeBuildCost(building);
		if (!string.IsNullOrEmpty(cost))
		{
			lines.Add(cost);
		}

		string description = CleanText(building.Description);
		if (!string.IsNullOrEmpty(description))
		{
			lines.Add(description);
		}

		lines.Add("Size " + (int)building.size.x + " by " + (int)building.size.z + ".");

		if (building.WorkersForFullYield > 0 && building.CategoryName != "house" && building.CategoryName != "ship")
		{
			lines.Add("Workers " + building.WorkersForFullYield + ".");
		}

		WagePayer wagePayer = building.GetComponent<WagePayer>();
		if (wagePayer != null && Player.inst != null && Player.inst.PlayerLandmassOwner != null)
		{
			float yearlyWage = wagePayer.GetGoldWage(Player.inst.PlayerLandmassOwner) * wagePayer.PaydaysPerYear();
			lines.Add("Wages " + yearlyWage.ToString("0.##") + " gold per year.");
		}

		return lines;
	}

	private string DescribeBuildLockedReason(Building building)
	{
		if (building == null || BuildInfoFloating.inst == null || Player.inst == null)
		{
			return string.Empty;
		}

		bool canAfford = BuildInfoFloating.CanAfford(Player.inst.FocusedLandMass, building);
		BuildInfoFloating.SpecialBuildingPrereq prereq;
		Building prereqBuilding = GetBuildPrerequisiteBuilding(building);
		bool enabled = BuildInfoFloating.inst.CheckPrereqs(building, canAfford, prereqBuilding, out prereq);
		if (enabled)
		{
			return string.Empty;
		}

		switch (prereq)
		{
			case BuildInfoFloating.SpecialBuildingPrereq.Building:
				return prereqBuilding != null ? "Requires " + prereqBuilding.FriendlyName + "." : "Requires another building.";
			case BuildInfoFloating.SpecialBuildingPrereq.ChamberOfWarOne:
				return "Only one Chamber of War is allowed on this landmass.";
			case BuildInfoFloating.SpecialBuildingPrereq.ShipTwoDocks:
				return prereqBuilding != null ? "Requires two " + prereqBuilding.FriendlyName + "." : "Requires two docks.";
			case BuildInfoFloating.SpecialBuildingPrereq.NotPlayerLandmass:
				return "Not on your landmass.";
			default:
				return canAfford ? string.Empty : "Not enough resources.";
		}
	}

	private static Building GetBuildPrerequisiteBuilding(Building building)
	{
		if (building == null || GameState.inst == null)
		{
			return null;
		}

		if (string.Equals(building.UniqueName, "transportship", StringComparison.OrdinalIgnoreCase))
		{
			return GameState.inst.GetPlaceableByUniqueName("dock");
		}

		BuildUI buildUi = BuildUI.inst;
		if (buildUi == null)
		{
			return null;
		}

		BuildTab[] tabs = new[]
		{
			buildUi.CastleTab,
			buildUi.TownTab,
			buildUi.AdvTownTab,
			buildUi.FoodTab,
			buildUi.IndustryTab,
			buildUi.MaritimeTab,
			buildUi.CemeteryTab,
			buildUi.StatueTab,
			buildUi.ParkTab
		};

		for (int i = 0; i < tabs.Length; i++)
		{
			BuildTab tab = tabs[i];
			if (tab == null)
			{
				continue;
			}
			for (int j = 0; j < tab.buttons.Count; j++)
			{
				BuildingCostUpdater updater = tab.buttons[j];
				if (updater != null && updater.prefab != null && updater.prefab.UniqueName == building.UniqueName)
				{
					return updater.PreReq;
				}
			}
		}

		return null;
	}

	private string DescribeBuildCost(Building building)
	{
		if (building == null || Player.inst == null || Player.inst.PlayerLandmassOwner == null)
		{
			return string.Empty;
		}

		ResourceAmount cost = building.GetCost(Player.inst.PlayerLandmassOwner);
		List<string> parts = new List<string>();
		AppendResourceCost(parts, cost.Get(FreeResourceType.Gold), "gold");
		AppendResourceCost(parts, cost.Get(FreeResourceType.Tree), "wood");
		AppendResourceCost(parts, cost.Get(FreeResourceType.Stone), "stone");
		AppendResourceCost(parts, cost.Get(FreeResourceType.Tools), "tools");
		AppendResourceCost(parts, cost.Get(FreeResourceType.Charcoal), "charcoal");
		AppendResourceCost(parts, cost.Get(FreeResourceType.IronOre), "iron ore");
		return parts.Count == 0 ? string.Empty : "Cost " + string.Join(", ", parts);
	}

	private static void AppendResourceCost(List<string> parts, int amount, string label)
	{
		if (parts == null || amount <= 0)
		{
			return;
		}

		parts.Add(amount + " " + label);
	}

	private int GetPressedPrimaryBuildCategoryShortcutIndex()
	{
		KeyCode[] keys = new[]
		{
			KeyCode.Alpha1,
			KeyCode.Alpha2,
			KeyCode.Alpha3,
			KeyCode.Alpha4,
			KeyCode.Alpha5,
			KeyCode.Alpha6
		};

		for (int i = 0; i < keys.Length; i++)
		{
			if (Input.GetKeyDown(keys[i]))
			{
				return i;
			}
		}

		return -1;
	}

	private int GetPrimaryBuildCategoryIndex(string title)
	{
		if (string.Equals(title, "Cemetery", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(title, "Statue", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(title, "Park", StringComparison.OrdinalIgnoreCase))
		{
			title = "AdvTown";
		}

		for (int i = 0; i < PrimaryBuildCategoryTitles.Length; i++)
		{
			if (string.Equals(PrimaryBuildCategoryTitles[i], title, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}

		return 0;
	}

	private string GetAccessibleBuildCategoryName(string title)
	{
		if (string.IsNullOrEmpty(title))
		{
			return "Build";
		}

		switch (title)
		{
			case "AdvTown":
				return "Advanced town";
			default:
				return SplitIdentifier(title);
		}
	}
}
