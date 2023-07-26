using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SLIDDES.Project.FPS
{
    /// <summary>
    /// Main class for interacting with weapons
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        /// <summary>
        /// Can the weapon controller switch weapons?
        /// </summary>
        public bool CanSwitchWeapons => !isReplacingWeapon;//!IsSwitchingWeapon;
        /// <summary>
        /// Is the weapon controller currently switching weapons?
        /// </summary>
        public bool IsSwitchingWeapon => isSwithingWeapon;
        /// <summary>
        /// Is the weapon put away?
        /// </summary>
        public bool WeaponIsPutAway => weaponIsPutAway;
        /// <summary>
        /// The current equipped & active weapon
        /// </summary>
        public Weapon CurrentWeapon
        {
            get
            {                
                return values.weapons[currentWeaponIndex];
            }
        }

        public Values values;
        public Components components;
        public Events events;
        public Input input;

        /// <summary>
        /// Is the weapon controller grounded?
        /// </summary>
        [HideInInspector] public bool isGrounded;
        /// <summary>
        /// The current velocity of the controller
        /// </summary>
        [HideInInspector] public Vector3 velocity;

        /// <summary>
        /// If the weapon is put away
        /// </summary>
        private bool weaponIsPutAway;
        /// <summary>
        /// Is the weapon controller currently replacing a weapon?
        /// </summary>
        private bool isReplacingWeapon;
        /// <summary>
        /// Is the weapon controller currently switching weapons?
        /// </summary>
        private bool isSwithingWeapon;
        /// <summary>
        /// The index of the current weapon in weapons
        /// </summary>
        private int currentWeaponIndex;
        /// <summary>
        /// When switching weapon, this is gonna become the new weaponIndex
        /// </summary>
        private int newWeaponIndex;
        /// <summary>
        /// Timer to await when allowed to switch again (prevents animation bug)
        /// </summary>
        private float weaponSwitchCooldownTimer;
        /// <summary>
        /// Coroutine for switching weapons
        /// </summary>
        private Coroutine coroutineSwitchWeapons;

        public void Initialize()
        {
            // Clear old
            foreach(Transform child in components.parentWeaponsTranform)
            {
                Destroy(child.gameObject);
            }
            values.weapons.Clear();

            AddWeapon(values.prefabStarterWeapon);
            SwitchWeapons(0);
        }

        // Start is called before the first frame update
        void Start()
        {
            if(values.initializeOnStart) Initialize();
        }

        // Update is called once per frame
        void Update()
        {
            weaponSwitchCooldownTimer = Mathf.Clamp(weaponSwitchCooldownTimer - Time.deltaTime, 0, 60);

            UpdateCurrentWeaponTransforms();
        }

        /// <summary>
        /// Add a weapon to the controller
        /// </summary>
        /// <param name="weaponPrefab">The weapon gameobject prefab with an weapon component attached</param>
        public void AddWeapon(GameObject weaponPrefab)
        {
            // Cant add weapon if replacing weapon
            if(isReplacingWeapon) return;

            if(weaponPrefab == null)
            {
                Debug.LogError("Tried to add weapon with null gameobject!");
                return;
            }

            Weapon prefabWeapon = weaponPrefab.GetComponent<Weapon>();
            if(prefabWeapon == null)
            {
                Debug.LogError("Tried adding weapon prefab with no weapon component attached!");
                return;
            }

            // Check if free weapon slot available
            if(values.weapons.Count >= values.maxWeapons)
            {
                // Max limit reached, replace current equipped weapon with new weapon
                ReplaceCurrentWeapon(weaponPrefab);
            }
            else
            {
                // Weapon slot available, add
                InstantiateWeapon(weaponPrefab, true);
            }
        }

        /// <summary>
        /// Bring out the current weapon if put away
        /// </summary>
        public void BringOutWeapon()
        {
            if(!weaponIsPutAway) return;

            weaponIsPutAway = false;
            CurrentWeapon.StateMachine.NewState("equip");
        }

        /// <summary>
        /// Check if the weaponController contains a weapon
        /// </summary>
        /// <param name="weapon">Weapon Component</param>
        /// <returns>True if it contains the same type of weapon, false if not</returns>
        public bool ContainsWeapon(Weapon weapon)
        {
            return values.weapons.FirstOrDefault(x => x.values.displayName == weapon.values.displayName) != null;
        }

        /// <summary>
        /// Get a weapon based on displayName
        /// </summary>
        /// <param name="displayName">The displayName of the weapon</param>
        /// <returns>Weapon if weapons contains value equal to displayName</returns>
        public Weapon GetWeapon(string displayName)
        {
            return values.weapons.FirstOrDefault(x => x.values.displayName == displayName);
        }

        /// <summary>
        /// Triggerd when jumping with weapon
        /// </summary>
        /// <param name="value"></param>
        public void OnWeaponJump(bool value)
        {
            if(!value) return;

            events.onWeaponJumping.Invoke(value);
            if(CurrentWeapon != null) CurrentWeapon.events.onJumping.Invoke(value);
        }

        /// <summary>
        /// Triggerd when landing with weapon
        /// </summary>
        public void OnWeaponLandGround()
        {
            events.onWeaponJumping.Invoke(false);
            if(CurrentWeapon != null) CurrentWeapon.events.onJumping.Invoke(false);
        }

        /// <summary>
        /// Triggerd if running with the weapon
        /// </summary>
        /// <param name="value"></param>
        public void OnWeaponRunning(bool value)
        {
            events.onWeaponRunning.Invoke(value);
            if(CurrentWeapon != null) CurrentWeapon.events.onRunning.Invoke(value);
        }

        /// <summary>
        /// Triggerd if walking with the weapon
        /// </summary>
        /// <param name="value"></param>
        public void OnWeaponWalking(bool value)
        {
            events.onWeaponWalking.Invoke(value);
            if(CurrentWeapon != null) CurrentWeapon.events.onWalking.Invoke(value);
        }

        /// <summary>
        /// Put away the current weapon
        /// </summary>
        public void PutAwayWeapon()
        {
            if(weaponIsPutAway) return;

            weaponIsPutAway = true;
            CurrentWeapon.StateMachine.NewState("deequip");
        }

        /// <summary>
        /// Replace current equipped weapon with a new weapon
        /// </summary>
        /// <param name="weaponPrefab"></param>
        public void ReplaceCurrentWeapon(GameObject weaponPrefab)
        {
            // Cant replace if still replacing
            if(isReplacingWeapon) return;

            StartCoroutine(ReplaceCurrentWeaponAsync(weaponPrefab));
        }

        /// <summary>
        /// Set the isgrounded value
        /// </summary>
        /// <param name="value"></param>
        public void SetIsGrounded(bool value)
        {
            isGrounded = value;
        }

        /// <summary>
        /// Set the velocity
        /// </summary>
        /// <param name="value"></param>
        public void SetVelocity(Vector3 value)
        {
            velocity = value;
        }

        /// <summary>
        /// Switch to a weapon
        /// </summary>
        /// <param name="switchForward">True if switching to the next weapon in weapons list</param>
        public void SwitchWeapons(bool switchForward)
        {
            int weaponIndex = IsSwitchingWeapon ? newWeaponIndex : currentWeaponIndex;
            int index = weaponIndex + (switchForward ? 1 : -1);
            // Loop index if out of range
            if(index < 0) index = values.weapons.Count - 1;
            if(index > values.weapons.Count - 1) index = 0;

            SwitchWeaponInternal(index);
        }

        /// <summary>
        /// Switch to a weapon
        /// </summary>
        /// <param name="index">The index of the weapon in weapons</param>
        public void SwitchWeapons(int index)
        {
            // Check index range
            if(index < 0 || index > values.weapons.Count - 1)
            {
                throw new System.ArgumentOutOfRangeException("index");
            }

            SwitchWeaponInternal(index);
        }


        /// <summary>
        /// Instantiate the weapon prefab
        /// </summary>
        /// <param name="weaponPrefab"></param>
        /// <param name="addToWeapons">Add the created weapon to values.weapons list?</param>
        private Weapon InstantiateWeapon(GameObject weaponPrefab, bool addToWeapons)
        {
            GameObject a = Instantiate(weaponPrefab, components.parentWeaponsTranform);
            Weapon weapon = a.GetComponent<Weapon>();
            if(addToWeapons) values.weapons.Add(weapon);
            weapon.Initialize(this);
            a.transform.SetLayerRecursively(components.parentWeaponsTranform.gameObject.layer);
            a.SetActive(false); // hide after creating
            return weapon;
        }

        private IEnumerator ReplaceCurrentWeaponAsync(GameObject weaponPrefab)
        {
            isReplacingWeapon = true;

            // Deequip weapon
            CurrentWeapon.Deequip();
            events.onWeaponDeequip.Invoke(CurrentWeapon);
            yield return new WaitForSeconds(((Deequip)CurrentWeapon.StateMachine.states["deequip"]).Time);

            // Remove weapon
            CurrentWeapon.Remove();

            // Add new weapon
            values.weapons[currentWeaponIndex] = InstantiateWeapon(weaponPrefab, false);
            
            // No longer replacing, false in order to switch weapons
            isReplacingWeapon = false;

            // Equip new weapon
            SwitchWeapons(currentWeaponIndex);

            yield break;
        }

        private IEnumerator SwitchWeaponAsync(int index)
        {
            isSwithingWeapon = true;
            newWeaponIndex = index;

            // First deequip Current weapon if equipped & not switching to current weapon
            if(CurrentWeapon.values.isEquipped && currentWeaponIndex != index)
            {
                // Deequip weapon
                CurrentWeapon.Deequip();
                events.onWeaponDeequip.Invoke(CurrentWeapon);
                yield return new WaitForSeconds(((Deequip)CurrentWeapon.StateMachine.states["deequip"]).Time);
                events.onWeaponDeequiped.Invoke(CurrentWeapon);
            }

            // Equip new weapon
            currentWeaponIndex = index;
            if(!CurrentWeapon.values.isEquipped)
            {
                CurrentWeapon.Equip();
                events.onWeaponEquip.Invoke(CurrentWeapon);
                yield return new WaitForSeconds(((Equip)CurrentWeapon.StateMachine.states["equip"]).Time);
                events.onWeaponEquipped.Invoke(CurrentWeapon);
            }

            isSwithingWeapon = false;
            yield break;
        }

        /// <summary>
        /// Internal check for switching
        /// </summary>
        /// <param name="index">The weapon index to switch to in weapons</param>
        private void SwitchWeaponInternal(int index)
        {
            if(!CanSwitchWeapons || weaponSwitchCooldownTimer > 0) return;
            weaponSwitchCooldownTimer = 0.1f;

            if(coroutineSwitchWeapons != null) StopCoroutine(coroutineSwitchWeapons);
            coroutineSwitchWeapons = StartCoroutine(SwitchWeaponAsync(index));
        }

        /// <summary>
        /// Update (animation) transforms assigned based on current weapon
        /// </summary>
        private void UpdateCurrentWeaponTransforms()
        {
            if(CurrentWeapon == null || !CurrentWeapon.gameObject.activeSelf) return;

            // Weapon origin point
            components.currentWeaponTransform.position = CurrentWeapon.transform.position;
            components.currentWeaponTransform.rotation = CurrentWeapon.transform.rotation;
            // Hand left
            components.currentWeaponHandLeftTransform.position = CurrentWeapon.components.handLeftTransform.position;
            components.currentWeaponHandLeftTransform.rotation = CurrentWeapon.components.handLeftTransform.rotation;
            // Hand right
            components.currentWeaponHandRightTransform.position = CurrentWeapon.components.handRightTransform.position;
            components.currentWeaponHandRightTransform.rotation = CurrentWeapon.components.handRightTransform.rotation;
        }

        #region Input

        public void OnInputMovement(InputAction.CallbackContext context)
        {
            input.movement = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// When the weapon gets switched
        /// </summary>
        /// <param name="context"></param>
        public void OnInputSwitch(InputAction.CallbackContext context)
        {
            if(context.performed)
            {
                // Check if interrupting running
                if(CurrentWeapon.components.stateMachine.CurrentState == CurrentWeapon.components.stateMachine.states["running"])
                {
                    events.onWeaponInterruptRunning.Invoke(CurrentWeapon);
                }

                SwitchWeapons(true);
            }
        }

        #endregion

        private void OnGUI()
        {
            if(!values.showGUI) return;

            GUI.Label(new Rect(0, 0, 100, 100), new GUIContent($"{CurrentWeapon.StateMachine.CurrentState} \n {CurrentWeapon.values.magazineCurrentAmmo}/{CurrentWeapon.values.currentAmmo}"));
        }

        [System.Serializable]
        public class Components
        {
            [Tooltip("Where all the weapon prefabs are parented under")]
            public Transform parentWeaponsTranform;
            [Tooltip("Copies the current weapon transform values (used in animation)")]
            public Transform currentWeaponTransform;
            [Tooltip("Copies the current weapon hand left transform values (used in animation)")]
            public Transform currentWeaponHandLeftTransform;
            [Tooltip("Copies the current weapon hand right transform values (used in animation)")]
            public Transform currentWeaponHandRightTransform;
            [Tooltip("Camera belonging to this weapon controller")]
            public Camera camera;
            [Tooltip("The weapon camera controller component for the weapon")]
            public WeaponCameraController weaponCameraController;
            [Tooltip("The transform of the fp model")]
            public Transform modelFirstPersonTransform;
            [Tooltip("Animator component of the first person models")]
            public Animator animatorFirstPerson;
            [Tooltip("Animator component of the third person models")]
            public Animator animatorThirdPerson;
        }

        [System.Serializable]
        public class Events
        {
            [Tooltip("Triggerd when the weapon ammo changes")]
            public UnityEvent<Weapon> onWeaponAmmoChange;
            [Tooltip("Triggerd when current weapon shot")]
            public UnityEvent<Weapon> onWeaponShot;
            [Tooltip("Triggerd when current weapon gets deequipped")]
            public UnityEvent<Weapon> onWeaponDeequip;
            [Tooltip("Triggerd when the current weapon is deequiped")]
            public UnityEvent<Weapon> onWeaponDeequiped;
            [Tooltip("Triggerd when a new weapon gets equipped")]
            public UnityEvent<Weapon> onWeaponEquip;
            [Tooltip("Triggerd when the weapon is equipped")]
            public UnityEvent<Weapon> onWeaponEquipped;
            [Tooltip("Triggerd when current weapon reloads")]
            public UnityEvent<Weapon> onWeaponReload;
            [Tooltip("Triggerd when the weapon crosshair has to be size small")]
            public UnityEvent<Weapon> onWeaponCrosshairSmall;
            [Tooltip("Triggerd when the weapon crosshair has to be size normal")]
            public UnityEvent<Weapon> onWeaponCrosshairNormal;
            [Tooltip("Triggerd when the weapon crosshair has to be size big")]
            public UnityEvent<Weapon> onWeaponCrosshairBig;
            [Tooltip("Triggerd when the weapon wants to interrupt the running state")]
            public UnityEvent<Weapon> onWeaponInterruptRunning;
            [Space]

            [Tooltip("Triggerd when jumping with the weapon")]
            public UnityEvent<bool> onWeaponJumping;
            [Tooltip("Triggerd when running with the weapon")]
            public UnityEvent<bool> onWeaponRunning;
            [Tooltip("Triggerd when walking with the weapon")]
            public UnityEvent<bool> onWeaponWalking;

        }

        [System.Serializable]
        public class Input
        {
            public Vector2 movement;
        }

        [System.Serializable]
        public class Values
        {
            [Tooltip("Initialize the controller on start")]
            public bool initializeOnStart = true;
            [Tooltip("The maximum amount of weapons that can be held")]
            public int maxWeapons = 2;
            [Tooltip("The prefab of the starter weapon")]
            public GameObject prefabStarterWeapon;
            [Tooltip("List of the current equipped weapons")]
            public List<Weapon> weapons = new List<Weapon>();

            [Space]

            public bool showGUI;            
        }
    }
}
