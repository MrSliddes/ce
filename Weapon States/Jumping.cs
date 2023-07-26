using SLIDDES.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLIDDES.Project.FPS
{
    public class Jumping : WeaponState // IS A SUBSTATE!
    {
        private TweenInfo tweenInfo;
        private float maxTranslationTime = 3;

        public Jumping() { }

        public override void OnEnter()
        {
            base.OnEnter();

            // Values
            Values.isJumping = true;
            if(!Values.isReloading || !Values.IsZooming)
            {
                WeaponController.events.onWeaponCrosshairBig.Invoke(Weapon);
            }

            // Show weapon model jump visual
            if(tweenInfo != null) tweenInfo.Free();
            tweenInfo = Tween.Value(0, maxTranslationTime, maxTranslationTime).OnChange(x =>
            {
                Weapon.ModelJumpPosition = Vector3.Lerp(Values.hipFireModelOnJumpTranslation.Evaluate(x.Float), Values.adsFireModelOnJumpTranslation.Evaluate(x.Float), Weapon.ZoomTimer);
            });
        }

        public override void OnExit()
        {
            base.OnExit();

            // Values
            Values.isJumping = false;
            if(!Values.isReloading || !Values.IsZooming)
            {
                WeaponController.events.onWeaponCrosshairNormal.Invoke(Weapon);
            }

            // Show weapon model on ground visual
            if(tweenInfo != null) tweenInfo.Free();
            tweenInfo = Tween.Value(0, maxTranslationTime, maxTranslationTime).OnChange(x =>
            {
                Weapon.ModelJumpPosition = Vector3.Lerp(Values.hipFireModelOnGroundTranslation.Evaluate(x.Float), Values.adsFireModelOnGroundTranslation.Evaluate(x.Float), Weapon.ZoomTimer);
            });
        }
    }
}
