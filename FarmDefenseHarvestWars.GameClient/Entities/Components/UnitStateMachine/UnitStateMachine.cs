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
    [Signal] public delegate void StateChangedEventHandler(int previousState, int newState);

    private readonly Dictionary<UnitStateEnum, IState> _states = [];

    private IState? _currentState;
    private UnitStateEnum _currentStateEnum;

    private bool _isActive = false;

    public UnitStateEnum CurrentState => _currentStateEnum;

    [Export]
    public int SyncedStateIndex
    {
        get => (int)_currentStateEnum;
        set
        {
            var newState = (UnitStateEnum)value;

            if (_currentStateEnum == newState && _currentState != null)
                return;

            if (_isActive)
            {
                TransitionTo(newState);
                return;
            }

            _currentStateEnum = newState;
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
        if (!_states.TryGetValue(stateId, out IState? newStateInstance) || newStateInstance == null)
        {
            GD.PrintErr($"[UnitStateMachine] State {stateId} not registered in {Name}!");
            return;
        }

        if (_currentStateEnum == stateId && _currentState != null)
            return;

        var previousState = _currentStateEnum;

        _currentState?.Exit();

        _currentStateEnum = stateId;
        _currentState = newStateInstance;

        _currentState.Enter();

        EmitSignal(SignalName.StateChanged, (int)previousState, (int)_currentStateEnum);
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