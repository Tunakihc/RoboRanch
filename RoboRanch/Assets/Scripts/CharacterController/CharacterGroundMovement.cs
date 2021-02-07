using UnityEngine;

namespace CharacterLogic
{
    [System.Serializable]
    public class GroundMovementModuleSettings : MovementModuleSettings
    {
        public LayerMask _physicalWorld;
      
        public float _moveSpeed;
        public float _accelerationTimeAirborn = 0.2f;
        public float _accelerationTimeGrounded = 0.1f;
        public float _jumpHeight = 4;
        public float _timeToJumpApex = 0.4f;
        public float _pushPower = 2.0f;
    }

    [RequireComponent(typeof(CharacterController))]
    public class CharacterGroundMovement : IMovementModule
    {
        public Vector3 Velocity { get; set; }
        public Vector3 InputVelocity { get; set; }
        public CharacterController Controller { get; set; }

        private GroundMovementModuleSettings _settings;

        private Transform _transform;
        private Vector3 _movementVelocitySmoothing;
        private Vector3 _inputVelocity;
        private float _gravity = -20;
        private float _jumpVel = 8;

        public class GroundInfo
        {
            public bool isGrounded;
            public Vector3 groundNormal;

            public Transform ground;

            public Vector3 globalPoint;
            public Vector3 localPoint;

            public Quaternion globalRotation;
            public Quaternion localRotation;
        }

        public GroundInfo _groundInfo;

        public void Init(MovementModuleSettings settings, Vector3 startVelocity, CharacterController controller)
        {
            _settings = (GroundMovementModuleSettings) settings;
            
            _groundInfo = new GroundInfo();

            _gravity = -((2 * _settings._jumpHeight) / Mathf.Pow(_settings._timeToJumpApex, 2));
            _jumpVel = Mathf.Abs(_gravity) * _settings._timeToJumpApex;
            
            Velocity = startVelocity;
            Controller = controller;
            _transform = controller.transform;
        }

        public void HorizontalMovement(Vector3 input)
        {
            InputVelocity = input;
        }

        public void VerticalMovement(float vel)
        {
            if (_groundInfo.isGrounded)
                Velocity += Mathf.Sign(vel) * _jumpVel * Vector3.up;
        }

        public void AddVelocity(Vector3 vel)
        {
            Velocity += vel;
        }

        public void Update()
        {
            UpdateGravity();
            UpdateGround();

            HorizontalMovement();

            UpdatePlatform();

            Controller.Move((Velocity + _inputVelocity) * Time.deltaTime);
        }

        void UpdatePlatform()
        {
            if (!_groundInfo.ground) return;

            var newGlobalPlatformPoint = _groundInfo.ground.TransformPoint(_groundInfo.localPoint);
            var moveDirection = newGlobalPlatformPoint - _groundInfo.globalPoint;

            Controller.Move(moveDirection);

            var newGlobalPlatformRotation = _groundInfo.ground.rotation * _groundInfo.localRotation;
            var rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(_groundInfo.globalRotation);

            rotationDiff = Quaternion.FromToRotation(rotationDiff * Vector3.up, Vector3.up) * rotationDiff;

            _transform.rotation = rotationDiff * _transform.rotation;
            _transform.eulerAngles = new Vector3(0, _transform.eulerAngles.y, 0);

            UpdateGroundInfo(_groundInfo.ground);
        }

        void UpdateGravity()
        {
            var velocity = Velocity;
            if (_groundInfo.isGrounded && velocity.y < 0)
                velocity.y = 0;
            else
                velocity.y += _gravity * Time.deltaTime;

            Velocity = velocity;
        }

        void UpdateGround()
        {
            var origin = _transform.position + Vector3.down * (Controller.height / 2 - Controller.radius);

            if (Physics.SphereCast(origin, Controller.radius, Vector3.down,
                out var hitInfo, Controller.skinWidth * 2, _settings._physicalWorld))
            {
                _groundInfo.groundNormal = hitInfo.normal;
                _groundInfo.isGrounded = true;

                if (_groundInfo.ground != hitInfo.transform)
                    UpdateGroundInfo(hitInfo.transform);

                _groundInfo.ground = hitInfo.transform;
            }
            else
            {
                _groundInfo.groundNormal = Vector3.up;
                _groundInfo.isGrounded = false;
                _groundInfo.ground = null;
            }
        }

        void UpdateGroundInfo(Transform ground)
        {
            _groundInfo.globalPoint = _transform.position;
            _groundInfo.localPoint = ground.InverseTransformPoint(_transform.position);
            _groundInfo.globalRotation = _transform.rotation;
            _groundInfo.localRotation = Quaternion.Inverse(ground.rotation) * _transform.rotation;
        }

        void HorizontalMovement()
        {
            var angle = Vector3.Angle(_groundInfo.groundNormal, Vector3.up);

            if (Mathf.Abs(angle) <= Controller.slopeLimit)
                InputVelocity = Quaternion.AngleAxis(-angle, Vector3.Cross(_groundInfo.groundNormal, Vector3.up)) *
                                InputVelocity;

            InputVelocity = InputVelocity.normalized;

            var targetVelocity = InputVelocity * _settings._moveSpeed;

            _inputVelocity = Vector3.SmoothDamp(_inputVelocity, targetVelocity, ref _movementVelocitySmoothing,
                _groundInfo.isGrounded ? _settings._accelerationTimeGrounded : _settings._accelerationTimeAirborn);
        }

        public void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.collider.attachedRigidbody;

            if (body == null || body.isKinematic)
                return;

            if (hit.moveDirection.y < -0.3)
                return;

            var pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

            body.velocity = pushDir * _settings._pushPower;
        }
    }
}
