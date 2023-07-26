using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLIDDES.Project.FPS
{
    public class Walking : WeaponState // IS A SUBSTATE!
    {
        public Walking() { }

        public override void OnEnter()
        {
            base.OnEnter();

            // Listeners
            ListenerWalking = true;

            // Values
            Values.isWalking = true;
            WeaponController.events.onWeaponCrosshairBig.Invoke(Weapon);
            // Bobbing
            if(!Values.IsZooming)
            {
                Weapon.BobbingCurrentPositionCurve = Values.bobCurveWalkingPosition;
                Weapon.BobbingCurrentRotationCurve = Values.bobCurveWalkingRotation;
                Weapon.BobbingCurveSpeed = Values.bobbingSpeedWalking;
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            // Listeners
            ListenerWalking = false;

            // Values
            Values.isWalking = false;
            WeaponController.events.onWeaponCrosshairNormal.Invoke(Weapon);
            // Bobbing
            if(!Values.IsZooming && stateMachine.CurrentState == stateMachine.states["idle"])
            {
                Weapon.BobbingCurrentPositionCurve = Values.bobCurveIdlePosition;
                Weapon.BobbingCurrentRotationCurve = Values.bobCurveIdleRotation;
                Weapon.BobbingCurveSpeed = Values.bobbingSpeedIdle;
            }
        }
    }
}
