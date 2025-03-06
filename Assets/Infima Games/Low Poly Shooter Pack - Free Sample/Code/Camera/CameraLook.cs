﻿// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Camera Look. Handles the rotation of the camera.
    /// </summary>
    public class CameraLook : MonoBehaviour
    {
        #region FIELDS SERIALIZED
        
        [Header("Settings")]
        public Vector2 pendingLookInput;

        [Tooltip("Sensitivity when looking around.")]
        [SerializeField]
        public Vector2 sensitivity = new Vector2(1, 1);

        [Tooltip("Minimum and maximum up/down rotation angle the camera can have.")]
        [SerializeField]
        public Vector2 yClamp = new Vector2(-60, 60);

        [Tooltip("Should the look rotation be interpolated?")]
        [SerializeField]
        public bool smooth;

        [Tooltip("The speed at which the look rotation is interpolated.")]
        [SerializeField]
        public float interpolationSpeed = 25.0f;
        
        #endregion
        
        #region FIELDS
        
        /// <summary>
        /// Player Character.
        /// </summary>
        private CharacterBehaviour playerCharacter;
        /// <summary>
        /// The player character's rigidbody component.
        /// </summary>
        private Rigidbody playerCharacterRigidbody;

        /// <summary>
        /// The player character's rotation.
        /// </summary>
        public Quaternion rotationCharacter;
        /// <summary>
        /// The camera's rotation.
        /// </summary>
        public Quaternion rotationCamera;

        #endregion
        
        #region UNITY

        private void Awake()
        {
            //Get Player Character.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
            //Cache the rigidbody.
            playerCharacterRigidbody = playerCharacter.GetComponent<Rigidbody>();
        }
        private void Start()
        {
            //Cache the character's initial rotation.
            rotationCharacter = playerCharacter.transform.localRotation;
            //Cache the camera's initial rotation.
            rotationCamera = transform.localRotation;
        }

        private void LateUpdate()
        {
            LookInput(pendingLookInput);
            pendingLookInput = Vector2.zero;
        }


        public void LookInput(Vector2 lookInput )
        {
            //Frame Input. The Input to add this frame!
            Vector2 frameInput = lookInput;
            //Sensitivity.
            frameInput *= sensitivity;

            //Yaw.
            Quaternion rotationYaw = Quaternion.Euler(0.0f, frameInput.x, 0.0f);
            //Pitch.
            Quaternion rotationPitch = Quaternion.Euler(-frameInput.y, 0.0f, 0.0f);

            //Save rotation. We use this for smooth rotation.
            rotationCamera *= rotationPitch;
            rotationCharacter *= rotationYaw;

            //Local Rotation.
            Quaternion localRotation = transform.localRotation;

            //Smooth.
            if (smooth)
            {
                //Interpolate local rotation.
                localRotation = Quaternion.Slerp(localRotation, rotationCamera, Time.deltaTime * interpolationSpeed);
                //Interpolate character rotation.
                playerCharacterRigidbody.MoveRotation(Quaternion.Slerp(playerCharacterRigidbody.rotation, rotationCharacter, Time.deltaTime * interpolationSpeed));
            }
            else
            {
                //Rotate local.
                localRotation *= rotationPitch;
                //Clamp.
                localRotation = Clamp(localRotation);

                //Rotate character.
                playerCharacterRigidbody.MoveRotation(playerCharacterRigidbody.rotation * rotationYaw);
            }

            //Set.
            transform.localRotation = localRotation;
        }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// Clamps the pitch of a quaternion according to our clamps.
        /// </summary>
        public Quaternion Clamp(Quaternion rotation)
        {
            rotation.x /= rotation.w;
            rotation.y /= rotation.w;
            rotation.z /= rotation.w;
            rotation.w = 1.0f;

            //Pitch.
            float pitch = 2.0f * Mathf.Rad2Deg * Mathf.Atan(rotation.x);

            //Clamp.
            pitch = Mathf.Clamp(pitch, yClamp.x, yClamp.y);
            rotation.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * pitch);

            //Return.
            return rotation;
        }

        #endregion
    }
}