using System.Collections;
using System.Collections.Generic;
using CharacterLogic;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineFreeLook))]
public class CharacterCameraController : MonoBehaviour
{
    [SerializeField] Character _character;
    [SerializeField] float _smoothTime;
    [SerializeField] float _followSpeed;
    [SerializeField] private float _heightChangeTimer = 0.1f;
    
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

    private float _currentChangeTimer = 0;
    private float _prevHeigh = 0;
    
    void LateUpdate()
    {
        var viewPos = _camera.WorldToViewportPoint(_character.transform.position + _character.Velocity * Time.deltaTime);

        if (_prevHeigh == _character.transform.position.y)
            _currentChangeTimer += Time.deltaTime;

        if (ghostPositionY == _character.transform.position.y)
            _currentChangeTimer = 0;

        _prevHeigh = _character.transform.position.y;

        if (viewPos.y > 0.85f || viewPos.y < 0.3f)
        {
            ghostPositionY = _character.transform.position.y;
        }
        else if (_currentChangeTimer >= _heightChangeTimer)
        {
            ghostPositionY = _character.transform.position.y;
        }

        var position = _character.transform.position;
        var desiredPosition = new Vector3(position.x, ghostPositionY, position.z);
        
        _target.position = Vector3.SmoothDamp(_target.position, desiredPosition, ref vel, _smoothTime, _followSpeed);
    }
}
