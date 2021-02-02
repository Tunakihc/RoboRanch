using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller))]
public class Character : MonoBehaviour
{
    private Controller _controller;

    void Start()
    {
        _controller = GetComponent<Controller>();
    }
}
