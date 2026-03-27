using System;
using System.Collections.Generic;
using System.Linq;
using Assets;
using Assets.Code;
using UnityEngine;

public partial class ScreenReaderAccessibilityBehaviour
{
	private const int DirectionalSummaryRadius = 10;
	private static readonly Vector2Int[] DirectionalMeshOffsetsForward = new[]
	{
		new Vector2Int(1, 1),
		new Vector2Int(1, 0),
		new Vector2Int(1, -1)
	};
	private static readonly Vector2Int[] DirectionalMeshOffsetsBackward = new[]
	{
		new Vector2Int(-1, -1),
		new Vector2Int(-1, 0),
		new Vector2Int(-1, 1)
	};
	private static readonly Vector2Int[] DirectionalMeshOffsetsLeft = new[]
	{
		new Vector2Int(-1, 1),
		new Vector2Int(0, 1),
		new Vector2Int(1, 1)
	};
	private static readonly Vector2Int[] DirectionalMeshOffsetsRight = new[]
	{
		new Vector2Int(1, -1),
		new Vector2Int(0, -1),
		new Vector2Int(-1, -1)
	};

	private sealed class GameplayCellMatch
	{
		public Cell Cell;
		public int DistanceSquared;
		public float TieBreakerAngle;
	}

	private bool TryHandleGameplayDiscoveryInput()
	{
		ExpirePendingGameplayCommandIfNeeded();

		if (TryHandlePendingGameplayCommand())
		{
			return true;
		}

		if (IsSearchPressed())
		{
			return RunKeepSiteSearch();
		}

		if (TryHandleKeepSiteSearchSessionInput())
		{
			return true;
		}
		if (BuildUI.inst != null && BuildUI.inst.Visible)
		{
			return false;
		}
		if (IsNamedBookmarkPressed())
		{
			BeginPendingGameplayCommand(PendingGameplayCommand.SetNamedBookmarkSlot, "Choose bookmark slot.");
			return true;
		}
		if (IsSetBookmarkPressed())
		{
			return SetBookmarkFromPressedSlot();
		}
		if (IsJumpBookmarkPressed())
		{
			return JumpToPressedBookmarkSlot();
		}
		if (IsResourceValuePromptPressed())
		{
			BeginPendingGameplayCommand(PendingGameplayCommand.ResourceValue, "Choose resource slot.");
			return true;
		}
		if (IsResourceMenuPressed())
		{
			BeginPendingGameplayCommand(PendingGameplayCommand.ResourceValue, "Resources. Press 1 to 0 for values.");
			return true;
		}
		if (IsCycleIslandsPressed())
		{
			return CycleFocusedIsland();
		}
		if (IsIslandInfoPressed())
		{
			return AnnounceFocusedIslandSummary();
		}
		if (IsKeepAnchorPressed())
		{
			return HandleKeepAnchor();
		}
		if (IsCycleMatchingTilesPressed())
		{
			return CycleMatchingTiles(reverse: IsShiftHeld());
		}
		if (IsMeshAwarenessPressed())
		{
			return HandleMeshAwareness();
		}
		if (IsDirectionalMeshPressed())
		{
			BeginPendingGameplayCommand(PendingGameplayCommand.DirectionalMesh, "Choose direction.");
			return true;
		}
		if (IsDirectionalScanPressed())
		{
			return AnnounceDirectionalScan();
		}
		if (IsDirectionalSummaryPressed())
		{
			return AnnounceDirectionalSummary();
		}
		if (IsIncreaseSpeedPressed())
		{
			return ChangeGameplaySpeed(1);
		}
		if (IsDecreaseSpeedPressed())
		{
			return ChangeGameplaySpeed(-1);
		}

		return false;
	}

	private bool SetBookmarkFromPressedSlot()
	{
		if (!TryGetPressedDigitSlot(out int slot))
		{
			return false;
		}

		return SetBookmark(slot, string.Empty, announceNamed: false);
	}

	private bool JumpToPressedBookmarkSlot()
	{
		if (!TryGetPressedDigitSlot(out int slot))
		{
			return false;
		}

		return JumpToBookmark(slot);
	}

