using System.Collections;
using System.Collections.Generic;
using DitzelGames.FastIK;
using UnityEngine;

public class ProceduralFootAnim : MonoBehaviour
{
    [System.Serializable]
    public class Foot
    {
        public Transform Target;
        public FastIKFabric IK;
        public Transform Plane;
        
        public bool isStep;
        [HideInInspector]
        public float StepProcess;
        [HideInInspector]
        public Vector3 Offset;
        [HideInInspector]
        public Vector3 CurrentPos;
        [HideInInspector] 
        public Vector3 TargetPosition;
    }

    [Header("Conponents")]
    [SerializeField] List<Foot> Foots = new List<Foot>();
    
    [Header("Foots info")]
    [SerializeField] private float _footPares;
    [SerializeField] private float _footRadius;
    [SerializeField] private LayerMask _collisionLayer;

    [Header("Step info")]
    [SerializeField] float _stepDistance;
    [SerializeField] float _stepDuration;
    [SerializeField] float _stepOvershoot;
    [SerializeField] private AnimationCurve _stepCurve;
    [SerializeField] private AnimationCurve _stepTrajectoryCurve;

    void Start()
    {
        foreach (var foot in Foots)
        {
            foot.Target.position = foot.IK.transform.position;
            foot.Offset = transform.InverseTransformPoint(foot.Target.position);
            foot.CurrentPos = foot.Target.position;
        }
    }

    void LateUpdate()
    {
        UpdateRig();
    }
    
    void UpdateRig()
    {
        for (int i = 0; i < Foots.Count; i++)
        {
            UpdateFoot(Foots[i]);
        }
    }

    void UpdateFoot(Foot f)
    {
        if (!f.isStep)
        {
            f.Target.position = f.CurrentPos;
            
            f.CurrentPos = GetTargetStepPos(f.CurrentPos);

            var targetPos = GetTargetStepPos(transform.TransformPoint(f.Offset));
            
            if (Vector3.Distance(targetPos, f.CurrentPos) > _stepDistance)
            {
                targetPos += transform.forward * (_stepDistance * _stepOvershoot);
                targetPos = GetTargetStepPos(targetPos);
                
                f.TargetPosition = targetPos;
                f.isStep = true;
                f.StepProcess = 0;
            }
        }

        if (f.isStep)
        {
            f.StepProcess += Time.deltaTime / _stepDuration;
            f.Target.position = Vector3.Lerp(f.CurrentPos, f.TargetPosition, _stepCurve.Evaluate(f.StepProcess));
            f.Target.position += Vector3.up * (_stepTrajectoryCurve.Evaluate(f.StepProcess) *
                                   Vector3.Distance(f.CurrentPos, f.TargetPosition));
            if (f.StepProcess >= 1)
            {
                f.isStep = false;
                f.CurrentPos = f.Target.position;
            }
        }
    }

    Vector3 GetTargetStepPos(Vector3 targetStep)
    {
        for(int i = 0; i < Foots.Count; i++)
        {
            var foot = Foots[i];
            var targetPos = new Vector3(targetStep.x,foot.Target.position.y + 0.5f, targetStep.z);
            var ray = new Ray(targetPos, Vector3.down);
            
            var hitInfo = new RaycastHit();
            
            if(Physics.SphereCast(ray, _footRadius, out hitInfo, 10f, _collisionLayer))
                return hitInfo.point + Vector3.up * _footRadius;
        }

        return targetStep;
    }
}
