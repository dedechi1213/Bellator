using UnityEngine;

namespace Bellator.Character
{
    /// <summary>Only drives replaceable visual pivots, never the CharacterController or camera anchor.</summary>
    public sealed class PrimitiveHumanoidAnimator : MonoBehaviour
    {
        [SerializeField] private ThirdPersonMotor motor;
        [SerializeField] private Transform body, leftArm, rightArm, leftLeg, rightLeg;
        private float phase, stride;
        public void Configure(ThirdPersonMotor owner,Transform visualBody,Transform armL,Transform armR,Transform legL,Transform legR)
        { motor = owner; body = visualBody; leftArm = armL; rightArm = armR; leftLeg = legL; rightLeg = legR; }
        private void LateUpdate()
        {
            if (motor == null || body == null) return;
            var health=motor.GetComponent<CharacterHealth>();
            if(health!=null && !health.IsAlive) return;
            float dt = Time.deltaTime;
            bool dodge = motor.IsDodging;
            if (motor.GetComponent<PlayerAnimationBridge>()?.DrivesAnimator ?? false) return;
            stride = Mathf.Lerp(stride,Mathf.Clamp01(motor.PlanarSpeed/6.2f),1-Mathf.Exp(-12*dt));
            phase += dt * (motor.IsRunning ? 13 : 9) * Mathf.Clamp01(motor.PlanarSpeed);
            float swing = Mathf.Sin(phase) * Mathf.Lerp(0,42,stride);
            float breathe = Mathf.Sin(Time.time*2)*.012f;
            float dodgeArc = Mathf.Sin(motor.DodgeProgress * Mathf.PI);
            Vector3 localDodge = motor.transform.InverseTransformDirection(motor.DodgeDirection);
            float lean = motor.DodgeStyle == DodgeStyle.Roll ? 70 : 23;
            body.localPosition = new Vector3(0,dodge ? -.16f * dodgeArc : breathe+Mathf.Abs(Mathf.Sin(phase))*stride*.035f,0);
            body.localRotation = dodge ? Quaternion.Euler(lean*dodgeArc*localDodge.z,0,-lean*dodgeArc*localDodge.x) : Quaternion.Euler(motor.IsRunning ? 7 : 0,0,0);
            Pose(leftArm,dodge ? -65 : swing,0,-5);
            Pose(rightArm,dodge ? -65 : -swing,0,5);
            Pose(leftLeg,dodge ? -40 : -swing,0,0);
            Pose(rightLeg,dodge ? 45 : swing,0,0);
        }
        private static void Pose(Transform pivot,float x,float y,float z)
        { if(pivot != null) pivot.localRotation = Quaternion.Slerp(pivot.localRotation,Quaternion.Euler(x,y,z),1-Mathf.Exp(-15*Time.deltaTime)); }
    }
}



