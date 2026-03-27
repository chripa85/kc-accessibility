using System.Collections.Generic;
using Assets.Code;
using Assets.Code.Jobs;
using UnityEngine;

public partial class ScreenReaderAccessibilityBehaviour
{
	private static readonly FreeResourceType[] FocusedResourceAnnouncementOrder = new[]
	{
		FreeResourceType.Tree,
		FreeResourceType.Stone,
		FreeResourceType.Gold,
		FreeResourceType.Charcoal,
		FreeResourceType.IronOre,
		FreeResourceType.Tools,
		FreeResourceType.Wheat,
		FreeResourceType.Fish,
		FreeResourceType.Apples,
		FreeResourceType.Pork,
		FreeResourceType.Armament
	};

	private string DescribeCurrentPlacementStatus()
	{
		if (!IsPlacementModeActive())
		{
			return string.Empty;
		}

		Building hoverBuilding = GameUI.inst.CurrPlacementMode.GetHoverBuilding();
		if (hoverBuilding == null)
		{
			return string.Empty;
		}

		PlacementValidationResult result = World.inst.CanPlace(hoverBuilding);
		if (result == PlacementValidationResult.Valid)
		{
			return JoinParts("Valid placement", gameplayMapCursorCell != null ? DescribeGameplayMapCell(gameplayMapCursorCell) : string.Empty);
		}

		return JoinParts("Invalid placement", SplitIdentifier(result.ToString()));
	}

	private bool TryHandleGameplayMapNavigation()
	{
		if (!gameplayKeyboardNavigationActive || !IsPlayingModeActive() || World.inst == null)
		{
			return false;
		}

		bool isPlacing = IsPlacementModeActive();

		if (IsNavigateNextPressed())
		{
			AnnounceCurrentGameplayMapCursor(interrupt: false);
			return true;
		}
		if (IsNavigateUpPressed())
		{
			return MoveGameplayMapCursor(0, 1);
		}
		if (IsNavigateDownPressed())
		{
			return MoveGameplayMapCursor(0, -1);
		}
		if (IsNavigateLeftPressed())
		{
			return MoveGameplayMapCursor(-1, 0);
		}
		if (IsNavigateRightPressed())
		{
			return MoveGameplayMapCursor(1, 0);
		}
		if (IsSubmitPressed())
		{
			return isPlacing ? TryHandleGameplayPrimaryActionAtCursor() : CycleGameplayTileElementAtCursor();
		}
		if (IsOpenBuildMenuPressed())
		{
			return OpenAccessibleBuildMenuAtCursor();
		}
		if (IsCancelPressed())
		{
			if (isPlacing)
			{
				return TryHandleGameplaySecondaryActionAtCursor();
			}

			AnnounceCurrentGameplayMapCursor(interrupt: true);
			return true;
		}

		return false;
	}

	private bool CycleToNextHomeKeep()
	{
		if (!IsPlayingModeActive() || World.inst == null || Player.inst == null || Player.inst.PlayerLandmassOwner == null)
		{
			return false;
		}

		List<Building> keeps = GetOwnedKeepsSorted();
		if (keeps.Count == 0)
		{
			AnnounceText("No home keeps found.", true);
			return true;
		}

		int nextIndex = 0;
		if (gameplayMapCursorCell != null)
		{
			for (int i = 0; i < keeps.Count; i++)
			{
				Cell keepCell = keeps[i] != null ? keeps[i].GetCell() : null;
				if (keepCell != null && keepCell.x == gameplayMapCursorCell.x && keepCell.z == gameplayMapCursorCell.z)
				{
					nextIndex = (i + 1) % keeps.Count;
					break;
				}
			}
		}

		Building targetKeep = keeps[nextIndex];
		if (targetKeep == null)
		{
			return false;
		}

		Cell targetCell = targetKeep.GetCell();
		if (targetCell == null)
		{
			return false;
		}

		gameplayKeyboardNavigationActive = true;
		if (GameUI.inst != null)
		{
			GameUI.inst.ClearUIForClick();
			GameUI.inst.ClearSelection();
			GameUI.inst.ClearCellSelected();
		}

		ConsoleUIItem focusedItem = GetFocusedItem();
		if (focusedItem != null)
		{
			focusedItem.Dehover();
		}

		FocusGameplayMapCell(targetCell, announce: false);
		AnnounceText(JoinParts("Home keep", (nextIndex + 1) + " of " + keeps.Count, DescribeGameplayMapCell(targetCell)), true);
		return true;
	}

