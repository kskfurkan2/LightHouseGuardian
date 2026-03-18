using UnityEngine;

namespace Kalender.CameraSystem
{
    public class CameraFocuser : MonoBehaviour
    {
        [Header("Odaklanma Ayarları")]
        [Tooltip("Bu değer true (açık) yapıldığında kamera hedefe doğru döner. Visual Scripting ile bu değeri değiştirebilirsin.")]
        public bool isFocusing = false;

        [Tooltip("Kameranın bakacağı hedef obje.")]
        public Transform targetObject;

        [Tooltip("Kameranın hedefe dönerkenki yumuşaklığı (hızı).")]
        public float smoothSpeed = 5f;

        private void Update()
        {
            // Eğer odaklanma açıksa ve hedef belirlenmişse
            if (isFocusing && targetObject != null)
            {
                // Kameradan hedefe doğru olan yönü hesapla
                Vector3 direction = targetObject.position - transform.position;

                // Eğer hedef objeyle kamera tam olarak aynı noktada değilse dönme işlemini yap
                if (direction != Vector3.zero)
                {
                    // Hedefe bakmamız gereken rotasyonu (açıyı) bul
                    Quaternion targetRotation = Quaternion.LookRotation(direction);

                    // Kameranın şu anki açısından, hedefin açısına doğru yumuşak (slerp) bir geçiş yap
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
                }
            }
        }
    }
}
