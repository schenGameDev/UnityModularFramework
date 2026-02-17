using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class InputSystemSO
{
    public enum InputDeviceType {KEYBOARD_MOUSE,GAMEPAD}

    [Flags]
    private enum ActionTiming : byte
    {
        NONE = 0,
        STARTED = 1 << 1,
        PERFORMED = 1 << 2,
        CANCELED = 1 << 3
    }

    public const string NONE_ACTION = "None";

    [Serializable]
    private class ActionChannel
    {
        [Dropdown(nameof(InputKeys))] 
        public string input;
        public ActionTiming timing;
        [EditorAttributes.TypeFilter(typeof(EventChannel<ActionTiming>),typeof(EventChannel<bool>), 
            typeof(EventChannel), typeof(EventChannel<Vector3>), typeof(EventChannel<Vector2>))]
        public ScriptableObject channel;
        public string label;

        public ActionChannel(ActionTiming timing, string label = null)
        {            
            this.label = label;
            this.timing = timing;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(label)? input : label;
        }
    }
    
    private bool IsInputAsset => inputAsset != null;
    private string[] InputKeys => GetInputActions(inputAsset);
    
    public static string[] GetInputActions(InputActionAsset inputAsset)
    {
        var actions = new List<string> {NONE_ACTION};
        inputAsset.Select(x=>x.actionMap.name + "/" + x.name).ForEach(x=>actions.Add(x));
        return actions.ToArray();
    }

    private static bool ValidateActionChannel(ActionChannel actionChannel, InputAction inputAction)
    {
        var channelType = actionChannel.GetType();

        if (inputAction.type == InputActionType.Button)
        {
            if (channelType == typeof(EventChannel<Vector3>) || channelType == typeof(EventChannel<Vector2>))
            {
                Debug.LogError($"Input {actionChannel.input} does not have Vector output");
                return false;
            }
        } else if (inputAction.type == InputActionType.Value)
        {
            if (channelType == typeof(EventChannel<ActionTiming>) ||
                channelType == typeof(EventChannel<bool>) ||
                channelType == typeof(EventChannel))
            {
                Debug.LogError($"Input {actionChannel.input} can not have Button output");
                return false;
            }

            if (inputAction.expectedControlType != "Vector2")
            {
                Debug.LogError($"Input {actionChannel.input} does not have Vector output");
                return false;
            }
        }
        return true;
    }
}