	private bool CycleFocusedResourcesAnnouncement()
	{
		if (!IsPlayingModeActive() || Player.inst == null || World.inst == null)
		{
			return false;
		}

		int landMass = ResolveFocusedOwnedLandMass();
		if (landMass < 0)
		{
			AnnounceText("No owned island resources available.", true);
			return true;
		}

		ResourceAmount resources = Player.inst.resourcesPerLandmass != null && landMass < Player.inst.resourcesPerLandmass.Length
			? Player.inst.resourcesPerLandmass[landMass]
			: default(ResourceAmount);

		float now = Time.realtimeSinceStartup;
		bool advanceToNextResource = now - lastGameplayResourceCycleInputTime <= RepeatedCommandWindowSeconds;
		if (gameplayResourceIndex < 0)
		{
			gameplayResourceIndex = 0;
		}
		else if (advanceToNextResource)
		{
			gameplayResourceIndex = (gameplayResourceIndex + 1) % FocusedResourceAnnouncementOrder.Length;
		}

		lastGameplayResourceCycleInputTime = now;
		FreeResourceType resourceType = FocusedResourceAnnouncementOrder[gameplayResourceIndex];
		int amount = GetFocusedResourceAmount(resources, landMass, resourceType);
		AnnounceText(JoinParts(GetFocusedLandMassResourcePrefix(landMass), DescribeFocusedResource(resourceType, amount)), true);
		return true;
	}

	private int ResolveFocusedOwnedLandMass()
	{
		if (Player.inst == null || Player.inst.PlayerLandmassOwner == null)
		{
			return -1;
		}

		int focusedLandMass = Player.inst.FocusedLandMass;
		if (focusedLandMass >= 0 && Player.inst.PlayerLandmassOwner.OwnsLandMass(focusedLandMass))
		{
			return focusedLandMass;
		}

		if (gameplayMapCursorCell != null && gameplayMapCursorCell.landMassIdx >= 0 && Player.inst.PlayerLandmassOwner.OwnsLandMass(gameplayMapCursorCell.landMassIdx))
		{
			return gameplayMapCursorCell.landMassIdx;
		}

		if (Player.inst.keep != null)
		{
			return Player.inst.keep.LandMass();
		}

		return Player.inst.PlayerLandmassOwner.ownedLandMasses.Count > 0 ? Player.inst.PlayerLandmassOwner.ownedLandMasses.data[0] : -1;
	}

	private int GetFocusedResourceAmount(ResourceAmount resources, int landMass, FreeResourceType resourceType)
	{
		if (resourceType == FreeResourceType.Gold)
		{
			LandmassOwner owner = World.GetLandmassOwner(landMass);
			return owner != null ? owner.Gold : 0;
		}

		return resources.Get(resourceType);
	}

	private string GetFocusedLandMassResourcePrefix(int landMass)
	{
		string landMassName = string.Empty;
		if (Player.inst != null && landMass == Player.inst.FocusedLandMass)
		{
			landMassName = CleanText(Player.inst.GetFocusedLandMassName());
		}

		return string.IsNullOrEmpty(landMassName) ? "Resources" : landMassName + " resources";
	}

	private string DescribeFocusedResource(FreeResourceType resourceType, int amount)
	{
		return JoinParts(GetFocusedResourceName(resourceType), amount.ToString());
	}

