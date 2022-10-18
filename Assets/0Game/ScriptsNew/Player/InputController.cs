using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class InputController : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static bool fetchInput = true;

    private Player _player;
    private NetworkInputData _frameworkInput;
    private Vector3 _moveDelta;

    private void Update()
    {
        //i do it custom since i want to be able to rebind movement buttons
        //and inputaxis cannot be changed in runtime
        _moveDelta = Vector3.zero;

        if (InputManager.Instance == null) return;

        if (InputManager.Instance.GetKey(KeyBindingActions.Up)) _moveDelta.z = 1;
        if (InputManager.Instance.GetKey(KeyBindingActions.Down)) _moveDelta.z = -1;
        if (InputManager.Instance.GetKey(KeyBindingActions.Left)) _moveDelta.x = -1;
        if (InputManager.Instance.GetKey(KeyBindingActions.Right)) _moveDelta.x = 1;

        _moveDelta.y = 0;
        //_moveDelta = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
    }

    public override void Spawned()
    {
        //_mobileInput = FindObjectOfType<MobileInput>(true);
        _player = GetComponent<Player>();
        // Technically, it does not really matter which InputController fills the input structure, since the actual data will only be sent to the one that does have authority,
        // but in the name of clarity, let's make sure we give input control to the gameobject that also has Input authority.
        if (Object.HasInputAuthority)
        {
            Runner.AddCallbacks(this);
        }

        Debug.Log("Spawned [" + this + "] IsClient=" + Runner.IsClient + " IsServer=" + Runner.IsServer + " HasInputAuth=" + Object.HasInputAuthority + " HasStateAuth=" + Object.HasStateAuthority);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (_player!=null && _player.Object!=null && (_player.State == Player.PlayerState.Active || _player.WaitForInput) && fetchInput)
        {
            if (_moveDelta != Vector3.zero)
            {
                _player._timeSinceInput = 0;

                float targetAngle = Mathf.Atan2(_moveDelta.x, _moveDelta.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                _frameworkInput.Direction = moveDir;
                _frameworkInput.TargetAngle = targetAngle;
            }
            else
            {
                _frameworkInput.Direction = _moveDelta;
            }

            if (InputManager.Instance != null)
            {
                _frameworkInput.buttons.Set(NetworkInputData.Buttons.Jump, _player.WaitForInput ? Input.anyKey && !Input.GetKey(KeyCode.LeftWindows) && !Input.GetKey(KeyCode.RightWindows) && !Input.GetKey(KeyCode.LeftApple) && !Input.GetKey(KeyCode.RightApple) : InputManager.Instance.GetKey(KeyBindingActions.Jump) /*Input.GetKey(KeyCode.Space)*/);
                _frameworkInput.buttons.Set(NetworkInputData.Buttons.Sprint, InputManager.Instance.GetKey(KeyBindingActions.Sprint) /*Input.GetKey(KeyCode.LeftShift)*/);
            }            
        }

        // Hand over the data to Fusion
        input.Set(_frameworkInput);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnDisconnectedFromServer(NetworkRunner runner) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }

    public void OnSceneLoadDone(NetworkRunner runner) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }
}