	private bool SetBookmark(int slot, string name, bool announceNamed)
	{
		Cell target = EnsureGameplayMapCursorCell();
		if (target == null)
		{
			return false;
		}

		GameplayBookmark bookmark = gameplayBookmarks[slot];
		bookmark.X = target.x;
		bookmark.Z = target.z;
		bookmark.LandMass = target.landMassIdx;
		bookmark.Name = name ?? string.Empty;
		bookmark.IsSet = true;

		string prefix = announceNamed
			? bookmark.Name
			: "Bookmark " + GetBookmarkSlotNumber(slot);
		AnnounceText(JoinParts(prefix, DescribeGameplayMapCell(target)), true);
		return true;
	}

	private bool JumpToBookmark(int slot)
	{
		GameplayBookmark bookmark = gameplayBookmarks[slot];
		if (bookmark == null || !bookmark.IsSet || World.inst == null)
		{
			AnnounceText("Bookmark " + GetBookmarkSlotNumber(slot) + " is empty.", true);
			return true;
		}

		Cell target = World.inst.GetCellData(bookmark.X, bookmark.Z);
		if (target == null)
		{
			AnnounceText("Bookmark " + GetBookmarkSlotNumber(slot) + " is unavailable.", true);
			return true;
		}

		InvalidateMatchingCellCache();
		FocusGameplayMapCell(target, announce: false);
		string name = string.IsNullOrEmpty(bookmark.Name) ? "Bookmark " + GetBookmarkSlotNumber(slot) : bookmark.Name;
		AnnounceText(JoinParts(name, DescribeGameplayMapCell(target)), true);
		return true;
	}

	private int GetBookmarkSlotNumber(int slot)
	{
		return slot == 9 ? 10 : slot + 1;
	}

	private bool HandleKeepAnchor()
	{
		Cell current = EnsureGameplayMapCursorCell();
		if (current == null)
		{
			return false;
		}

		float now = Time.realtimeSinceStartup;
		if (now - lastKeepAnchorInputTime <= RepeatedCommandWindowSeconds)
		{
			lastKeepAnchorInputTime = -10f;
			return MoveToIslandKeep(current.landMassIdx);
		}

		lastKeepAnchorInputTime = now;
		return AnnounceKeepRelativePosition(current.landMassIdx, current);
	}

	private bool AnnounceKeepRelativePosition(int landMass, Cell current)
	{
		if (!TryGetOwnedKeepCell(landMass, out Cell keepCell))
		{
			return true;
		}

		AnnounceText(DescribeRelativeOffsetToCell(current, keepCell), true);
		return true;
	}

	private bool MoveToIslandKeep(int landMass)
	{
		if (!TryGetOwnedKeepCell(landMass, out Cell keepCell))
		{
			return true;
		}

		InvalidateMatchingCellCache();
		FocusGameplayMapCell(keepCell, announce: false);
		AnnounceText(DescribeGameplayMapCell(keepCell), true);
		return true;
	}

	private string DescribeRelativeOffsetToCell(Cell origin, Cell target)
	{
		if (origin == null || target == null)
		{
			return string.Empty;
		}

		List<string> parts = new List<string>(2);
		int dx = target.x - origin.x;
		int dz = target.z - origin.z;
		if (dx != 0)
		{
			parts.Add(Mathf.Abs(dx) + (dx > 0 ? " east" : " west"));
		}
		if (dz != 0)
		{
			parts.Add(Mathf.Abs(dz) + (dz > 0 ? " north" : " south"));
		}

		return parts.Count == 0 ? "At keep" : string.Join(", ", parts);
	}

	private Building GetOwnedKeepForLandMass(int landMass)
	{
		List<Building> keeps = GetOwnedKeepsSorted();
		for (int i = 0; i < keeps.Count; i++)
		{
			if (keeps[i] != null && keeps[i].LandMass() == landMass)
			{
				return keeps[i];
			}
		}

		return Player.inst != null && Player.inst.keep != null ? Player.inst.keep.GetComponent<Building>() : null;
	}

