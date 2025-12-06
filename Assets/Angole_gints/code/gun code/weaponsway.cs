using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Com.Kawaiisun.SimpleHostile
{
    public class WeaponSway : MonoBehaviour
    {
        [Header("Weapon Aim")]
        public Vector3 moveOffset;
        public KeyCode activationKey = KeyCode.Mouse1;
        private Vector3 originalPosition;
        public float AimIntensity = 2.5f;
        public float aimPositionSpeed = 10f; // Smooth aim position transition

        [Header("Weapon Motion - Sway")]
        public float intensity = 5f;
        public float smooth = 5f; // Added default value
        private Quaternion origin_rotation;

        [Header("FOV Zoom")]
        public Camera playerCamera;
        public float zoomFOV = 40f;
        public float defaultFOV = 60f;
        public float zoomSpeed = 10f;
        private float originalFOV;

        [Header("Advanced Sway (Optional)")]
        public bool enableIdleSway = true;
        public float idleSwayAmount = 0.5f;
        public float idleSwaySpeed = 1f;

        // Private variables
        private bool isAiming = false;
        private Vector3 targetPosition;
        private float targetFOV;

        private void Start()
        {
            origin_rotation = transform.localRotation;
            originalPosition = transform.localPosition;
            targetPosition = originalPosition;

            if (playerCamera != null)
            {
                originalFOV = playerCamera.fieldOfView;
                targetFOV = originalFOV;
            }
        }

        private void Update()
        {
            // Cache input state
            isAiming = Input.GetKey(activationKey);

            UpdateSway();
            UpdateAimPosition();
            UpdateZoom();
        }

        private void UpdateSway()
        {
            float currentIntensity = isAiming ? AimIntensity : intensity;

            // Mouse input
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Calculate target rotation based on mouse movement
            Quaternion xRotation = Quaternion.AngleAxis(-currentIntensity * mouseX, Vector3.up);
            Quaternion yRotation = Quaternion.AngleAxis(currentIntensity * mouseY, Vector3.right);
            Quaternion targetRotation = origin_rotation * xRotation * yRotation;

            // Add subtle idle sway when not moving mouse
            if (enableIdleSway && Mathf.Abs(mouseX) < 0.01f && Mathf.Abs(mouseY) < 0.01f)
            {
                float idleX = Mathf.Sin(Time.time * idleSwaySpeed) * idleSwayAmount;
                float idleY = Mathf.Cos(Time.time * idleSwaySpeed * 0.8f) * idleSwayAmount * 0.5f;

                Quaternion idleRotation = Quaternion.AngleAxis(idleX, Vector3.up) *
                                        Quaternion.AngleAxis(idleY, Vector3.right);
                targetRotation *= idleRotation;
            }

            // Smooth rotation interpolation
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);
        }

        private void UpdateAimPosition()
        {
            // Set target position based on aiming state
            targetPosition = isAiming ? originalPosition + moveOffset : originalPosition;

            // Smoothly interpolate to target position
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * aimPositionSpeed);
        }

        private void UpdateZoom()
        {
            if (playerCamera == null) return;

            // Set target FOV based on aiming state
            targetFOV = isAiming ? zoomFOV : originalFOV;

            // Smoothly interpolate FOV
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }

        // Utility method to reset weapon position (useful for weapon switching)
        public void ResetWeaponTransform()
        {
            transform.localRotation = origin_rotation;
            transform.localPosition = originalPosition;
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = originalFOV;
            }
        }

        // Method to temporarily disable sway (useful for cutscenes, etc.)
        public void SetSwayEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                ResetWeaponTransform();
            }
        }

        private void OnValidate()
        {
            // Ensure smooth value is never zero to prevent division issues
            if (smooth <= 0) smooth = 1f;
            if (aimPositionSpeed <= 0) aimPositionSpeed = 1f;
            if (zoomSpeed <= 0) zoomSpeed = 1f;
        }
    }
}