using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;
using Godot;
using System.Collections.Generic;

namespace FarmDefenseHarvestWars.GameClient.Core.StateMachine;

public enum UnitStateEnum
{
    Idle,
    Moving,
    Attacking,
    Dying
}

public partial class UnitStateMachine : Node
{
    private readonly Dictionary<UnitStateEnum, IState> _states = [];

    private IState _currentState = null!;
    private UnitStateEnum _currentStateEnum;

    private bool _isActive = false;

    [Export]
    public int SyncedStateIndex
    {
        get => (int)_currentStateEnum;
        set
        {
            var newState = (UnitStateEnum)value;

            if (_currentStateEnum == newState && _currentState != null)
                return;

            _currentStateEnum = newState;

            if (_isActive)
            {
                TransitionTo(_currentStateEnum);
            }
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
        _isActive = true;

        TransitionTo(startStateId);

        SetProcess(true);
        SetPhysicsProcess(true);
    }

    public override void _Process(double delta)
    {
        // Rulează pe toate instanțele (Server + Clienți).
        // Aici se execută vizualurile, interfața, animațiile.
        _currentState?.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        // Rulează STRICT pe server (Autoritate).
        // Aici se calculează viteza, coliziunile, raycast-urile.
        if (IsMultiplayerAuthority())
        {
            _currentState?.PhysicsUpdate(delta);
        }
    }

    private void TransitionTo(UnitStateEnum stateId)
    {
        if (!_states.TryGetValue(stateId, out IState newStateInstance))
        {
            GD.PrintErr($"[UnitStateMachine] State {stateId} not registered in {Name}!");
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
            // Modificarea declanșează automat setter-ul și trimite valoarea prin rețea.
            SyncedStateIndex = (int)newStateId;
        }
        else
        {
            GD.PushWarning($"[UnitStateMachine] Client attempted to change state to {newStateId} directly.");
        }
    }
}