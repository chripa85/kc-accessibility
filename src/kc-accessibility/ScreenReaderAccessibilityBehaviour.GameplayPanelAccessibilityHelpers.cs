using System;
using System.Collections.Generic;
using Assets.Code;
using UnityEngine;
using UnityEngine.UI;

public partial class ScreenReaderAccessibilityBehaviour
{
	private bool TryHandleAccessiblePlayingPanelNavigation(ConsoleUIItem current)
	{
		string handledBy;
		return TryHandleAccessiblePlayingPanelNavigationRouted(current, out handledBy);
	}

	private List<AccessiblePanelItem> GetAccessibleAdvisorItems(AdvisorUI advisor)
	{
		List<AccessiblePanelItem> items = new List<AccessiblePanelItem>();
		if (advisor == null)
		{
			return items;
		}

		for (int i = 0; i < 3; i++)
		{
			int advisorIndex = i;
			items.Add(new AccessiblePanelItem
			{
				Speech = GetAdvisorLabel(advisorIndex) + ". Submit to hear message.",
				Activate = delegate
				{
					InvokeWithHoverAnnouncementSuppressed(delegate
					{
						advisor.OnAdvisorClicked(advisorIndex);
					});

					string message = CleanText(advisor.messageTxt != null ? advisor.messageTxt.text : string.Empty);
					if (!string.IsNullOrEmpty(message))
					{
						AnnounceText(JoinParts(GetAdvisorLabel(advisorIndex), message), true);
					}
				}
			});
		}

		items.Add(new AccessiblePanelItem
		{
			Speech = "Close advisors.",
			Activate = delegate
			{
				InvokeWithHoverAnnouncementSuppressed(advisor.Hide);
			}
		});
		return items;
	}

	private static string GetAdvisorLabel(int advisorIndex)
	{
		switch (advisorIndex)
		{
			case 0:
				return "Food advisor";
			case 1:
				return "City advisor";
			case 2:
				return "Military advisor";
			default:
				return "Advisor";
		}
	}

	private List<AccessiblePanelItem> GetAccessiblePersonItems(PersonUI person)
	{
		List<AccessiblePanelItem> items = new List<AccessiblePanelItem>();
		if (person == null || person.villager == null)
		{
			return items;
		}

		Villager villager = person.villager;
		string name = CleanText(person.villagerName != null ? person.villagerName.text : villager.name);
		string age = CleanText(person.age != null ? person.age.text : string.Empty);
		string description = CleanText(person.description != null ? person.description.text : string.Empty);
		items.Add(new AccessiblePanelItem
		{
			Speech = JoinParts(name, age, description)
		});

		string thought = CleanText(person.thought != null ? person.thought.text : string.Empty);
		if (!string.IsNullOrEmpty(thought))
		{
			items.Add(new AccessiblePanelItem
			{
				Speech = "Thought. " + thought + "."
			});
		}

		for (int i = 0; i < person.skillName.Length && i < person.skillValue.Length; i++)
		{
			string skillName = CleanText(person.skillName[i] != null ? person.skillName[i].text : string.Empty);
			string skillValue = CleanText(person.skillValue[i] != null ? person.skillValue[i].text : string.Empty);
			if (string.IsNullOrEmpty(skillName) && string.IsNullOrEmpty(skillValue))
			{
				continue;
			}

			items.Add(new AccessiblePanelItem
			{
				Speech = JoinParts(skillName, skillValue)
			});
		}

		items.Add(new AccessiblePanelItem
		{
			Speech = "Track villager.",
			Activate = person.OnClickTrack
		});

		if (person.residenceTrackBtn != null && person.residenceTrackBtn.activeInHierarchy)
		{
			items.Add(new AccessiblePanelItem
			{
				Speech = "Track residence.",
				Activate = person.OnClickTrackResidence
			});
		}

		items.Add(new AccessiblePanelItem
		{
			Speech = "Close person details.",
			Activate = delegate
			{
				InvokeWithHoverAnnouncementSuppressed(delegate
				{
					person.SetVisible(v: false);
				});
			}
		});
		return items;
	}

