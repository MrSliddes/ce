using SLIDDES.StateMachines.Trinity;
using SLIDDES.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

namespace SLIDDES.Project.FPS
{
    /// <summary>
    /// Base class for a weapon
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        /// <summary>
        /// How fast the bobbing curve value is updated
        /// </summary>
        public float BobbingCurveSpeed { get; set; }
        /// <summary>
        /// The progress of the bobbing curve
        /// </summary>
        public float BobbingTime { get; set; }
        /// <summary>
        /// The normalized zoom timer of the weapon. 0 means hip, 1 means ads
        /// </summary>
        public float ZoomTimer => ((Zoom)components.stateMachine.states["zoom"]).ZoomTimer;
        /// <summary>
        /// The visual position of the weapon
        /// </summary>
        public Vector3 ModelPosition { get; set; }
        /// <summary>
        /// The position of the model when jumping (gets added to Model Position in UpdateModelVisual)
        /// </summary>
        public Vector3 ModelJumpPosition;
        /// <summary>
        /// The visual rotation of the weapon model
        /// </summary>
        public Vector3 ModelRotation { get; set; }
        /// <summary>
        /// The current position curve for bobbing
        /// </summary>
        public Vector3AnimationCurve BobbingCurrentPositionCurve { get; set; }
        /// <summary>
        /// The current rotation curve for bobbing
        /// </summary>
        public Vector3AnimationCurve BobbingCurrentRotationCurve { get; set; }
        /// <summary>
        /// The statemachine of the weapon
        /// </summary>
        public StateMachine StateMachine => components.stateMachine;
        /// <summary>
        /// The weapon controller of this weapon
        /// </summary>
        public WeaponController WeaponController => components.weaponController;

        // Internal classes
        public Values values;
        public Components components;
        public Events events;
        public Input input;

        /// <summary>
        /// Has the weapon been initialized?
        /// </summary>
        private bool initialized;
        /// <summary>
        /// The position of the visual bobbing
        /// </summary>
        private Vector3 bobbingPosition;
        /// <summary>
        /// The rotation of the visual bobbing
        /// </summary>
        private Vector3 bobbingRotation;
        /// <summary>
        /// The position of the sway
        /// </summary>
        private Vector3 swayPosition;
        /// <summary>
        /// The rotation of the sway
        /// </summary>
        private Vector3 swayRotation;

        /// <summary>
        /// Initialize the weapon
        /// </summary>
        /// <param name="weaponController">The weapon Controller that handles this weapon</param>
        public void Initialize(WeaponController weaponController)
        {
            // Set
            components.weaponController = weaponController;
            ModelPosition = values.hipFirePosition;
            ModelRotation = values.hipFireRotation;
            BobbingCurrentPositionCurve = values.bobCurveIdlePosition;
            BobbingCurrentRotationCurve = values.bobCurveIdleRotation;
            events.onFiredProjectile.AddListener(VisualFiring);
            events.onZoom.AddListener(VisualZooming);

            Assemble();
            initialized = true;
        }

        // Update is called once per frame
        void Update()
        {
            if(!initialized) return;

            // Update timeSinceLastFire
            values.timeSinceLastShot = Mathf.Clamp(values.timeSinceLastShot += Time.deltaTime, 0, 60);

            VisualSway();
            VisualBobbing();
            UpdateModelVisual();
        }

        /// <summary>
        /// Assemble the weapon
        /// </summary>
        public void Assemble()
        {
            // Set values
            MaxAmmo();

            // Set statemachine
            components.stateMachine.Initialize(gameObject);
        }
        
        /// <summary>
        /// Called to equip the weapon
        /// </summary>
        /// <returns>Time in seconds it takes to equip</returns>
        public void Equip()
        {
            components.stateMachine.NewState("equip");
        }

        /// <summary>
        /// called to deequip the weapon
        /// </summary>
        /// <returns>Time in seconds it takes to deequip</returns>
        public void Deequip()
        {
            components.stateMachine.NewState("deequip");
        }

