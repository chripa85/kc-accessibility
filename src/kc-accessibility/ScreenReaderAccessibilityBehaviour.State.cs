using TMPro;
using UnityEngine;

public partial class ScreenReaderAccessibilityBehaviour
{
	private enum PendingGameplayCommand
	{
		None,
		DirectionalMesh,
		ResourceValue,
		SetBookmark,
		JumpBookmark,
		SetNamedBookmarkSlot
	}

	private sealed class GameplayBookmark
	{
		public int X;
		public int Z;
		public int LandMass = -1;
		public string Name = string.Empty;
		public bool IsSet;
	}

	private sealed class KeepSiteCandidate
	{
		public int CenterX;
		public int CenterZ;
		public int LandMass = -1;
		public int RequiredRadius;
		public int TreeDistance = int.MaxValue;
		public int StoneDistance = int.MaxValue;
		public int FertileDistance = int.MaxValue;
		public int LandDistance = int.MaxValue;
		public int NearbyUsableLandCount;

		public bool HasFertileLand
		{
			get { return FertileDistance != int.MaxValue; }
		}

		public int ExpansionDistance
		{
			get { return HasFertileLand ? FertileDistance : LandDistance; }
		}
	}

	private sealed class KeepSiteSearchSession
	{
		public int LandMass = -1;
		public KeepSiteCandidate[] Candidates = new KeepSiteCandidate[0];
		public int CurrentIndex;
	}

	private sealed class MainMenuNavigationRuntimeState
	{
		public int TopLevelMenuIndex = -1;
		public int ChooseModeIndex = -1;
		public int ChooseDifficultyIndex = -1;
		public int NameBannerIndex = -1;
		public int NewMapIndex = -1;
		public int SettingsMenuIndex = -1;
		public int PauseMenuIndex = -1;
		public int ConfirmationDialogIndex = -1;
		public MainMenuMode.State? LastAnnouncedState;
		public TMP_InputField ActiveTextField;
		public string ActiveTextFieldLabel = string.Empty;
	}

	private sealed class GameplayNavigationRuntimeState
	{
		public bool KeyboardNavigationActive;
		public Cell MapCursorCell;
		public int TileElementIndex = -1;
		public int PanelIndex = -1;
		public int WorkerIndex = -1;
		public int AdvisorIndex = -1;
		public int PersonIndex = -1;
		public int JobPriorityIndex = -1;
		public int ConstructIndex = -1;
		public int ResourceIndex = -1;
		public float LastResourceCycleInputTime = -10f;
		public int BuildMenuItemIndex = -1;
		public int BuildMenuDetailIndex = -1;
		public int SuppressedHoverAnnouncements;
		public float LastKeepAnchorInputTime = -10f;
		public float LastMeshAwarenessInputTime = -10f;
		public int MeshAwarenessTapCount;
		public PendingGameplayCommand PendingCommand;
		public float PendingCommandTime = -10f;
		public int BookmarkSlot = -1;
		public int[] MatchingCellXs = new int[0];
		public int[] MatchingCellZs = new int[0];
		public int MatchingCellCycleIndex = -1;
		public int MatchingCellAnchorX = int.MinValue;
		public int MatchingCellAnchorZ = int.MinValue;
		public int MatchingCellAnchorLandMass = -1;
		public string MatchingCellKind = string.Empty;
		public GameplayBookmark[] Bookmarks = CreateBookmarks();
		public KeepSiteSearchSession KeepSiteSearch;
	}

	private sealed class SpeechRuntimeState
	{
		public string LastAnnouncement = string.Empty;
		public float LastAnnouncementTime = -10f;
		public float GenericHoverAnnouncementsSuppressedUntil = -1f;
	}

	private readonly MainMenuNavigationRuntimeState mainMenuNavigationState = new MainMenuNavigationRuntimeState();
	private readonly GameplayNavigationRuntimeState gameplayNavigationState = new GameplayNavigationRuntimeState();
	private readonly SpeechRuntimeState speechRuntimeState = new SpeechRuntimeState();

	private bool gameplayKeyboardNavigationActive
	{
		get { return gameplayNavigationState.KeyboardNavigationActive; }
		set { gameplayNavigationState.KeyboardNavigationActive = value; }
	}

	private Cell gameplayMapCursorCell
	{
		get { return gameplayNavigationState.MapCursorCell; }
		set { gameplayNavigationState.MapCursorCell = value; }
	}

	private int gameplayTileElementIndex
	{
		get { return gameplayNavigationState.TileElementIndex; }
		set { gameplayNavigationState.TileElementIndex = value; }
	}

	private int gameplayPanelIndex
	{
		get { return gameplayNavigationState.PanelIndex; }
		set { gameplayNavigationState.PanelIndex = value; }
	}

	private int gameplayWorkerIndex
	{
		get { return gameplayNavigationState.WorkerIndex; }
		set { gameplayNavigationState.WorkerIndex = value; }
	}