	private List<AccessiblePanelItem> GetAccessibleWorkerItems(WorkerUI worker)
	{
		List<AccessiblePanelItem> items = new List<AccessiblePanelItem>();
		if (worker == null)
		{
			return items;
		}

		Building building = GameUI.inst != null ? GameUI.inst.GetBuildingSelected() : null;
		if (building == null)
		{
			return items;
		}

		items.Add(new AccessiblePanelItem
		{
			Speech = DescribeAccessibleWorkerSummary(worker, building)
		});

		KeepUI keepUi = worker.GetComponentInChildren<KeepUI>(includeInactive: true);
		if (keepUi != null && keepUi.ShouldShow())
		{
			items.Add(new AccessiblePanelItem
			{
				Speech = "Change banner.",
				Activate = delegate
				{
					InvokeWithHoverAnnouncementSuppressed(keepUi.OnClick);
				}
			});
		}

		if (worker.customButtonContainer != null && worker.customButtonContainer.activeInHierarchy && worker.customButton != null)
		{
			items.Add(new AccessiblePanelItem
			{
				Speech = DescribeAccessibleWorkerCustomAction(worker),
				Activate = delegate
				{
					InvokeWithHoverAnnouncementSuppressed(delegate
					{
						worker.customButton.onClick.Invoke();
					});
				}
			});
		}

		if (worker.openCheckbox != null && worker.openCheckbox.gameObject.activeInHierarchy)
		{
			items.Add(new AccessiblePanelItem
			{
				Speech = worker.openCheckbox.isOn ? "Close building." : "Open building.",
				Activate = delegate
				{
					InvokeWithHoverAnnouncementSuppressed(delegate
					{
						worker.openCheckbox.isOn = !worker.openCheckbox.isOn;
					});
				}
			});
		}

		if (worker.findVillagersButton != null && worker.findVillagersButton.activeInHierarchy)
		{
			items.Add(new AccessiblePanelItem
			{
				Speech = GetAccessibleWorkerFindLabel(building),
				Activate = delegate
				{
					Button button = worker.findVillagersButton.GetComponent<Button>();
					if (button != null)
					{
						InvokeWithHoverAnnouncementSuppressed(delegate
						{
							button.onClick.Invoke();
						});
					}
				}
			});
		}

		List<PersonListItemUI> people = GetAccessibleWorkerPeople(worker);
		for (int i = 0; i < people.Count; i++)
		{
			PersonListItemUI personItem = people[i];
			int personIndex = i;
			items.Add(new AccessiblePanelItem
			{
				Speech = DescribeAccessibleWorkerPersonItem(personItem, building, personIndex),
				Activate = delegate
				{
					if (personItem.MagGlass != null)
					{
						InvokeWithHoverAnnouncementSuppressed(delegate
						{
							personItem.MagGlass.onClick.Invoke();
						});
					}
				}
			});
		}

		Button trashButton = worker.GetTrashButton();
		if (trashButton != null && trashButton.gameObject.activeInHierarchy)
		{
			items.Add(new AccessiblePanelItem
			{
				Speech = "Demolish building.",
				Activate = delegate
				{
					InvokeWithHoverAnnouncementSuppressed(delegate
					{
						trashButton.onClick.Invoke();
					});
				}
			});
		}

		items.Add(new AccessiblePanelItem
		{
			Speech = "Close building panel.",
			Activate = delegate
			{
				InvokeWithHoverAnnouncementSuppressed(delegate
				{
					worker.SetVisible(v: false);
				});
			}
		});
		return items;
	}