        /// <summary>
        /// Refill weapon ammo to max
        /// </summary>
        public void MaxAmmo()
        {
            values.currentAmmo = values.maxAmmo;
            values.magazineCurrentAmmo = values.magazineMaxAmmo;
            if(components.weaponController.CurrentWeapon == this) components.weaponController.events.onWeaponAmmoChange.Invoke(this);
        }
        
        /// <summary>
        /// Called when the weapon needs to be removed
        /// </summary>
        public void Remove()
        {
            Destroy(gameObject);
        }

        #region Visual

        /// <summary>
        /// Update the visual bobbing
        /// </summary>
        private void VisualBobbing()
        {            
            BobbingTime += Time.deltaTime * BobbingCurveSpeed;
            BobbingTime %= 1; // looop back to 0 when 1

            Vector3 bobPos = Vector3.zero;
            Vector3 bobRot = Vector3.zero;

            if(values.bobbing && !values.isShooting)
            {
                bobPos = BobbingCurrentPositionCurve.Evaluate(BobbingTime);
                bobPos.x *= components.weaponController.input.movement.x;
                bobPos.z *= components.weaponController.input.movement.y;
                bobRot = BobbingCurrentRotationCurve.Evaluate(BobbingTime);
            }

            bobbingPosition = bobPos;
            bobbingRotation = bobRot;
        }

        /// <summary>
        /// Triggerd when the weapon fires a projectile
        /// </summary>
        private void VisualFiring()
        {
            float fireSpeed = 1 / ((Shooting)components.stateMachine.states["shooting"]).FireSpeed; // how many shots in a second

            float hipTranslationCurveRandomRange = Random.Range(values.hipFireTranslationCurveRandomRange.x, values.hipFireTranslationCurveRandomRange.y); // randomness applied to curve
            float adsTranslationCurveRandomRange = Random.Range(values.adsFireTranslationRandomRange.x, values.adsFireTranslationRandomRange.y); // randomness applied to curve

            float hipRotationCurveRandomRange = Random.Range(values.hipFireRotationCurveRandomRange.x, values.hipFireRotationCurveRandomRange.y);
            float adsRotationCurveRandomRange = Random.Range(values.adsFireRotationCurveRandomRange.x, values.adsFireTranslationRandomRange.y);

            Tween.Value(0, 1, fireSpeed).OnChange(x =>
            {
                // Translate. Get the lerped value from zoom out / in based on zoomTimer (for going from hipfiring to ads firing)
                components.modelTransform.localPosition = Vector3.Lerp(values.hipFireTranslationCurve.Evaluate(x.Float) * hipTranslationCurveRandomRange, values.adsFireTranslationCurve.Evaluate(x.Float) * adsTranslationCurveRandomRange, ((Zoom)components.stateMachine.states["zoom"]).ZoomTimer);
                // Rotate. Get the lerped value from zoom out / in based on zoomTimer (for going from hipfiring to ads firing)
                components.modelTransform.localRotation = Quaternion.Euler(Vector3.Lerp(values.hipFireRotationCurve.Evaluate(x.Float) * hipRotationCurveRandomRange, values.adsFireRotationCurve.Evaluate(x.Float) * adsRotationCurveRandomRange, ((Zoom)components.stateMachine.states["zoom"]).ZoomTimer));
            });
        }