	private string GetFocusedResourceName(FreeResourceType resourceType)
	{
		switch (resourceType)
		{
			case FreeResourceType.Tree:
				return "Wood";
			case FreeResourceType.Stone:
				return "Stone";
			case FreeResourceType.Gold:
				return "Gold";
			case FreeResourceType.Charcoal:
				return "Charcoal";
			case FreeResourceType.IronOre:
				return "Iron ore";
			case FreeResourceType.Tools:
				return "Tools";
			case FreeResourceType.Wheat:
				return "Wheat";
			case FreeResourceType.Fish:
				return "Fish";
			case FreeResourceType.Apples:
				return "Apples";
			case FreeResourceType.Pork:
				return "Pork";
			case FreeResourceType.Armament:
				return "Armaments";
			default:
				return SplitIdentifier(resourceType.ToString());
		}
	}

	private bool TryHandleAccessibleChopShortcut()
	{
		if (!IsPlayingModeActive() || GameUI.inst == null || World.inst == null)
		{
			return false;
		}

		if (IsPlacementModeActive())
		{
			AnnounceText("Cannot chop while placing a building.", true);
			return true;
		}

		Cell target = GetGameplayActionTargetCell();
		if (target == null)
		{
			AnnounceText("No tile selected for chopping.", true);
			return true;
		}

		if (target.TreeAmount <= 0)
		{
			AnnounceText("No trees on this tile.", true);
			return true;
		}

		if (target.landMassIdx < 0 || Player.inst == null || Player.inst.PlayerLandmassOwner == null || !Player.inst.PlayerLandmassOwner.OwnsLandMass(target.landMassIdx))
		{
			AnnounceText("Cannot chop outside your territory.", true);
			return true;
		}

		ClearCutterJob existingJob = GameUI.inst.GetClearCutterJob(target);
		if (existingJob != null)
		{
			GameUI.inst.RemoveChopJobFromCell(target, sfx: true);
			AnnounceText(JoinParts("Chop canceled", DescribeGameplayMapCell(target)), true);
			return true;
		}

		if (target.Busy)
		{
			AnnounceText("Cannot chop this tile right now.", true);
			return true;
		}

		if (!GameUI.inst.AddChopJobToCell(target, sfx: true))
		{
			AnnounceText("Unable to start chopping on this tile.", true);
			return true;
		}

		AnnounceText(JoinParts("Chop queued", DescribeGameplayMapCell(target)), true);
		return true;
	}

	private List<Building> GetOwnedKeepsSorted()
	{
		List<Building> keeps = new List<Building>();
		if (Player.inst == null || Player.inst.PlayerLandmassOwner == null)
		{
			return keeps;
		}

		for (int i = 0; i < Player.inst.PlayerLandmassOwner.ownedLandMasses.Count; i++)
		{
			int landMass = Player.inst.PlayerLandmassOwner.ownedLandMasses.data[i];
			ArrayExt<Building> buildings = Player.inst.GetBuildingListForLandMass(landMass, World.keepHash);
			if (buildings == null)
			{
				continue;
			}

			for (int j = 0; j < buildings.Count; j++)
			{
				Building keep = buildings.data[j];
				if (keep == null)
				{
					continue;
				}

				keeps.Add(keep);
			}
		}

		keeps.Sort(delegate(Building left, Building right)
		{
			Cell leftCell = left != null ? left.GetCell() : null;
			Cell rightCell = right != null ? right.GetCell() : null;
			int leftLandMass = left != null ? left.LandMass() : -1;
			int rightLandMass = right != null ? right.LandMass() : -1;

			int landMassCompare = leftLandMass.CompareTo(rightLandMass);
			if (landMassCompare != 0)
			{
				return landMassCompare;
			}

			int leftX = leftCell != null ? leftCell.x : int.MinValue;
			int rightX = rightCell != null ? rightCell.x : int.MinValue;
			int xCompare = leftX.CompareTo(rightX);
			if (xCompare != 0)
			{
				return xCompare;
			}

			int leftZ = leftCell != null ? leftCell.z : int.MinValue;
			int rightZ = rightCell != null ? rightCell.z : int.MinValue;
			return leftZ.CompareTo(rightZ);
		});

		return keeps;
	}

