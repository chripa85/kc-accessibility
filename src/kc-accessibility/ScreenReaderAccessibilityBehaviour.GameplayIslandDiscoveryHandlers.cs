using System;
using System.Collections.Generic;
using System.Linq;
using Assets;
using Assets.Code;
using UnityEngine;

public partial class ScreenReaderAccessibilityBehaviour
{
	private const int KeepSiteSearchMinRadius = 2;
	private const int KeepSiteSearchMaxRadius = 10;
	private const int KeepSiteSearchResultLimit = 5;
	private const int KeepFootprintRadius = 1;
	private const int KeepSiteUsableLandRadius = 4;
	private const int KeepSiteClusterSpacing = 8;

	private bool TryHandleKeepSiteSearchSessionInput()
	{
		KeepSiteSearchSession session = gameplayKeepSiteSearch;
		if (session == null)
		{
			return false;
		}

		if (IsGameplayDiscoveryBlockedByUiContext())
		{
			return false;
		}

		Cell current = EnsureGameplayMapCursorCell();
		if (current == null)
		{
			return false;
		}

		if (current.landMassIdx != session.LandMass)
		{
			InvalidateKeepSiteSearchSession();
			return false;
		}

		if (IsCtrlHeld() || IsAltHeld() || IsShiftHeld())
		{
			return false;
		}

		if (IsNavigateUpPressed())
		{
			return BrowseKeepSiteSearch(-1);
		}
		if (IsNavigateDownPressed())
		{
			return BrowseKeepSiteSearch(1);
		}
		if (IsSubmitPressed())
		{
			return JumpToCurrentKeepSiteCandidate();
		}
		if (IsCancelPressed())
		{
			InvalidateKeepSiteSearchSession();
			AnnounceText("Keep site search closed.", true);
			return true;
		}

		return false;
	}

	private bool RunKeepSiteSearch()
	{
		if (IsGameplayDiscoveryBlockedByUiContext())
		{
			return false;
		}

		Cell current = EnsureGameplayMapCursorCell();
		if (current == null || World.inst == null)
		{
			return false;
		}

		if (current.landMassIdx < 0)
		{
			InvalidateKeepSiteSearchSession();
			AnnounceText("No island selected.", true);
			return true;
		}

		if (!TryValidateKeepSiteIsland(current.landMassIdx, out string islandFailure))
		{
			InvalidateKeepSiteSearchSession();
			AnnounceText(islandFailure, true);
			return true;
		}

		List<KeepSiteCandidate> candidates = FindKeepSiteCandidates(current.landMassIdx);
		if (candidates.Count == 0)
		{
			InvalidateKeepSiteSearchSession();
			AnnounceText("No valid keep sites found within 10 tiles.", true);
			return true;
		}

		gameplayKeepSiteSearch = new KeepSiteSearchSession
		{
			LandMass = current.landMassIdx,
			Candidates = candidates.Take(KeepSiteSearchResultLimit).ToArray(),
			CurrentIndex = 0
		};

		AnnounceCurrentKeepSiteCandidate();
		return true;
	}

	private bool TryValidateKeepSiteIsland(int landMass, out string failure)
	{
		bool hasTrees = false;
		bool hasStone = false;
		bool hasIron = false;
		bool hasExpansion = false;

		World.inst.ForEachTile(0, 0, World.inst.GridWidth - 1, World.inst.GridHeight - 1, delegate(int x, int z, Cell cell)
		{
			if (cell == null || cell.landMassIdx != landMass)
			{
				return;
			}

			hasTrees |= cell.TreeAmount > 0;
			hasStone |= cell.Type == ResourceType.Stone;
			hasIron |= cell.Type == ResourceType.IronDeposit;
			hasExpansion |= IsExpansionTerrainCell(cell);
		});

		List<string> missing = new List<string>(4);
		if (!hasTrees)
		{
			missing.Add("trees");
		}
		if (!hasStone)
		{
			missing.Add("quarry stone");
		}
		if (!hasIron)
		{
			missing.Add("iron");
		}
		if (!hasExpansion)
		{
			missing.Add("land");
		}

		if (missing.Count == 0)
		{
			failure = string.Empty;
			return true;
		}

		failure = "Island missing " + string.Join(", ", missing) + ".";
		return false;
	}

