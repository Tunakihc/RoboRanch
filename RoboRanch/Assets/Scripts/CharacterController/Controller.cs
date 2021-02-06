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

    struct CollisionState
    {
        public bool ClimbingSlope;
        public bool DescendingSlope;
        public float SlopeAngle, SlopeAngleOld;
        public Vector3 SlopeDir;
        public Vector3 VelocityOld;

        public void Reset()
        {
            ClimbingSlope = false;
            DescendingSlope = false;
            SlopeAngleOld = SlopeAngle;
            SlopeAngle = 0;
            SlopeDir = Vector3.zero;
       
        }
    }

    Vector3 RotateAround(Vector3 point, Vector3 position, float radius, Vector3 axis, float length)
    {
        var angle = (Mathf.Clamp(length / (2 * Mathf.PI * radius),0,1)) * 360;
        
        var vector3 = Quaternion.AngleAxis(angle, axis) * (position - point);
        return point + vector3;
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
    
    public bool IsCollide(Vector3 dir, float offset)
    {
        for (var i = 0; i < _raycastOrigins.Count; i++)
        {
            if (!CloseDir(dir, _raycastOrigins[i].Dir, offset)) continue;
            if (_raycastOrigins[i].IsCollide) return true;
        }

        return false;
    }
    
    private CharacterController _collider;
    private List<RaycastInfo> _raycastOrigins = new List<RaycastInfo>();
    private CollisionState _collision;

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

    public bool IsGrounded => _collider.isGrounded;

    public void Move(Vector3 velocity)
    {
        _collider.Move(velocity);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // no rigidbody
        if (body == null || body.isKinematic)
        {
            return;
        }

        // We dont want to push objects below us
        if (hit.moveDirection.y < -0.3)
        {
            return;
        }

        // Calculate push direction from move direction,
        // we only push objects to the sides never up and down
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // If you know how fast your character is trying to move,
        // then you can also multiply the push velocity by that.

        // Apply the push
        body.velocity = pushDir * 5;
    }

    void CheckHorizontalCollision(ref Vector3 velocity)
    {
        if (velocity.x == 0 && velocity.z == 0)
            return;
        
        var checkDir = new Vector3(velocity.x, 0, velocity.z).normalized;
        var hit = CheckCollision(ref velocity, checkDir, 0.1f);

        if (hit != null)
        {
            var resVel = (hit.Hit.distance - _collider.skinWidth) * checkDir;

            velocity.x = resVel.x;
            velocity.z = resVel.z;


        }
    }

    void SlideCheck(ref Vector3 velocity)
    {
        var dir = new Vector3(velocity.x, velocity.y, velocity.z);
        var rayLength = Mathf.Abs(dir.magnitude) + _collider.skinWidth;
        dir = dir.normalized;
        
        var hit = GetRayInfo(velocity, dir, rayLength,0.1f);

        if (hit != null)
        {
            var resVel = (hit.Hit.distance - _collider.skinWidth) * -hit.Hit.normal;
            var targetDir = new Vector3(velocity.x, 0, velocity.z);
            var projection = Vector3.Project(targetDir, resVel);
            var newVel = targetDir - projection;

            velocity.x = newVel.x;
            velocity.z = newVel.z;
            velocity.y = newVel.y;
        }
    }

    void CheckVerticalCollision(ref Vector3 velocity)
    {
        if(velocity.y == 0) return;
        
        var dir = Vector3.up * Mathf.Sign(velocity.y);
        
        var rayLength = Mathf.Abs(velocity.y) + _collider.skinWidth;

        var hit = GetRayInfo(velocity, dir, rayLength, 0.5f);

        if (hit == null) return;
        
        if (_collision.ClimbingSlope)
        {
            velocity.x = Mathf.Lerp(velocity.x,
                Mathf.Abs(velocity.y) / Mathf.Tan(_collision.SlopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x),
                _collision.SlopeDir.x);

            velocity.z = Mathf.Lerp(velocity.z,
                Mathf.Abs(velocity.y) / Mathf.Tan(_collision.SlopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.z),
                _collision.SlopeDir.z);
        }
        
        // if(_collision.DescendingSlope && Vector3.Dot(hit.Dir, _collision.SlopeDir) > 0) return;
        
        var resVel = (hit.Hit.distance - _collider.skinWidth) * dir;
        var angle = Vector3.Angle(dir.normalized, resVel.normalized);

        velocity.y = resVel.y + Mathf.Sin(angle * Mathf.Deg2Rad) * velocity.y;
    }

    // void NewClimbing(ref Vector3 velocity)
    // {
    //     var dir = Vector3.up * Mathf.Sign(velocity.y);
    //     var rayLength = Mathf.Abs(velocity.y) + _collider.skinWidth;
    //     dir = dir.normalized;
    //     
    //     var hit = GetRayInfo(velocity, dir, rayLength,0.5f);
    //
    //
    //     
    //     if (hit != null)
    //     {
    //         var horizontalNormal = -hit.Hit.normal;
    //         horizontalNormal.y = 0;
    //         horizontalNormal = horizontalNormal.normalized;
    //     
    //         var slopeAngle = -Vector3.Angle(Vector3.down * Mathf.Sign(velocity.y), hit.Hit.normal);
    //     
    //         var targetDir = new Vector3(velocity.x, 0, velocity.z);
    //         
    //         if (Vector3.Dot(targetDir, horizontalNormal) < 0)
    //             slopeAngle *= -1;
    //
    //         Debug.Log(slopeAngle);
    //         
    //         if (Mathf.Abs(slopeAngle) <= _collider.slopeLimit)
    //         {
    //             var resVel = (hit.Hit.distance - _collider.skinWidth) * -hit.Hit.normal;
    //             var targetVel = Vector3.up * velocity.y;
    //             var projection = Vector3.Project(targetVel, resVel);
    //             var newVel = targetVel - projection;
    //
    //             if (slopeAngle <= 0)
    //                 velocity.y = newVel.y;
    //             else
    //                 velocity.y -= newVel.y;
    //         }
    //     } 
    // }

    void ClimbSlopeCheck(ref Vector3 velocity, bool simpleCheck = false)
    {
        // if(velocity.y == 0) return;

        var dir = Vector3.up * Mathf.Sign(velocity.y);
        var magnitude = velocity.magnitude;
        var rayLength = Mathf.Abs(magnitude) + _collider.skinWidth;
        var hit = GetRayInfo(velocity, dir, rayLength,0.1f);

        if (hit == null) return;

        var horizontalNormal = -hit.Hit.normal;
        horizontalNormal.y = 0;
        horizontalNormal = horizontalNormal.normalized;
        
        // var cross = Vector3.Cross(horizontalNormal, dir);

        var slopeAngle = -Vector3.Angle(Vector3.down * Mathf.Sign(velocity.y), hit.Hit.normal);//AngleSigned(Vector3.down * Mathf.Sign(velocity.y), hit.Hit.normal, cross);//

        var targetDir = new Vector3(velocity.x, 0, velocity.z);
        if (Vector3.Dot(targetDir, horizontalNormal) < 0)
        {
            slopeAngle *= -1;
        }
        
        Debug.Log(slopeAngle);

        if (Mathf.Abs(slopeAngle) <= _collider.slopeLimit)
        {
            _collision.SlopeDir = hit.Hit.point - (hit.Origin + transform.position);
            _collision.SlopeDir.y = 0;
            _collision.SlopeDir = _collision.SlopeDir.normalized;


            var moveDistance = targetDir.magnitude;

            velocity.y = Mathf.Sin(-slopeAngle * Mathf.Deg2Rad) * moveDistance;
            velocity.x = Mathf.Cos(Mathf.Abs(slopeAngle) * Mathf.Deg2Rad) * targetDir.x;
            velocity.z = Mathf.Cos(Mathf.Abs(slopeAngle) * Mathf.Deg2Rad) * targetDir.z;

            _collision.SlopeAngle = slopeAngle;
        }
        else
        {
            //TODO: Slide the slope
            // velocity.y = resVel.y;
        }
    }

    RaycastInfo CheckCollision(ref Vector3 velocity, Vector3 checkDir, float offset = 0)
    {
        var directionXyz = new Vector3(velocity.x * checkDir.x, velocity.y * checkDir.y, velocity.z * checkDir.z);
        
        var rayLength = Mathf.Abs(directionXyz.magnitude) + _collider.skinWidth;

        var hit = GetRayInfo(velocity, checkDir, rayLength, offset);

        return hit ?? null;
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
        
        _collision.Reset();
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
