using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterLogic
{
    public interface IMovementModule
    {
        Vector3 Velocity { get; set; }
        Vector3 InputVelocity { get; set; }
        CharacterController Controller { get; set; }
        void Init(MovementModuleSettings settings, Vector3 startVelocity, CharacterController controller);
        void HorizontalMovement(Vector3 input);
        void VerticalMovement(float vel);
        void AddVelocity(Vector3 vel);
        void Update();
        void OnControllerColliderHit(ControllerColliderHit hit);
    }
}
