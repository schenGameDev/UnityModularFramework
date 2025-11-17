using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using Unity.Mathematics;
using UnityEngine;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName ="InputSystem_SO",menuName ="Game Module/Input/Input System")]
public class InputSystemSO : GameSystem<InputSystemSO>,ILive {
    [RuntimeObject] public static InputDeviceType InputDevice { get; private set; } = InputDeviceType.KEYBOARD_MOUSE;
    
    public enum ActionType
    {
        PRESS,HOLD,VECTOR2
    }
    
    public const string NONE_ACTION = "None";
    
    [Serializable]
    private class ActionChannel
    {
        [Dropdown(nameof(InputKeys))] public string input;
        public ActionType type;
        [ShowField(nameof(type), ActionType.PRESS)] public EventChannel channel;
        [ShowField(nameof(type), ActionType.HOLD),Rename("channel")] public EventChannel<bool> boolChannel;
        [ShowField(nameof(type), ActionType.VECTOR2),Rename("channel")] public EventChannel<Vector2> vector2Channel;
    }
    
    
    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField, ShowField(nameof(IsInputAsset))] private ActionChannel[] inputs;
    [SerializeField, ShowField(nameof(IsInputAsset)),Dropdown(nameof(InputKeys)),Rename("Move")] 
    private string moveInput;
    public EventChannel<Vector3> moveDirectionChannel;
    [SerializeField, ShowField(nameof(IsInputAsset)),Dropdown(nameof(InputKeys)),Rename("View")] 
    private string viewInput;
    public EventChannel<Vector3> viewDirectionChannel;
    private bool IsInputAsset => inputAsset != null;
    private string[] InputKeys => GetInputActions(inputAsset);
    private Vector2 PlayerCameraPosition => Camera.main.WorldToScreenPoint( _player.transform.position);
    public CameraAngle CameraMode {get; set;}
    
    [field: SerializeField,ReadOnly,RuntimeObject] public bool Live { get; set; }
    
    [RuntimeObject] public Vector2 LookDeltaMovement {get; private set;}
    [RuntimeObject] public Vector2 PointerCameraPosition {get; private set;}
    [RuntimeObject] private List<(InputAction,ActionType,Action<InputAction.CallbackContext>)> _actionCache = new();
    [SceneRef("PLAYER")] private Transform _player;

    protected override void OnAwake() { }

    protected override void OnStart()
    {
        foreach (var actionChannel in inputs)
        {
            if (actionChannel.input==NONE_ACTION)
            {
                continue;
            }
            if (!actionChannel.channel && !actionChannel.boolChannel && !actionChannel.vector2Channel)
            {
                Debug.LogError($"No channel in action {actionChannel.input}");
                continue;
            }
            
            var i = inputAsset.FindAction(actionChannel.input);

            if (i == null)
            {
                Debug.LogError($"Can't find action {actionChannel.input}");
                continue;
            }
            switch (actionChannel.type)
            {
                case ActionType.PRESS or ActionType.HOLD when 
                    i.type != InputActionType.Button:
                    Debug.LogError($"Input action {actionChannel.input} is not a button");
                    continue;
                case ActionType.VECTOR2 when i.type != InputActionType.Value:
                    Debug.LogError($"Input action {actionChannel.input} is not value");
                    continue;
            }

            Action<InputAction.CallbackContext> a = context => Raise(context, actionChannel);
            switch (actionChannel.type)
            {
                case ActionType.PRESS:
                    i.started += a;
                    break;
                case ActionType.HOLD:
                    i.started += a;
                    i.canceled += a;
                    break;
                case ActionType.VECTOR2:
                    i.performed += a;
                    break;    
            }
            _actionCache.Add((i, actionChannel.type, a));
        }

        if (moveInput!=NONE_ACTION)
        {
            inputAsset.FindAction(moveInput).performed += OnMove;
        }
        if (viewInput!=NONE_ACTION)
        {
            inputAsset.FindAction(viewInput).performed += OnViewDirection;
        }
    }
    
    protected override void OnDestroy()
    {
        _actionCache.ForEach(x=>
        {
            switch (x.Item2)
            {
                case ActionType.PRESS:
                    x.Item1.started -= x.Item3;
                    break;
                case ActionType.HOLD:
                    x.Item1.started -= x.Item3;
                    x.Item1.canceled -= x.Item3;
                    break;
                case ActionType.VECTOR2:
                    x.Item1.performed -= x.Item3;
                    break;    
            }
        });
        if (moveInput!=NONE_ACTION)
        {
            inputAsset.FindAction(moveInput).performed -= OnMove;
        }
        if (viewInput!=NONE_ACTION)
        {
            inputAsset.FindAction(viewInput).performed -= OnViewDirection;
        }
    }
    
    private void Raise(InputAction.CallbackContext context, ActionChannel actionChannel)
    {
        if (!Live)
        {
            return;
        }
        
        CheckInputDevice(context);
        
        switch (actionChannel.type)
        {
            case ActionType.PRESS:
                actionChannel.channel?.Raise();
                break;
            case ActionType.HOLD:
                if(context.started) actionChannel.boolChannel?.Raise(true);
                if(context.canceled) actionChannel.boolChannel?.Raise(false);
                break;
            case ActionType.VECTOR2:
                actionChannel.vector2Channel?.Raise(context.ReadValue<Vector2>());
                break;    
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if(!Live) return;
        CheckInputDevice(context);

        var moveDirection = CameraToWorldSpace(context.ReadValue<Vector2>());
        moveDirectionChannel?.Raise(moveDirection);
        if(this.CameraMode == CameraAngle.FOLLOW) {
            viewDirectionChannel.Raise(moveDirection);
        }
    }
    
    public void OnViewDirection(InputAction.CallbackContext context)
    {
        if(!Live) return;

        if(CameraMode == CameraAngle.FOLLOW) return;

        CheckInputDevice(context);

        var readValue = context.ReadValue<Vector2>();
        Vector3 viewDirection;
        if(InputDevice==InputDeviceType.GAMEPAD && math.any(readValue)) {
            viewDirection = CameraToWorldSpace(readValue);
        } else {
            PointerCameraPosition = readValue;
            viewDirection = CameraToWorldSpace(PointerCameraPosition - PlayerCameraPosition);
        }
        viewDirectionChannel.Raise(viewDirection);
    }

    private Vector3 CameraToWorldSpace(Vector2 cameraVector) {
        var dir = CameraMode switch
        {
            CameraAngle.SIDE => new Vector3(cameraVector.x, 0, 0),
            _ => new Vector3(cameraVector.x, 0, cameraVector.y),
        };
        return dir.normalized;
    }
    
    public static bool CheckInputDevice(InputAction.CallbackContext context) {
        if (InputDevice!=InputDeviceType.GAMEPAD && (context.control.device is Gamepad))
        {
            InputDevice = InputDeviceType.GAMEPAD;
            Cursor.visible = false;
            DebugUtil.DebugLog("Switch to Gamepad");
            return true;
        } 
        if (InputDevice != InputDeviceType.KEYBOARD_MOUSE && (context.control.device is Keyboard || context.control.device is Mouse))
        {
            InputDevice = InputDeviceType.KEYBOARD_MOUSE;
            Cursor.visible = true;
            DebugUtil.DebugLog("Switch to Keyboard/Mouse");
            return true;
        }

        return false;
    }

    public static string[] GetInputActions(InputActionAsset inputAsset)
    {
        List<string> actions = new List<string> {NONE_ACTION};
        inputAsset.Select(x=>x.actionMap.name + "/" + x.name).ForEach(x=>actions.Add(x));
        return actions.ToArray();
    }
}
public enum InputDeviceType {KEYBOARD_MOUSE,GAMEPAD}
public enum CameraAngle {
    TOP,FOLLOW,SIDE
}