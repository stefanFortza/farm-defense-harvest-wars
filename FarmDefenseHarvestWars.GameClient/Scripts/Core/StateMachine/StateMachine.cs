using Godot;
using System;
using System.Collections.Generic;

namespace FarmDefenseHarvestWars.GameClient.Core.StateMachine;

public enum UnitStateEnum
{
    Idle,
    Moving,
    Attacking,
    Dying
}

// Definim TState ca fiind obligatoriu un Enum
public partial class StateMachine : Node
{
    private readonly Dictionary<UnitStateEnum, IState> _states = [];

    private IState _currentState = null!;
    private UnitStateEnum _currentStateEnum;

    // Proprietatea expusă pentru MultiplayerSynchronizer.
    // Godot nu poate exporta TState generic, așa că serializăm ca int.
    [Export]
    public int SyncedStateIndex
    {
        get => (int)_currentStateEnum;
        set
        {
            // Conversie eficientă din int în Enum
            var newState = (UnitStateEnum)value;

            // Evităm tranziția dacă starea e deja activă
            if (_currentStateEnum == newState) return;

            // Trigger tranziție (venită de pe server)
            TransitionTo(newState);
        }
    }

    public override void _Ready()
    {
        SetProcess(false);
        SetPhysicsProcess(false);
    }

    public void RegisterState(UnitStateEnum stateId, IState stateInstance)
    {
        _states[stateId] = stateInstance;
    }

    public void Start(UnitStateEnum startStateId)
    {
        _currentStateEnum = startStateId;
        TransitionTo(startStateId);

        SetProcess(true);
        SetPhysicsProcess(true);
    }

    public override void _Process(double delta)
    {
        if (IsMultiplayerAuthority())
        {
            _currentState?.Update(delta);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority())
        {
            _currentState?.PhysicsUpdate(delta);
        }
    }

    private void TransitionTo(UnitStateEnum stateId)
    {
        if (!_states.TryGetValue(stateId, out IState? newStateInstance))
        {
            GD.PrintErr($"[StateMachine] State {stateId} not registered in {Name}!");
            return;
        }

        _currentState?.Exit();

        _currentStateEnum = stateId;
        _currentState = newStateInstance;

        _currentState.Enter();
    }

    public void RequestStateChange(UnitStateEnum newStateId)
    {
        if (Multiplayer.IsServer())
        {
            // Setarea proprietății va declanșa replicarea prin MultiplayerSynchronizer
            // datorită setter-ului care face conversia.
            SyncedStateIndex = (int)newStateId;
        }
    }
}