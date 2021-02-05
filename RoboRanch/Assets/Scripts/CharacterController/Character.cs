using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller))]
public class Character : MonoBehaviour
{
    private float _gravity = -20;
    private Vector3 _velocity;
    
    private Controller _controller;

    public float _moveSpeed;
    public float _accelerationTimeAirborn = 0.2f;
    public float _accelerationTimeGrounded = 0.1f;
    

    public float _jumpHeight = 4;
    public float _timeToJumpApex = 0.4f;
    float _jumpVel = 8;

    private float _forwardVelocitySmoothing;
    private float _sidesVelocitySmoothing;

    void Start()
    {
        _controller = GetComponent<Controller>();

        _gravity = -((2 * _jumpHeight) / Mathf.Pow(_timeToJumpApex, 2));
        _jumpVel = Mathf.Abs(_gravity) * _timeToJumpApex;
    }

    void Update()
    {
        var isGrounded = _controller.IsCollide(-transform.up, 0.5f);
        
        if (_controller.IsCollide(transform.up, 0.5f) || isGrounded)
            _velocity.y = 0;
        
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        
        input = input.normalized;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            _velocity.y += _jumpVel;
        }

        var targetHorizontalVelocity = input * _moveSpeed;

        _velocity.x = Mathf.SmoothDamp(_velocity.x, targetHorizontalVelocity.x, ref _forwardVelocitySmoothing,
            isGrounded ? _accelerationTimeGrounded : _accelerationTimeAirborn);
        _velocity.z = Mathf.SmoothDamp(_velocity.z, targetHorizontalVelocity.y, ref _sidesVelocitySmoothing,
            isGrounded ? _accelerationTimeGrounded : _accelerationTimeAirborn);
        
        _velocity.y += _gravity * Time.deltaTime;
        
        _controller.Move(_velocity * Time.deltaTime);
    }
}
