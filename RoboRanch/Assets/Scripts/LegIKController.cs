using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DitzelGames.FastIK;
using Unity.Mathematics;
using UnityEngine;

public class LegIKController : MonoBehaviour
{
    public bool isEnabled;
    public bool isStep;
    public bool isGrounded;
    
    [Header("Components")]
    [SerializeField] FastIKFabric _ik;
    [SerializeField] Transform _base;
    
    [HideInInspector]
    public Transform _target;
    Transform _plane;

    [Header("Foots info")]
    [SerializeField] private float _footRadius;
    [SerializeField] private LayerMask _collisionLayer;

    [Header("Step info")]
    [SerializeField] float _stepDistance;
    [SerializeField] private float _stepHeight;
    [SerializeField] float _stepOvershoot;
    [SerializeField] private AnimationCurve _stepCurve;
    [SerializeField] private AnimationCurve _stepTrajectoryCurve;

    [Header("Behaviour info")] 
    [SerializeField] private float _minSpeed = 0.1f;
    [SerializeField] private float _returnPositionWaitTime = 0.1f;
    [SerializeField] private float _returnPositionDamp = 0.2f;
    [SerializeField] private float _flyDamp = 0.1f;

    private float _stepProgress;
    private Vector3 _offset;
    private Vector3 _currentPos;
    private Vector3 _startPos;
    private Vector3 _targetPosition;
    private Vector3 _speedDir;
    private float _length;

    private Action _onLand;

    void Start()
    {
        _target = _ik.Target;
        
        if (_target == null)
        {
            var newTarget = new GameObject(gameObject.name + "_target").transform;
            newTarget.SetParent(transform.parent);
            _target = _ik.Target = newTarget;
        }

        _plane = _ik.Pole;

        if (_base == null)
        {
            var newBase = new GameObject(gameObject.name + "_base").transform;
            newBase.SetParent(transform.parent);
            newBase.position = transform.position;
            _base = newBase;
        }

        _target.position = _ik.transform.position;
        _offset = _base.InverseTransformPoint(_target.position);
        _length = Vector3.Distance(_base.position, _target.position);
        
        _currentPos = _startPos = _target.position;
        _prevPos = GetTargetStepPos(_base.TransformPoint(_offset), out isGrounded);
        
        _ik.Init();
    }
    
    public void Init(Action onLand)
    {
        _onLand = onLand;
    }


    private Vector3 _prevPos;

    private Vector3 _velocityPos;
    private Vector3 _velocityOffset;

    private float _speed;

    private Vector3 _prevBodyPos;

    private float _speedDamp;

    public void UpdateFoot()
    {
        _speed = (_base.position - _prevBodyPos).magnitude / Time.deltaTime;

        if (_speed <= _minSpeed)
        {
            _speedDamp += Time.deltaTime;
        }
        else
        {
            _speedDamp = 0;
        }

        _prevBodyPos = _base.position;

        var wasInAir = isGrounded == false;
        
        var offset  = GetTargetStepPos(_base.TransformPoint(_offset), out isGrounded);

        if (offset != _prevPos)
        {
            _speedDir = offset - _prevPos;
            _speedDir = _speedDir.normalized;
        }

        if (!isEnabled)
        {
            _prevPos = offset;
        }

        if (wasInAir && isGrounded)
        {
            _prevPos = _startPos = _currentPos = offset;
            _velocityPos = Vector3.zero;
        }

        if (!isGrounded)
        {
            _prevPos = Vector3.SmoothDamp(_prevPos, offset, ref _velocityOffset, _flyDamp);
            _startPos = _currentPos = Vector3.SmoothDamp(_startPos, offset, ref _velocityPos, _flyDamp);
            isStep = false;
        }else if (_speedDamp > _returnPositionWaitTime)
        {
            _currentPos = _startPos = Vector3.SmoothDamp(_currentPos, offset, ref _velocityPos, _returnPositionDamp);
            _stepProgress = 0;
        }
        else
        {
            if (!isStep)
            {
                _stepProgress =
                    Mathf.Clamp(Vector3.Distance(offset, _prevPos) / (_stepDistance + _stepOvershoot), 0, 1);

                if (_stepProgress >= 1f )
                {
                    _prevPos = offset;

                    isStep = true;
                }
            }
            else
            {
                _stepProgress = Mathf.Clamp(Vector3.Distance(offset, _prevPos) / _stepDistance, 0, 1);
                
                _targetPosition = GetTargetStepPos(_prevPos + _speedDir * (_stepOvershoot + _stepDistance*2),
                    out var istargetGrounded);

                _currentPos = Vector3.Lerp(_startPos, _targetPosition, _stepCurve.Evaluate(_stepProgress));
                _currentPos += Vector3.up * (_stepTrajectoryCurve.Evaluate(_stepProgress) * _stepHeight);

                if (_stepProgress >= 1f)
                {
                    _onLand?.Invoke();
                    
                    _prevPos = offset;
                    _startPos = _currentPos;

                    isStep = false;
                }
            }
        }

        _target.position = _currentPos;
    }

    Vector3 GetTargetStepPos(Vector3 targetStep, out bool isGrounded)
    {
        
        var targetPos = new Vector3(targetStep.x,_base.position.y, targetStep.z);
        var ray = new Ray(targetPos, -_base.up);

        if (Physics.SphereCast(ray, _footRadius, out var hitInfo, _length + _footRadius,_collisionLayer))
        {
            isGrounded = true;
            _target.rotation = Quaternion.LookRotation(hitInfo.normal);
            return hitInfo.point + _base.up * _footRadius;
        }

        isGrounded = false;
        Quaternion.LookRotation(_base.up);
        return targetStep;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            var offset  = GetTargetStepPos(_base.TransformPoint(_offset), out var isGrounded);
            Gizmos.DrawSphere(offset, _footRadius);
            Gizmos.DrawSphere(_prevPos, _footRadius/2);


            if (isStep)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(_targetPosition, _footRadius);
                Gizmos.DrawLine(_startPos, _targetPosition);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_currentPos, _footRadius);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_startPos, _footRadius);
        }
    }
}
