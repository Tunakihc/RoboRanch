using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterLogic
{
    public class MovementModuleSettings
    {

    }

    [RequireComponent(typeof(CharacterController))]
    public class Character : MonoBehaviour
    {
        public enum MovementState
        {
            ground,
            water,
            pillar,
            zeroGravity
        }

        [SerializeField] private CharacterCameraController _camera;
        private CharacterController _controller;

        [Header("Movement Settings")] 
        [SerializeField] private GroundMovementModuleSettings _groundSettings;

        public Vector3 Velocity => _currentMovementModule?.Velocity ?? Vector3.zero; 
        
        public MovementState CurrentState = MovementState.ground;
        readonly Dictionary<MovementState, IMovementModule> _movementModules = new Dictionary<MovementState,IMovementModule>();
        private IMovementModule _currentMovementModule;
        
        IMovementModule GetMovementModule(MovementState _state)
        {
            
            if(_movementModules.ContainsKey(_state))
                return _movementModules[_state];
            
            switch (_state)
            {
                case MovementState.ground:
                    var module = new CharacterGroundMovement();
                    _movementModules.Add(_state, module);
                    return module;
                case MovementState.water:
                    break;
                case MovementState.pillar:
                    break;
                case MovementState.zeroGravity:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_state), _state, null);
            }

            return null;
        }

        MovementModuleSettings GetModuleSettings(MovementState _state)
        {
            switch (_state)
            {
                case MovementState.ground:
                    return _groundSettings;
                case MovementState.water:
                    break;
                case MovementState.pillar:
                    break;
                case MovementState.zeroGravity:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_state), _state, null);
            }

            return null;
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            
            ChangeMovementState(MovementState.ground);
        }

        private void ChangeMovementState(MovementState state)
        {
            CurrentState = state;
            
            var module = GetMovementModule(state);
            var settings = GetModuleSettings(state);
            
            module.Init(settings, Velocity, _controller);
            
            _currentMovementModule = module;
        }

        private void Update()
        {
            InputUpdate();
            
            _currentMovementModule?.Update();
        }

        void InputUpdate()
        {
            var input = GetCameraRelativeInput();

            var lookRotation = input;
            lookRotation.y = 0;
            lookRotation = lookRotation.normalized;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookRotation, Vector3.up),
                input.magnitude);
            
            _currentMovementModule?.HorizontalMovement(input);

            if (Input.GetKeyDown(KeyCode.Space))
                _currentMovementModule?.VerticalMovement(1);
        }

        Vector3 GetCameraRelativeInput()
        {
            var horizontalAxis = Input.GetAxisRaw("Horizontal");
            var verticalAxis = Input.GetAxisRaw("Vertical");

            var forward = _camera.transform.forward;
            var right = _camera.transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return forward * verticalAxis + right * horizontalAxis;
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _currentMovementModule?.OnControllerColliderHit(hit);
        }
    }
}