	private bool TryHandleGameplayPrimaryActionAtCursor()
	{
		if (!IsPlayingModeActive() || GameUI.inst == null || World.inst == null)
		{
			return false;
		}

		Cell target = GetGameplayActionTargetCell();
		if (target == null)
		{
			return false;
		}

		GameUI.inst.SelectCell(target, forceChange: true);

		if (IsPlacementModeActive())
		{
			Building hoverBuilding = GameUI.inst.CurrPlacementMode.GetHoverBuilding();
			if (hoverBuilding == null)
			{
				AnnounceText("Invalid placement.", true);
				return true;
			}

			PlacementValidationResult validation = World.inst.CanPlace(hoverBuilding);
			if (validation != PlacementValidationResult.Valid)
			{
				AnnounceText(JoinParts("Invalid placement", SplitIdentifier(validation.ToString())), true);
				return true;
			}

			InvokeWithHoverAnnouncementSuppressed(delegate
			{
				GameUI.inst.CurrPlacementMode.TryHandlePrimaryDown();
				GameUI.inst.AcceptCursorObjPlacement();
			});
			AnnounceCurrentGameplayMapCursor(interrupt: true);
			return true;
		}

		CursorMode cursorMode = null;
		if (GameUiCursorModeField != null)
		{
			cursorMode = GameUiCursorModeField.GetValue(GameUI.inst) as CursorMode;
		}

		Building targetBuilding = target.TopMostStructure;
		bool handled = false;
		if (cursorMode != null)
		{
			handled = targetBuilding != null
				? cursorMode.DoPrimaryClick(targetBuilding, null)
				: cursorMode.DoPrimaryClick(null, target);
		}

		if (!handled)
		{
			InvokeWithHoverAnnouncementSuppressed(delegate
			{
				GameUI.inst.ClearUIForClick();
				if (targetBuilding != null)
				{
					GameUI.inst.SetSelectedBuilding(targetBuilding);
					GameUI.inst.SelectCell(targetBuilding.GetCell(), forceChange: true);
				}
				else
				{
					GameUI.inst.ClearCellSelected();
					GameUI.inst.SelectCell(target, forceChange: true);
				}
			});
		}

		AnnounceCurrentGameplayMapCursor(interrupt: true);
		return true;
	}

	private bool OpenAccessibleBuildMenuAtCursor()
	{
		if (!IsPlayingModeActive() || GameUI.inst == null || World.inst == null)
		{
			return false;
		}

		if (IsPlacementModeActive())
		{
			AnnounceText("Already in placement mode.", true);
			return false;
		}

		Cell target = GetGameplayActionTargetCell();
		if (target == null)
		{
			return false;
		}

		GameUI.inst.ClearUIForClick();
		GameUI.inst.ClearSelection();
		GameUI.inst.SelectCell(target, forceChange: true);
		GameUI.inst.OnShowBuildTabClicked(PrimaryBuildCategoryTitles[0]);
		buildMenuItemIndex = 0;

		BuildTab tab = BuildUI.inst != null ? BuildUI.inst.GetCurrentTab() : null;
		if (tab != null)
		{
			AnnounceAccessibleBuildCategory(tab, interrupt: true);
			return true;
		}

		return false;
	}

	private bool CycleGameplayTileElementAtCursor()
	{
		if (!IsPlayingModeActive() || GameUI.inst == null || World.inst == null)
		{
			return false;
		}

		Cell target = GetGameplayActionTargetCell();
		if (target == null)
		{
			return false;
		}

		List<GameplayTileElement> elements = GetGameplayTileElements(target);
		if (elements.Count == 0)
		{
			gameplayTileElementIndex = -1;
			AnnounceText("No selectable elements on this tile.", true);
			return true;
		}

		gameplayTileElementIndex = (gameplayTileElementIndex + 1) % elements.Count;
		FocusGameplayTileElement(target, elements, gameplayTileElementIndex);
		return true;
	}

