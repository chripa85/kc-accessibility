using System;
using UnityEngine;

internal enum AccessibilityAction
{
	NavigateUp,
	NavigateDown,
	NavigateLeft,
	NavigateRight,
	NavigateNext,
	NavigatePrevious,
	Submit,
	SubmitAlternate,
	Cancel,
	OpenBuildMenu,
	ResourceMenu,
	FastMoveModifier,
	SetSpeedControlUI1,
	SetSpeedControlUI2,
	SetSpeedControlUI3,
	SetSpeedControlUI4,
	ChopShortcut,
	EscapeButtonBehavior,
	IslandInfo,
	KeepAnchor,
	CycleMatchingTiles,
	MeshAwareness,
	RotateBuilding,
	DeleteBuilding
}

internal sealed class ModKeyBindings
{
	private readonly KeyCode[] navigateUp = new[] { KeyCode.E };
	private readonly KeyCode[] navigateDown = new[] { KeyCode.D };
	private readonly KeyCode[] navigateLeft = new[] { KeyCode.S };
	private readonly KeyCode[] navigateRight = new[] { KeyCode.F };
	private readonly KeyCode[] navigateNext = new[] { KeyCode.Tab };
	private readonly KeyCode[] navigatePrevious = new[] { KeyCode.Tab };
	private readonly KeyCode[] submit = Array.Empty<KeyCode>();
	private readonly KeyCode[] submitAlternate = new[] { KeyCode.Space };
	private readonly KeyCode[] cancel = Array.Empty<KeyCode>();
	private readonly KeyCode[] openBuildMenu = new[] { KeyCode.B };
	private readonly KeyCode[] resourceMenu = new[] { KeyCode.R };
	private readonly KeyCode[] fastMoveModifier = new[] { KeyCode.LeftShift, KeyCode.RightShift };
	private readonly KeyCode[] setSpeedControlUI1 = new[] { KeyCode.Space };
	private readonly KeyCode[] setSpeedControlUI2 = new[] { KeyCode.Alpha1 };
	private readonly KeyCode[] setSpeedControlUI3 = new[] { KeyCode.Alpha2 };
	private readonly KeyCode[] setSpeedControlUI4 = new[] { KeyCode.Alpha3 };
	private readonly KeyCode[] chopShortcut = new[] { KeyCode.C };
	private readonly KeyCode[] escapeButtonBehavior = new[] { KeyCode.Escape };
	private readonly KeyCode[] islandInfo = new[] { KeyCode.Q };
	private readonly KeyCode[] keepAnchor = new[] { KeyCode.H };
	private readonly KeyCode[] cycleMatchingTiles = new[] { KeyCode.W };
	private readonly KeyCode[] meshAwareness = new[] { KeyCode.V };
	private readonly KeyCode[] rotateBuilding = new[] { KeyCode.T };
	private readonly KeyCode[] deleteBuilding = new[] { KeyCode.Delete };

	private ModKeyBindings()
	{
	}

	public static ModKeyBindings LoadOrCreate(Action<string> log)
	{
		if (log != null)
		{
			log("Using in-code keybinding defaults. External keybinding file loading is disabled in this runtime.");
		}
		return new ModKeyBindings();
	}

	public bool IsPressed(AccessibilityAction action)
	{
		KeyCode[] keys = GetKeys(action);
		for (int i = 0; i < keys.Length; i++)
		{
			if (Input.GetKeyDown(keys[i]))
			{
				if (action == AccessibilityAction.NavigateNext && IsShiftHeld())
				{
					continue;
				}
				if (action == AccessibilityAction.NavigatePrevious && !IsShiftHeld())
				{
					continue;
				}
				return true;
			}
		}
		return false;
	}

	public bool IsHeld(AccessibilityAction action)
	{
		KeyCode[] keys = GetKeys(action);
		for (int i = 0; i < keys.Length; i++)
		{
			if (Input.GetKey(keys[i]))
			{
				return true;
			}
		}
		return false;
	}

	private KeyCode[] GetKeys(AccessibilityAction action)
	{
		switch (action)
		{
			case AccessibilityAction.NavigateUp:
				return navigateUp;
			case AccessibilityAction.NavigateDown:
				return navigateDown;
			case AccessibilityAction.NavigateLeft:
				return navigateLeft;
			case AccessibilityAction.NavigateRight:
				return navigateRight;
			case AccessibilityAction.NavigateNext:
				return navigateNext;
			case AccessibilityAction.NavigatePrevious:
				return navigatePrevious;
			case AccessibilityAction.Submit:
				return submit;
			case AccessibilityAction.SubmitAlternate:
				return submitAlternate;
			case AccessibilityAction.Cancel:
				return cancel;
			case AccessibilityAction.OpenBuildMenu:
				return openBuildMenu;
			case AccessibilityAction.ResourceMenu:
				return resourceMenu;
			case AccessibilityAction.FastMoveModifier:
				return fastMoveModifier;
			case AccessibilityAction.SetSpeedControlUI1:
				return setSpeedControlUI1;
			case AccessibilityAction.SetSpeedControlUI2:
				return setSpeedControlUI2;
			case AccessibilityAction.SetSpeedControlUI3:
				return setSpeedControlUI3;
			case AccessibilityAction.SetSpeedControlUI4:
				return setSpeedControlUI4;
			case AccessibilityAction.ChopShortcut:
				return chopShortcut;
			case AccessibilityAction.EscapeButtonBehavior:
				return escapeButtonBehavior;
			case AccessibilityAction.IslandInfo:
				return islandInfo;
			case AccessibilityAction.KeepAnchor:
				return keepAnchor;
			case AccessibilityAction.CycleMatchingTiles:
				return cycleMatchingTiles;
			case AccessibilityAction.MeshAwareness:
				return meshAwareness;
			case AccessibilityAction.RotateBuilding:
				return rotateBuilding;
			case AccessibilityAction.DeleteBuilding:
				return deleteBuilding;
			default:
				return Array.Empty<KeyCode>();
		}
	}

	private static bool IsShiftHeld()
	{
		return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
	}
}
