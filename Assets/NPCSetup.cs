using TMPro;
using UnityEngine;
using player2_sdk;  // namespace from your pasted code

public class NPCSetup : MonoBehaviour
{
    [Header("References")]
    public NpcManager npcManager;         // Assign in Inspector
    public TextMeshProUGUI dialogueText;  // Your UI text element
    public GameObject npcObject;          // Your NPC GameObject in Unity

    [Header("Player2 Settings")]
    [SerializeField] private string npcId = "019175d9-2796-7695-81d8-e1df42426cbc";

    void Start()
    {
        if (string.IsNullOrEmpty(npcId))
        {
            Debug.LogError("NPC ID is empty! Paste your Player2 NPC ID in the Inspector.");
            return;
        }

        npcManager.RegisterNpc(npcId, dialogueText, npcObject);
        Debug.Log($"✅ Registered custom NPC with ID: {npcId}");
    }
}