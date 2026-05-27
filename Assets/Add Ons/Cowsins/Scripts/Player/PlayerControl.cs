using UnityEngine;
using FishNet.Object;

namespace cowsins
{
    /// <summary>
    /// Controls the controllable state of the player.
    /// If non-controllable ( controllable == false ), the player won't be able to move or perform any action.
    /// Integrates FishNet ownership checks to ensure only the local owner can control this player.
    /// </summary>
    public class PlayerControl : NetworkBehaviour, IPlayerControlProvider
    {
        public bool IsControllable => controllable;
        public bool IsMovementControllable => movementControllable;

        private bool controllable = true;
        private bool movementControllable = true;

        private IPlayerStatsProvider playerStatusProvider;

        private void Awake()
        {
            playerStatusProvider = GetComponent<IPlayerStatsProvider>();
            // Disable CompassElements before UI is disabled to prevent Start() race condition
            foreach (var compass in this.transform.root.GetComponentsInChildren<CompassElement>(true))
                compass.enabled = false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log($"OnStartClient fired — IsOwner: {IsOwner}");

            Transform root = transform.root;
            Camera cam = root.GetComponentInChildren<Camera>();
            Transform playerGraphics = root.Find("PlayerGraphics");

            if (!IsOwner)
            {
                // Set camera children to Weapons layer so owner can't see them
                if (cam != null)
                {
                    foreach (Transform child in cam.GetComponentsInChildren<Transform>())
                        child.gameObject.layer = LayerMask.NameToLayer("Weapons");
                }

                // Set PlayerGraphics to ObserverView so remote clients can see it
                if (playerGraphics != null)
                {
                    foreach (Transform child in playerGraphics.GetComponentsInChildren<Transform>(true))
                        child.gameObject.layer = LayerMask.NameToLayer("ObserverView");
                    playerGraphics.gameObject.layer = LayerMask.NameToLayer("ObserverView");
                }

                DisableForNonOwner();
                return;
            }
            else
            {
                foreach (Transform child in playerGraphics.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer("LocalView");
                playerGraphics.gameObject.layer = LayerMask.NameToLayer("LocalView");

            }

            // Owner - exclude Weapons and ObserverView from their own camera
            /*if (cam != null)
            {
                cam.cullingMask &= ~(1 << LayerMask.NameToLayer("Weapons"));
                cam.cullingMask &= ~(1 << LayerMask.NameToLayer("ObserverView"));
            }*/

            GrantControl();
        }


        /// <summary>
        /// Disables all components that should only run on the local owner.
        /// Called automatically for non-owner clients.
        /// </summary>
        private void DisableForNonOwner()
        {
            Transform root = transform.root;

            // Disable Input Manager
            Transform inputManagerObj = root.Find("Input Manager");
            if (inputManagerObj != null) inputManagerObj.gameObject.SetActive(false);

            // Disable Camera GameObject entirely
            Transform camera = root.Find("Camera");
            if (camera != null) camera.gameObject.SetActive(false);

            // Disable UI
            Transform playerUI = root.Find("PlayerUI");
            if (playerUI != null) playerUI.gameObject.SetActive(false);

            // Disable specific components on Player Controller, keeping the GameObject active
            Transform playerController = root.Find("Player Controller");
            if (playerController != null)
            {
                // Disable local-only scripts
                DisableComponent<PlayerMovement>(playerController);
                DisableComponent<WeaponController>(playerController);
                DisableComponent<InteractManager>(playerController);
                DisableComponent<CameraEffects>(playerController);
                DisableComponent<WeaponEffects>(playerController);
                DisableComponent<WeaponStates>(playerController);
                DisableComponent<WeaponAnimator>(playerController);
                DisableComponent<PlayerStates>(playerController);

                // Keep active: PlayerStats, PlayerControl, PlayerStates, 
                //              PlayerMultipliers, PlayerDependencies
                // Keep active: CapsuleCollider, Rigidbody
            }
        }

        private void DisableComponent<T>(Transform target) where T : MonoBehaviour
        {
            T component = target.GetComponent<T>();
            if (component != null) component.enabled = false;
        }

        /***************************************** GLOBAL/CORE CONTROL *************************************************/

        /// <summary>
        /// Forces the player to be controlled. CheckIfCanGrantControl() is recommended instead.
        /// </summary>
        public void GrantControl()
        {
            if (!IsOwner) return;
            controllable = true;
            GrantMovementControl();
        }

        /// <summary>
        /// Disallows the player from being controlled.
        /// </summary>
        public void LoseControl()
        {
            if (!IsOwner) return;
            controllable = false;
        }

        /// <summary>
        /// Toggles the controllable state of the player.
        /// </summary>
        public void ToggleControl()
        {
            if (!IsOwner) return;
            controllable = !controllable;
        }

        /// <summary>
        /// Checks if the game is paused or the player is dead before granting control.
        /// </summary>
        public void CheckIfCanGrantControl()
        {
            if (!IsOwner) return;
            if (PauseMenu.isPaused || playerStatusProvider?.IsDead == true) return;
            GrantControl();
        }

        /***************************************** MOVEMENT CONTROL *************************************************/

        /// <summary>
        /// Allows movement. Has no effect if Global Control is disabled.
        /// </summary>
        public void GrantMovementControl()
        {
            if (!IsOwner) return;
            movementControllable = true;
        }

        /// <summary>
        /// Disallows player movement. Independent of the Global Control system.
        /// </summary>
        public void LoseMovementControl()
        {
            if (!IsOwner) return;
            movementControllable = false;
        }

        /// <summary>
        /// Toggles the movement controllable state of the player.
        /// </summary>  
        public void ToggleMovementControl()
        {
            if (!IsOwner) return;
            movementControllable = !movementControllable;
        }
    }
}