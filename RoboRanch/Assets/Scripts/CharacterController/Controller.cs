using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Controller : MonoBehaviour
{
    class RaycastInfo
    {
        public Vector3 Origin;
        public Vector3 Dir;
        public bool IsCollide;
        public bool isActive;
        public RaycastHit Hit;
    }

    Vector3 RotateAround(Vector3 point, Vector3 position, float radius, Vector3 axis, float length)
    {
        var angle = (Mathf.Clamp(length / (2 * Mathf.PI * radius),0,1)) * 360;
        
        var vector3 = Quaternion.AngleAxis(angle, axis) * (position - point);
        return point + vector3;
    }
    
    private CharacterController _collider;
    private List<RaycastInfo> _raycastOrigins = new List<RaycastInfo>();

    [Range(5,100)]
    [SerializeField] private int _horizontalRayCount = 5;
    [Range(5,100)]
    [SerializeField] private int _verticalRayCount = 5;

    // [Range(0, 1)] [SerializeField] private float _horSearchAngle = 0;
    // [Range(0, 1)] [SerializeField] private float _verSearchAngle = 0;

    [SerializeField] private LayerMask _collisionMask;

    private float _horizontalRaySpacing;
    private float _verticalRaySpacing;
    

    
    void Start()
    {
        _collider = GetComponent<CharacterController>();
        
        CalculateRaySpacing();
        UpdateRaycastOrigins();
    }

    public bool IsCollide(Vector3 dir, float offset)
    {
        for (var i = 0; i < _raycastOrigins.Count; i++)
        {
            if (!CloseDir(dir, _raycastOrigins[i].Dir, offset)) continue;
            if (_raycastOrigins[i].IsCollide) return true;
        }

        return false;
    }
    
    public void Move(Vector3 velocity)
    {
        ResetCollisions();

        // velocity.y = 0;
        
        ClimbSlopeCheck(ref velocity);
        SlideWallCheck(ref velocity);

        transform.Translate(velocity);
        // _collider.Move(velocity);
    }

    bool CloseDir(Vector3 moveDir, Vector3 rayDir, float offset)
    {
        // var xAxis = (Mathf.Sign(moveDir.x) == Mathf.Sign(rayDir.x) && Mathf.Abs(moveDir.x) > 0 && Mathf.Abs(rayDir.x )> 0) || moveDir.x == 0;
        // var yAxis = (Mathf.Sign(moveDir.y) == Mathf.Sign(rayDir.y) && Mathf.Abs(moveDir.y) > 0 && Mathf.Abs(rayDir.y )> 0) || moveDir.y == 0;
        // var zAxis = (Mathf.Sign(moveDir.z) == Mathf.Sign(rayDir.z) && Mathf.Abs(moveDir.z) > 0 && Mathf.Abs(rayDir.z )> 0) || moveDir.z == 0;
        //
        // if (moveDir.y == 0)
        // {
        //     yAxis = rayDir.y == 0;
        // }

        return Vector3.Dot(moveDir, rayDir) >= 1-offset; //xAxis && zAxis && yAxis ;
    }
    
    float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
    {
        return Mathf.Atan2(
            Vector3.Dot(n, Vector3.Cross(v1, v2)),
            Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
    }

    void SlideWallCheck(ref Vector3 velocity)
    {
        if (velocity.x == 0 && velocity.z == 0)
            return;
        
        var dir = new Vector3(velocity.x,0,velocity.z);
        var rayLength = Mathf.Abs(dir.magnitude) + _collider.skinWidth;
        dir = dir.normalized;
        
        var hit = GetRayInfo(velocity, dir, rayLength,0.5f);

        if(hit == null) return;

        var resVel = (hit.Hit.distance - _collider.skinWidth) * -hit.Hit.normal;
        
        var targetDir = new Vector3(velocity.x, 0, velocity.z);

        var projection = Vector3.Project(targetDir, resVel);

        var newVel = targetDir - projection;
        
        velocity.x = newVel.x;
        velocity.z = newVel.z;

        CheckWallCollision(ref velocity);
    }

    void CheckWallCollision(ref Vector3 velocity)
    {
        var checkDir = new Vector3(velocity.x, 0, velocity.z).normalized;
        var hit = CheckCollision(ref velocity, checkDir, 0.1f);

        if (hit == null) return;

        var resVel = (hit.Hit.distance- _collider.skinWidth) * hit.Dir;

        velocity.x = resVel.x;
        velocity.z = resVel.z;
    }

    //TODO: Diagonal slope climbing
    void ClimbSlopeCheck(ref Vector3 velocity)
    {
        if (velocity.y == 0) return;

        var dir = transform.up * Mathf.Sign(velocity.y);
        
        var rayLength = Mathf.Abs(velocity.magnitude) + _collider.skinWidth;
        var hit = GetRayInfo(velocity, dir, rayLength,0.5f);
            
        if(hit == null) return;

        var cross = Vector3.Cross(new Vector3(velocity.x, 0, velocity.z).normalized, dir);

        var slopeAngle = -1 * AngleSigned(Vector3.down * Mathf.Sign(velocity.y), hit.Hit.normal, cross);

        
        if (slopeAngle <= _collider.slopeLimit)
            ClimbSlope(ref velocity, slopeAngle);
        else
        {
            var resVel = (hit.Hit.distance - _collider.skinWidth) * dir;
        
            velocity.y = resVel.y;
        }
    }
    
    void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {
        var targetDir = new Vector2(velocity.x, velocity.z);
        var moveDistance = targetDir.magnitude;

        var targetYVelocity = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (velocity.y <= targetYVelocity)
        {
            velocity.y = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * targetDir.x;
            velocity.z = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * targetDir.y;
        }
    }

    //TODO: Check work of velocity changing on collision
    //TODO: Stuck to the wall bug
    RaycastInfo CheckCollision(ref Vector3 velocity, Vector3 checkDir, float offset = 0)
    {
        var directionXyz = new Vector3(velocity.x * checkDir.x, velocity.y * checkDir.y, velocity.z * checkDir.z);
        
        var rayLength = Mathf.Abs(directionXyz.magnitude) + _collider.skinWidth;

        var hit = GetRayInfo(velocity, checkDir, rayLength, offset);

        return hit ?? null;

        // var resVel = (hit.Hit.distance- _collider.skinWidth) * hit.Dir;

        // var velocityChangePower = Mathf.Clamp(Vector3.Dot(checkDir, hit.Dir), 0, 1);
        //
        // resVel *= velocityChangePower;
        //
        // // var targetVel = Vector3.Project(resVel*velocityChangePower, checkDir);
        //
        //
        // velocity.x = Mathf.Lerp(velocity.x, resVel.x,  Mathf.Abs(checkDir.x));
        // velocity.y = Mathf.Lerp(velocity.y, resVel.y,  Mathf.Abs(checkDir.y));
        // velocity.z = Mathf.Lerp(velocity.z, resVel.z,  Mathf.Abs(checkDir.z));
        //
        // // targetVel.x *= Mathf.Abs(checkDir.x);
        // // targetVel.y *= Mathf.Abs(checkDir.y);
        // // targetVel.z *= Mathf.Abs(checkDir.z);
        //
        // // velocity = targetVel;
    }

    RaycastInfo GetRayInfo(Vector3 velocity, Vector3 dir, float dist, float offset = 0)
    {
        offset = Mathf.Clamp(offset, 0, 1);
        
        var velNorm = velocity.normalized;
        
        var crossVelocity = (velNorm - dir);
        crossVelocity = new Vector3(crossVelocity.x * velocity.x,crossVelocity.y * velocity.y,crossVelocity.z * velocity.z);

        var minAngle = -1f;
        var minDist = dist;
        RaycastInfo returnHit = null;
        
        for (var i = 0; i < _raycastOrigins.Count; i++)
        {
            var angle = Vector3.Dot(dir, _raycastOrigins[i].Dir);
            
            if (!CloseDir(dir, _raycastOrigins[i].Dir, offset)) continue;

            _raycastOrigins[i].isActive = true;

            var rayOrigin = transform.position + _raycastOrigins[i].Origin + crossVelocity;
                
            var ray = new Ray(rayOrigin, _raycastOrigins[i].Dir);

            if (!Physics.SphereCast(ray, _collider.skinWidth, out var hit, dist, _collisionMask)) continue;
            
            _raycastOrigins[i].IsCollide = true;
            _raycastOrigins[i].Hit = hit;

            if (hit.distance < minDist || hit.distance == minDist && angle > minAngle)
            {
                minDist = hit.distance;
                returnHit = _raycastOrigins[i];
                minAngle = angle;
            }
        }

        return returnHit;
    }

    void ResetCollisions()
    {
        for (var i = 0; i < _raycastOrigins.Count; i++)
            _raycastOrigins[i].IsCollide = _raycastOrigins[i].isActive = false;
    }

    void UpdateRaycastOrigins()
    {
        var maxCount = _horizontalRayCount * _verticalRayCount;
        if (_raycastOrigins.Count > maxCount)
        {
            var difference = maxCount - _raycastOrigins.Count;
            
            if(difference > 0)
                _raycastOrigins.RemoveRange(_raycastOrigins.Count - (difference + 1), difference);
        }

        var startPos = transform.position - transform.up * (_collider.height / 2);
        
        var radius = _collider.radius;
        var radiusLength = (Mathf.PI * radius)/2;
        var verticalLength = (_collider.height - radius * 2) + (Mathf.PI * radius);
        
        var i = 0;
        for (var h = 0; h < _horizontalRayCount; h++)
        {
            for (var v = 0; v < _verticalRayCount; v++)
            {
                if (_raycastOrigins.Count == i)
                    _raycastOrigins.Add(new RaycastInfo());

                var currentHeight = _verticalRaySpacing * v;
                var currentHorizontal = _horizontalRaySpacing * h;

                var heighClamped = LengthToHeight(currentHeight);
                
                var origin = startPos + transform.up * heighClamped;

                var targetOrigin = RotateAround(origin, origin + Vector3.right * radius, radius, Vector3.up, currentHorizontal);
                
                if (currentHeight <= radiusLength)
                {
                    var axis = Vector3.Cross(Vector3.up, (targetOrigin - origin).normalized);

                    _raycastOrigins[i].Origin = RotateAround(origin,targetOrigin, radius, axis,  radiusLength - currentHeight);
                }
                else if (currentHeight >= _collider.height - _collider.radius)
                {
                    var axis = Vector3.Cross(Vector3.down, (targetOrigin - origin).normalized);
                        
                    _raycastOrigins[i].Origin = RotateAround(origin, targetOrigin, radius, axis, currentHeight - (verticalLength - radiusLength));
                }
                else
                {
                    _raycastOrigins[i].Origin = targetOrigin;
                }

                _raycastOrigins[i].Dir = ( _raycastOrigins[i].Origin - origin).normalized;
                _raycastOrigins[i].Origin -= transform.position;

                i++;
            }
        }
    }

    float LengthToHeight(float length)
    {
        var radius = _collider.radius;
        var radiusLength = (Mathf.PI * radius)/2;
        var verticalLength = (_collider.height - radius * 2) + (Mathf.PI * radius);
        
        if (length <= radiusLength)
        {
            length = _collider.radius;
        }else if (length >= verticalLength - radiusLength)
        {
            length = _collider.height - _collider.radius;
        }
        else
        {
            length = (length - radiusLength) + _collider.radius;
        }

        return length;
    }

    void CalculateRaySpacing()
    {
        var radius = _collider.radius;

        var verticalLength = (_collider.height - radius * 2) + (Mathf.PI * radius);
        var horizontalLength = 2 * Mathf.PI * radius;

        _verticalRayCount = Mathf.Clamp(_verticalRayCount, 5, int.MaxValue);
        _horizontalRayCount = Mathf.Clamp(_horizontalRayCount, 5, int.MaxValue);

        _horizontalRaySpacing = horizontalLength / (_horizontalRayCount - 1);
        _verticalRaySpacing = verticalLength / (_verticalRayCount - 1);
    }

    private void OnDrawGizmos()
    {
        bool enabled = true;
        
        if (!Application.isPlaying || !enabled) return;
        
        float raysLength = 1;
        
        for (var i = 0; i < _raycastOrigins.Count; i++)
        {
            if(!_raycastOrigins[i].isActive) continue;
            
            Gizmos.color = Color.red;
            
            if(_raycastOrigins[i].IsCollide)
                Gizmos.color = Color.green;

            if (_raycastOrigins[i].IsCollide && _raycastOrigins[i].Hit.collider != null)
            {
                Gizmos.color = Color.blue;
            }

            Gizmos.DrawLine(transform.position + _raycastOrigins[i].Origin, transform.position + _raycastOrigins[i].Origin + _raycastOrigins[i].Dir *raysLength);
        }
    }
}
