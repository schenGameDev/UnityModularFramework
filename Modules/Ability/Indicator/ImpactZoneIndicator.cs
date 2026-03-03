using System;
using EditorAttributes;
using ModularFramework.Modules.Targeting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityTimer;

namespace ModularFramework.Modules.Ability
{
    public class ImpactZoneIndicator : MonoBehaviour
    {
        // in Universal Renderer Data, Add Decal, Technique: Screen Space

        private static readonly int COLOR = Shader.PropertyToID("_Color");
        private static readonly int IS_SQUARE = Shader.PropertyToID("_IsSquare");
        private static readonly int ANGLE_OR_WIDTH = Shader.PropertyToID("_AngleOrWidth");
        private static readonly int MIN_RANGE = Shader.PropertyToID("_MinRange");
        private static readonly int MAX_RANGE = Shader.PropertyToID("_MaxRange");
        private static readonly int FACE_DIRECTION = Shader.PropertyToID("_FaceDirection"); // vector2

        [SerializeField] private DecalProjector decalProjector;
        [SerializeField] private bool preset;

        [SerializeField, ShowField(nameof(preset))]
        RangeFilter rangeFilter;

        [SerializeField, ShowField(nameof(preset))]
        Color color;

        private (int, int) _lastCmd = (0, 0);

        private void Awake()
        {
            decalProjector.material = new Material(decalProjector.material);
            if (preset) ShowInWorldCoordinate(transform.position, transform.forward, rangeFilter, color);
        }

        public void Hide()
        {
            if (IsSameCmd((0, 0))) return;
            decalProjector.enabled = false;
            TimerManager.Tick -= Move;
            _isMoving = false;
            _speed = 0f;
        }
        
        public void ShowInLocalCoordinate(Transform parent, Vector3 center, Vector3 forward, RangeFilter filter, Color color)
        {
            transform.parent = parent;
            Show(center, forward, GetSize(filter),filter, color, true, true);
        }
        
        public void ShowInWorldCoordinate(Vector3 center, Vector3 forward, RangeFilter filter, Color color)
        {
            transform.parent = null;
            Show(center, forward,GetSize(filter), filter, color, false, true);
        }
        
        public void ShowInLocalCoordinate(Transform parent, Vector3 center, Vector3 forward, RangeFilter filter, Color color,
            float acceleration, float maxSpeed)
        {
            transform.parent = parent;
            _acceleration = acceleration;
            _maxSpeed = maxSpeed;
            _isLocalCoordinate = true;
            Show(center, forward, GetSize(filter),filter, color, true, false);
        }
        
        public void ShowInWorldCoordinate(Vector3 center, Vector3 forward, RangeFilter filter, Color color,
            float acceleration, float maxSpeed)
        {
            transform.parent = null;
            _acceleration = acceleration;
            _maxSpeed = maxSpeed;
            _isLocalCoordinate = false;
            Show(center, forward, GetSize(filter), filter, color, false, false);
        }

        private void Show(Vector3 center, Vector3 forward, Vector3 size, RangeFilter filter, Color color, bool isLocalCoordinate, bool snapToTarget) // Use local coordinate if this is a child object
        {
            // attack range
            if (IsSameCmd((1, HashCode.Combine(center, forward, filter, color)))) return;
            decalProjector.enabled = true;
            FitSize(size);
            var groundForward = new Vector3(forward.x, 0, forward.z); //z.back is up
            _target = center;
            if (!_isMoving || snapToTarget)
            {
                if (isLocalCoordinate)
                {
                    transform.localPosition = center;
                    transform.localRotation = Quaternion.LookRotation(Vector3.down, groundForward);
                }
                else
                {
                    transform.position = center;
                    transform.rotation = Quaternion.LookRotation(Vector3.down, groundForward);
                }

                if (!snapToTarget)
                {
                    TimerManager.Tick += Move;
                    _isMoving = true;
                }
            }


            decalProjector.material.SetColor(COLOR, color);
            bool isSquare = filter.rangeType is RangeFilter.RangeType.SQUARE or RangeFilter.RangeType.BOX;
            decalProjector.material.SetInt(IS_SQUARE, isSquare ? 1 : 0);
            decalProjector.material.SetFloat(ANGLE_OR_WIDTH, isSquare ? filter.width : filter.viewAngle);
            decalProjector.material.SetFloat(MIN_RANGE, filter.minMaxRange.x);
            decalProjector.material.SetFloat(MAX_RANGE, filter.minMaxRange.y);
            decalProjector.material.SetVector(FACE_DIRECTION, new Vector2(transform.up.x, transform.up.z));
        }
        
