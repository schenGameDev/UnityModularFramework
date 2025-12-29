using System;
using System.Collections.Generic;
using EditorAttributes;
using ModularFramework;
using UnityEngine;
using UnityEngine.InputSystem;
using static InputSystemSO;

/// <summary>
/// Listen to keyboard, mouse and controller, then dispatch input to matching UI buttons
/// </summary>
[CreateAssetMenu(fileName = "UIKeyMapSystem_SO", menuName = "Game Module/Input/UI Key Map")]
public class UIKeyMapSystemSO : GameSystem<UIKeyMapSystemSO>, IRegistrySO, ILive
{
    public enum UIInputKey
    {
        CHOICE_1, CHOICE_2, CHOICE_3, CHOICE_4, MENU, UP, DOWN, LEFT, RIGHT
    }
    [Serializable]
    private struct ActionKeyPair
    {
        [Dropdown(nameof(InputKeys)),Rename("input")] public string inputAction;
        public UIInputKey key;
    }
    [Serializable]
    private struct KeyIcon
    {
        public UIInputKey key;
        public InputDeviceType device;
        public Sprite icon;
    }
    
    [SerializeField] private InputActionAsset inputAsset;
    
    [SerializeField, DataTable, ShowField(nameof(IsInputAsset))] private ActionKeyPair[] inputs;
    [SerializeField, DataTable, ShowField(nameof(IsInputAsset))] private KeyIcon[] icons;
    
    [field: RuntimeObject] public bool Live { get; set; }
    
    [RuntimeObject] private Dictionary<UIInputKey,List<ButtonKeyMapper>> _listeners = new();
    [RuntimeObject] private List<(InputAction,Action<InputAction.CallbackContext>)> _actionCache = new();
    [RuntimeObject] private Dictionary<UIInputKey, Dictionary<InputDeviceType,Sprite>> _iconMap = new();
    private bool IsInputAsset => inputAsset != null;
    private string[] InputKeys => GetInputActions(inputAsset);

    protected override void OnAwake()
    {
        inputAsset.Enable();
    }
    protected override void OnStart()
    {
        inputs.ForEach(actionKeyPair =>
        {
            if (actionKeyPair.inputAction == NONE_ACTION)
            {
                return;
            }
            var i = inputAsset.FindAction(actionKeyPair.inputAction);
            if (i == null)
            {
                Debug.LogError($"Can't find action {actionKeyPair.inputAction}");
                return;
            }
            Action<InputAction.CallbackContext> a = context => Raise(context, actionKeyPair.key);
            i.started += a;
            _actionCache.Add((i, a));
        });
        
        icons.ForEach(keyIcon => _iconMap.GetOrCreateDefault(keyIcon.key)
            .Add(keyIcon.device, keyIcon.icon));
    }
    
    protected override void OnSceneDestroy()
    {
        _actionCache.ForEach(x=>x.Item1.started-=x.Item2);
    }

    private void Raise(InputAction.CallbackContext context, UIInputKey key)
    {
        if (!Live)
        {
            return;
        }
        if (CheckInputDevice(context))
        {
            UpdateIcon();
        }
        
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
    
    private void UpdateIcon()
    {
        _listeners.ForEach((key, listeners) =>
        {
            var opt = _iconMap.Get(key);
            var icon = opt.IsEmpty? null : opt.Get().Get(InputSystemSO.InputDevice).OrElse(null);
    
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
                var icon = opt.IsEmpty? null : opt.Get().Get(InputSystemSO.InputDevice).OrElse(null);
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
