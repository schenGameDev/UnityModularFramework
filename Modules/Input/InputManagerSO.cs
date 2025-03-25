using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName ="InputManager_SO",menuName ="Game Module/Input")]
public class InputManagerSO : GameModule, PlayerActions.IInputActions, ILive {

    public EventChannel ActionInteract,OpenEyeEvent,ActionUse;
    public EventChannel ActionLock;
    public EventChannel<bool> ActionSwitchLockTarget;

    public EventChannel<Vector3> MoveDirectionChannel;
    public EventChannel<Vector3> ViewDirectionChannel;

    public Vector2 LookDeltaMovement {get; private set;}
    public Vector2 PointerCameraPosition {get; private set;}

    public bool IsController {get; private set;}

    public Vector3 PlayerWorldPosition {get; set;}
    private Vector2 PlayerCameraPosition => Camera.main.WorldToScreenPoint(PlayerWorldPosition);
    public CameraAngle CameraMode {get; private set;}
    public bool Live { get => _live; set => _live = Live; }

    [SerializeField] private bool _live = true;


    private PlayerActions _input;

    public InputManagerSO() {
        Keywords = new[] {"CAMERA_MODE"};
        updateMode = UpdateMode.NONE;
    }

    public override void OnAwake(Dictionary<string, string> flags, Dictionary<string,GameObject> references)
    {
        base.OnAwake(flags, references);
        CameraMode = EnumExtension.GetEnumValue<CameraAngle>(flags["CAMERA_MODE"]);
    }

    protected override void Reset()
    {
        base.Reset();
        LookDeltaMovement = Vector2.zero;
        PointerCameraPosition = Vector2.zero;
        PlayerWorldPosition = Vector3.zero;
        IsController = false;
    }

    private void OnEnable() {
        if(!_live) return;

        if(_input == null) {
            _input = new PlayerActions();
            _input.Input.SetCallbacks(this); // action map name

            _input.Input.Enable();
        }
    }

    private void OnDisable() {
        _input?.Input.Disable();
    }

    private void CheckInputDevice(InputAction.CallbackContext context) {

        if (!IsController && (context.control.device is Gamepad)) {
            IsController = true;
            Cursor.visible = false;
            DebugUtil.DebugLog("Switch to Gamepad",this.name);

        } else if (IsController && (context.control.device is Keyboard || context.control.device is Mouse)) {
            IsController = false;
            Cursor.visible = true;
            DebugUtil.DebugLog("Switch to Keyboard/Mouse",this.name);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if(!_live) return;
        CheckInputDevice(context);

        var moveDirection = CameraToWorldSpace(context.ReadValue<Vector2>());
        MoveDirectionChannel?.Raise(moveDirection);
        if(this.CameraMode == CameraAngle.FOLLOW) {
            ViewDirectionChannel.Raise(moveDirection);
        }
    }

    public void OnOpenCloseEye(InputAction.CallbackContext context)
    {
        if(!_live) return;
        CheckInputDevice(context);

        if(context.started) OpenEyeEvent.Raise();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if(!_live) return;
        CheckInputDevice(context);

        if(context.started) ActionInteract.Raise();
    }

    public void OnUse(InputAction.CallbackContext context)
    {
        if(!_live) return;
        CheckInputDevice(context);

        if(context.started) ActionUse.Raise();
    }

    public void OnViewDirection(InputAction.CallbackContext context)
    {
        if(!_live) return;

        if(CameraMode == CameraAngle.FOLLOW) return;

        CheckInputDevice(context);

        var readValue = context.ReadValue<Vector2>();
        Vector3 viewDirection;
        if(IsController && math.any(readValue)) {
            viewDirection = CameraToWorldSpace(readValue);
        } else {
            PointerCameraPosition = readValue;
            viewDirection = CameraToWorldSpace(PointerCameraPosition - PlayerCameraPosition);
        }
        ViewDirectionChannel?.Raise(viewDirection);
    }

    private Vector3 CameraToWorldSpace(Vector2 cameraVector) {
        var dir = CameraMode switch
        {
            CameraAngle.SIDE => new Vector3(cameraVector.x, 0, 0),
            _ => new Vector3(cameraVector.x, 0, cameraVector.y),
        };
        return dir.normalized;
    }
}

public enum CameraAngle {
    TOP,FOLLOW,SIDE
}