	private bool CycleMatchingTiles(bool reverse)
	{
		Cell current = EnsureGameplayMapCursorCell();
		if (current == null)
		{
			return false;
		}

		string kind = DescribeGameplayCellPrimaryKind(current);
		if (string.IsNullOrEmpty(kind))
		{
			AnnounceText("No matching tile type.", true);
			return true;
		}

		if (!IsMatchingCellCacheValid(current, kind))
		{
			RebuildMatchingCellCache(current, kind);
		}

		if (gameplayMatchingCellXs.Length == 0)
		{
			AnnounceText("No other matching tiles.", true);
			return true;
		}

		if (reverse)
		{
			gameplayMatchingCellCycleIndex = gameplayMatchingCellCycleIndex <= 0
				? gameplayMatchingCellXs.Length - 1
				: gameplayMatchingCellCycleIndex - 1;
		}
		else
		{
			gameplayMatchingCellCycleIndex = (gameplayMatchingCellCycleIndex + 1) % gameplayMatchingCellXs.Length;
		}

		Cell target = World.inst.GetCellData(gameplayMatchingCellXs[gameplayMatchingCellCycleIndex], gameplayMatchingCellZs[gameplayMatchingCellCycleIndex]);
		if (target == null)
		{
			AnnounceText("No other matching tiles.", true);
			return true;
		}

		FocusGameplayMapCell(target, announce: false);
		AnnounceText(DescribeGameplayMapCell(target), true);
		return true;
	}

	private bool IsMatchingCellCacheValid(Cell current, string kind)
	{
		return gameplayMatchingCellXs.Length > 0
			&& gameplayMatchingCellZs.Length == gameplayMatchingCellXs.Length
			&& gameplayMatchingCellAnchorLandMass == current.landMassIdx
			&& string.Equals(gameplayMatchingCellKind, kind, StringComparison.Ordinal);
	}

	private void RebuildMatchingCellCache(Cell current, string kind)
	{
		List<GameplayCellMatch> matches = new List<GameplayCellMatch>();
		int landMass = current.landMassIdx;
		World.inst.ForEachTile(0, 0, World.inst.GridWidth - 1, World.inst.GridHeight - 1, delegate(int x, int z, Cell cell)
		{
			if (cell == null || cell == current || cell.landMassIdx != landMass)
			{
				return;
			}

			if (!string.Equals(DescribeGameplayCellPrimaryKind(cell), kind, StringComparison.Ordinal))
			{
				return;
			}

			int dx = cell.x - current.x;
			int dz = cell.z - current.z;
			matches.Add(new GameplayCellMatch
			{
				Cell = cell,
				DistanceSquared = dx * dx + dz * dz,
				TieBreakerAngle = GetClockwiseTieBreakerAngle(dx, dz)
			});
		});

		matches.Sort(delegate(GameplayCellMatch left, GameplayCellMatch right)
		{
			int distanceCompare = left.DistanceSquared.CompareTo(right.DistanceSquared);
			if (distanceCompare != 0)
			{
				return distanceCompare;
			}

			int angleCompare = left.TieBreakerAngle.CompareTo(right.TieBreakerAngle);
			if (angleCompare != 0)
			{
				return angleCompare;
			}

			int xCompare = left.Cell.x.CompareTo(right.Cell.x);
			return xCompare != 0 ? xCompare : left.Cell.z.CompareTo(right.Cell.z);
		});

		int matchCount = matches.Count;
		gameplayMatchingCellXs = new int[matchCount];
		gameplayMatchingCellZs = new int[matchCount];
		for (int i = 0; i < matchCount; i++)
		{
			Cell matchCell = matches[i].Cell;
			gameplayMatchingCellXs[i] = matchCell.x;
			gameplayMatchingCellZs[i] = matchCell.z;
		}
		gameplayMatchingCellCycleIndex = -1;
		gameplayMatchingCellAnchorX = current.x;
		gameplayMatchingCellAnchorZ = current.z;
		gameplayMatchingCellAnchorLandMass = current.landMassIdx;
		gameplayMatchingCellKind = kind;
	}

