using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Controller : MonoBehaviour
{
    struct RaycastInfo
    {
        public Vector3 Origin;
        public Vector3 Dir;
    }
    
    public Vector3 OrbitPosition(Vector3 centerPoint, float radius, float dist, Vector3 up)
    {
        var angle = (dist / (2 * Mathf.PI * radius)) * 360;
        
        Vector3 tmp;
        // calculate position X
        tmp.x = Mathf.Sin(angle * (Mathf.PI / 180)) * radius + centerPoint.x;
        // calculate position Y
        tmp.y = centerPoint.y;
        
        tmp.z =  Mathf.Sin(angle * (Mathf.PI / 180)) * radius + centerPoint.y;
        
        
        
        return tmp;
    }
    
    private CharacterController _collider;
    private List<RaycastInfo> _raycastOrigins = new List<RaycastInfo>();

    [SerializeField] private int _horizontalRayCount = 4;
    [SerializeField] private int _verticalRayCount = 4;

    private float _horizontalRaySpacing;
    private float _verticalRaySpacing;
    
    void Start()
    {
        _collider = GetComponent<CharacterController>();
    }

    void Update()
    {
        CalculateRaySpacing();
        UpdateRaycastOrigins();

        for (int i = 0; i < _raycastOrigins.Count; i++)
        {
            Debug.DrawRay(_raycastOrigins[i].Origin, _raycastOrigins[i].Dir, Color.red, 2);
        }
    }

    void UpdateRaycastOrigins()
    {
        int i = 0;
        for (int h = 0; h < _horizontalRayCount; h++)
        {
            for (int v = 0; v < _verticalRayCount; v++)
            {
                if (_raycastOrigins.Count == i)
                {
                    _raycastOrigins.Add(new RaycastInfo());
                }

                var currentHeight = _verticalRaySpacing * v;
                var currentHorizontal = _horizontalRaySpacing * h;
                

                i++;
            }
        }
    }

    void CalculateRaySpacing()
    {
        var verticalLength = _collider.height - _collider.radius * 2 + (Mathf.PI * _collider.radius);
        var horizontalLength = 2 * Mathf.PI * _collider.radius;

        _verticalRayCount = Mathf.Clamp(_verticalRayCount, 2, int.MaxValue);
        _horizontalRayCount = Mathf.Clamp(_verticalRayCount, 2, int.MaxValue);

        _horizontalRaySpacing = horizontalLength / (_horizontalRayCount - 1);
        _verticalRaySpacing = verticalLength / (_verticalRayCount - 1);
    }
}
