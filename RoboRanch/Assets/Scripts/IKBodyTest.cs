using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKBodyTest : MonoBehaviour
{
    // [SerializeField] private float _targetHeight;
    [SerializeField] List<LegIKController> _targets = new List<LegIKController>();

    void LateUpdate()
    {
        var targetNormal = Vector3.zero;
        var targetPosition = Vector3.zero;

        for (int i = 0; i < _targets.Count; i++)
        {
            targetNormal += _targets[i]._target.forward;
            targetPosition += _targets[i]._target.position;
        }

        targetNormal /= _targets.Count;
        
        Debug.Log(targetNormal);
        
        // targetPosition /= _targets.Count;

        transform.up = targetNormal;

        // if (transform.position.y < targetPosition.y + _targetHeight)
        // {
        //     transform.position += targetNormal * ((targetPosition.y + _targetHeight) - transform.position.y);
        // }
    }
}
