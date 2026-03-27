using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class ScreenReaderAccessibilityBehaviour
{
	private bool TryHandleAccessibleAdvisorNavigation(ConsoleUIItem current)
	{
		AdvisorUI advisor = AdvisorUI.inst;
		if (advisor == null || !advisor.IsVisible() || advisor.consoleItem == null)
		{
			gameplayAdvisorIndex = -1;
			return false;
		}

		if (current != null && !IsFocusedWithin(current, advisor.consoleItem))
		{
			gameplayAdvisorIndex = -1;
			return false;
		}

		List<AccessiblePanelItem> items = GetAccessibleAdvisorItems(advisor);
		if (items.Count == 0)
		{
			return false;
		}

		if (gameplayAdvisorIndex < 0 || gameplayAdvisorIndex >= items.Count)
		{
			gameplayAdvisorIndex = NormalizePanelIndex(gameplayAdvisorIndex, items.Count);
			AnnounceText(items[gameplayAdvisorIndex].Speech, true);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			gameplayAdvisorIndex = WrapPanelIndex(gameplayAdvisorIndex, items.Count, -1);
			AnnounceText(items[gameplayAdvisorIndex].Speech, false);
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			gameplayAdvisorIndex = WrapPanelIndex(gameplayAdvisorIndex, items.Count, 1);
			AnnounceText(items[gameplayAdvisorIndex].Speech, false);
			return true;
		}

		if (IsSubmitPressed())
		{
			items[gameplayAdvisorIndex].Activate?.Invoke();
			if (advisor.IsVisible())
			{
				List<AccessiblePanelItem> refreshed = GetAccessibleAdvisorItems(advisor);
				gameplayAdvisorIndex = AnnounceRefreshedPanelItem(refreshed, gameplayAdvisorIndex);
			}
			else
			{
				AnnounceCurrentGameplayMapCursor(interrupt: true);
			}
			return true;
		}

		if (IsCancelPressed())
		{
			gameplayAdvisorIndex = -1;
			InvokeWithHoverAnnouncementSuppressed(advisor.Hide);
			AnnounceCurrentGameplayMapCursor(interrupt: true);
			return true;
		}

		return true;
	}

	private bool TryHandleAccessiblePersonNavigation()
	{
		PersonUI person = PersonUI.inst;
		if (person == null || !person.Visible || person.villager == null)
		{
			gameplayPersonIndex = -1;
			return false;
		}

		List<AccessiblePanelItem> items = GetAccessiblePersonItems(person);
		if (items.Count == 0)
		{
			return false;
		}

		if (gameplayPersonIndex < 0 || gameplayPersonIndex >= items.Count)
		{
			gameplayPersonIndex = NormalizePanelIndex(gameplayPersonIndex, items.Count);
			AnnounceText(items[gameplayPersonIndex].Speech, true);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			gameplayPersonIndex = WrapPanelIndex(gameplayPersonIndex, items.Count, -1);
			AnnounceText(items[gameplayPersonIndex].Speech, false);
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			gameplayPersonIndex = WrapPanelIndex(gameplayPersonIndex, items.Count, 1);
			AnnounceText(items[gameplayPersonIndex].Speech, false);
			return true;
		}

		if (IsSubmitPressed())
		{
			items[gameplayPersonIndex].Activate?.Invoke();
			if (person.Visible)
			{
				List<AccessiblePanelItem> refreshed = GetAccessiblePersonItems(person);
				gameplayPersonIndex = AnnounceRefreshedPanelItem(refreshed, gameplayPersonIndex);
			}
			else
			{
				AnnounceWorkerOrGameplaySelection();
			}
			return true;
		}

		if (IsCancelPressed())
		{
			gameplayPersonIndex = -1;
			InvokeWithHoverAnnouncementSuppressed(delegate
			{
				person.SetVisible(v: false);
			});
			AnnounceWorkerOrGameplaySelection();
			return true;
		}

		return true;
	}

	private bool TryHandleAccessibleWorkerNavigation(ConsoleUIItem current)
	{
		WorkerUI worker = GameUI.inst != null ? GameUI.inst.workerUI : null;
		ConsoleUIItem root = worker != null ? worker.GetComponent<ConsoleUIItem>() : null;
		if (worker == null || !worker.Visible || root == null)
		{
			gameplayWorkerIndex = -1;
			return false;
		}

		if (current != null && !IsFocusedWithin(current, root))
		{
			gameplayWorkerIndex = -1;
			return false;
		}

		List<AccessiblePanelItem> items = GetAccessibleWorkerItems(worker);
		if (items.Count == 0)
		{
			return false;
		}

		if (gameplayWorkerIndex < 0 || gameplayWorkerIndex >= items.Count)
		{
			gameplayWorkerIndex = NormalizePanelIndex(gameplayWorkerIndex, items.Count);
			AnnounceText(items[gameplayWorkerIndex].Speech, true);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			gameplayWorkerIndex = WrapPanelIndex(gameplayWorkerIndex, items.Count, -1);
			AnnounceText(items[gameplayWorkerIndex].Speech, false);
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			gameplayWorkerIndex = WrapPanelIndex(gameplayWorkerIndex, items.Count, 1);
			AnnounceText(items[gameplayWorkerIndex].Speech, false);
			return true;
		}

		if (IsSubmitPressed())
		{
			items[gameplayWorkerIndex].Activate?.Invoke();
			if (AdvisorUI.inst != null && AdvisorUI.inst.IsVisible())
			{
				gameplayAdvisorIndex = -1;
				return true;
			}
			if (PersonUI.inst != null && PersonUI.inst.Visible)
			{
				gameplayPersonIndex = -1;
				return true;
			}
			if (worker.Visible)
			{
				List<AccessiblePanelItem> refreshed = GetAccessibleWorkerItems(worker);
				gameplayWorkerIndex = AnnounceRefreshedPanelItem(refreshed, gameplayWorkerIndex);
			}
			else
			{
				AnnounceCurrentGameplayMapCursor(interrupt: true);
			}
			return true;
		}

		if (IsCancelPressed())
		{
			gameplayWorkerIndex = -1;
			InvokeWithHoverAnnouncementSuppressed(delegate
			{
				worker.SetVisible(v: false);
			});
			AnnounceCurrentGameplayMapCursor(interrupt: true);
			return true;
		}

		return true;
	}

	private bool TryHandleAccessibleConstructNavigation(ConsoleUIItem current)
	{
		ConstructUI construct = GameUI.inst != null ? GameUI.inst.constructUI : null;
		if (construct == null || !construct.Visible || construct.consoleItem == null)
		{
			gameplayConstructIndex = -1;
			return false;
		}

		if (!IsFocusedWithin(current, construct.consoleItem))
		{
			gameplayConstructIndex = -1;
			return false;
		}

		List<string> items = GetAccessibleConstructItems(construct);
		if (items.Count == 0)
		{
			return false;
		}

		if (gameplayConstructIndex < 0 || gameplayConstructIndex >= items.Count)
		{
			gameplayConstructIndex = NormalizePanelIndex(gameplayConstructIndex, items.Count);
			AnnounceText(items[gameplayConstructIndex], true);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			gameplayConstructIndex = WrapPanelIndex(gameplayConstructIndex, items.Count, -1);
			AnnounceText(items[gameplayConstructIndex], false);
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			gameplayConstructIndex = WrapPanelIndex(gameplayConstructIndex, items.Count, 1);
			AnnounceText(items[gameplayConstructIndex], false);
			return true;
		}

		if (IsSubmitPressed())
		{
			ActivateAccessibleConstructItem(construct, gameplayConstructIndex);
			List<string> refreshedItems = GetAccessibleConstructItems(construct);
			gameplayConstructIndex = AnnounceRefreshedPanelItem(refreshedItems, gameplayConstructIndex);
			return true;
		}

		if (IsCancelPressed())
		{
			gameplayConstructIndex = -1;
			InvokeWithHoverAnnouncementSuppressed(delegate
			{
				construct.SetVisible(visible: false);
			});
			AnnounceCurrentGameplayMapCursor(interrupt: true);
			return true;
		}

		return true;
	}

	private bool TryHandleAccessibleDecreeNavigation(ConsoleUIItem current)
	{
		DecreeUI decree = DecreeUI.inst;
		if (decree == null || !decree.Visible || decree.consoleEntry == null)
		{
			gameplayJobPriorityIndex = -1;
			return false;
		}

		if (!IsFocusedWithin(current, decree.consoleEntry))
		{
			BuildPriorityItem focusedItem = current != null ? current.GetComponentInParent<BuildPriorityItem>() : null;
			if (focusedItem == null)
			{
				gameplayJobPriorityIndex = -1;
				return false;
			}
		}

		List<BuildPriorityItem> items = GetAccessibleDecreeItems(decree);
		if (items.Count == 0)
		{
			return false;
		}

		if (gameplayJobPriorityIndex < 0 || gameplayJobPriorityIndex >= items.Count)
		{
			int focusedIndex = GetFocusedAccessibleDecreeItemIndex(items, current);
			gameplayJobPriorityIndex = focusedIndex >= 0 ? focusedIndex : 0;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			gameplayJobPriorityIndex = WrapPanelIndex(gameplayJobPriorityIndex, items.Count, -1);
			AnnounceText(DescribeAccessibleDecreeItem(items[gameplayJobPriorityIndex]), false);
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			gameplayJobPriorityIndex = WrapPanelIndex(gameplayJobPriorityIndex, items.Count, 1);
			AnnounceText(DescribeAccessibleDecreeItem(items[gameplayJobPriorityIndex]), false);
			return true;
		}

		if (IsNavigateLeftPressed())
		{
			MoveAccessibleDecreeItem(items, gameplayJobPriorityIndex, -1);
			return true;
		}

		if (IsNavigateRightPressed())
		{
			MoveAccessibleDecreeItem(items, gameplayJobPriorityIndex, 1);
			return true;
		}

		if (IsSubmitPressed())
		{
			ToggleAccessibleDecreeItem(items[gameplayJobPriorityIndex]);
			return true;
		}

		if (IsCancelPressed())
		{
			gameplayJobPriorityIndex = -1;
			InvokeWithHoverAnnouncementSuppressed(delegate
			{
				decree.SetVisible(v: false);
				IslandInfoContainer islandInfo = GameUI.inst != null ? GameUI.inst.islandInfoUI : null;
				if (islandInfo != null && islandInfo.consoleItemEntry != null && islandInfo.isShowing)
				{
					islandInfo.consoleItemEntry.Hover();
				}
			});
			gameplayPanelIndex = 1;
			AnnounceText(DescribeIslandInfoJobPriorities(GameUI.inst != null ? GameUI.inst.islandInfoUI : null), true);
			return true;
		}

		return true;
	}

	private bool TryHandleAccessibleIslandInfoNavigation(ConsoleUIItem current)
	{
		IslandInfoContainer islandInfo = GameUI.inst != null ? GameUI.inst.islandInfoUI : null;
		if (islandInfo == null || !islandInfo.isShowing || islandInfo.consoleItemEntry == null)
		{
			return false;
		}

		if (!IsFocusedWithin(current, islandInfo.consoleItemEntry))
		{
			if (gameplayPanelIndex >= 0)
			{
				gameplayPanelIndex = -1;
			}
			return false;
		}

		List<string> items = GetAccessibleIslandInfoItems(islandInfo);
		if (items.Count == 0)
		{
			return false;
		}

		if (gameplayPanelIndex < 0 || gameplayPanelIndex >= items.Count)
		{
			gameplayPanelIndex = NormalizePanelIndex(gameplayPanelIndex, items.Count);
			AnnounceText(items[gameplayPanelIndex], true);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			gameplayPanelIndex = WrapPanelIndex(gameplayPanelIndex, items.Count, -1);
			AnnounceText(items[gameplayPanelIndex], false);
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			gameplayPanelIndex = WrapPanelIndex(gameplayPanelIndex, items.Count, 1);
			AnnounceText(items[gameplayPanelIndex], false);
			return true;
		}

		if (gameplayPanelIndex == 2 && (IsNavigateLeftPressed() || IsNavigateRightPressed()))
		{
			TaxRateUI tax = islandInfo.TaxRateUI;
			if (tax != null)
			{
				Button button = IsNavigateLeftPressed() ? tax.decrease : tax.increase;
				if (button != null && button.gameObject.activeInHierarchy && button.interactable)
				{
					button.onClick.Invoke();
				}
			}
			List<string> refreshedItems = GetAccessibleIslandInfoItems(islandInfo);
			gameplayPanelIndex = AnnounceRefreshedPanelItem(refreshedItems, gameplayPanelIndex);
			return true;
		}

		if (IsSubmitPressed())
		{
			ActivateAccessibleIslandInfoItem(islandInfo, gameplayPanelIndex);
			if (gameplayPanelIndex == 1 && DecreeUI.inst != null && DecreeUI.inst.Visible)
			{
				return true;
			}
			List<string> refreshedItems = GetAccessibleIslandInfoItems(islandInfo);
			gameplayPanelIndex = AnnounceRefreshedPanelItem(refreshedItems, gameplayPanelIndex);
			return true;
		}

		if (IsCancelPressed())
		{
			gameplayPanelIndex = -1;
			islandInfo.consoleItemEntry.Cancel();
			if (GetFocusedItem() == null)
			{
				AnnounceCurrentGameplayMapCursor(interrupt: true);
			}
			return true;
		}

		return true;
	}

	private bool TryHandleAccessibleForeignIslandInfoNavigation(ConsoleUIItem current)
	{
		ForeignIslandInfoContainer foreignInfo = GameUI.inst != null ? GameUI.inst.foreignIslandInfoUI : null;
		if (foreignInfo == null || !foreignInfo.isShowing || foreignInfo.consoleItemEntry == null)
		{
			return false;
		}

		if (!IsFocusedWithin(current, foreignInfo.consoleItemEntry))
		{
			if (gameplayPanelIndex >= 0)
			{
				gameplayPanelIndex = -1;
			}
			return false;
		}

		List<string> items = GetAccessibleForeignIslandInfoItems(foreignInfo);
		if (items.Count == 0)
		{
			return false;
		}

		if (gameplayPanelIndex < 0 || gameplayPanelIndex >= items.Count)
		{
			gameplayPanelIndex = NormalizePanelIndex(gameplayPanelIndex, items.Count);
			AnnounceText(items[gameplayPanelIndex], true);
			return true;
		}

		if (IsNavigateUpPressed() || IsNavigatePreviousPressed())
		{
			gameplayPanelIndex = WrapPanelIndex(gameplayPanelIndex, items.Count, -1);
			AnnounceText(items[gameplayPanelIndex], false);
			return true;
		}

		if (IsNavigateDownPressed() || IsNavigateNextPressed())
		{
			gameplayPanelIndex = WrapPanelIndex(gameplayPanelIndex, items.Count, 1);
			AnnounceText(items[gameplayPanelIndex], false);
			return true;
		}

		if (IsSubmitPressed())
		{
			if (gameplayPanelIndex == 1 && foreignInfo.hostilityButton != null && foreignInfo.hostilityButton.gameObject.activeInHierarchy)
			{
				foreignInfo.OnClickedHostility();
			}
			List<string> refreshedItems = GetAccessibleForeignIslandInfoItems(foreignInfo);
			gameplayPanelIndex = AnnounceRefreshedPanelItem(refreshedItems, gameplayPanelIndex);
			return true;
		}

		if (IsCancelPressed())
		{
			gameplayPanelIndex = -1;
			foreignInfo.consoleItemEntry.Cancel();
			if (GetFocusedItem() == null)
			{
				AnnounceCurrentGameplayMapCursor(interrupt: true);
			}
			return true;
		}

		return true;
	}

	private static int WrapPanelIndex(int index, int count, int delta)
	{
		return (index + delta + count) % count;
	}

	private static int NormalizePanelIndex(int index, int count)
	{
		return index < 0 || index >= count ? 0 : index;
	}

	private int AnnounceRefreshedPanelItem(List<string> items, int currentIndex)
	{
		if (items == null || items.Count == 0)
		{
			return currentIndex;
		}

		int clampedIndex = Math.Min(currentIndex, items.Count - 1);
		AnnounceText(items[clampedIndex], true);
		return clampedIndex;
	}

	private int AnnounceRefreshedPanelItem(List<AccessiblePanelItem> items, int currentIndex)
	{
		if (items == null || items.Count == 0)
		{
			return currentIndex;
		}

		int clampedIndex = Math.Min(currentIndex, items.Count - 1);
		AnnounceText(items[clampedIndex].Speech, true);
		return clampedIndex;
	}
}
