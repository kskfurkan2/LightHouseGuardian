using UnityEngine;

namespace Kalender.CameraSystem
{
    public class CameraFocusTrigger : MonoBehaviour
    {
        [Header("Focus Settings")]
        [Tooltip("The target transform the camera should look at.")]
        public Transform targetToLookAt;

        [Tooltip("How long the camera should stay focused on the target.")]
        public float duration = 5.0f;

        [Tooltip("If true, triggers automatically on Start.")]
        public bool triggerOnStart = false;

        private void Start()
        {
            if (triggerOnStart)
            {
                TriggerFocus();
            }
        }

        /// <summary>
        /// Call this method to start the camera focus sequence.
        /// </summary>
        public void TriggerFocus()
        {
            if (CameraFocusController.Instance != null && targetToLookAt != null)
            {
                CameraFocusController.Instance.FocusOn(targetToLookAt, duration);
            }
            else
            {
                if (CameraFocusController.Instance == null)
                    Debug.LogWarning("CameraFocusTrigger: CameraFocusController instance not found in scene!");
                if (targetToLookAt == null)
                    Debug.LogWarning("CameraFocusTrigger: Target to look at is not assigned!", gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                 TriggerFocus();
            }
        }
    }
}
