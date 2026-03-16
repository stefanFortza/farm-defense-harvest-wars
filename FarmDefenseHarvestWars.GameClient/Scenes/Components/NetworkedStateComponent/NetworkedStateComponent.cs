using Godot;
using System;

[GlobalClass]
public partial class NetworkedStateComponent : Node
{
	private int _currentStateId;

	public event Action<int>? OnStateEntered;
	public event Action<int>? OnStateExited;

	[Export]
	public int CurrentStateId
	{
		get => _currentStateId;
		set
		{
			if (_currentStateId == value) return;

			// Când valoarea se schimbă (fie local de către server, fie via rețea pe client),
			// declanșăm evenimentele pentru cei care ascultă (ex: PlayerController).
			OnStateExited?.Invoke(_currentStateId);
			_currentStateId = value;
			OnStateEntered?.Invoke(_currentStateId);
		}
	}

	// Metodă helper de siguranță: doar serverul are voie să schimbe starea
	public void ChangeState(int newStateId)
	{
		if (Multiplayer.IsServer())
		{
			CurrentStateId = newStateId;
		}
		else
		{
			GD.PushWarning("Client attempted to change authoritative state directly!");
		}
	}
}