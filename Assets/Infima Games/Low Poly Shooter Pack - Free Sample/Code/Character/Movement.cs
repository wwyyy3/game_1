using System.Linq;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Audio Clips")]

        [Tooltip("The audio clip that is played while walking.")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("The audio clip that is played while running.")]
        [SerializeField]
        private AudioClip audioClipRunning;

        [Header("Speeds")]

        [SerializeField]
        private float speedWalking = 5.0f;

        [Tooltip("How fast the player moves while running."), SerializeField]
        private float speedRunning = 9.0f;

        [Header("Step Settings")]
        [Tooltip("Maximum step height that can be climbed.")]
        [SerializeField]
        private float stepHeight = 0.5f;

        [Tooltip("Distance for step detection in front of the character.")]
        [SerializeField]
        private float stepCheckDistance = 0.5f;

        #endregion

        #region PROPERTIES

        // Current speed.
        public Vector3 Velocity
        {
            get => rigidBody.linearVelocity;
            set => rigidBody.linearVelocity = value;
        }

        #endregion

        #region FIELDS

        private Rigidbody rigidBody;
        private CapsuleCollider capsule;
        private AudioSource audioSource;
        private bool grounded;
        private CharacterBehaviour playerCharacter;
        private WeaponBehaviour equippedWeapon;

        /// <summary>
        /// Array for raycast hits used to detect contact with the ground.
        /// </summary>
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        #endregion

        #region UNITY FUNCTIONS

        protected override void Awake()
        {
            // Get the player character reference.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        protected override void Start()
        {
            // Initialize the Rigidbody and freeze rotation.
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

            // Cache the CapsuleCollider.
            capsule = GetComponent<CapsuleCollider>();

            // Set up the AudioSource.
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;

            // If a CharacterController exists, set its stepOffset (for step climbing).
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.stepOffset = 1f;
            }

            rigidBody.linearDamping = 0f;
        }

        /// <summary>
        /// Use SphereCast to detect if the character is on the ground.
        /// </summary>
        private void OnCollisionStay()
        {
            Bounds bounds = capsule.bounds;
            Vector3 extents = bounds.extents;
            float radius = extents.x - 0.01f;

            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
                groundHits, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);

            if (!groundHits.Any(hit => hit.collider != null && hit.collider != capsule))
                return;

            // Clear the hit array.
            for (var i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();

            grounded = true;
        }

        protected override void FixedUpdate()
        {
            // Move the character.
            //MoveCharacter();

            // Detect and handle step climbing based on the current
            // horizontal movement direction.
            Vector3 horizontalMovement = new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z);
            if (horizontalMovement.magnitude > 0.1f)
            {
                HandleStepClimb(horizontalMovement.normalized);
            }

            // Reset the grounded state each frame, it will be re-detected
            // by OnCollisionStay.
            grounded = false;
        }

        protected override void Update()
        {
            // Get the currently equipped weapon (if needed).。
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();

            // Play footstep sounds. When the character is grounded and moving,
            // choose the corresponding audio clip based on the current speed.
            PlayFootstepSounds();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Calculate and update the character's movement speed based on player input.
        /// </summary>
        private void MoveCharacter()
        {
            Vector2 frameInput = playerCharacter.GetInputMovement();
            var movement = new Vector3(frameInput.x, rigidBody.linearVelocity.y, frameInput.y);

            if (playerCharacter.IsRunning())
                movement *= speedRunning;
            else
                movement *= speedWalking;

            // Convert local direction to world direction.
            movement = transform.TransformDirection(movement);

            Velocity = new Vector3(movement.x, rigidBody.linearVelocity.y, movement.z);
        }

        /// <summary>
        /// Play footstep sounds based on the character's movement 
        /// and whether it is on the ground.
        /// </summary>
        private void PlayFootstepSounds()
        {
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }

        /// <summary>
        /// Detect if there is a climbable step in front, and if conditions are met, 
        /// move the character upward vertically.
        /// </summary>
        /// <param name="moveDirection">The character's horizontal movement direction (unit vector).</param>
        private void HandleStepClimb(Vector3 moveDirection)
        {
            // Cast a low ray from slightly above the character's bottom to detect obstacles.
            Vector3 lowerOrigin = transform.position + Vector3.up * 0.1f;
            if (Physics.Raycast(lowerOrigin, moveDirection, out RaycastHit lowerHit, stepCheckDistance))
            {
                float obstacleHeight = lowerHit.point.y;
                if (obstacleHeight - transform.position.y <= stepHeight)
                {
                    // Cast a ray from a higher position to check
                    // if the space above the step is clear.
                    Vector3 upperOrigin = transform.position + Vector3.up * (stepHeight + 0.1f);
                    if (!Physics.Raycast(upperOrigin, moveDirection, out RaycastHit upperHit, stepCheckDistance))
                    {
                        // Calculate the height to move upward,
                        // adding a slight offset to ensure smooth step climbing.
                        float stepUpAmount = (obstacleHeight + 0.05f) - transform.position.y;
                        Vector3 newPosition = rigidBody.position + new Vector3(0, stepUpAmount, 0);
                        rigidBody.MovePosition(newPosition);
                    }
                }
            }
        }

        #endregion
    }
}