	private string DescribeAccessibleWorkerSummary(WorkerUI worker, Building building)
	{
		string name = CleanText(building.customName);
		if (string.IsNullOrEmpty(name))
		{
			name = CleanText(building.FriendlyName);
		}

		if (string.IsNullOrEmpty(name))
		{
			name = SplitIdentifier(building.UniqueName);
		}

		List<string> parts = new List<string>();
		parts.Add(name);
		string description = CleanText(worker.desc != null ? worker.desc.text : building.Description);
		if (!string.IsNullOrEmpty(description))
		{
			parts.Add(description);
		}

		string people = CleanText(worker.personDesc != null && worker.personDesc.gameObject.activeInHierarchy ? worker.personDesc.text : string.Empty);
		if (!string.IsNullOrEmpty(people))
		{
			parts.Add(people);
		}

		if (worker.openCheckbox != null && worker.openCheckbox.gameObject.activeInHierarchy)
		{
			parts.Add(worker.openCheckbox.isOn ? "Open" : "Closed");
		}

		int lifePercent = building.ModifiedMaxLife <= 0f ? 0 : Mathf.RoundToInt(building.Life / building.ModifiedMaxLife * 100f);
		parts.Add("Health " + Mathf.Clamp(lifePercent, 0, 100) + " percent");

		if (World.IsImmuneToFire(building))
		{
			parts.Add("Fire safe");
		}
		else
		{
			int fireRiskPercent = Mathf.RoundToInt(Mathf.Clamp01(1f - building.GetMaxWellProtection()) * 100f);
			parts.Add("Fire risk " + fireRiskPercent + " percent");
		}

		return JoinParts(parts.ToArray());
	}

	private string DescribeAccessibleWorkerCustomAction(WorkerUI worker)
	{
		string title = CleanText(worker.customButtonTitle != null ? worker.customButtonTitle.text : string.Empty);
		string text = CleanText(worker.customButtonText != null ? worker.customButtonText.text : string.Empty);
		string state = worker.customButton != null && !worker.customButton.interactable ? "Unavailable" : string.Empty;
		return JoinParts(string.IsNullOrEmpty(title) ? "Custom action" : title, text, state);
	}

	private string GetAccessibleWorkerFindLabel(Building building)
	{
		if (building != null && building.GetComponent<IResidence>() != null)
		{
			return "Find residents.";
		}

		return "Find workers.";
	}

	private List<PersonListItemUI> GetAccessibleWorkerPeople(WorkerUI worker)
	{
		List<PersonListItemUI> people = new List<PersonListItemUI>();
		if (worker == null || worker.villagerListUi == null || !worker.villagerListUi.gameObject.activeInHierarchy)
		{
			return people;
		}

		PersonListItemUI[] listItems = worker.villagerListUi.GetComponentsInChildren<PersonListItemUI>(includeInactive: false);
		for (int i = 0; i < listItems.Length; i++)
		{
			if (listItems[i] != null && listItems[i].gameObject.activeInHierarchy)
			{
				people.Add(listItems[i]);
			}
		}

		return people;
	}

	private string DescribeAccessibleWorkerPersonItem(PersonListItemUI personItem, Building building, int index)
	{
		string role = building != null && building.GetComponent<IResidence>() != null ? "Resident" : "Worker";
		string name = CleanText(personItem != null && personItem.Description != null ? personItem.Description.text : string.Empty);
		string hoh = personItem != null && personItem.HeadOfHouseholdIcon != null && personItem.HeadOfHouseholdIcon.activeInHierarchy ? "Head of household" : string.Empty;
		return JoinParts(role + " " + (index + 1), name, hoh);
	}

	private void AnnounceWorkerOrGameplaySelection()
	{
		WorkerUI worker = GameUI.inst != null ? GameUI.inst.workerUI : null;
		if (worker != null && worker.Visible)
		{
			List<AccessiblePanelItem> items = GetAccessibleWorkerItems(worker);
			if (items.Count > 0)
			{
				gameplayWorkerIndex = Mathf.Clamp(gameplayWorkerIndex, 0, items.Count - 1);
				AnnounceText(items[gameplayWorkerIndex].Speech, true);
				return;
			}
		}

		AnnounceCurrentGameplayMapCursor(interrupt: true);
	}

