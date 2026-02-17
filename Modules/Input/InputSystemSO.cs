using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EditorAttributes;
using ModularFramework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName ="InputSystem_SO",menuName ="Game Module/Input/Input System")]
public partial class InputSystemSO : GameSystem<InputSystemSO>,ILive {
    [RuntimeObject] public static InputDeviceType InputDevice { get; private set; } = InputDeviceType.KEYBOARD_MOUSE;
    
    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField, ShowField(nameof(IsInputAsset))] 
    private ActionChannel[] inputs =
    {
        new (ActionTiming.PERFORMED | ActionTiming.CANCELED, "Move Vector2"),
        new (ActionTiming.PERFORMED, "View Vector2"),
        new (ActionTiming.STARTED, "Press Button"),
        new (ActionTiming.STARTED | ActionTiming.CANCELED, "Hold Button")
    };

    private Vector2 PlayerCameraPosition => Camera.main.WorldToScreenPoint( _player.transform.position);
    public CameraAngle CameraMode {get; set;}
    
    [field: SerializeField,RuntimeObject] public bool Live { get; set; }
    [RuntimeObject] public Vector2 PointerCameraPosition {get; private set;}
    [RuntimeObject] private List<(InputAction,Action<InputAction.CallbackContext>)> _actionCache = new();
    [SceneRef("PLAYER")] private Transform _player;

    protected override void OnAwake()
    {
        inputAsset.Enable();
    }

    protected override void OnStart()
    {
        foreach (var actionChannel in inputs)
        {
            if (actionChannel.input==NONE_ACTION)
            {
                continue;
            }
            if (!actionChannel.channel)
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

            if (!ValidateActionChannel(actionChannel, i))
            {
                continue;
            }

            Action<InputAction.CallbackContext> a = context => Raise(context, actionChannel);
            if(IsActiveTiming(actionChannel.timing, ActionTiming.STARTED))
            {
                i.started += a;
            }
            if(IsActiveTiming(actionChannel.timing, ActionTiming.PERFORMED))
            {
                i.performed += a;
            }

            if (IsActiveTiming(actionChannel.timing, ActionTiming.CANCELED))
            {
                i.canceled += a;
            }
            
            _actionCache.Add((i, a));
        }
    }
    
    protected override void OnSceneDestroy()
    {
        _actionCache.ForEach(x=>
        {
            x.Item1.started -= x.Item2;
            x.Item1.performed -= x.Item2; 
            x.Item1.canceled -= x.Item2;
        });
    }
    
    private void Raise(InputAction.CallbackContext context, ActionChannel actionChannel)
    {
        if (!Live)
        {
            return;
        }

        if (CheckInputDevice(context))
        {
            Debug.Log($"Switch to {InputDevice}");
        }
        
        switch (actionChannel.channel)
        {
            case EventChannel<ActionTiming> ch1:
                ch1.Raise(GetActionTiming(context));
                break;
            case EventChannel<bool> ch2:
                ch2.Raise(context.started || context.performed);
                break;
            case EventChannel ch3:
                ch3.Raise();
                break;
            case EventChannel<Vector3> ch4:
                ch4.Raise(context.canceled? Vector2.zero : context.ReadValue<Vector2>());
                break;  
            case EventChannel<Vector2> ch5:
                ch5.Raise(context.canceled? Vector2.zero : context.ReadValue<Vector2>());
                break;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Scroll(Vector2 scrollVector)
    {
        return scrollVector.y switch
        {
            0 => 0,
            > 0 => 1,
            _ => -1
        };
    }
    
    public Vector3 GetViewWorldDirection(Vector2 cameraVector)
    {
        Vector3 viewDirection;
        if(InputDevice==InputDeviceType.GAMEPAD && math.any(cameraVector)) {
            viewDirection = CameraDirectionToWorldSpace(cameraVector);
        } else {
            PointerCameraPosition = cameraVector;
            viewDirection = CameraDirectionToWorldSpace(PointerCameraPosition - PlayerCameraPosition);
        }
        return WithinViewDeadZone(viewDirection)? Vector3.zero : viewDirection.normalized;
    }
    
    private const float MOUSE_DEAD_ZONE = 1f; // 0 ~ infinity
    private const float GAMEPAD_DEAD_ZONE = 0.2f; // 0 ~ 1
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool WithinViewDeadZone(Vector3 dir) {
        if (InputDevice == InputDeviceType.KEYBOARD_MOUSE)
            return Mathf.Abs(dir.x) < MOUSE_DEAD_ZONE &&
                   Mathf.Abs(dir.z) < MOUSE_DEAD_ZONE;
        // gamepad
        return Mathf.Abs(dir.x) < GAMEPAD_DEAD_ZONE &&
               Mathf.Abs(dir.z) < GAMEPAD_DEAD_ZONE;
    }
    /// <summary>
    /// won't normalize the vector
    /// </summary>
    /// <param name="cameraVector"></param>
    /// <returns></returns>
    public Vector3 CameraDirectionToWorldSpace(Vector2 cameraVector) {
        var dir = CameraMode switch
        {
            CameraAngle.SIDE => new Vector3(cameraVector.x, 0, 0),
            _ => new Vector3(cameraVector.x, 0, cameraVector.y),
        };
        return dir;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsActiveTiming(ActionTiming mask, ActionTiming currentTiming) 
        => (mask & currentTiming) != ActionTiming.NONE;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ActionTiming GetActionTiming(InputAction.CallbackContext context)
    {
        if (context.started) return ActionTiming.STARTED;
        if (context.performed) return ActionTiming.PERFORMED;
        return context.canceled ? ActionTiming.CANCELED : ActionTiming.NONE;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckInputDevice(InputAction.CallbackContext context) {
        if (InputDevice!=InputDeviceType.GAMEPAD && context.control.device is Gamepad)
        {
            InputDevice = InputDeviceType.GAMEPAD;
            Cursor.visible = false;
            return true;
        } 
        if (InputDevice != InputDeviceType.KEYBOARD_MOUSE && context.control.device is Keyboard or Mouse)
        {
            InputDevice = InputDeviceType.KEYBOARD_MOUSE;
            Cursor.visible = true;
            return true;
        }
        return false;
    }
}

public enum CameraAngle {
    TOP,FOLLOW,SIDE
}