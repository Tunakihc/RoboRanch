using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastController : MonoBehaviour
{
    [SerializeField] private float _groundSpeed;
    [SerializeField] private float _flySpeed;
    [SerializeField] private float _jumpForce = 10;
    [SerializeField] private Rigidbody _body;

    [SerializeField] private LayerMask _ground;
    [SerializeField] private float _bodyRadius;

    private bool isGrounded;

    // Update is called once per frame
    void Update()
    {
        GetTargetStepPos();

        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");

        var speed = isGrounded ? _groundSpeed : _flySpeed;
        _body.AddRelativeForce(new Vector3(horizontal,0, vertical) * (speed * Time.deltaTime), ForceMode.Force);
        
        if (isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _body.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
            }
        }
    }
    
    void GetTargetStepPos()
    {
        var ray = new Ray(transform.position, -transform.up);

        if (Physics.SphereCast(ray, _bodyRadius, out var hitInfo, _bodyRadius, _ground))
        {
            isGrounded = true;
            return;
        }

        isGrounded = false;
    }
}