	private List<string> GetAccessibleConstructItems(ConstructUI construct)
	{
		List<string> items = new List<string>();
		if (construct == null)
		{
			return items;
		}

		Building building = GameUI.inst != null ? GameUI.inst.GetBuildingSelected() : null;
		if (building == null)
		{
			return items;
		}

		items.Add(DescribeAccessibleConstructSummary(construct, building));
		items.Add(DescribeAccessibleConstructPauseState(building));
		if (construct.FindBuildersButton != null && construct.FindBuildersButton.gameObject.activeInHierarchy)
		{
			items.Add("Find builders. button.");
		}

		Button trashButton = construct.GetTrashButton();
		if (trashButton != null && trashButton.gameObject.activeInHierarchy)
		{
			items.Add("Demolish. button.");
		}

		return items;
	}

	private string DescribeAccessibleConstructSummary(ConstructUI construct, Building building)
	{
		string name = CleanText(building.FriendlyName);
		if (string.IsNullOrEmpty(name))
		{
			name = SplitIdentifier(building.UniqueName);
		}

		string progress = "Progress " + Mathf.RoundToInt(building.constructionProgress * 100f) + " percent";
		string workers = "Workers " + building.WorkersAllocated + " of " + building.BuildAllowedWorkers;
		ResourceAmount remaining = building.ResourcesNotCollected();
		remaining.Set(FreeResourceType.Gold, 0);
		string remainingResources = CleanText(remaining.ToString(", "));
		string remainingText = string.IsNullOrEmpty(remainingResources) ? "Finishing up" : "Waiting for " + remainingResources;
		string paused = building.constructionPaused ? "Paused" : "Building";
		return JoinParts(name, paused, progress, workers, remainingText);
	}

	private string DescribeAccessibleConstructPauseState(Building building)
	{
		return building != null && building.constructionPaused ? "Resume construction. button." : "Pause construction. button.";
	}

	private void ActivateAccessibleConstructItem(ConstructUI construct, int index)
	{
		if (construct == null)
		{
			return;
		}

		Building building = GameUI.inst != null ? GameUI.inst.GetBuildingSelected() : null;
		if (building == null)
		{
			return;
		}

		if (index == 1)
		{
			InvokeWithHoverAnnouncementSuppressed(delegate
			{
				if (building.constructionPaused)
				{
					if (construct.play != null)
					{
						construct.play.isOn = true;
					}

					construct.PlayToggle();
				}
				else
				{
					if (construct.pause != null)
					{
						construct.pause.isOn = true;
					}

					construct.PauseToggle();
				}
			});
			return;
		}

		int actionIndex = 2;
		if (construct.FindBuildersButton != null && construct.FindBuildersButton.gameObject.activeInHierarchy)
		{
			if (index == actionIndex)
			{
				InvokeWithHoverAnnouncementSuppressed(delegate
				{
					construct.FindBuildersButton.onClick.Invoke();
				});
				return;
			}

			actionIndex++;
		}

		Button trashButton = construct.GetTrashButton();
		if (trashButton != null && trashButton.gameObject.activeInHierarchy && index == actionIndex)
		{
			InvokeWithHoverAnnouncementSuppressed(delegate
			{
				trashButton.onClick.Invoke();
			});
		}
	}

	private List<BuildPriorityItem> GetAccessibleDecreeItems(DecreeUI decree)
	{
		List<BuildPriorityItem> items = new List<BuildPriorityItem>();
		if (decree == null || decree.prioritized == null)
		{
			return items;
		}

		for (int i = 0; i < decree.prioritized.transform.childCount; i++)
		{
			BuildPriorityItem item = decree.prioritized.transform.GetChild(i).GetComponent<BuildPriorityItem>();
			if (item != null && item.gameObject.activeInHierarchy)
			{
				items.Add(item);
			}
		}

		return items;
	}

