using UnityEngine;

namespace Kalender.QuestSystem
{
    [AddComponentMenu("Quest System/Quest Trigger")]
    public class QuestTrigger : MonoBehaviour
    {
        [Tooltip("The quest to complete when this trigger is activated.")]
        public Quest questToComplete;

        [Tooltip("If true, this will try to accept the quest instead of completing it.")]
        public bool isAcceptAction = false;

        /// <summary>
        /// Call this method from Visual Scripting or UnityEvents.
        /// </summary>
        public void TriggerAction()
        {
            if (questToComplete == null)
            {
                Debug.LogWarning("QuestTrigger: No quest assigned!", gameObject);
                return;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogError("QuestTrigger: QuestManager not found in scene!");
                return;
            }

            if (isAcceptAction)
            {
                QuestManager.Instance.AcceptQuest(questToComplete);
            }
            else
            {
                QuestManager.Instance.CompleteQuest(questToComplete);
            }
        }
    }
}