	private List<KeepSiteCandidate> FindKeepSiteCandidates(int landMass)
	{
		List<Cell> potentialCenters = new List<Cell>();
		World.inst.ForEachTile(0, 0, World.inst.GridWidth - 1, World.inst.GridHeight - 1, delegate(int x, int z, Cell cell)
		{
			if (IsValidKeepSiteCenter(cell, landMass))
			{
				potentialCenters.Add(cell);
			}
		});

		Dictionary<int, KeepSiteCandidate> accepted = new Dictionary<int, KeepSiteCandidate>();
		for (int radius = KeepSiteSearchMinRadius; radius <= KeepSiteSearchMaxRadius; radius++)
		{
			for (int i = 0; i < potentialCenters.Count; i++)
			{
				Cell center = potentialCenters[i];
				int key = (center.x << 16) ^ (center.z & 0xFFFF);
				if (accepted.ContainsKey(key))
				{
					continue;
				}

				KeepSiteCandidate candidate = EvaluateKeepSiteCandidate(center, landMass, radius);
				if (candidate != null)
				{
					accepted[key] = candidate;
				}
			}

			if (accepted.Count >= 50)
			{
				break;
			}
		}

		List<KeepSiteCandidate> candidates = accepted.Values.ToList();
		candidates.Sort(CompareKeepSiteCandidates);
		return SelectClusteredKeepSiteCandidates(candidates);
	}

	private bool IsValidKeepSiteCenter(Cell center, int landMass)
	{
		if (center == null || center.landMassIdx != landMass)
		{
			return false;
		}

		for (int dx = -KeepFootprintRadius; dx <= KeepFootprintRadius; dx++)
		{
			for (int dz = -KeepFootprintRadius; dz <= KeepFootprintRadius; dz++)
			{
				Cell cell = World.inst.GetCellData(center.x + dx, center.z + dz);
				if (!IsValidKeepFootprintCell(cell, landMass))
				{
					return false;
				}
			}
		}

		return true;
	}

	private bool IsValidKeepFootprintCell(Cell cell, int landMass)
	{
		return cell != null
			&& cell.landMassIdx == landMass
			&& cell.Type == ResourceType.None
			&& cell.TreeAmount <= 0
			&& cell.TopMostStructure == null
			&& !cell.Busy
			&& cell.GetEffectiveFertility() <= 0;
	}