	private float GetClockwiseTieBreakerAngle(int dx, int dz)
	{
		float angle = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
		return angle < 0f ? angle + 360f : angle;
	}

	private bool HandleMeshAwareness()
	{
		Cell current = EnsureGameplayMapCursorCell();
		if (current == null)
		{
			return false;
		}

		float now = Time.realtimeSinceStartup;
		if (now - lastMeshAwarenessInputTime > RepeatedCommandWindowSeconds)
		{
			meshAwarenessTapCount = 0;
		}

		lastMeshAwarenessInputTime = now;
		meshAwarenessTapCount++;

		if (meshAwarenessTapCount == 1)
		{
			AnnounceMeshNeighbors(current);
			return true;
		}
		if (meshAwarenessTapCount == 2)
		{
			meshAwarenessTapCount = 0;
			AnnounceMeshNeighborSummary(current);
			return true;
		}

		meshAwarenessTapCount = 1;
		AnnounceMeshNeighbors(current);
		return true;
	}

	private void AnnounceMeshNeighbors(Cell current)
	{
		List<string> parts = new List<string>(8);
		foreach (Cell neighbor in GetClockwiseNeighborCells(current))
		{
			parts.Add(neighbor != null ? DescribeGameplayCellPrimaryKind(neighbor) : "Edge");
		}

		AnnounceText(string.Join(". ", parts) + ".", true);
	}

	private void AnnounceMeshNeighborSummary(Cell current)
	{
		Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (Cell neighbor in GetClockwiseNeighborCells(current))
		{
			string key = neighbor != null ? DescribeGameplayCellPrimaryKind(neighbor) : "Edge";
			if (!counts.ContainsKey(key))
			{
				counts[key] = 0;
			}
			counts[key]++;
		}

		List<string> parts = new List<string>(counts.Count);
		foreach (KeyValuePair<string, int> pair in counts.OrderBy(pair => pair.Key))
		{
			parts.Add(pair.Value + " " + pair.Key.ToLowerInvariant());
		}

		AnnounceText(string.Join(", ", parts), true);
	}

	private IEnumerable<Cell> GetClockwiseNeighborCells(Cell current)
	{
		int[,] offsets = new int[,]
		{
			{ -1, 1 },
			{ 0, 1 },
			{ 1, 1 },
			{ 1, 0 },
			{ 1, -1 },
			{ 0, -1 },
			{ -1, -1 },
			{ -1, 0 }
		};

		for (int i = 0; i < offsets.GetLength(0); i++)
		{
			yield return World.inst.GetCellData(current.x + offsets[i, 0], current.z + offsets[i, 1]);
		}
	}

	private void AnnounceDirectionalMesh(int dx, int dz)
	{
		Cell current = EnsureGameplayMapCursorCell();
		if (current == null)
		{
			return;
		}

		List<string> parts = new List<string>(3);
		foreach (Vector2Int offset in GetDirectionalMeshOffsets(dx, dz))
		{
			Cell cell = World.inst.GetCellData(current.x + offset.x, current.z + offset.y);
			parts.Add(cell != null ? DescribeGameplayCellPrimaryKind(cell) : "Edge");
		}

		AnnounceText(string.Join(". ", parts) + ".", true);
	}

	private Vector2Int[] GetDirectionalMeshOffsets(int dx, int dz)
	{
		if (dx > 0)
		{
			return DirectionalMeshOffsetsForward;
		}
		if (dx < 0)
		{
			return DirectionalMeshOffsetsBackward;
		}
		if (dz > 0)
		{
			return DirectionalMeshOffsetsLeft;
		}

		return DirectionalMeshOffsetsRight;
	}