	private bool TryHandleGameplaySecondaryActionAtCursor()
	{
		if (!IsPlayingModeActive() || GameUI.inst == null || World.inst == null)
		{
			return false;
		}

		Cell target = GetGameplayActionTargetCell();
		if (target == null)
		{
			return false;
		}

		if (IsPlacementModeActive())
		{
			GameUI.inst.AbortCursorObjPlacement(refund: true);
			if (gameplayMapCursorCell != null)
			{
				GameUI.inst.SelectCell(gameplayMapCursorCell, forceChange: true);
			}
			AnnounceText("Placement canceled.", true);
			AnnounceCurrentGameplayMapCursor(interrupt: false);
			return true;
		}

		GameUI.inst.MoveUnitsToPosition(target.Center);
		AdvisorUI.inst.Hide();
		if (GameUI.inst.creativeModeOptions != null)
		{
			CreativeModeOptions options = GameUI.inst.creativeModeOptions.GetComponent<CreativeModeOptions>();
			if (options != null)
			{
				options.HideOptions();
			}
		}
		if (GameUI.inst.merchantUI != null)
		{
			GameUI.inst.merchantUI.SetVisible(visible: false);
		}
		if (GameUI.inst.researchUI != null)
		{
			GameUI.inst.researchUI.Hide();
		}
		if (PersonUI.inst != null)
		{
			PersonUI.inst.SetVisible(v: false);
		}
		GameUI.inst.CancelWaitToPlace();
		GameUI.inst.ClearCellSelected();
		if (HappinessUI.inst != null)
		{
			HappinessUI.inst.HideOverlay();
		}
		GameUI.inst.ReturnToDefaultCursorMode();
		return true;
	}

	private bool IsPlacementModeActive()
	{
		return GameUI.inst != null
			&& GameUI.inst.CurrPlacementMode != null
			&& GameUI.inst.CurrPlacementMode.IsPlacing();
	}

	private Cell GetGameplayActionTargetCell()
	{
		return gameplayMapCursorCell ?? (GameUI.inst != null ? GameUI.inst.GetCellSelected() : null);
	}

	private enum GameplayMapInitAnnouncement
	{
		None,
		EnabledOnly,
		Full
	}

	private void InitializeGameplayMapCursor(GameplayMapInitAnnouncement announceMode)
	{
		if (World.inst == null)
		{
			return;
		}

		Cell anchor = GameUI.inst != null ? GameUI.inst.GetCellSelected() : null;
		if (anchor == null && Cam.inst != null)
		{
			anchor = World.inst.GetCellDataClamped(Cam.inst.TrackingPos);
		}
		if (anchor == null)
		{
			anchor = World.inst.GetCellDataClamped(World.inst.GridCenter());
		}
		if (anchor == null)
		{
			return;
		}

		FocusGameplayMapCell(anchor, announce: false);
		if (announceMode == GameplayMapInitAnnouncement.EnabledOnly || announceMode == GameplayMapInitAnnouncement.Full)
		{
			AnnounceText("Gameplay navigation enabled. Map tile navigation.", true);
		}
		if (announceMode == GameplayMapInitAnnouncement.Full)
		{
			AnnounceCurrentGameplayMapCursor(interrupt: true);
		}
	}

	private bool MoveGameplayMapCursor(int dx, int dz)
	{
		if (World.inst == null)
		{
			return false;
		}

		if (gameplayMapCursorCell == null)
		{
			InitializeGameplayMapCursor(GameplayMapInitAnnouncement.None);
		}
		if (gameplayMapCursorCell == null)
		{
			return false;
		}

		int step = IsFastMoveModifierHeld() ? 10 : 1;
		Cell target = World.inst.GetCellDataClamped(gameplayMapCursorCell.x + (dx * step), gameplayMapCursorCell.z + (dz * step));
		if (target == null)
		{
			return false;
		}

		if (target == gameplayMapCursorCell || (target.x == gameplayMapCursorCell.x && target.z == gameplayMapCursorCell.z))
		{
			return true;
		}

		InvalidateMatchingCellCache();
		FocusGameplayMapCell(target, announce: true);
		return true;
	}

