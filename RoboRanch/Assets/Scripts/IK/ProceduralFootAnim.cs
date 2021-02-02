using System.Collections.Generic;
using UnityEngine;

public class ProceduralFootAnim : MonoBehaviour
{
    [System.Serializable]
    public class FootInfo
    {
        public LegIKController Controller;
        public int[] Pares;
    }
    
    [Header("Conponents")] [SerializeField]
    List<FootInfo> Foots = new List<FootInfo>();

    void Start()
    {
        foreach (var foot in Foots)
        {
            foot.Controller.Init(() => { ChangeLeg(foot); });
            foot.Controller.isEnabled = foot.Controller.isStep;
        }
    }

    void ChangeLeg(FootInfo info)
    {
        info.Controller.isEnabled = false;

        for (int i = 0; i < info.Pares.Length; i++)
            Foots[info.Pares[i]].Controller.isEnabled = true;
    }

    void LateUpdate()
    {
        UpdateRig();
    }
    
    void UpdateRig()
    {
        for (int i = 0; i < Foots.Count; i++)
        {
            Foots[i].Controller.UpdateFoot();
        }
    }


}
