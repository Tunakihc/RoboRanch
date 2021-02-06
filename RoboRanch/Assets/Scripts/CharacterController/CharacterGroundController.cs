using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterGroundController : MonoBehaviour
{
    public LayerMask _physicalWorld;
    
    private float _gravity = -20;
    public Vector3 _velocity;
    private Vector3 _inputVelocity;
    
    private CharacterController _controller;

    public float _moveSpeed;
    public float _accelerationTimeAirborn = 0.2f;
    public float _accelerationTimeGrounded = 0.1f;
    
    public float _jumpHeight = 4;
    public float _timeToJumpApex = 0.4f;
    float _jumpVel = 8;
    
    public float _pushPower = 2.0f;

    private Vector3 _movementVelocitySmoothing;

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


    void Start()
    {
        _groundInfo = new GroundInfo();
        _controller = GetComponent<CharacterController>();

        _gravity = -((2 * _jumpHeight) / Mathf.Pow(_timeToJumpApex, 2));
        _jumpVel = Mathf.Abs(_gravity) * _timeToJumpApex;
    }
    
    void Update()
    {
        UpdateGravity();
        UpdateGround();
        
        HorizontalMovement();
        VerticalMovement();

        UpdatePlatform();
        
        _controller.Move(( _velocity + _inputVelocity) * Time.deltaTime);
    }

    void UpdatePlatform()
    {
        if (!_groundInfo.ground) return;
        
        var newGlobalPlatformPoint = _groundInfo.ground.TransformPoint(_groundInfo.localPoint);
        var moveDirection = newGlobalPlatformPoint - _groundInfo.globalPoint;
        
        _controller.Move(moveDirection);

        var newGlobalPlatformRotation = _groundInfo.ground.rotation * _groundInfo.localRotation;
        var rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(_groundInfo.globalRotation);

        rotationDiff = Quaternion.FromToRotation(rotationDiff * Vector3.up, Vector3.up) * rotationDiff;
            
        transform.rotation = rotationDiff * transform.rotation;
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

        UpdateGroundInfo(_groundInfo.ground);
    }

    void UpdateGravity()
    {
        if (_groundInfo.isGrounded && _velocity.y < 0)
            _velocity.y = 0;
        else
            _velocity.y += _gravity * Time.deltaTime;
    }

    void UpdateGround()
    {
        var origin = transform.position + Vector3.down * (_controller.height / 2 - _controller.radius);
        
        if (Physics.SphereCast(origin , _controller.radius, Vector3.down,
            out RaycastHit hitInfo, _controller.skinWidth * 2, _physicalWorld))
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
        _groundInfo.globalPoint = transform.position;
        _groundInfo.localPoint = ground.InverseTransformPoint(transform.position);
        _groundInfo.globalRotation = transform.rotation;
        _groundInfo.localRotation = Quaternion.Inverse(ground.rotation) * transform.rotation;
    }

    void HorizontalMovement()
    {
        var input = GetCameraRelativeInput();//new Vector3(Input.GetAxisRaw("Horizontal"),0 , Input.GetAxisRaw("Vertical"));
        
        var angle = Vector3.Angle( _groundInfo.groundNormal, Vector3.up);
        
        if(Mathf.Abs(angle) <= _controller.slopeLimit)
            input =  Quaternion.AngleAxis(-angle, Vector3.Cross( _groundInfo.groundNormal, Vector3.up)) * input;

        input = input.normalized;
        
        var targetVelocity = input * _moveSpeed;
        
        _inputVelocity = Vector3.SmoothDamp(_inputVelocity, targetVelocity, ref _movementVelocitySmoothing,
            _groundInfo.isGrounded ? _accelerationTimeGrounded : _accelerationTimeAirborn);

        var lookRotation = input;
        lookRotation.y = 0;
        lookRotation = lookRotation.normalized;
        transform.rotation = Quaternion.LookRotation(lookRotation, Vector3.up);
    }

    Vector3 GetCameraRelativeInput()
    {
        float horizontalAxis = Input.GetAxisRaw("Horizontal");
        float verticalAxis = Input.GetAxisRaw("Vertical");
         
        //assuming we only using the single camera:
        var camera = Camera.main;
 
        //camera forward and right vectors:
        var forward = camera.transform.forward;
        var right = camera.transform.right;
 
        //project forward and right vectors on the horizontal plane (y = 0)
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
 
        //this is the direction in the world space we want to move:
        return forward * verticalAxis + right * horizontalAxis;
    }

    void VerticalMovement()
    {
        if (Input.GetKeyDown(KeyCode.Space) &&  _groundInfo.isGrounded)
        {
            _velocity.y += _jumpVel;
        }
    }
    
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        
        if (body == null || body.isKinematic)
            return;

        if (hit.moveDirection.y < -0.3)
            return;

        var pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        body.velocity = pushDir * _pushPower;
    }
}
