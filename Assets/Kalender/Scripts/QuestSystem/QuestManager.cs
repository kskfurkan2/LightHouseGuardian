using System.Collections.Generic;
using UnityEngine;

namespace Kalender.QuestSystem
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance;

        public List<Quest> allQuests;
        
        [Tooltip("If true, quests with no prerequisites are automatically available on start.")]
        public bool autoUnlockBaseQuests = true;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            InitializeQuests();
        }

        public void InitializeQuests()
        {
            // In a real game, Load save data here.
            // For now, we just check prerequisites.
            foreach (var quest in allQuests)
            {
                // Reset state for new session testing
                quest.state = QuestState.Locked;
            }

            CheckAllQuestAvailability();
        }

        public void CheckAllQuestAvailability()
        {
            foreach (var quest in allQuests)
            {
                if (quest.state == QuestState.Locked)
                {
                    if (CheckPrerequisites(quest))
                    {
                        if (quest.prerequisites.Count == 0 && !autoUnlockBaseQuests)
                            continue;
                            
                        quest.state = QuestState.Available;
                        Debug.Log($"Quest Available: {quest.title}");
                    }
                }
            }
        }

        public bool CheckPrerequisites(Quest quest)
        {
            if (quest.prerequisites == null || quest.prerequisites.Count == 0)
                return true;

            foreach (var preReq in quest.prerequisites)
            {
                if (preReq.state != QuestState.Completed)
                    return false;
            }
            return true;
        }

        public void AcceptQuest(Quest quest)
        {
            if (quest.state == QuestState.Available)
            {
                quest.state = QuestState.Active;
                Debug.Log($"Quest Accepted: {quest.title}");
            }
        }

        public void CompleteQuest(Quest quest)
        {
            if (quest.state == QuestState.Active)
            {
                quest.state = QuestState.Completed;
                Debug.Log($"Quest Completed: {quest.title}");
                // Give rewards here
                
                // Check if this unlocks other quests
                CheckAllQuestAvailability();
            }
            else
            {
                 Debug.LogWarning($"Cannot complete quest {quest.title}, state is {quest.state}");
            }
        }
    }
}
