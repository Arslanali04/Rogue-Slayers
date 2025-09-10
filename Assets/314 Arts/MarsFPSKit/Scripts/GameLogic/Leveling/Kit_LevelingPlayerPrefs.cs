using UnityEngine;
using UnityEngine.UI;

namespace MarsFPSKit
{
    namespace UI
    {
        [CreateAssetMenu(menuName = "MarsFPSKit/Leveling/PlayerPrefs based")]
        public class Kit_LevelingPlayerPrefs : Kit_LevelingBase
        {
            [Header("Settings")]
            public int maxLevel = 10;
           
            public System.Action<int> OnLevelUp; // Event with new level as param

            [Header("XP Requirements for Each Level")]
            [Tooltip("XP needed to reach each level (index 0 = Level 1 to 2, index 1 = Level 2 to 3, etc.)")]
            public int[] xpRequirementsPerLevel = new int[] {
                400,  // Level 1 -> 2
                600,  // Level 2 -> 3
                800,  // Level 3 -> 4
                1000,  // Level 4 -> 5
                1200,  // Level 5 -> 6
                1400,  // Level 6 -> 7
                1600,  // Level 7 -> 8
                1800,  // Level 8 -> 9
                2000, // Level 9 -> 10
                2200     // Level 10 (max level)
            };

            public int currentLevel = 1;
            public int currentXp;

            public override void AddXp(Kit_IngameMain main, int xp)
            {
                currentXp += xp;
                RecalculateLevelWithLevelUp(main);
            }

            public override int GetLevel()
            {
                return currentLevel;
            }

            public override int GetMaxLevel()
            {
                return maxLevel;
            }

            public override float GetPercentageToNextLevel()
            {
                if (currentLevel >= maxLevel) return 1f;
                
                int xpForCurrentLevel = GetXpRequiredForLevel(currentLevel);
                int xpForNextLevel = GetXpRequiredForLevel(currentLevel + 1);
                
                return (float)(currentXp - xpForCurrentLevel) / (xpForNextLevel - xpForCurrentLevel);
            }

            public override void Initialize(Kit_MenuManager menu)
            {
                // Initialize event if null
                OnLevelUp ??= delegate { };
                
                // Ensure maxLevel matches our XP requirements array
                maxLevel = xpRequirementsPerLevel.Length + 1;
                
                // Load XP
                currentXp = PlayerPrefs.GetInt(Kit_GameSettings.userName + "_xp", 0);
                
                // Recalc Level
                RecalculateLevel();
            }

            private int GetXpRequiredForLevel(int level)
            {
                if (level <= 1) return 0;
                if (level > maxLevel) return xpRequirementsPerLevel[maxLevel - 2];
                
                return xpRequirementsPerLevel[level - 2];
            }

            private int GetTotalXpForLevel(int level)
            {
                int totalXp = 0;
                for (int i = 0; i < level - 1 && i < xpRequirementsPerLevel.Length; i++)
                {
                    totalXp += xpRequirementsPerLevel[i];
                }
                return totalXp;
            }

            private void RecalculateLevel()
            {
                int totalXp = 0;
                currentLevel = 1;

                for (int i = 0; i < xpRequirementsPerLevel.Length; i++)
                {
                    totalXp += xpRequirementsPerLevel[i];
                    if (currentXp >= totalXp)
                    {
                        currentLevel = i + 2;
                    }
                    else
                    {
                        break;
                    }
                }

                // Cap at max level
                currentLevel = Mathf.Min(currentLevel, maxLevel);
            }

            private void RecalculateLevelWithLevelUp(Kit_IngameMain main)
            {
                int oldLevel = currentLevel;
                RecalculateLevel();

                if (currentLevel > oldLevel)
                {
                    // Level up!
                    Save();
                    OnLevelUp?.Invoke(currentLevel); // ✅ Notify listeners (like UI)
                    Debug.Log($"Level Up! New Level: {currentLevel}");
                }
                else
                {
                    // Just save XP progress
                    Save();
                }
            }

            public override void Save()
            {
                PlayerPrefs.SetInt(Kit_GameSettings.userName + "_xp", currentXp);
                PlayerPrefs.Save();
            }

            // Helper method to see XP requirements
            public void PrintXpRequirements()
            {
                Debug.Log("XP Requirements:");
                for (int i = 0; i < xpRequirementsPerLevel.Length; i++)
                {
                    Debug.Log($"Level {i + 1} -> {i + 2}: {xpRequirementsPerLevel[i]} XP");
                }
            }
        }
    }
}