using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Kalender.QuestSystem; // Added missing using

namespace Kalender.QuestSystem.Editor
{
    public class QuestEditorWindow : EditorWindow
    {
        private List<Quest> allQuests = new List<Quest>();
        private Quest selectedQuest;
        private Vector2 scrollPos;
        private string questsPath = "Assets/Kalender/Scripts/QuestSystem/Data"; // Define path once

        [MenuItem("Tools/Quest System/Quest Editor")]
        public static void ShowWindow()
        {
            GetWindow<QuestEditorWindow>("Quest Editor");
        }

        private void OnEnable()
        {
            RefreshQuestList();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // Left Sidebar: Quest List
            DrawSidebar();

            // Right Panel: Quest Details
            DrawQuestDetails();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(250), GUILayout.ExpandHeight(true));
            
            EditorGUILayout.LabelField("All Quests", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create New Quest"))
            {
                CreateNewQuest();
                GUIUtility.ExitGUI();
            }
            
            if (GUILayout.Button("Refresh List"))
            {
                RefreshQuestList();
                GUIUtility.ExitGUI();
            }

            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (allQuests != null)
            {
                for (int i = 0; i < allQuests.Count; i++)
                {
                     if (allQuests[i] == null) continue;

                     // Highlight selected
                     GUIStyle style = (selectedQuest == allQuests[i]) ? new GUIStyle(GUI.skin.button) : new GUIStyle(GUI.skin.button);
                     if (selectedQuest == allQuests[i])
                     {
                         style.normal.textColor = Color.cyan;
                     }

                     if (GUILayout.Button(allQuests[i].title, style))
                     {
                         selectedQuest = allQuests[i];
                         GUI.FocusControl(null); 
                         GUIUtility.ExitGUI();
                     }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawQuestDetails()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

            if (selectedQuest != null)
            {
                EditorGUILayout.LabelField("Edit Quest", EditorStyles.boldLabel);
                
                SerializedObject serializedQuest = new SerializedObject(selectedQuest);
                serializedQuest.Update();

                EditorGUILayout.PropertyField(serializedQuest.FindProperty("id"));
                EditorGUILayout.PropertyField(serializedQuest.FindProperty("title"));
                EditorGUILayout.PropertyField(serializedQuest.FindProperty("description"));
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Pre-requisites", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedQuest.FindProperty("prerequisites"), true); // True for children
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedQuest.FindProperty("rewardAmount"));

                serializedQuest.ApplyModifiedProperties();

                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Ping Asset", GUILayout.Height(30)))
                {
                    EditorGUIUtility.PingObject(selectedQuest);
                }
                
                if (GUILayout.Button("Delete Quest", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Delete Quest", 
                        $"Are you sure you want to delete '{selectedQuest.title}'?", "Yes", "No"))
                    {
                        DeleteQuest(selectedQuest);
                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("Select a quest from the list to edit.", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void RefreshQuestList()
        {
            allQuests.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Quest"); // Find all assets of type Quest
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Quest q = AssetDatabase.LoadAssetAtPath<Quest>(path);
                if (q != null)
                {
                    allQuests.Add(q);
                }
            }
        }

        private void CreateNewQuest()
        {
            // Ensure the directory exists
            if (!AssetDatabase.IsValidFolder(questsPath))
            {
                 // Create Data folder if not exists.
                 if (!AssetDatabase.IsValidFolder("Assets/Kalender/Scripts/QuestSystem"))
                 {
                    AssetDatabase.CreateFolder("Assets/Kalender/Scripts", "QuestSystem");
                 }
                 
                 AssetDatabase.CreateFolder("Assets/Kalender/Scripts/QuestSystem", "Data");
            }

            Quest newQuest = ScriptableObject.CreateInstance<Quest>();
            newQuest.title = "New Quest";
            newQuest.id = System.Guid.NewGuid().ToString();

            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(questsPath + "/NewQuest.asset");
            
            AssetDatabase.CreateAsset(newQuest, uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); 
            
            RefreshQuestList();
            // Select the newly created quest
            // Since RefreshQuestList clears the list, we need to find the new one or just add it.
            // But RefreshQuestList checks disk, so it should be there.
            // Let's find it by path to be sure.
            selectedQuest = AssetDatabase.LoadAssetAtPath<Quest>(uniquePath);
        }

        private void DeleteQuest(Quest quest)
        {
            string path = AssetDatabase.GetAssetPath(quest);
            AssetDatabase.DeleteAsset(path);
            selectedQuest = null;
            RefreshQuestList();
        }
    }
}