	private int gameplayAdvisorIndex
	{
		get { return gameplayNavigationState.AdvisorIndex; }
		set { gameplayNavigationState.AdvisorIndex = value; }
	}

	private int gameplayPersonIndex
	{
		get { return gameplayNavigationState.PersonIndex; }
		set { gameplayNavigationState.PersonIndex = value; }
	}

	private int gameplayJobPriorityIndex
	{
		get { return gameplayNavigationState.JobPriorityIndex; }
		set { gameplayNavigationState.JobPriorityIndex = value; }
	}

	private int gameplayConstructIndex
	{
		get { return gameplayNavigationState.ConstructIndex; }
		set { gameplayNavigationState.ConstructIndex = value; }
	}

	private int gameplayResourceIndex
	{
		get { return gameplayNavigationState.ResourceIndex; }
		set { gameplayNavigationState.ResourceIndex = value; }
	}

	private float lastGameplayResourceCycleInputTime
	{
		get { return gameplayNavigationState.LastResourceCycleInputTime; }
		set { gameplayNavigationState.LastResourceCycleInputTime = value; }
	}

	private int buildMenuItemIndex
	{
		get { return gameplayNavigationState.BuildMenuItemIndex; }
		set { gameplayNavigationState.BuildMenuItemIndex = value; }
	}

	private int buildMenuDetailIndex
	{
		get { return gameplayNavigationState.BuildMenuDetailIndex; }
		set { gameplayNavigationState.BuildMenuDetailIndex = value; }
	}

	private int suppressedHoverAnnouncements
	{
		get { return gameplayNavigationState.SuppressedHoverAnnouncements; }
		set { gameplayNavigationState.SuppressedHoverAnnouncements = value; }
	}

	private float lastKeepAnchorInputTime
	{
		get { return gameplayNavigationState.LastKeepAnchorInputTime; }
		set { gameplayNavigationState.LastKeepAnchorInputTime = value; }
	}

	private float lastMeshAwarenessInputTime
	{
		get { return gameplayNavigationState.LastMeshAwarenessInputTime; }
		set { gameplayNavigationState.LastMeshAwarenessInputTime = value; }
	}

	private int meshAwarenessTapCount
	{
		get { return gameplayNavigationState.MeshAwarenessTapCount; }
		set { gameplayNavigationState.MeshAwarenessTapCount = value; }
	}

	private PendingGameplayCommand pendingGameplayCommand
	{
		get { return gameplayNavigationState.PendingCommand; }
		set { gameplayNavigationState.PendingCommand = value; }
	}

	private float pendingGameplayCommandTime
	{
		get { return gameplayNavigationState.PendingCommandTime; }
		set { gameplayNavigationState.PendingCommandTime = value; }
	}

	private int gameplayBookmarkSlot
	{
		get { return gameplayNavigationState.BookmarkSlot; }
		set { gameplayNavigationState.BookmarkSlot = value; }
	}

	private int[] gameplayMatchingCellXs
	{
		get { return gameplayNavigationState.MatchingCellXs; }
		set { gameplayNavigationState.MatchingCellXs = value ?? new int[0]; }
	}

	private int[] gameplayMatchingCellZs
	{
		get { return gameplayNavigationState.MatchingCellZs; }
		set { gameplayNavigationState.MatchingCellZs = value ?? new int[0]; }
	}

	private int gameplayMatchingCellCycleIndex
	{
		get { return gameplayNavigationState.MatchingCellCycleIndex; }
		set { gameplayNavigationState.MatchingCellCycleIndex = value; }
	}

	private int gameplayMatchingCellAnchorX
	{
		get { return gameplayNavigationState.MatchingCellAnchorX; }
		set { gameplayNavigationState.MatchingCellAnchorX = value; }
	}

	private int gameplayMatchingCellAnchorZ
	{
		get { return gameplayNavigationState.MatchingCellAnchorZ; }
		set { gameplayNavigationState.MatchingCellAnchorZ = value; }
	}

	private int gameplayMatchingCellAnchorLandMass
	{
		get { return gameplayNavigationState.MatchingCellAnchorLandMass; }
		set { gameplayNavigationState.MatchingCellAnchorLandMass = value; }
	}

	private string gameplayMatchingCellKind
	{
		get { return gameplayNavigationState.MatchingCellKind; }
		set { gameplayNavigationState.MatchingCellKind = value ?? string.Empty; }
	}

	private GameplayBookmark[] gameplayBookmarks
	{
		get { return gameplayNavigationState.Bookmarks; }
	}

	private KeepSiteSearchSession gameplayKeepSiteSearch
	{
		get { return gameplayNavigationState.KeepSiteSearch; }
		set { gameplayNavigationState.KeepSiteSearch = value; }
	}

	private int topLevelMenuIndex
	{
		get { return mainMenuNavigationState.TopLevelMenuIndex; }
		set { mainMenuNavigationState.TopLevelMenuIndex = value; }
	}

	private int chooseModeIndex
	{
		get { return mainMenuNavigationState.ChooseModeIndex; }
		set { mainMenuNavigationState.ChooseModeIndex = value; }
	}

