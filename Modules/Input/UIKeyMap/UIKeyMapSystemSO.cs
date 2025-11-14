using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// Listen to keyboard, mouse and controller, then dispatch input to matching UI buttons
/// </summary>
[CreateAssetMenu(fileName = "UIKeyMapSystem_SO", menuName = "Game Module/Input/UI Key Map")]
public class UIKeyMapSystemSO : GameSystem, IRegistrySO
{
    public enum InputDeviceType {KEYBOARD_MOUSE,GAMEPAD}
    public enum UIInputKey
    {
        CHOICE_1, CHOICE_2, CHOICE_3, CHOICE_4, MENU, UP, DOWN, LEFT, RIGHT
    }
    [Serializable]
    private struct ActionKeyPair
    {
        [Dropdown(nameof(InputKeys))] public string inputAction;
        public UIInputKey key;
    }
    [Serializable]
    private struct KeyIcon
    {
        public UIInputKey key;
        public InputDeviceType deviceType;
        public Sprite icon;
    }
    
    [SerializeField] private InputActionAsset inputAsset;
    
    [SerializeField, DataTable, ShowField(nameof(IsInputAsset))] private ActionKeyPair[] inputs;
    [SerializeField, DataTable, ShowField(nameof(IsInputAsset))] private KeyIcon[] icons;
    
    [RuntimeObject] private Dictionary<UIInputKey,List<ButtonKeyMapper>> _listeners = new();
    [RuntimeObject] private List<(InputAction,Action<InputAction.CallbackContext>)> _actionCache = new();
    [RuntimeObject] private InputDeviceType _inputDevice = InputDeviceType.KEYBOARD_MOUSE;
    [RuntimeObject] private Dictionary<UIInputKey, Dictionary<InputDeviceType,Sprite>> _iconMap = new();
    private bool IsInputAsset => inputAsset != null;
    private string[] InputKeys => inputAsset.Select(x=>x.actionMap.name + "/" + x.name).ToArray();
    
    public override void OnStart()
    {
        inputs.ForEach(actionKeyPair =>
        {
            var i = inputAsset.FindAction(actionKeyPair.inputAction);
            Action<InputAction.CallbackContext> a = context => Raise(context, actionKeyPair.key);
            i.started += a;
            _actionCache.Add((i, a));
        });
        
        icons.ForEach(keyIcon => _iconMap.GetOrCreateDefault(keyIcon.key)
            .Add(keyIcon.deviceType, keyIcon.icon));
    }
    
    public override void OnDestroy()
    {
        _actionCache.ForEach(x=>x.Item1.started-=x.Item2);
    }

    public void Raise(InputAction.CallbackContext context, UIInputKey key)
    {
        CheckInputDevice(context);
        _listeners.Get(key).Do(listeners =>
        {
            listeners.RemoveWhere(l =>
            {
                if (!l) return true;
                l.Raise();
                return false;
            });
        });
    }

    private void CheckInputDevice(InputAction.CallbackContext context) {

       
        if ( _inputDevice!=InputDeviceType.GAMEPAD && (context.control.device is Gamepad))
        {
            _inputDevice = InputDeviceType.GAMEPAD;
            Cursor.visible = false;
            UpdateIcon();
            DebugUtil.DebugLog("Switch to Gamepad",this.name);
        } else if (_inputDevice != InputDeviceType.KEYBOARD_MOUSE && (context.control.device is Keyboard || context.control.device is Mouse))
        {
            _inputDevice = InputDeviceType.KEYBOARD_MOUSE;
            Cursor.visible = true;
            UpdateIcon();
            DebugUtil.DebugLog("Switch to Keyboard/Mouse",this.name);
        }
    }

    private void UpdateIcon()
    {
        _listeners.ForEach((key, listeners) =>
        {
            var opt = _iconMap.Get(key);
            if(opt.IsEmpty) return;
            var icon = opt.Get().Get(_inputDevice).OrElse(null);
    
            listeners.RemoveWhere(l =>
            {
                if (!l) return true;
                l.SetIcon(icon);
                return false;
            });
        });
    }
    
    public void Register(Transform transform)
    {
        ButtonKeyMapper bkm = transform.GetComponent<ButtonKeyMapper>();
        if (bkm && bkm.mappedKeys != null)
        {
            foreach (var key in bkm.mappedKeys)
            {
                _listeners.GetOrCreateDefault(key).Add(bkm);
                var opt = _iconMap.Get(key);
                if(opt.IsEmpty) continue;
                var icon = opt.Get().Get(_inputDevice).OrElse(null);
                bkm.SetIcon(icon);
            }
        }
    }

    public void Unregister(Transform transform)
    {
        ButtonKeyMapper bkm = transform.GetComponent<ButtonKeyMapper>();
        if (bkm && bkm.mappedKeys != null)
        {
            foreach (var key in bkm.mappedKeys)
            {
                _listeners.Get(key).Do(list => list.Remove(bkm));
            }
        }
    }
}
