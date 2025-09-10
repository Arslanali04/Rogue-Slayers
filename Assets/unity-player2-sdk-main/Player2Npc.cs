using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace player2_sdk
{
    [Serializable]
    public class SpawnNpc
    {
        public string short_name;
        public string name;
        public string character_description;
        public string system_prompt;
        public string voice_id;
        public List<SerializableFunction> commands;
    }

    [Serializable]
    public class ChatRequest
    {
        public string sender_name;
        public string sender_message;
        public string game_state_info;
        public string tts;
    }

    public class Player2Npc : MonoBehaviour
    {
        [Header("State Config")]
        [SerializeField] private NpcManager npcManager;

        [Header("NPC Configuration")]
        [SerializeField] private string shortName = "Victor";
        [SerializeField] private string fullName = "Victor J. Johnson";
        [SerializeField] private string characterDescription = "I am crazed scientist on the hunt for gold!";
        [SerializeField] private string systemPrompt = "Victor is a scientist obsessed with finding gold.";
        [SerializeField] public string voiceId = "01955d76-ed5b-7451-92d6-5ef579d3ed28";
        [SerializeField] private bool persistent = false;

        [Header("Chat UI")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private GameObject openChatButton;
        [SerializeField] private GameObject closeChatButton;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private ScrollRect chatScroll;
        [SerializeField] private RectTransform chatContent;
        [SerializeField] private GameObject chatEntryPrefab;

        [Header("Fade Settings")]
      //  [SerializeField] private CanvasGroup chatBackground;
       // [SerializeField] private CanvasGroup chatMessages;
        [SerializeField] private float fadeOutTimeNormal = 4f;
        [SerializeField] private float fadeOutTimeMessages = 8f;

        private string _npcID = null;
        private float lastMessageReceived = -100f;
        private float lastChatOpen = -100f;
        private bool isChatOpen;

        private readonly List<GameObject> activeChatEntries = new();
        public int maxNumberOfActiveChatEntries = 32;

        private string _gameID() => npcManager.gameId;
        private string _baseUrl() => NpcManager.GetBaseUrl();

        private void Start()
        {
            Debug.Log("Starting Player2Npc with NPC: " + fullName);

            // Setup buttons
            sendButton.onClick.AddListener(() =>
            {
                var message = inputField.text;
                OnChatMessageSubmitted(message);
                inputField.text = string.Empty;
            });

            //openChatButton.onClick.AddListener(OpenChat);
           // closeChatButton.onClick.AddListener(CloseChat);

            inputField.onSelect.AddListener(_ => Time.timeScale = 0f);
            inputField.onDeselect.AddListener(_ => Time.timeScale = 1f);

            OnSpawnTriggered();
        }

        public void OpenChat()
        {
            chatPanel.SetActive(true);
            openChatButton.SetActive(false);
            closeChatButton.SetActive(true);
            isChatOpen = true;
            lastChatOpen = Time.time;
        }

        public void CloseChat()
        {
            openChatButton.SetActive(true);
            closeChatButton.SetActive(false);
            chatPanel.SetActive(false);
            isChatOpen = false;
            lastChatOpen = Time.time;
        }

        private void Update()
        {
            // if (isChatOpen)
            // {
            //     chatBackground.alpha = 1f;
            //     chatMessages.alpha = 1f;
            // }
            // else
            // {
            //     chatBackground.alpha = Mathf.Clamp01((lastChatOpen + fadeOutTimeNormal) - Time.time);
            //     chatMessages.alpha = Mathf.Clamp01((lastMessageReceived + fadeOutTimeMessages) - Time.time);
            // }
        }

        private void AddChatEntry(string sender, string message)
        {
            lastMessageReceived = Time.time;

            if (maxNumberOfActiveChatEntries > 0 && activeChatEntries.Count >= maxNumberOfActiveChatEntries)
            {
                var go = activeChatEntries[0];
                activeChatEntries.RemoveAt(0);
                Destroy(go);
            }

            var newEntry = Instantiate(chatEntryPrefab, chatContent, false);
            newEntry.transform.localScale = Vector3.one;
            var text = newEntry.GetComponentInChildren<TextMeshProUGUI>();
            text.text = $"<b>{sender}:</b> {message}";

            activeChatEntries.Add(newEntry);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
            chatScroll.verticalNormalizedPosition = 0f;
        }

        private void OnSpawnTriggered()
        {
            _ = SpawnNpcAsync();
        }

        private void OnChatMessageSubmitted(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            AddChatEntry("You", message);

            var aiHandler = GetComponent<NpcAIHandler>();
            if (aiHandler != null)
            {
                aiHandler.ProcessCommand(message);
            }

            _ = SendChatMessageAsync(message);
        }

        private async Awaitable SpawnNpcAsync()
        {
            try
            {
                var spawnData = new SpawnNpc
                {
                    short_name = shortName,
                    name = fullName,
                    character_description = characterDescription,
                    system_prompt = systemPrompt,
                    voice_id = voiceId,
                    commands = npcManager.GetSerializableFunctions()
                };

                string url = $"{_baseUrl()}/npc/games/{_gameID()}/npcs/spawn";
                string json = JsonConvert.SerializeObject(spawnData, npcManager.JsonSerializerSettings);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

                using var request = new UnityWebRequest(url, "POST");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _npcID = request.downloadHandler.text.Trim('"');
                    npcManager.RegisterNpc(_npcID, null, gameObject);
                }
                else
                {
                    Debug.LogError($"Failed to spawn NPC: {request.error} - Response: {request.downloadHandler.text}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error during NPC spawn: {ex.Message}");
            }
        }

        private async Awaitable SendChatMessageAsync(string message)   
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                if (string.IsNullOrEmpty(_npcID))
                {
                    Debug.LogWarning("NPC ID is not set! Cannot send message.");
                    return;
                }

                var chatRequest = new ChatRequest
                {
                    sender_name = fullName,
                    sender_message = message,
                    tts = null
                };

                await SendChatRequestAsync(chatRequest);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error sending chat message: {ex.Message}");
            }
        }

        private async Awaitable SendChatRequestAsync(ChatRequest chatRequest)
        {
            if (npcManager.TTS)
            {
                chatRequest.tts = "local_client";
            }

            string url = $"{_baseUrl()}/npc/games/{_gameID()}/npcs/{_npcID}/chat";
            string json = JsonConvert.SerializeObject(chatRequest, npcManager.JsonSerializerSettings);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to send message: {request.error} - Response: {request.downloadHandler.text}");
            }
        }

        // Called externally by NpcManager when NPC responds
        public void OnNpcResponse(string npcMessage)
        {
            AddChatEntry(fullName, npcMessage);
        }
    }
}
