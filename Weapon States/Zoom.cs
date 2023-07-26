using SLIDDES.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLIDDES.Project.FPS
{
    public class Zoom : WeaponState // IS A SUBSTATE!
    {
        /// <summary>
        /// Value from 0 to 1 that keeps track of how far the weapon is zoomed in (1 is all the way)
        /// </summary>
        public float ZoomTimer => zoomTimer;

        /// <summary>
        /// Value from 0 to 1 that keeps track of how far the weapon is zoomed in (1 is all the way)
        /// </summary>
        private float zoomTimer;
        private TweenInfo tweenInfoZoom;

        public Zoom() { }

        public override void OnEnter()
        {
            base.OnEnter();

            // Values
            Values.isZoomingIn = true;
            WeaponController.events.onWeaponCrosshairSmall.Invoke(Weapon);
            // Bobbing
            Weapon.BobbingCurrentPositionCurve = Values.bobCurveADSFirePosition;
            Weapon.BobbingCurrentRotationCurve = Values.bobCurveADSFireRotation;
            Weapon.BobbingCurveSpeed = Values.bobbingSpeedZoomedIn;

            // Stop tweenInfo if running
            if(tweenInfoZoom != null) { tweenInfoZoom.Free(); }
            // (zoomTimer to 1)
            tweenInfoZoom = Tween.Value(zoomTimer, 1, Values.zoomTime - (Values.zoomTime * zoomTimer)).OnChange(x =>
            {
                zoomTimer = x.Float;
                Events.onZoom.Invoke(zoomTimer);
            }).
            OnComplete(x =>
            {
                zoomTimer = 1;
                Values.isZoomedIn = true;
            });
        }

        public override void OnExit()
        {
            base.OnExit();

            // Values
            Values.isZoomingIn = false;
            Values.isZoomedIn = false;
            WeaponController.events.onWeaponCrosshairNormal.Invoke(Weapon);
            // Bobbing (Exit to right bobbing)
            if(stateMachine.CurrentState == stateMachine.states["idle"])
            {
                // Weapon walking state is substate, so it can be active in idle state
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

            // Stop tweenInfo if running
            if(tweenInfoZoom != null) { tweenInfoZoom.Free(); }
            // (zoomTimer to 0)
            tweenInfoZoom = Tween.Value(zoomTimer, 0, Values.zoomTime * zoomTimer).OnChange(x =>
            {
                zoomTimer = x.Float;
                Events.onZoom.Invoke(zoomTimer);
            }).
            OnComplete(x =>
            {
                zoomTimer = 0;
            });
        }
    }
}
