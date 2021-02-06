using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineFreeLook))]
public class CharacterCameraController : MonoBehaviour
{
    [SerializeField] CharacterGroundController _character;
    [SerializeField] float _smoothTime;
    [SerializeField] float _followSpeed;
    
    private Transform _target;
    private float ghostPositionY;
    private Vector3 vel;

    private CinemachineFreeLook _cinemachine;
    private Camera _camera;

    void Start()
    {
        _target = new GameObject("CharacterCameraTarget").transform;
        _cinemachine = GetComponent<CinemachineFreeLook>();
        _camera = GetComponent<Camera>();
        _cinemachine.Follow = _cinemachine.LookAt = _target;
        _target.position = _character.transform.position;
    }
    
    void LateUpdate()
    {
        var viewPos = _camera.WorldToViewportPoint(_character.transform.position + _character._velocity * Time.deltaTime);
        
        if (viewPos.y > 0.85f || viewPos.y < 0.3f)
        {
            ghostPositionY = _character.transform.position.y;
        }
        else if(_character._groundInfo.isGrounded)
        {
            ghostPositionY = _character.transform.position.y;
        }

        var desiredPosition = new Vector3(_character.transform.position.x, ghostPositionY, _character.transform.position.z);
        
        _target.position = Vector3.SmoothDamp(_target.position, desiredPosition, ref vel, _smoothTime, _followSpeed);
    }
}
