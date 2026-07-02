using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    public class AnimationNode : ActionNode
    {
        public AnimationConfig animConfig;
        [ShowField(nameof(IsAnimFlag))] public bool waitForAnimComplete;
        private bool IsAnimFlag =>  animConfig.type == AnimationConfigType.FLAG;
        
        private float _time;
        private float _timer;
        private bool _animComplete;
        private BTAnimation _animation;

        public override void Prepare()
        {
            base.Prepare();
            _animation = GetComponentInMe<BTAnimation>();
            _time = animConfig.type == AnimationConfigType.WAIT ? float.Parse(animConfig.waitTime) : 0f;
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            _animComplete = false;
            if (animConfig.type == AnimationConfigType.NONE) return;
            if (animConfig.type == AnimationConfigType.FLAG)
            {
                if (waitForAnimComplete)
                {
                    _animation.SetFlag(animConfig, () => _animComplete=true);
                }
                else
                {
                    _animation.SetFlag(animConfig, null);
                    _animComplete = true;
                }
            
            }
            else if (_time > 0f)
            {
                _timer = Time.time;
            }
        }

        protected override State OnUpdate()
        {
            if (_time > 0f) _animComplete = _timer + _time < Time.time;
            return _animComplete ? State.Success : State.Running;
        }
        
        protected override void OnExit()
        {
            base.OnExit();
            if (animConfig.type == AnimationConfigType.FLAG)
            {
                _animation.ReverseFlag(animConfig, !_animComplete);
            }
        }

        public override BTNode Clone()
        {
            AnimationNode node = Instantiate(this);
            return node;
        }

        public override string ToString()
        {
            return base.ToString() + (animConfig.type == AnimationConfigType.NONE ? "" : " (" + animConfig.type + ")");
        }

        AnimationNode()
        {
            description = "Stand still and play animation";
        }
    }
}