	private void FocusGameplayMapCell(Cell cell, bool announce)
	{
		if (cell == null)
		{
			return;
		}

		if (gameplayKeepSiteSearch != null && gameplayKeepSiteSearch.LandMass != cell.landMassIdx)
		{
			InvalidateKeepSiteSearchSession();
		}

		gameplayMapCursorCell = cell;
		gameplayTileElementIndex = -1;
		if (GameUI.inst != null)
		{
			GameUI.inst.SelectCell(cell, forceChange: true);
		}
		if (Cam.inst != null)
		{
			Cam.inst.SetDesiredTrackingPos(cell.Center);
		}
		if (announce)
		{
			AnnounceCurrentGameplayMapCursor(interrupt: false);
		}
	}

	private void InvalidateMatchingCellCache()
	{
		gameplayMatchingCellXs = new int[0];
		gameplayMatchingCellZs = new int[0];
		gameplayMatchingCellCycleIndex = -1;
		gameplayMatchingCellAnchorX = int.MinValue;
		gameplayMatchingCellAnchorZ = int.MinValue;
		gameplayMatchingCellAnchorLandMass = -1;
		gameplayMatchingCellKind = string.Empty;
	}

	private void AnnounceCurrentGameplayMapCursor(bool interrupt)
	{
		if (gameplayMapCursorCell == null)
		{
			return;
		}
		AnnounceText(DescribeGameplayMapCell(gameplayMapCursorCell), interrupt);
	}

	private List<GameplayTileElement> GetGameplayTileElements(Cell cell)
	{
		List<GameplayTileElement> elements = new List<GameplayTileElement>();
		if (cell == null)
		{
			return elements;
		}

		if (OrdersManager.inst != null)
		{
			ArrayExt<IMoveableUnit> units = OrdersManager.inst.FindUnitsAt(cell.x, cell.z);
			if (units != null)
			{
				for (int i = 0; i < units.Count; i++)
				{
					IMoveableUnit unit = units.data[i];
					if (unit == null || unit.IsBeingCarried())
					{
						continue;
					}
					elements.Add(new GameplayTileElement
					{
						Unit = unit
					});
				}
			}
		}

		for (int i = cell.OccupyingStructure.Count - 1; i >= 0; i--)
		{
			Building building = cell.OccupyingStructure[i];
			if (building == null)
			{
				continue;
			}
			elements.Add(new GameplayTileElement
			{
				Building = building
			});
		}

		for (int i = cell.SubStructure.Count - 1; i >= 0; i--)
		{
			Building building = cell.SubStructure[i];
			if (building == null)
			{
				continue;
			}
			elements.Add(new GameplayTileElement
			{
				Building = building
			});
		}

		return elements;
	}

	private void FocusGameplayTileElement(Cell cell, List<GameplayTileElement> elements, int index)
	{
		if (cell == null || elements == null || index < 0 || index >= elements.Count)
		{
			return;
		}

		GameplayTileElement element = elements[index];
		if (element.IsUnit)
		{
			ISelectable selectable = element.Unit as ISelectable;
			if (selectable != null)
			{
				GameUI.inst.ClearSelection();
				GameUI.inst.TrySelect(selectable);
			}
			if (UnitUI.inst != null)
			{
				UnitUI.inst.SetSelectedUnit(element.Unit, force: true);
			}
			GameUI.inst.ClearCellSelected();
		}
		else if (element.IsBuilding)
		{
			GameUI.inst.ClearUIForClick();
			GameUI.inst.ClearSelection();
			GameUI.inst.SetSelectedBuilding(element.Building);
			GameUI.inst.SelectCell(element.Building.GetCell(), forceChange: true);
		}
		else
		{
			GameUI.inst.SelectCell(cell, forceChange: true);
		}

		AnnounceText(DescribeGameplayTileElement(element, index, elements.Count), true);
	}

