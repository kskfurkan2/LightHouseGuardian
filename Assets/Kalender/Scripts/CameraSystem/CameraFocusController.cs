using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Kalender.CameraSystem
{
    public class CameraFocusController : MonoBehaviour
    {
        public static CameraFocusController Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Speed of camera rotation towards the target.")]
        public float focusSpeed = 2.0f;
        [Tooltip("Speed of camera rotation back to original position.")]
        public float returnSpeed = 2.0f;

        [Header("Events")]
        public UnityEvent OnFocusStart;
        public UnityEvent OnFocusEnd;

        private Camera mainCamera;
        private Coroutine currentFocusRoutine;
        private Quaternion originalRotation;
        private bool isFocusing = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("CameraFocusController: Main Camera not found!");
            }
        }

        /// <summary>
        /// Focuses the camera on a specific transform for a duration.
        /// </summary>
        /// <param name="target">The transform to look at.</param>
        /// <param name="duration">How long to look at the target (in seconds).</param>
        public void FocusOn(Transform target, float duration)
        {
            if (mainCamera == null) return;
            if (currentFocusRoutine != null) StopCoroutine(currentFocusRoutine);

            currentFocusRoutine = StartCoroutine(FocusRoutine(target, duration));
        }


        private IEnumerator FocusRoutine(Transform target, float duration)
        {
            isFocusing = true;
            OnFocusStart?.Invoke();
            
            // Store original rotation
            originalRotation = mainCamera.transform.rotation;

            float timeElapsed = 0f;
            Quaternion startRotation = mainCamera.transform.rotation;

            // Rotate towards target
            while (Quaternion.Angle(mainCamera.transform.rotation, Quaternion.LookRotation(target.position - mainCamera.transform.position)) > 0.1f)
            {
                // Calculate target rotation every frame in case target moves
                Quaternion targetRotation = Quaternion.LookRotation(target.position - mainCamera.transform.position);
                mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, Time.deltaTime * focusSpeed);
                yield return null;
            }

            // Hold focus
            float focusTimer = 0f;
            while (focusTimer < duration)
            {
                // Prepare to look at target every frame
                Quaternion targetRotation = Quaternion.LookRotation(target.position - mainCamera.transform.position);
                // Keep looking at target if it moves
                mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, Time.deltaTime * 5f); 
                
                focusTimer += Time.deltaTime;
                yield return null;
            }

            // Rotate back to original
            timeElapsed = 0f;
            startRotation = mainCamera.transform.rotation;
            
            // We want to return to the original rotation. 
            // Note: If the player character rotates while this is happening, originalRotation might be stale relative to the character body.
            // However, usually input is disabled, so character shouldn't rotate.
            
            while (Quaternion.Angle(mainCamera.transform.rotation, originalRotation) > 0.1f)
            {
                mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, originalRotation, Time.deltaTime * returnSpeed);
                yield return null;
            }

            // Snap to final
            mainCamera.transform.rotation = originalRotation;

            isFocusing = false;
            OnFocusEnd?.Invoke();
            currentFocusRoutine = null;
        }
    }
}