        /// <summary>
        /// Updates the weapon sway
        /// </summary>
        private void VisualSway()
        {
            // Translation
            if(values.swayTranslation)
            {
                Vector3 currentSway = values.isZoomedIn ? values.swayTranslationADSFire : values.swayTranslationHipFire;
                Vector3 movement = -1 * currentSway.x * input.lookDirection; // no y ?
                movement.x += components.weaponController.input.movement.x * (values.isZoomedIn ? values.swayTranslationSidewaysADSFire : values.swayTranslationSidewaysHipFire);
                swayPosition = new Vector3(Mathf.Clamp(movement.x, -currentSway.x, currentSway.x), Mathf.Clamp(movement.y, -currentSway.y, currentSway.y), 0);
            }
            else
            {
                swayPosition = Vector3.zero;
            }

            // Rotation
            if(values.swayRotation)
            {
                Vector3 currentRotationSway = values.isZoomedIn ? values.swayRotationADSFire : values.swayRotationHipFire;
                Vector2 rotationMovement = -1 * currentRotationSway * input.lookDirection;
                rotationMovement.x = Mathf.Clamp(rotationMovement.x, -currentRotationSway.x, currentRotationSway.x);
                rotationMovement.y = Mathf.Clamp(rotationMovement.y, -currentRotationSway.y, currentRotationSway.y);
                swayRotation = new Vector3(rotationMovement.y, rotationMovement.x, rotationMovement.x); // switch x & y axis
            }
            else
            {
                swayRotation = Vector3.zero;
            }
        }

        /// <summary>
        /// Update the weapon model postion based on zooming state
        /// </summary>
        /// <param name="value">0 to 1 of zoom</param>
        private void VisualZooming(float value)
        {
            // Weapon localposition
            if(!values.IsRunning) // theses values are set when running, so dont update them if running
            {
                ModelPosition = Vector3.Lerp(values.hipFirePosition, values.adsFirePosition, value);
                ModelRotation = Vector3.Lerp(values.hipFireRotation, values.adsFireRotation, value);
            }

            // Camera
            components.weaponController.components.camera.fieldOfView = values.cameraFOVCurve.Evaluate(value);
        }

