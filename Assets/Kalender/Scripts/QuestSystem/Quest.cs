using System.Collections.Generic;
using UnityEngine;

namespace Kalender.QuestSystem
{
    public enum QuestState
    {
        Locked,
        Available,
        Active,
        Completed
    }

    [CreateAssetMenu(fileName = "New Quest", menuName = "Quest System/Quest")]
    public class Quest : ScriptableObject
    {
        [Header("General Info")]
        public string id;
        public string title;
        [TextArea(3, 10)]
        public string description;

        [Header("Requirements")]
        public List<Quest> prerequisites = new List<Quest>();

        [Header("Rewards")]
        public int rewardAmount;

        [Header("Status (Runtime Only)")]
        public QuestState state = QuestState.Locked;

        private void OnEnable()
        {
            // Reset state in Editor for testing, but in a real game this would be loaded from save data
            #if UNITY_EDITOR
            state = QuestState.Locked;
            #endif
        }
    }
}
