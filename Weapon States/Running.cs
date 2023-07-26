using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLIDDES.Project.FPS
{
    public class Running : WeaponState
    {
        public Running() { }

        public override void OnEnter()
        {
            base.OnEnter();

            // If zoomed in zoom out
            if(Values.IsZooming) stateMachine.states["zoom"].OnExit();

            // Listeners
            ListenerRunning = true;
            ListenerShooting = true; // for interrupting running
            ListenerJumping = true;
            ListenerZooming = true; // for interrupting running

            // Values
            Values.isRunning = true;
            WeaponController.events.onWeaponCrosshairSmall.Invoke(Weapon);
            Weapon.ModelPosition = Values.runningPosition;
            Weapon.ModelRotation = Values.runningRotation;
            // Bobbing
            Weapon.BobbingCurrentPositionCurve = Values.bobCurveRunningPosition;
            Weapon.BobbingCurrentRotationCurve = Values.bobCurveRunningRotation;
            Weapon.BobbingCurveSpeed = Values.bobbingSpeedRunning;
        }

        public override void OnExit()
        {
            base.OnExit();

            // Listeners
            ListenerRunning = false;
            ListenerShooting = false; // for interrupting running
            ListenerJumping = false;
            ListenerZooming = false; // for interrupting running

            // Values
            Values.isRunning = false;
            Weapon.ModelPosition = Values.hipFirePosition;
            Weapon.ModelRotation = Values.hipFireRotation;
        }
    }
}