	private int chooseDifficultyIndex
	{
		get { return mainMenuNavigationState.ChooseDifficultyIndex; }
		set { mainMenuNavigationState.ChooseDifficultyIndex = value; }
	}

	private int nameBannerIndex
	{
		get { return mainMenuNavigationState.NameBannerIndex; }
		set { mainMenuNavigationState.NameBannerIndex = value; }
	}

	private int newMapIndex
	{
		get { return mainMenuNavigationState.NewMapIndex; }
		set { mainMenuNavigationState.NewMapIndex = value; }
	}

	private int settingsMenuIndex
	{
		get { return mainMenuNavigationState.SettingsMenuIndex; }
		set { mainMenuNavigationState.SettingsMenuIndex = value; }
	}

	private int pauseMenuIndex
	{
		get { return mainMenuNavigationState.PauseMenuIndex; }
		set { mainMenuNavigationState.PauseMenuIndex = value; }
	}

	private int confirmationDialogIndex
	{
		get { return mainMenuNavigationState.ConfirmationDialogIndex; }
		set { mainMenuNavigationState.ConfirmationDialogIndex = value; }
	}

	private MainMenuMode.State? lastAnnouncedState
	{
		get { return mainMenuNavigationState.LastAnnouncedState; }
		set { mainMenuNavigationState.LastAnnouncedState = value; }
	}

	private TMP_InputField activeTextField
	{
		get { return mainMenuNavigationState.ActiveTextField; }
		set { mainMenuNavigationState.ActiveTextField = value; }
	}

	private string activeTextFieldLabel
	{
		get { return mainMenuNavigationState.ActiveTextFieldLabel; }
		set { mainMenuNavigationState.ActiveTextFieldLabel = value ?? string.Empty; }
	}

	private string lastAnnouncement
	{
		get { return speechRuntimeState.LastAnnouncement; }
		set { speechRuntimeState.LastAnnouncement = value ?? string.Empty; }
	}

	private float lastAnnouncementTime
	{
		get { return speechRuntimeState.LastAnnouncementTime; }
		set { speechRuntimeState.LastAnnouncementTime = value; }
	}

	private float genericHoverAnnouncementsSuppressedUntil
	{
		get { return speechRuntimeState.GenericHoverAnnouncementsSuppressedUntil; }
		set { speechRuntimeState.GenericHoverAnnouncementsSuppressedUntil = value; }
	}

	private struct AccessibilityStateSnapshot
	{
		public readonly bool IsMainMenuActive;
		public readonly bool IsPlayingModeActive;
		public readonly MainMenuMode.State? MainMenuState;
		public readonly bool IsBuildMenuVisible;
		public readonly bool IsPlacing;
		public readonly bool IsAdvisorVisible;
		public readonly bool IsIslandInfoVisible;

		public AccessibilityStateSnapshot(
			bool isMainMenuActive,
			bool isPlayingModeActive,
			MainMenuMode.State? mainMenuState,
			bool isBuildMenuVisible,
			bool isPlacing,
			bool isAdvisorVisible,
			bool isIslandInfoVisible)
		{
			IsMainMenuActive = isMainMenuActive;
			IsPlayingModeActive = isPlayingModeActive;
			MainMenuState = mainMenuState;
			IsBuildMenuVisible = isBuildMenuVisible;
			IsPlacing = isPlacing;
			IsAdvisorVisible = isAdvisorVisible;
			IsIslandInfoVisible = isIslandInfoVisible;
		}
	}

	private AccessibilityStateSnapshot CaptureAccessibilityStateSnapshot()
	{
		GameState state = GameState.inst;
		MainMenuMode.State? mainMenuState = null;
		bool isMainMenuActive = false;
		bool isPlayingModeActive = false;

		if (state != null)
		{
			isMainMenuActive = state.CurrMode == state.mainMenuMode;
			isPlayingModeActive = state.CurrMode == state.playingMode;
			if (state.mainMenuMode != null)
			{
				mainMenuState = state.mainMenuMode.GetState();
			}
		}

		bool isBuildMenuVisible = BuildUI.inst != null && BuildUI.inst.Visible;
		bool isPlacing = IsPlacementModeActive();
		bool isAdvisorVisible = AdvisorUI.inst != null && AdvisorUI.inst.IsVisible();
		bool isIslandInfoVisible = IsIslandInfoPanelVisible(GameUI.inst);

		return new AccessibilityStateSnapshot(
			isMainMenuActive,
			isPlayingModeActive,
			mainMenuState,
			isBuildMenuVisible,
			isPlacing,
			isAdvisorVisible,
			isIslandInfoVisible);
	}

	private static GameplayBookmark[] CreateBookmarks()
	{
		GameplayBookmark[] bookmarks = new GameplayBookmark[10];
		for (int i = 0; i < bookmarks.Length; i++)
		{
			bookmarks[i] = new GameplayBookmark();
		}
		return bookmarks;
	}
}
