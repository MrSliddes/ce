using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLIDDES.Project.FPS
{
    public class Idle : WeaponState
    {
        public Idle() { }

        public override void OnEnter()
        {
            base.OnEnter();

            // Continue shooting if holding shoot (usecase: after equipping weapon)
            if(Input.shoot && Values.CanShoot)
            {
                stateMachine.NewState("shooting");
                return;
            }

            ListenerJumping = true;
            ListenerReloading = true;
            ListenerRunning = true;
            ListenerShooting = true;
            ListenerWalking = true;
            ListenerZooming = true;

            // Animation
            if(WeaponController.components.animatorFirstPerson != null) WeaponController.components.animatorFirstPerson.CrossFade("Empty", 0.1f);

            // Values
            if(!Values.isWalking)
            {
                WeaponController.events.onWeaponCrosshairNormal.Invoke(Weapon);
            }
            else
            {
                WeaponController.events.onWeaponCrosshairBig.Invoke(Weapon);
            }

            // Bobbing
            if(!Values.IsZooming)
            {
                if(!Values.isWalking)
                {
                    Weapon.BobbingCurrentPositionCurve = Values.bobCurveIdlePosition;
                    Weapon.BobbingCurrentRotationCurve = Values.bobCurveIdleRotation;
                    Weapon.BobbingCurveSpeed = Values.bobbingSpeedIdle;
                }
                else
                {
                    Weapon.BobbingCurrentPositionCurve = Values.bobCurveWalkingPosition;
                    Weapon.BobbingCurrentRotationCurve = Values.bobCurveWalkingRotation;
                    Weapon.BobbingCurveSpeed = Values.bobbingSpeedWalking;
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            ListenerJumping = false;
            ListenerReloading = false;
            ListenerRunning = false;
            ListenerShooting = false;
            ListenerWalking = false;
            ListenerZooming = false;
        }
    }
}
