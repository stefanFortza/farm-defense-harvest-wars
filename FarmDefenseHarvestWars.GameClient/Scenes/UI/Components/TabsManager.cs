using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

namespace FarmDefenseHarvestWars.GameClient.Scenes.UI.Components;

public partial class TabsManager : Control
{
	[Export] private Control _leftZone = null!;
	[Export] private Control _rightZone = null!;

	// Pages discovered in zones by TabKey (node names should match TabKey)
	private readonly Dictionary<string, Control?> _leftPages = new();
	private readonly Dictionary<string, Control?> _rightPages = new();
	public override void _Ready()
	{
		this.EnsureNotNull(_leftZone, nameof(_leftZone));
		this.EnsureNotNull(_rightZone, nameof(_rightZone));

		// Iterate through all children and connect to TabButton signals
		foreach (var child in GetChildren())
		{
			if (child is TabButton tabBtn)
			{
				// Connect to custom animation finished signal
				tabBtn.AnimationFinished += OnTabAnimationFinished;

				// Also connect to Toggled for immediate Z-Index response
				tabBtn.Toggled += (pressed) => OnTabToggled(tabBtn, pressed);
			}
		}

		// Discover pre-placed pages by key (node name == TabKey) and hide all
		BuildPagesFromZones();

		var debugChildren = GetChildren().OfType<TabButton>().Select(t => t.Name).ToArray();

		// Initialize visibility based on the currently active tab (if any)
		var activeTab = GetChildren().OfType<TabButton>().FirstOrDefault(t => t.ButtonPressed);
		if (activeTab != null)
		{
			ShowTabByKey(activeTab.TabKey);
		}
		else
		{
			// If none pressed, hide all
			HideAllPages();
		}
	}

	private void OnTabToggled(TabButton btn, bool pressed)
	{
		if (pressed)
		{
			ShowTabByKey(btn.TabKey);
		}
		else
		{
			// Inactive tabs go back to normal depth
			btn.ZIndex = 0;
		}
	}

	private void OnTabAnimationFinished(TabButton btn, TabButton.TabButtonState finalState)
	{

		switch (finalState)
		{
			case TabButton.TabButtonState.Active:
				btn.ZIndex = 10;
				ShowTabByKey(btn.TabKey);
				break;

			case TabButton.TabButtonState.Hovered:
				break;

			case TabButton.TabButtonState.Inactive:
				btn.ZIndex = 0;
				break;
		}
	}

	private void BuildPagesFromZones()
	{
		// Left pages
		if (_leftZone != null)
		{
			foreach (var child in _leftZone.GetChildren())
			{
				if (child is Control n)
				{
					n.Visible = false;
					_leftPages[n.Name] = n;
				}
			}
		}

		// Right pages
		if (_rightZone != null)
		{
			foreach (var child in _rightZone.GetChildren())
			{
				if (child is Control n)
				{
					n.Visible = false;
					_rightPages[n.Name] = n;
				}
			}
		}
	}

	private void HideAllPages()
	{
		foreach (var n in _leftPages.Values)
		{
			n?.Visible = false;
		}
		foreach (var n in _rightPages.Values)
		{
			n?.Visible = false;
		}
	}

	private void ShowTabByKey(string key)
	{

		if (string.IsNullOrEmpty(key))
		{
			HideAllPages();
			return;
		}

		HideAllPages();

		if (_leftPages.TryGetValue(key, out var left) && left != null)
		{
			left.Visible = true;
		}
		if (_rightPages.TryGetValue(key, out var right) && right != null)
		{
			right.Visible = true;
		}
	}
}