        /// <summary>
        /// Update the weapon model visual
        /// </summary>
        private void UpdateModelVisual()
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, ModelPosition + ModelJumpPosition + swayPosition + bobbingPosition, Time.deltaTime * values.visualPositionSmoothing);

            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(ModelRotation) * Quaternion.Euler(swayRotation) * Quaternion.Euler(bobbingRotation), Time.deltaTime * values.visualRotationSmoothing);
        }

        #endregion

        #region Input

        /// <summary>
        /// When the look direction changes of the weapon
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnInputLook(InputAction.CallbackContext context)
        {
            input.lookDirection = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// When the weapon needs to be reloaded
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnInputReload(InputAction.CallbackContext context)
        {
            if(WeaponController.WeaponIsPutAway) return;

            events.onInputReload.Invoke(context);
        }

        /// <summary>
        /// When the weapon needs to be fired
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnInputShoot(InputAction.CallbackContext context)
        {
            bool value = context.ReadValue<float>() > 0;
            if(values.needToReleaseTrigger && !value) values.needToReleaseTrigger = false;

            if(WeaponController.WeaponIsPutAway) return;

            input.shoot = value;
            events.onInputShoot.Invoke(context);
        }

        /// <summary>
        /// When the weapon needs to zoom
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnInputZoom(InputAction.CallbackContext context)
        {
            if(WeaponController.WeaponIsPutAway) return;

            input.zoom = context.ReadValue<float>() > 0;
            events.onInputZoom.Invoke(context);
        }

        #endregion

        private void OnDestroy()
        {
            events.onDestroy.Invoke();
        }

        [System.Serializable]
        public class Components
        {
            [Tooltip("The statemachine of the weapon")]
            public StateMachine stateMachine;
            [Tooltip("The associated weapon controller of the weapon")]
            public WeaponController weaponController;
            [Tooltip("The crosshair the weapon uses")]
            public Crosshair crosshair;
            [Tooltip("The weapon projectile SO")]
            public Projectile projectile;
            [Tooltip("The recoil data of the weapon")]
            public WeaponRecoil recoil;
            [Tooltip("The point at which the projectile is created")]
            public Transform projectileInstantiatePoint;
            [Tooltip("The transform of the hand left position/rotation")]
            public Transform handLeftTransform;
            [Tooltip("The transform of the hand right position/rotation")]
            public Transform handRightTransform;
            [Tooltip("The weapon model transform")]
            public Transform modelTransform;
            [Tooltip("The muzzle flash vfx of the weapon")]
            public VisualEffect vfxMuzzleFlash;
        }

        [System.Serializable]
        public class Events
        {
            [Tooltip("Triggerd when the weapon gameobject gets destroyed")]
            public UnityEvent onDestroy;
            [Tooltip("When the weapon fires but is empty")]
            public UnityEvent onEmpty;
            [Tooltip("Triggerd when the weapon hits a gameobject")]
            public UnityEvent<GameObject> onHit;
            [Tooltip("Triggerd when the weapon shoots a projectile")]
            public UnityEvent onFiredProjectile;
            [Tooltip("When a reload gets started")]
            public UnityEvent onReloadStart;
            [Tooltip("When a reload gets finished")]
            public UnityEvent onReloadFinished;
            [Tooltip("Triggerd when the weapon zooms in/out. Value has range of 0 to 1 with 1 being completely zoomed in")]
            public UnityEvent<float> onZoom;
            [Tooltip("Triggerd when the weapon is jumped with")]
            public UnityEvent<bool> onJumping;
            [Tooltip("Triggerd when weapon is run with")]
            public UnityEvent<bool> onRunning;
            [Tooltip("Triggerd when weapon is walked with")]
            public UnityEvent<bool> onWalking;

            [Header("Input")]
            [Tooltip("Triggerd when the weapon wants to reload")]
            public UnityEvent<InputAction.CallbackContext> onInputReload;
            [Tooltip("Triggerd when the weapon wants to shoot")]
            public UnityEvent<InputAction.CallbackContext> onInputShoot;
            [Tooltip("Triggerd when the weapon wants to zoom")]
            public UnityEvent<InputAction.CallbackContext> onInputZoom;
        }

        [System.Serializable]
        public class Input
        {
            [Tooltip("Does the weapon want to be shooting?")]
            public bool shoot;
            [Tooltip("Does the weapon want to be zooming?")]
            public bool zoom;
            [Tooltip("Input of the mouse")]
            public Vector2 lookDirection;
        }

        [System.Serializable]
        public class Values
        {
            [Tooltip("The display name of the weapon")]
            public string displayName = "Weapon";
            [Tooltip("Firemode of the weapon")]
            public FireMode fireMode;
            [Tooltip("If firemoDe is burst, the amount of shots it is allowed to fire before having to pull the trigger again")]
            public int burstFireAmount;
            [Tooltip("The max amount of projectiles a magazine can hold")]
            public int magazineMaxAmmo = 32;
            [Tooltip("The max ammo count of the weapon.")]
            public int maxAmmo = 128;
            [Tooltip("How fast the weapon can shoot per minute")]
            public int roundsPerMinute = 600;
            [Tooltip("How much the weapon affects movement. (Faster -1 / slower +1)")]
            public float movementWeightPenalty;
            [Tooltip("How much the weapon projectile spreads from center when fired from hip")]
            public Vector2 projectileSpread;
            [Tooltip("Should the projectile spread be added when the weapon is zoomed in? (example: true for shotgun)")]
            public bool projectileSpreadADSFire;
            [Tooltip("How far the weapon can shoot in meters")]
            public float maxShootingRange = 99;

            [Tooltip("The time in seconds it takes to equip / deequip this weapon")]
            public float equippingTime = 1;
            [Tooltip("The time in seconds it takes to reload the weapon")]
            public float reloadTime = 1;
            [Tooltip("The time in seconds it takes to zoom the weapon")]
            public float zoomTime = 1;

            [Header("Visual")]
            public float visualPositionSmoothing = 10;
            public float visualRotationSmoothing = 10;

            [Header("Visual Firing")]
            public Vector3AnimationCurve hipFireTranslationCurve;
            [Tooltip("The random range applied to the curve")]
            public Vector2 hipFireTranslationCurveRandomRange = new Vector2(-1, 1);
            public Vector3AnimationCurve hipFireRotationCurve;
            public Vector2 hipFireRotationCurveRandomRange = new Vector2(0.8f, 1);
            public Vector3AnimationCurve adsFireTranslationCurve;
            [Tooltip("The random range applied to the curve")]
            public Vector2 adsFireTranslationRandomRange = new Vector2(-1, 1);
            public Vector3AnimationCurve adsFireRotationCurve;
            public Vector2 adsFireRotationCurveRandomRange = new Vector2(-1, 1);

            [Header("Visual Zooming")]
            [Tooltip("The FOV of the camera based on zoom")]
            public AnimationCurve cameraFOVCurve = new AnimationCurve(new Keyframe[] { new Keyframe() { time = 0, value = 90 }, new Keyframe() { time = 1, value = 60 } });
            [Tooltip("The local position of the weapon when zoomed out")]
            public Vector3 hipFirePosition = new Vector3(0.104f, -0.19f, 0.042f);
            [Tooltip("The local euler angle of the weapon when zoomed out")]
            public Vector3 hipFireRotation;
            [Tooltip("The local position of the weapon when zoomed in")]
            public Vector3 adsFirePosition = new Vector3(0, -0.15f, -0.125f);
            [Tooltip("The local euler angle of the weapon when zoomed in")]
            public Vector3 adsFireRotation;

            [Header("Visual Sway")]
            public bool swayTranslation = true;
            [Tooltip("x = amount, y = maxAmount, z = smoothAmount")]
            public Vector3 swayTranslationHipFire = new Vector3(0.05f, 0.05f, 10);
            [Tooltip("x = amount, y = maxAmount, z = smoothAmount")]
            public Vector3 swayTranslationADSFire = new Vector3(0.005f, 0, 0);
            [Tooltip("How much the weapon sways sideways when moving left/right. Clamped between swayTranslationZoomedOut.x!")]
            public float swayTranslationSidewaysHipFire = 0.02f;
            [Tooltip("How much the weapon sways sideways when moving left/right. Clamped between swayTranslationZoomedOut.x!")]
            public float swayTranslationSidewaysADSFire = 0.005f;
            [Space]
            public bool swayRotation = true;
            [Tooltip("x = amount, y = maxAmount, z = smoothAmount")]
            public Vector3 swayRotationHipFire = new Vector3(10f, 10f, 10);
            [Tooltip("x = amount, y = maxAmount, z = smoothAmount")]
            public Vector3 swayRotationADSFire = new Vector3(1f, 1f, 10);

            [Header("Visual Bobbing")]
            public bool bobbing = true;
            public float bobbingSpeedIdle = 1;
            public Vector3AnimationCurve bobCurveIdlePosition;
            public Vector3AnimationCurve bobCurveIdleRotation;
            public float bobbingSpeedWalking = 1;
            public Vector3AnimationCurve bobCurveWalkingPosition;
            public Vector3AnimationCurve bobCurveWalkingRotation;
            public float bobbingSpeedRunning = 1;
            public Vector3AnimationCurve bobCurveRunningPosition;
            public Vector3AnimationCurve bobCurveRunningRotation;
            public float bobbingSpeedZoomedIn = 1;
            public Vector3AnimationCurve bobCurveADSFirePosition;
            public Vector3AnimationCurve bobCurveADSFireRotation;

            [Header("Visual Running")]
            [Tooltip("The position of the weapon model when running")]
            public Vector3 runningPosition;
            [Tooltip("The rotation of the weapon model when running")]
            public Vector3 runningRotation;

            [Header("Visual Jumping")]
            [Tooltip("The translation of the weapon model in hip fire on jumping (max 3 sec)")]
            public Vector3AnimationCurve hipFireModelOnJumpTranslation;
            [Tooltip("The translation of the weapon model in hip fire on landing from jump (max 3 sec)")]
            public Vector3AnimationCurve hipFireModelOnGroundTranslation;
            [Tooltip("The translation of the weapon model in ads fire on jumping (max 3 sec)")]
            public Vector3AnimationCurve adsFireModelOnJumpTranslation;
            [Tooltip("The translation of the weapon model in ads fire on landing from jump (max 3 sec")]
            public Vector3AnimationCurve adsFireModelOnGroundTranslation;

            [Header("Ref")]
            public string refCurrentState;


            /// <summary>
            /// Can the weapon fire?
            /// </summary>
            public bool CanShoot => !needToReleaseTrigger && !isReloading && !IsRunning && magazineCurrentAmmo > 0 && timeSinceLastShot > (1f / (roundsPerMinute / 60));
            /// <summary>
            /// Can the weapon reload?
            /// </summary>
            public bool CanReload => currentAmmo > 0 && !isReloading && magazineCurrentAmmo < magazineMaxAmmo;
            /// <summary>
            /// Can the weapon zoom?
            /// </summary>
            public bool CanZoom => !isReloading && !isRunning;
            /// <summary>
            /// Is the weapon running?
            /// </summary>
            public bool IsRunning => isRunning;
            /// <summary>
            /// Is the weapon shooting?
            /// </summary>
            public bool IsShooting => isShooting;
            /// <summary>
            /// Is the weapon magazine empty?
            /// </summary>
            public bool IsMagazineEmpty => magazineCurrentAmmo <= 0;
            /// <summary>
            /// Is the weapon zoomed in or in progress of zooming in?
            /// </summary>
            public bool IsZooming => isZoomedIn || isZoomingIn;
            /// <summary>
            /// Current ammo + magazine ammo count
            /// </summary>
            public int CurrentTotalAmmo => currentAmmo + magazineCurrentAmmo;
            /// <summary>
            /// The amount of shots per second
            /// </summary>
            public float FireSpeed => roundsPerMinute / 60;

            /// <summary>
            /// Is the weapon equipped?
            /// </summary>
            [HideInInspector] public bool isEquipped;
            /// <summary>
            /// Is the weapon currently equipping?
            /// </summary>
            [HideInInspector] public bool isEquipping;
            /// <summary>
            /// Is the weapon currently jumped with?
            /// </summary>
            [HideInInspector] public bool isJumping;
            /// <summary>
            /// Is the weapon currently reloading
            /// </summary>
            [HideInInspector] public bool isReloading;
            /// <summary>
            /// Is the weapon currently run with?
            /// </summary>
            [HideInInspector] public bool isRunning;
            /// <summary>
            /// Is the weapon currently shooting?
            /// </summary>
            [HideInInspector] public bool isShooting;
            /// <summary>
            /// Is the weapon currently walked with?
            /// </summary>
            [HideInInspector] public bool isWalking;
            /// <summary>
            /// Is the weapon zoomed in? (All the way)
            /// </summary>
            [HideInInspector] public bool isZoomedIn;
            /// <summary>
            /// Is the weapon currently zooming in?
            /// </summary>
            [HideInInspector] public bool isZoomingIn;
            /// <summary>
            /// Does the weapon need to release the trigger?
            /// </summary>
            [HideInInspector] public bool needToReleaseTrigger;
            /// <summary>
            /// The current ammo of the weapon (excluding magazineCurrentAmmo)
            /// </summary>
            [HideInInspector] public int currentAmmo;
            /// <summary>
            /// The current ammo in the magazine
            /// </summary>
            [HideInInspector] public int magazineCurrentAmmo;
            /// <summary>
            /// Time in seconds since last weapon projectile shot
            /// </summary>
            [HideInInspector] public float timeSinceLastShot = 60;
            /// <summary>
            /// Time in seconds left to equip weapon
            /// </summary>
            [HideInInspector] public float equippingTimer;
            /// <summary>
            /// Value depicting equip (1) / deequip (0) state
            /// </summary>
            [HideInInspector] public float equippingTimerNormalized;
        }
    }
}