	private KeepSiteCandidate EvaluateKeepSiteCandidate(Cell center, int landMass, int radius)
	{
		int nearestTree = int.MaxValue;
		int nearestStone = int.MaxValue;
		int nearestFertile = int.MaxValue;
		int nearestLand = int.MaxValue;

		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dz = -radius; dz <= radius; dz++)
			{
				Cell cell = World.inst.GetCellData(center.x + dx, center.z + dz);
				if (cell == null || cell.landMassIdx != landMass)
				{
					continue;
				}

				int distance = GetKeepSiteDistance(center, cell);
				if (cell.TreeAmount > 0 && distance < nearestTree)
				{
					nearestTree = distance;
				}
				if (cell.Type == ResourceType.Stone && distance < nearestStone)
				{
					nearestStone = distance;
				}
				if (IsFertileExpansionCell(cell) && distance < nearestFertile)
				{
					nearestFertile = distance;
				}
				if (IsPlainExpansionCell(cell) && distance < nearestLand)
				{
					nearestLand = distance;
				}
			}
		}

		if (nearestTree == int.MaxValue || nearestStone == int.MaxValue || (nearestFertile == int.MaxValue && nearestLand == int.MaxValue))
		{
			return null;
		}

		return new KeepSiteCandidate
		{
			CenterX = center.x,
			CenterZ = center.z,
			LandMass = landMass,
			RequiredRadius = radius,
			TreeDistance = nearestTree,
			StoneDistance = nearestStone,
			FertileDistance = nearestFertile,
			LandDistance = nearestLand,
			NearbyUsableLandCount = CountNearbyUsableLand(center, landMass, KeepSiteUsableLandRadius)
		};
	}

	private int CompareKeepSiteCandidates(KeepSiteCandidate left, KeepSiteCandidate right)
	{
		bool leftPreferredFertile = left.HasFertileLand && left.RequiredRadius <= right.RequiredRadius + 1;
		bool rightPreferredFertile = right.HasFertileLand && right.RequiredRadius <= left.RequiredRadius + 1;
		if (leftPreferredFertile != rightPreferredFertile)
		{
			return leftPreferredFertile ? -1 : 1;
		}

		int radiusCompare = left.RequiredRadius.CompareTo(right.RequiredRadius);
		if (radiusCompare != 0)
		{
			return radiusCompare;
		}

		if (left.HasFertileLand != right.HasFertileLand)
		{
			return left.HasFertileLand ? -1 : 1;
		}

		int leftMaxDistance = Mathf.Max(left.TreeDistance, Mathf.Max(left.StoneDistance, left.ExpansionDistance));
		int rightMaxDistance = Mathf.Max(right.TreeDistance, Mathf.Max(right.StoneDistance, right.ExpansionDistance));
		int maxDistanceCompare = leftMaxDistance.CompareTo(rightMaxDistance);
		if (maxDistanceCompare != 0)
		{
			return maxDistanceCompare;
		}

		int leftTotalDistance = left.TreeDistance + left.StoneDistance + left.ExpansionDistance;
		int rightTotalDistance = right.TreeDistance + right.StoneDistance + right.ExpansionDistance;
		int totalDistanceCompare = leftTotalDistance.CompareTo(rightTotalDistance);
		if (totalDistanceCompare != 0)
		{
			return totalDistanceCompare;
		}

		int usableLandCompare = right.NearbyUsableLandCount.CompareTo(left.NearbyUsableLandCount);
		if (usableLandCompare != 0)
		{
			return usableLandCompare;
		}

		int xCompare = left.CenterX.CompareTo(right.CenterX);
		return xCompare != 0 ? xCompare : left.CenterZ.CompareTo(right.CenterZ);
	}

	private List<KeepSiteCandidate> SelectClusteredKeepSiteCandidates(List<KeepSiteCandidate> candidates)
	{
		List<KeepSiteCandidate> selected = new List<KeepSiteCandidate>(KeepSiteSearchResultLimit);
		for (int i = 0; i < candidates.Count; i++)
		{
			KeepSiteCandidate candidate = candidates[i];
			if (IsKeepSiteCandidateTooCloseToSelected(candidate, selected, KeepSiteClusterSpacing))
			{
				continue;
			}

			selected.Add(candidate);
			if (selected.Count >= KeepSiteSearchResultLimit)
			{
				break;
			}
		}

		if (selected.Count == 0)
		{
			selected.AddRange(candidates.Take(KeepSiteSearchResultLimit));
		}

		return selected;
	}

	private bool IsKeepSiteCandidateTooCloseToSelected(KeepSiteCandidate candidate, List<KeepSiteCandidate> selected, int minimumDistance)
	{
		for (int i = 0; i < selected.Count; i++)
		{
			KeepSiteCandidate existing = selected[i];
			int distance = Mathf.Max(Mathf.Abs(existing.CenterX - candidate.CenterX), Mathf.Abs(existing.CenterZ - candidate.CenterZ));
			if (distance < minimumDistance)
			{
				return true;
			}
		}

		return false;
	}

	private bool AnnounceFocusedIslandSummary()
	{
		Cell current = EnsureGameplayMapCursorCell();
		if (current == null || World.inst == null)
		{
			return false;
		}

		if (current.landMassIdx < 0)
		{
			AnnounceText("No island selected.", true);
			return true;
		}

		AnnounceText(DescribeIslandSummary(current.landMassIdx), true);
		return true;
	}

	private bool CycleFocusedIsland()
	{
		if (World.inst == null)
		{
			return false;
		}

		List<int> landMasses = GetAllLandMasses();
		if (landMasses.Count == 0)
		{
			AnnounceText("No islands found.", true);
			return true;
		}

		Cell current = EnsureGameplayMapCursorCell();
		int currentLandMass = current != null ? current.landMassIdx : -1;
		int currentIndex = -1;
		for (int i = 0; i < landMasses.Count; i++)
		{
			if (landMasses[i] == currentLandMass)
			{
				currentIndex = i;
				break;
			}
		}

		int nextIndex = landMasses.Count == 1
			? 0
			: ((currentIndex >= 0 ? currentIndex : -1) + 1 + landMasses.Count) % landMasses.Count;
		int nextLandMass = landMasses[nextIndex];
		Cell target = FindRepresentativeIslandCell(nextLandMass);
		if (target == null)
		{
			AnnounceText("Unable to move to next island.", true);
			return true;
		}

		InvalidateMatchingCellCache();
		FocusGameplayMapCell(target, announce: false);
		AnnounceText(DescribeIslandSummary(nextLandMass), true);
		return true;
	}

	private List<int> GetAllLandMasses()
	{
		List<int> landMasses = new List<int>();
		if (World.inst == null)
		{
			return landMasses;
		}

		for (int landMass = 0; landMass < World.inst.NumLandMasses; landMass++)
		{
			ArrayExt<Cell> cells = World.inst.cellsToLandmass != null && landMass < World.inst.cellsToLandmass.Length
				? World.inst.cellsToLandmass[landMass]
				: null;
			if (cells != null && cells.Count > 0)
			{
				landMasses.Add(landMass);
			}
		}

		return landMasses;
	}

	private Cell FindRepresentativeIslandCell(int landMass)
	{
		if (World.inst == null || landMass < 0 || World.inst.cellsToLandmass == null || landMass >= World.inst.cellsToLandmass.Length)
		{
			return null;
		}

		ArrayExt<Cell> cells = World.inst.cellsToLandmass[landMass];
		if (cells == null || cells.Count <= 0)
		{
			return null;
		}

		float averageX = 0f;
		float averageZ = 0f;
		for (int i = 0; i < cells.Count; i++)
		{
			Cell cell = cells.data[i];
			if (cell == null)
			{
				continue;
			}

			averageX += cell.x;
			averageZ += cell.z;
		}

		averageX /= Mathf.Max(1, cells.Count);
		averageZ /= Mathf.Max(1, cells.Count);

		Cell bestLand = null;
		float bestLandDistance = float.MaxValue;
		Cell bestAny = null;
		float bestAnyDistance = float.MaxValue;
		for (int i = 0; i < cells.Count; i++)
		{
			Cell cell = cells.data[i];
			if (cell == null)
			{
				continue;
			}

			float distance = Mathf.Abs(cell.x - averageX) + Mathf.Abs(cell.z - averageZ);
			if (distance < bestAnyDistance)
			{
				bestAny = cell;
				bestAnyDistance = distance;
			}

			if (!IsCycleIslandRepresentativeCell(cell))
			{
				continue;
			}

			if (distance < bestLandDistance)
			{
				bestLand = cell;
				bestLandDistance = distance;
			}
		}

		return bestLand ?? bestAny;
	}

	private bool IsCycleIslandRepresentativeCell(Cell cell)
	{
		return cell != null
			&& cell.Type != ResourceType.Water
			&& !cell.deepWater
			&& cell.TopMostStructure == null;
	}

	private string DescribeIslandSummary(int landMass)
	{
		IslandSummary summary = AnalyzeIsland(landMass);
		if (summary == null)
		{
			return "No island info available.";
		}

		return JoinParts(
			GetIslandName(landMass),
			GetIslandOwnershipSummary(landMass),
			summary.Size > 0 ? "Size " + summary.Size : string.Empty,
			summary.HasTrees ? "Trees" : "No trees",
			summary.HasStone ? "Stone" : "No stone",
			summary.HasIron ? "Iron" : "No iron",
			summary.HasFertileLand ? "Fertile land" : (summary.HasLand ? "Land" : "No land"));
	}

	private sealed class IslandSummary
	{
		public int Size;
		public bool HasTrees;
		public bool HasStone;
		public bool HasIron;
		public bool HasFertileLand;
		public bool HasLand;
	}

	private IslandSummary AnalyzeIsland(int landMass)
	{
		if (World.inst == null || landMass < 0 || World.inst.cellsToLandmass == null || landMass >= World.inst.cellsToLandmass.Length)
		{
			return null;
		}

		ArrayExt<Cell> cells = World.inst.cellsToLandmass[landMass];
		if (cells == null || cells.Count <= 0)
		{
			return null;
		}

		IslandSummary summary = new IslandSummary();
		for (int i = 0; i < cells.Count; i++)
		{
			Cell cell = cells.data[i];
			if (cell == null)
			{
				continue;
			}

			summary.Size++;
			summary.HasTrees |= cell.TreeAmount > 0;
			summary.HasStone |= cell.Type == ResourceType.Stone;
			summary.HasIron |= cell.Type == ResourceType.IronDeposit;
			summary.HasFertileLand |= IsFertileExpansionCell(cell);
			summary.HasLand |= IsExpansionTerrainCell(cell);
		}

		return summary;
	}

	private string GetIslandOwnershipSummary(int landMass)
	{
		LandmassOwner owner = World.GetLandmassOwner(landMass);
		if (owner == null)
		{
			return "Unowned";
		}

		if (Player.inst != null && Player.inst.PlayerLandmassOwner == owner)
		{
			return "Owned";
		}

		return "Foreign owned";
	}

	private string GetIslandName(int landMass)
	{
		string islandName = Player.inst != null
			&& Player.inst.LandMassNames != null
			&& landMass >= 0
			&& landMass < Player.inst.LandMassNames.Count
			? CleanText(Player.inst.LandMassNames[landMass])
			: string.Empty;
		return string.IsNullOrEmpty(islandName) ? "Island " + (landMass + 1) : islandName;
	}

	private bool BrowseKeepSiteSearch(int step)
	{
		KeepSiteSearchSession session = gameplayKeepSiteSearch;
		if (session == null || session.Candidates == null || session.Candidates.Length == 0)
		{
			InvalidateKeepSiteSearchSession();
			AnnounceText("No keep site results.", true);
			return true;
		}

		int count = session.Candidates.Length;
		session.CurrentIndex = (session.CurrentIndex + step + count) % count;
		AnnounceCurrentKeepSiteCandidate();
		return true;
	}

	private bool JumpToCurrentKeepSiteCandidate()
	{
		KeepSiteSearchSession session = gameplayKeepSiteSearch;
		if (session == null || session.Candidates == null || session.Candidates.Length == 0)
		{
			InvalidateKeepSiteSearchSession();
			AnnounceText("No keep site selected.", true);
			return true;
		}

		KeepSiteCandidate candidate = session.Candidates[Mathf.Clamp(session.CurrentIndex, 0, session.Candidates.Length - 1)];
		Cell target = World.inst.GetCellData(candidate.CenterX, candidate.CenterZ);
		if (target == null)
		{
			AnnounceText("Keep site is unavailable.", true);
			return true;
		}

		FocusGameplayMapCell(target, announce: false);
		AnnounceText(JoinParts("Keep site", DescribeGameplayMapCell(target)), true);
		return true;
	}

	private void AnnounceCurrentKeepSiteCandidate()
	{
		KeepSiteSearchSession session = gameplayKeepSiteSearch;
		if (session == null || session.Candidates == null || session.Candidates.Length == 0)
		{
			InvalidateKeepSiteSearchSession();
			return;
		}

		int index = Mathf.Clamp(session.CurrentIndex, 0, session.Candidates.Length - 1);
		KeepSiteCandidate candidate = session.Candidates[index];
		List<string> parts = new List<string>(8)
		{
			(index + 1) + " of " + session.Candidates.Length,
			candidate.CenterX + ", " + candidate.CenterZ,
			"radius " + candidate.RequiredRadius,
			"tree " + candidate.TreeDistance,
			"stone " + candidate.StoneDistance
		};

		if (candidate.HasFertileLand)
		{
			parts.Add("fertile " + candidate.FertileDistance);
		}
		else
		{
			parts.Add("land " + candidate.LandDistance);
			parts.Add("no fertile");
		}

		AnnounceText(string.Join(". ", parts) + ".", true);
	}

	private void InvalidateKeepSiteSearchSession()
	{
		gameplayKeepSiteSearch = null;
	}

	private int CountNearbyUsableLand(Cell center, int landMass, int radius)
	{
		int count = 0;
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dz = -radius; dz <= radius; dz++)
			{
				Cell cell = World.inst.GetCellData(center.x + dx, center.z + dz);
				if (cell != null && cell.landMassIdx == landMass && IsExpansionTerrainCell(cell))
				{
					count++;
				}
			}
		}
		return count;
	}

	private int GetKeepSiteDistance(Cell origin, Cell target)
	{
		return Mathf.Max(Mathf.Abs(target.x - origin.x), Mathf.Abs(target.z - origin.z));
	}

	private bool IsExpansionTerrainCell(Cell cell)
	{
		return IsFertileExpansionCell(cell) || IsPlainExpansionCell(cell);
	}

	private bool IsFertileExpansionCell(Cell cell)
	{
		return cell != null
			&& cell.Type == ResourceType.None
			&& cell.TreeAmount <= 0
			&& cell.TopMostStructure == null
			&& !cell.Busy
			&& cell.GetEffectiveFertility() >= 2;
	}

	private bool IsPlainExpansionCell(Cell cell)
	{
		return cell != null
			&& cell.Type == ResourceType.None
			&& cell.TreeAmount <= 0
			&& cell.TopMostStructure == null
			&& !cell.Busy
			&& cell.GetEffectiveFertility() == 1;
	}
}