	private bool AnnounceDirectionalScan()
	{
		if (!TryResolveDirectionalScanInput(out Cell current, out int dx, out int dz))
		{
			return false;
		}

		List<string> parts = new List<string>(3);
		HashSet<string> seenKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int distance = 1; distance <= DirectionalSummaryRadius; distance++)
		{
			Cell cell = World.inst.GetCellData(current.x + (dx * distance), current.z + (dz * distance));
			if (cell == null || cell.landMassIdx != current.landMassIdx)
			{
				break;
			}

			string kind = DescribeGameplayCellPrimaryKind(cell);
			if (!IsDirectionalScanInteresting(cell, kind) || !seenKinds.Add(kind))
			{
				continue;
			}

			parts.Add(kind + " " + distance + " tiles");
			if (parts.Count == 3)
			{
				break;
			}
		}

		AnnounceText(parts.Count > 0 ? string.Join(". ", parts) + "." : "No notable tiles in that direction.", true);
		return true;
	}

	private bool IsDirectionalScanInteresting(Cell cell, string kind)
	{
		return cell != null && (!string.Equals(kind, "Land", StringComparison.OrdinalIgnoreCase) || cell.TopMostStructure != null);
	}

	private bool AnnounceDirectionalSummary()
	{
		if (!TryResolveDirectionalScanInput(out Cell current, out int dx, out int dz))
		{
			return false;
		}

		Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int distance = 1; distance <= DirectionalSummaryRadius; distance++)
		{
			Cell cell = World.inst.GetCellData(current.x + (dx * distance), current.z + (dz * distance));
			if (cell == null || cell.landMassIdx != current.landMassIdx)
			{
				break;
			}

			string kind = DescribeGameplayCellPrimaryKind(cell);
			if (!counts.ContainsKey(kind))
			{
				counts[kind] = 0;
			}
			counts[kind]++;
		}

		if (counts.Count == 0)
		{
			AnnounceText("No tiles in that direction.", true);
			return true;
		}

		List<string> parts = counts
			.OrderByDescending(pair => pair.Value)
			.ThenBy(pair => pair.Key)
			.Select(pair => pair.Value + " " + pair.Key.ToLowerInvariant())
			.ToList();
		AnnounceText(string.Join(", ", parts) + " within " + DirectionalSummaryRadius + " tiles", true);
		return true;
	}

	private bool TryGetDirectionalStep(out int dx, out int dz)
	{
		if (IsNavigateLeftPressed())
		{
			dx = -1;
			dz = 0;
			return true;
		}
		if (IsNavigateRightPressed())
		{
			dx = 1;
			dz = 0;
			return true;
		}
		if (IsNavigateUpPressed())
		{
			dx = 0;
			dz = 1;
			return true;
		}
		if (IsNavigateDownPressed())
		{
			dx = 0;
			dz = -1;
			return true;
		}

		dx = 0;
		dz = 0;
		return false;
	}

	private bool TryResolveDirectionalScanInput(out Cell current, out int dx, out int dz)
	{
		current = EnsureGameplayMapCursorCell();
		if (current == null)
		{
			dx = 0;
			dz = 0;
			return false;
		}

		return TryGetDirectionalStep(out dx, out dz);
	}

	private bool IsGameplayDiscoveryBlockedByUiContext()
	{
		return (BuildUI.inst != null && BuildUI.inst.Visible) || IsPlacementModeActive();
	}

	private bool TryGetOwnedKeepCell(int landMass, out Cell keepCell)
	{
		keepCell = null;
		Building keep = GetOwnedKeepForLandMass(landMass);
		if (keep == null)
		{
			AnnounceText("No home keep on this island.", true);
			return false;
		}

		keepCell = keep.GetCell();
		if (keepCell == null)
		{
			AnnounceText("No home keep on this island.", true);
			return false;
		}

		return true;
	}

	private bool AnnounceResourceSlot(int slot)
	{
		int landMass = ResolveFocusedOwnedLandMass();
		if (landMass < 0)
		{
			AnnounceText("No owned island resources available.", true);
			return true;
		}

		FreeResourceType[] resourceOrder = FocusedResourceAnnouncementOrder;
		if (slot < 0 || slot >= resourceOrder.Length)
		{
			AnnounceText("No resource assigned to that slot.", true);
			return true;
		}

		ResourceAmount resources = Player.inst.resourcesPerLandmass != null && landMass < Player.inst.resourcesPerLandmass.Length
			? Player.inst.resourcesPerLandmass[landMass]
			: default(ResourceAmount);
		FreeResourceType resourceType = resourceOrder[slot];
		int amount = GetFocusedResourceAmount(resources, landMass, resourceType);
		Cell nearestSource = FindNearestResourceSourceCell(resourceType, landMass);
		if (nearestSource != null)
		{
			InvalidateMatchingCellCache();
			FocusGameplayMapCell(nearestSource, announce: false);
			AnnounceText(JoinParts(DescribeFocusedResource(resourceType, amount), DescribeGameplayMapCell(nearestSource)), true);
			return true;
		}

		AnnounceText(DescribeFocusedResource(resourceType, amount), true);
		return true;
	}

	private Cell FindNearestResourceSourceCell(FreeResourceType resourceType, int landMass)
	{
		Cell origin = EnsureGameplayMapCursorCell();
		if (origin == null || World.inst == null)
		{
			return null;
		}

		Cell bestCell = null;
		int bestDistance = int.MaxValue;
		float bestAngle = float.MaxValue;
		World.inst.ForEachTile(0, 0, World.inst.GridWidth - 1, World.inst.GridHeight - 1, delegate(int x, int z, Cell cell)
		{
			if (cell == null || cell.landMassIdx != landMass || !MatchesResourceSource(resourceType, cell))
			{
				return;
			}

			int dx = cell.x - origin.x;
			int dz = cell.z - origin.z;
			int distance = dx * dx + dz * dz;
			float angle = GetClockwiseTieBreakerAngle(dx, dz);
			if (distance < bestDistance || (distance == bestDistance && angle < bestAngle))
			{
				bestCell = cell;
				bestDistance = distance;
				bestAngle = angle;
			}
		});

		return bestCell;
	}

	private bool MatchesResourceSource(FreeResourceType resourceType, Cell cell)
	{
		switch (resourceType)
		{
			case FreeResourceType.Tree:
				return cell.TreeAmount > 0;
			case FreeResourceType.Stone:
				return cell.Type == ResourceType.Stone || cell.Type == ResourceType.UnusableStone;
			case FreeResourceType.IronOre:
				return cell.Type == ResourceType.IronDeposit;
			case FreeResourceType.Wheat:
				return cell.GetEffectiveFertility() >= 2 && cell.TopMostStructure == null && cell.Type != ResourceType.Water;
			case FreeResourceType.Fish:
				return cell.Type == ResourceType.Water && !cell.deepWater;
			default:
				return false;
		}
	}

	private bool ChangeGameplaySpeed(int delta)
	{
		if (SpeedControlUI.inst == null)
		{
			return false;
		}

		int current = GetCurrentGameplaySpeedIndex();
		int target = Mathf.Clamp(current + delta, 0, 3);
		if (target == current)
		{
			AnnounceText("Speed unchanged.", true);
			return true;
		}

		SpeedControlUI.inst.SetSpeed(target, skipNextSfx: false);
		AnnounceText(target == 0 ? "Paused" : "Speed " + target, true);
		return true;
	}

	private int GetCurrentGameplaySpeedIndex()
	{
		if (SpeedControlUI.inst == null)
		{
			return 1;
		}
		if (SpeedControlUI.inst.pauseButton != null && SpeedControlUI.inst.pauseButton.isOn)
		{
			return 0;
		}
		if (SpeedControlUI.inst.playButton2 != null && SpeedControlUI.inst.playButton2.isOn)
		{
			return 2;
		}
		if (SpeedControlUI.inst.playButton3 != null && SpeedControlUI.inst.playButton3.isOn)
		{
			return 3;
		}
		return 1;
	}

	private Cell EnsureGameplayMapCursorCell()
	{
		if (gameplayMapCursorCell == null)
		{
			InitializeGameplayMapCursor(GameplayMapInitAnnouncement.None);
		}

		return gameplayMapCursorCell;
	}
}