        public void ShowInLocalCoordinate(Transform parent, Vector3 center, float radius, Color color, float height = 20f)
        {
            transform.parent = parent;
            Show(center, radius, new Vector3(radius * 2, height, radius * 2), color, true, true);
        }
        
        public void ShowInWorldCoordinate(Vector3 center, float radius, Color color, float height = 20f)
        {
            transform.parent = null;
            Show(center, radius, new Vector3(radius * 2, height, radius * 2), color,false, true);
        }

        public void ShowInLocalCoordinate(Transform parent, Vector3 center,float radius, Color color, float acceleration, 
            float maxSpeed, float height = 20f)
        {
            transform.parent = parent;
            _acceleration = acceleration;
            _maxSpeed = maxSpeed;
            _isLocalCoordinate = true;
            Show(center, radius, new Vector3(radius * 2, height, radius * 2), color, true, false);
        }
        
        public void ShowInWorldCoordinate(Vector3 center, float radius, Color color, float acceleration, float maxSpeed,
            float height = 20f)
        {
            transform.parent = null;
            _acceleration = acceleration;
            _maxSpeed = maxSpeed;
            _isLocalCoordinate = false;
            Show(center, radius, new Vector3(radius * 2, height, radius * 2), color, false, false);
        }
        
        private void Show(Vector3 center, float radius,  Vector3 size, Color color, bool isLocalCoordinate, bool snapToTarget)
        {
            // shadow/ airdrop/ shell explosion range
            if (IsSameCmd((2, HashCode.Combine(center, radius, color)))) return;
            decalProjector.enabled = true;
            FitSize(size);
            _target = center;

            if (!_isMoving || snapToTarget)
            {
                if (isLocalCoordinate)
                {
                    transform.localPosition = center;
                }
                else
                {
                    transform.position = center;
                }

                if (!snapToTarget)
                {
                    TimerManager.Tick += Move;
                    _isMoving = true;
                }
            }

            decalProjector.material.SetColor(COLOR, color);
            decalProjector.material.SetInt(IS_SQUARE, 0);
            decalProjector.material.SetFloat(ANGLE_OR_WIDTH, 360);
            decalProjector.material.SetFloat(MIN_RANGE, 0);
            decalProjector.material.SetFloat(MAX_RANGE, radius);
            decalProjector.material.SetVector(FACE_DIRECTION, Vector2.up);
        }
        
        private bool _isMoving;
        private bool _isLocalCoordinate;
        private float _acceleration = 15f;
        private float _speed;
        private float _maxSpeed = 60f;
        private Vector3 _target;

        private void Move(float deltaTime)
        {
            if ((_isLocalCoordinate && _target == transform.localPosition) || _target == transform.position)
            {
                _speed = 0;
                return;
            }


            var newSpd = _speed + _acceleration * Time.deltaTime;
            if (newSpd > _maxSpeed) newSpd = _maxSpeed;
            var deltaMove = (newSpd + _speed) / 2 * Time.deltaTime;
            if (_isLocalCoordinate)
            {
                if (deltaMove >= Vector3.Distance(transform.localPosition, _target))
                {
                    transform.localPosition = _target;
                }
                else
                {
                    transform.localPosition += (_target - transform.localPosition).normalized * deltaMove;
                    _speed = newSpd;
                }
            }
            else
            {
                if (deltaMove >= Vector3.Distance(transform.position, _target))
                {
                    transform.position = _target;
                }
                else
                {
                    transform.position += (_target - transform.position).normalized * deltaMove;
                    _speed = newSpd;
                }
            }
        }

        private bool IsSameCmd((int, int) newCmd)
        {
            if (_lastCmd.Item1 == newCmd.Item1 && _lastCmd.Item2 == newCmd.Item2) return true;
            _lastCmd = newCmd;
            return false;
        }

        private Vector3 GetSize(RangeFilter filter)
        {
            var maxWidth = filter.rangeType is RangeFilter.RangeType.SQUARE or RangeFilter.RangeType.BOX
                ? filter.width
                : filter.minMaxRange.y * 2;
            var maxLength = (filter.minMaxRange.y - filter.minMaxRange.x) * 2;
            var maxHeight = filter.rangeType is RangeFilter.RangeType.CYLINDER or RangeFilter.RangeType.BOX
                ? Mathf.Max(Mathf.Abs(filter.minMaxHeight.y), Mathf.Abs(filter.minMaxHeight.x)) * 2
                : 100;
            return new Vector3(maxWidth , maxHeight, maxLength);
        }

        private void FitSize(Vector3 size)
        {
            decalProjector.size = new Vector3(size.x, size.z, size.y);
        }


        private void OnDestroy()
        {
            TimerManager.Tick -= Move;
        }
    }
}