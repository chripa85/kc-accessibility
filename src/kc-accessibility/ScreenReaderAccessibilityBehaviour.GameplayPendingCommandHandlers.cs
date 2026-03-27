using UnityEngine;

public partial class ScreenReaderAccessibilityBehaviour
{
	private bool TryHandlePendingGameplayCommand()
	{
		switch (pendingGameplayCommand)
		{
			case PendingGameplayCommand.None:
				return false;
			case PendingGameplayCommand.DirectionalMesh:
				return TryHandleDirectionalMeshSelection();
			case PendingGameplayCommand.ResourceValue:
				return TryHandleResourceValueSelection();
			case PendingGameplayCommand.SetNamedBookmarkSlot:
				return TryHandleNamedBookmarkSlotSelection();
			default:
				return false;
		}
	}

	private void ExpirePendingGameplayCommandIfNeeded()
	{
		if (pendingGameplayCommand == PendingGameplayCommand.None)
		{
			return;
		}

		if (pendingGameplayCommand == PendingGameplayCommand.ResourceValue)
		{
			return;
		}

		if (Time.realtimeSinceStartup - pendingGameplayCommandTime <= PendingGameplayCommandTimeoutSeconds)
		{
			return;
		}

		pendingGameplayCommand = PendingGameplayCommand.None;
		gameplayBookmarkSlot = -1;
	}

	private bool TryHandleDirectionalMeshSelection()
	{
		if (!TryGetDirectionalStep(out int dx, out int dz))
		{
			if (IsCancelPressed())
			{
				ClearPendingGameplayCommand();
				AnnounceText("Directional mesh canceled.", true);
				return true;
			}

			return false;
		}

		ClearPendingGameplayCommand();
		AnnounceDirectionalMesh(dx, dz);
		return true;
	}

	private bool TryHandleResourceValueSelection()
	{
		if (IsCancelPressed())
		{
			ClearPendingGameplayCommand();
			AnnounceText("Resource menu closed.", true);
			return true;
		}

		if (!TryGetPressedDigitSlot(out int slot))
		{
			if (HasPendingResourceMenuInterference())
			{
				AnnounceText("Resources. Press 1 to 0 for values.", true);
				return true;
			}

			return false;
		}

		ClearPendingGameplayCommand();
		return AnnounceResourceSlot(slot);
	}

	private bool TryHandleNamedBookmarkSlotSelection()
	{
		if (IsCancelPressed())
		{
			ClearPendingGameplayCommand();
			AnnounceText("Named bookmark canceled.", true);
			return true;
		}

		if (!TryGetPressedDigitSlot(out int slot))
		{
			return false;
		}

		ClearPendingGameplayCommand();
		return SetBookmark(slot, "Bookmark " + GetBookmarkSlotNumber(slot), announceNamed: true);
	}

	private void BeginPendingGameplayCommand(PendingGameplayCommand command, string prompt)
	{
		pendingGameplayCommand = command;
		pendingGameplayCommandTime = Time.realtimeSinceStartup;
		AnnounceText(prompt, true);
	}

	private void ClearPendingGameplayCommand()
	{
		pendingGameplayCommand = PendingGameplayCommand.None;
	}

	private bool HasPendingResourceMenuInterference()
	{
		return IsNavigateUpPressed()
			|| IsNavigateDownPressed()
			|| IsNavigateLeftPressed()
			|| IsNavigateRightPressed()
			|| IsNavigateNextPressed()
			|| IsNavigatePreviousPressed()
			|| IsSubmitPressed()
			|| IsSubmitAlternatePressed()
			|| IsResourceMenuPressed()
			|| IsResourceValuePromptPressed()
			|| IsOpenBuildMenuPressed()
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
			|| IsDecreaseSpeedPressed()
			|| IsChopShortcutPressed();
	}
}
