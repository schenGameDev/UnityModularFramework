using System;
using EditorAttributes;
using UnityEngine;
using Void = EditorAttributes.Void;

namespace ModularFramework.Modules.BehaviorTree
{
    [Serializable]
    public class AnimationConfig
    {
        [SerializeField, HorizontalGroup(nameof(type), nameof(waitTime))]
        private Void groupHolder;
        
        [HideLabel,HideProperty] public AnimationConfigType type;

        [HideLabel,HideProperty, ShowField(nameof(type), AnimationConfigType.WAIT)]
        public string waitTime;

        [ShowField(nameof(type), AnimationConfigType.FLAG), DataTable(showLabels: false)]
        public AnimationFlagConfig[] flags;
    }

    [Serializable]
    public class AnimationFlagConfig
    {
        public AnimationFlagType flagType;
        public string flag;
        public string value;
        
        [HideLabel, SerializeField]
        private ReverseType reverse;
        
        public bool reverseAtEnd => (reverse & ReverseType.REVERSE_END) != 0;
        public bool reverseAtInterrupt => (reverse & ReverseType.REVERSE_INTERRUPT) != 0;
    }

    public enum AnimationConfigType
    {
        NONE,
        WAIT,
        FLAG
    }

    public enum AnimationFlagType
    {
        BOOL,
        INT
    }
    [Flags]
    public enum ReverseType : byte
    {
        NONE = 0,
        REVERSE_END = 1 << 0,
        REVERSE_INTERRUPT = 1 << 1,
    }
}