	private string DescribeGameplayTileElement(GameplayTileElement element, int index, int total)
	{
		if (element == null)
		{
			return string.Empty;
		}

		string prefix = "Element " + (index + 1) + " of " + total;
		if (element.IsBuilding)
		{
			string name = CleanText(element.Building.FriendlyName);
			if (string.IsNullOrEmpty(name))
			{
				name = SplitIdentifier(element.Building.UniqueName);
			}
			return JoinParts(prefix, name, "building");
		}
		if (element.IsUnit)
		{
			return JoinParts(prefix, DescribeGameplayUnit(element.Unit), "unit");
		}
		return prefix + ".";
	}

	private string DescribeGameplayUnit(IMoveableUnit unit)
	{
		if (unit == null)
		{
			return string.Empty;
		}

		UnitSystem.Army army = unit as UnitSystem.Army;
		if (army != null)
		{
			if (!string.IsNullOrEmpty(CleanText(army.generalName)))
			{
				return CleanText(army.generalName);
			}
			return SplitIdentifier(army.armyType.ToString());
		}

		return SplitIdentifier(unit.GetType().Name);
	}

	private string DescribeGameplayMapCell(Cell cell)
	{
		if (cell == null)
		{
			return "No tile selected.";
		}

		return JoinParts(DescribeGameplayCellPrimaryKind(cell) + ".", cell.x + ", " + cell.z);
	}

	private string DescribeGameplayCellPrimaryKind(Cell cell)
	{
		if (cell == null)
		{
			return string.Empty;
		}

		if (cell.TopMostStructure != null)
		{
			string structureName = CleanText(cell.TopMostStructure.FriendlyName);
			if (string.IsNullOrEmpty(structureName))
			{
				structureName = SplitIdentifier(cell.TopMostStructure.UniqueName);
			}
			return structureName;
		}
		if (cell.TreeAmount > 0)
		{
			return "Trees";
		}
		if (cell.Type == ResourceType.None)
		{
			return DescribeGameplayTerrain(cell);
		}
		if (cell.Type == ResourceType.Water)
		{
			return cell.deepWater ? "Deep water" : "Shallow water";
		}
		switch (cell.Type)
		{
			case ResourceType.Stone:
				return "Stone";
			case ResourceType.UnusableStone:
				return "Unusable stone";
			case ResourceType.IronDeposit:
				return "Iron";
			case ResourceType.EmptyCave:
				return "Empty cave";
			case ResourceType.WolfDen:
				return "Wolf den";
			case ResourceType.WitchHut:
				return "Witch hut";
			default:
				return SplitIdentifier(cell.Type.ToString());
		}
	}

	private string DescribeGameplayTerrain(Cell cell)
	{
		if (cell == null)
		{
			return string.Empty;
		}

		if (cell.Type == ResourceType.Water)
		{
			return cell.deepWater ? "Deep water" : "Shallow water";
		}

		int fertility = cell.GetEffectiveFertility();
		if (fertility <= 0)
		{
			return "Barren land";
		}
		if (fertility >= 2)
		{
			return "Fertile land";
		}

		return "Land";
	}

	private bool HasPlayingModeNavigationInput()
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

	private ConsoleUIItem GetPlayingModeEntryItem()
	{
		if (!IsPlayingModeActive() || GameUI.inst == null)
		{
			return null;
		}

		if (BuildUI.inst != null && BuildUI.inst.Visible)
		{
			BuildTab currentTab = BuildUI.inst.GetCurrentTab();
			if (currentTab != null && currentTab.Visible)
			{
				ConsoleUIItem tabItem = currentTab.GetComponent<ConsoleUIItem>();
				if (tabItem != null && tabItem.gameObject.activeInHierarchy)
				{
					return tabItem;
				}
			}
		}

		if (GameUI.inst.creativeModeOptions != null)
		{
			CreativeModeOptions creative = GameUI.inst.creativeModeOptions.GetComponent<CreativeModeOptions>();
			if (creative != null && creative.optionRoot != null && creative.optionRoot.gameObject.activeInHierarchy && creative.consoleItemEntry != null)
			{
				return creative.consoleItemEntry;
			}
		}

		return GetVisibleIslandInfoEntry(GameUI.inst);
	}
}