	private int GetFocusedAccessibleDecreeItemIndex(List<BuildPriorityItem> items, ConsoleUIItem current)
	{
		if (items == null || current == null)
		{
			return -1;
		}

		BuildPriorityItem focusedItem = current.GetComponentInParent<BuildPriorityItem>();
		if (focusedItem == null)
		{
			return -1;
		}

		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] == focusedItem)
			{
				return i;
			}
		}

		return -1;
	}

	private string DescribeAccessibleDecreeItem(BuildPriorityItem item)
	{
		if (item == null)
		{
			return string.Empty;
		}

		string name = CleanText(item.Name != null ? item.Name.text : SplitIdentifier(item.Category.ToString()));
		string priority = CleanText(item.Priority != null ? item.Priority.text : string.Empty);
		string enabled = item.DisableToggle != null && item.DisableToggle.isOn ? "Enabled" : "Disabled";
		string filled = CleanText(item.FilledWorkers != null ? item.FilledWorkers.text : string.Empty);
		string available = CleanText(item.AvailableWorkersInput != null ? item.AvailableWorkersInput.text : string.Empty);
		string max = CleanText(item.MaxAvailableWorkers != null ? item.MaxAvailableWorkers.text : string.Empty);
		string workers = string.IsNullOrEmpty(available)
			? (string.IsNullOrEmpty(filled) ? string.Empty : "Workers " + filled)
			: "Workers " + filled + " filled, " + available + " available " + max;
		string controls = "Left and right change priority. Submit toggles enabled.";
		return JoinParts(name, string.IsNullOrEmpty(priority) ? "No priority" : "Priority " + priority, enabled, workers, controls);
	}

	private void MoveAccessibleDecreeItem(List<BuildPriorityItem> items, int index, int direction)
	{
		if (items == null || index < 0 || index >= items.Count)
		{
			return;
		}

		int targetIndex = index + direction;
		if (targetIndex < 0 || targetIndex >= items.Count)
		{
			AnnounceText(DescribeAccessibleDecreeItem(items[index]), true);
			return;
		}

		BuildPriorityItem current = items[index];
		BuildPriorityItem target = items[targetIndex];
		int currentSiblingIndex = current.transform.GetSiblingIndex();
		int targetSiblingIndex = target.transform.GetSiblingIndex();
		current.transform.SetSiblingIndex(targetSiblingIndex);
		target.transform.SetSiblingIndex(currentSiblingIndex);
		if (DecreeUI.inst != null)
		{
			DecreeUI.inst.UpdatePriorityNumbers();
			DecreeUI.inst.SavePriority();
		}

		gameplayJobPriorityIndex = targetIndex;
		List<BuildPriorityItem> refreshedItems = GetAccessibleDecreeItems(DecreeUI.inst);
		if (gameplayJobPriorityIndex >= 0 && gameplayJobPriorityIndex < refreshedItems.Count)
		{
			AnnounceText(DescribeAccessibleDecreeItem(refreshedItems[gameplayJobPriorityIndex]), true);
		}
	}

	private void ToggleAccessibleDecreeItem(BuildPriorityItem item)
	{
		if (item == null || item.DisableToggle == null)
		{
			return;
		}

		item.DisableToggle.isOn = !item.DisableToggle.isOn;
		AnnounceText(DescribeAccessibleDecreeItem(item), true);
	}

	private List<string> GetAccessibleIslandInfoItems(IslandInfoContainer islandInfo)
	{
		List<string> items = new List<string>();
		if (islandInfo == null)
		{
			return items;
		}

		items.Add(DescribeIslandInfoSummary(islandInfo));
		items.Add(DescribeIslandInfoJobPriorities(islandInfo));
		items.Add(DescribeIslandInfoTaxRate(islandInfo));
		items.Add(DescribeIslandInfoHappiness(islandInfo));
		items.Add(DescribeIslandInfoHealth(islandInfo));
		items.Add(DescribeIslandInfoIntegrity(islandInfo));
		return items;
	}

	private List<string> GetAccessibleForeignIslandInfoItems(ForeignIslandInfoContainer foreignInfo)
	{
		List<string> items = new List<string>();
		if (foreignInfo == null)
		{
			return items;
		}

		items.Add(JoinParts("Foreign island", CleanText(foreignInfo.relationStatusTxt != null ? foreignInfo.relationStatusTxt.text : string.Empty), CleanText(foreignInfo.opinionStatusTxt != null ? foreignInfo.opinionStatusTxt.text : string.Empty)));
		if (foreignInfo.hostilityButton != null && foreignInfo.hostilityButton.gameObject.activeInHierarchy)
		{
			items.Add(JoinParts("Diplomacy action", CleanText(foreignInfo.relationButtonTxt != null ? foreignInfo.relationButtonTxt.text : string.Empty), foreignInfo.hostilityButton.interactable ? "button" : "button unavailable"));
		}

		if (foreignInfo.missionRoot != null && foreignInfo.missionRoot.activeInHierarchy)
		{
			items.Add(JoinParts("Mission", CleanText(foreignInfo.missionDescTxt != null ? foreignInfo.missionDescTxt.text : string.Empty), CleanText(foreignInfo.missionConditionTxt != null ? foreignInfo.missionConditionTxt.text : string.Empty)));
		}

		if (foreignInfo.missionResultRoot != null && foreignInfo.missionResultRoot.activeInHierarchy)
		{
			items.Add(JoinParts("Mission result", CleanText(foreignInfo.missionResult != null ? foreignInfo.missionResult.text : string.Empty)));
		}

		return items;
	}

	private string DescribeIslandInfoSummary(IslandInfoContainer islandInfo)
	{
		if (Player.inst == null || World.inst == null)
		{
			string fallbackName = CleanText(islandInfo.regionTitleBar != null && islandInfo.regionTitleBar.regionNameInput != null ? islandInfo.regionTitleBar.regionNameInput.text : string.Empty);
			return JoinParts(fallbackName);
		}

		int landMass = Player.inst.FocusedLandMass;
		string name = CleanText(Player.inst.GetFocusedLandMassName());
		if (string.IsNullOrEmpty(name))
		{
			name = CleanText(islandInfo.regionTitleBar != null && islandInfo.regionTitleBar.regionNameInput != null ? islandInfo.regionTitleBar.regionNameInput.text : string.Empty);
		}

		string population = landMass >= 0 ? World.inst.GetVillagersForLandMass(landMass).Count.ToString() : string.Empty;
		string freeWorkers = landMass >= 0 ? World.inst.AvailableWorkersOnLandMass(landMass).ToString() : string.Empty;
		string housing = landMass >= 0 ? Player.inst.TotalResidentialSlotsOnLandMass(landMass).ToString() : string.Empty;
		return JoinParts(name, string.IsNullOrEmpty(population) ? string.Empty : "Population " + population, string.IsNullOrEmpty(freeWorkers) ? string.Empty : "Free workers " + freeWorkers, string.IsNullOrEmpty(housing) ? string.Empty : "Housing " + housing);
	}

	private string DescribeIslandInfoJobPriorities(IslandInfoContainer islandInfo)
	{
		return JoinParts("Job priorities", islandInfo != null && islandInfo.JobPriotitiesButton != null ? ExtractRoleAndState(islandInfo.JobPriotitiesButton) : "button");
	}

	private string DescribeIslandInfoTaxRate(IslandInfoContainer islandInfo)
	{
		TaxRateUI tax = islandInfo.TaxRateUI;
		if (tax == null)
		{
			return "Tax rate.";
		}

		string value = CleanText(tax.taxRateTxt != null ? tax.taxRateTxt.text : string.Empty);
		string availability = tax.increase != null && tax.increase.interactable && tax.decrease != null && tax.decrease.interactable
			? "Adjust with left and right"
			: "Unavailable";
		return JoinParts("Tax rate", value, availability);
	}

	private string DescribeIslandInfoHappiness(IslandInfoContainer islandInfo)
	{
		Player.HappinessInfo happiness = Player.inst != null ? Player.inst.HappinessForFocusedLandMass : null;
		if (happiness == null)
		{
			return JoinParts("Happiness", islandInfo != null && islandInfo.HappinessUI != null && islandInfo.HappinessUI.overlayOn ? "overlay on" : "overlay off");
		}

		return JoinParts("Happiness", happiness.currHappiness.ToString(), DescribeHappinessRating(happiness.currHappiness), islandInfo != null && islandInfo.HappinessUI != null && islandInfo.HappinessUI.overlayOn ? "overlay on" : "overlay off");
	}

	private string DescribeIslandInfoHealth(IslandInfoContainer islandInfo)
	{
		Player.HealthInfo health = Player.inst != null ? Player.inst.HealthForFocusedLandMass : null;
		if (health == null)
		{
			return JoinParts("Health", islandInfo != null && islandInfo.HealthUI != null && islandInfo.HealthUI.overlayOn ? "overlay on" : "overlay off");
		}

		return JoinParts("Health", health.currHealth.ToString(), SplitIdentifier(Player.HealthInfo.ToRating(health.currHealth).ToString()), islandInfo != null && islandInfo.HealthUI != null && islandInfo.HealthUI.overlayOn ? "overlay on" : "overlay off");
	}

	private string DescribeIslandInfoIntegrity(IslandInfoContainer islandInfo)
	{
		Player.IntegrityInfo integrity = Player.inst != null ? Player.inst.IntegrityForFocusedLandMass : null;
		if (integrity == null)
		{
			return JoinParts("Integrity", islandInfo != null && islandInfo.IntegrityUI != null && islandInfo.IntegrityUI.overlayOn ? "overlay on" : "overlay off");
		}

		return JoinParts("Integrity", integrity.currentIntegrity.ToString(), SplitIdentifier(Player.IntegrityInfo.ToRating(integrity.currentIntegrity).ToString()), islandInfo != null && islandInfo.IntegrityUI != null && islandInfo.IntegrityUI.overlayOn ? "overlay on" : "overlay off");
	}

	private static string DescribeHappinessRating(int happiness)
	{
		if (happiness >= 90)
		{
			return "Very happy";
		}

		if (happiness >= 70)
		{
			return "Happy";
		}

		if (happiness >= 50)
		{
			return "Neutral";
		}

		if (happiness >= 35)
		{
			return "Unhappy";
		}

		return "Very unhappy";
	}

	private void ActivateAccessibleIslandInfoItem(IslandInfoContainer islandInfo, int index)
	{
		switch (index)
		{
			case 1:
				if (islandInfo.JobPriotitiesButton != null)
				{
					Button button = islandInfo.JobPriotitiesButton.GetComponent<Button>();
					if (button != null && button.gameObject.activeInHierarchy)
					{
						InvokeWithHoverAnnouncementSuppressed(delegate
						{
							button.onClick.Invoke();
						});
						List<BuildPriorityItem> items = GetAccessibleDecreeItems(DecreeUI.inst);
						if (items.Count > 0)
						{
							gameplayJobPriorityIndex = 0;
							AnnounceText(DescribeAccessibleDecreeItem(items[0]), true);
						}
					}
				}
				break;
			case 3:
				if (islandInfo.HappinessUI != null)
				{
					islandInfo.HappinessUI.OnClickedShowOverlay();
				}
				break;
			case 4:
				if (islandInfo.HealthUI != null)
				{
					islandInfo.HealthUI.OnClickedShowOverlay();
				}
				break;
			case 5:
				if (islandInfo.IntegrityUI != null)
				{
					islandInfo.IntegrityUI.OnClickedShowOverlay();
				}
				break;
		}
	}
}
