using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

#if !UNITY_WEBGL || UNITY_EDITOR
using Discord.Sdk;   // Only works on PC/Editor, NOT WebGL
#endif

public class DiscordManager : MonoBehaviour
{
    [Header("Discord Settings")]
    [SerializeField] private ulong clientId;
    [SerializeField] private TMP_Dropdown friendsDropdown;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button startSessionButton;
    public GameObject invitefriendpanel;

#if !UNITY_WEBGL || UNITY_EDITOR
    // -------- PC/Editor SDK mode --------
    private Client client;
    private string codeVerifier;
    private string sessionJoinSecret;
    private List<UserHandle> friendsList = new List<UserHandle>();
    private bool isLoggedIn = false;
#else
    // -------- WebGL mode --------
    private string discordToken;
    private bool isLoggedIn = false;
#endif

    void Start()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        // -------- Initialize Discord SDK (PC/Editor only) --------
        client = new Client();
        client.AddLogCallback((msg, sev) => Debug.Log($"[Discord] [{sev}] {msg}"), LoggingSeverity.Info);
        client.SetStatusChangedCallback(OnStatusChanged);
        client.SetActivityJoinCallback(OnActivityJoin);

        loginButton.onClick.AddListener(StartOAuthFlow);
        startSessionButton.onClick.AddListener(StartMultiplayerSession);

        statusText.text = "Initializing Discord...";
#else
        // -------- WebGL mode --------
        loginButton.onClick.AddListener(WebGL_Login);
        startSessionButton.onClick.AddListener(() =>
        {
            statusText.text = "Multiplayer not supported in WebGL build.";
        });

        statusText.text = "Ready for WebGL login.";
#endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    // ---------------- PC/Editor SDK Methods ----------------
    private void OnStatusChanged(Client.Status status, Client.Error error, int errorCode)
    {
        statusText.text = $"Discord Status: {status}";
        if (status == Client.Status.Ready)
        {
            isLoggedIn = true;
            statusText.text = "Logged in! Loading friends...";
            LoadFriends();
        }
    }

    private void StartOAuthFlow()
    {
        if (isLoggedIn)
        {
            invitefriendpanel.SetActive(true);
            statusText.text = "Already logged in.";
            return;
        }

        var verifier = client.CreateAuthorizationCodeVerifier();
        codeVerifier = verifier.Verifier();

        var args = new AuthorizationArgs();
        args.SetClientId(clientId);
        args.SetScopes(Client.GetDefaultPresenceScopes());
        args.SetCodeChallenge(verifier.Challenge());

        client.Authorize(args, OnAuthorizeResult);
    }

    private void OnAuthorizeResult(ClientResult result, string code, string redirectUri)
    {
        if (!result.Successful())
        {
            statusText.text = "Authorization failed!";
            return;
        }

        client.GetToken(clientId, code, codeVerifier, redirectUri, (res, token, refresh, type, expires, scope) =>
        {
            if (!string.IsNullOrEmpty(token))
            {
                client.UpdateToken(AuthorizationTokenType.Bearer, token, r => client.Connect());
            }
            else
            {
                statusText.text = "Failed to get token!";
            }
        });
    }

    private void LoadFriends()
    {
        friendsList = client.GetRelationships().Select(r => r.User()).ToList();

        friendsDropdown.ClearOptions();
        List<string> options = friendsList.Select(f => f.Username()).ToList();
        friendsDropdown.AddOptions(options);

        statusText.text = $"Friends loaded: {friendsList.Count}";
        invitefriendpanel.SetActive(true);
    }

    private void StartMultiplayerSession()
    {
        if (client == null || client.GetStatus() != Client.Status.Ready)
        {
            statusText.text = "Discord not ready yet!";
            return;
        }

        sessionJoinSecret = System.Guid.NewGuid().ToString();

        Activity activity = new Activity();
        activity.SetType(ActivityTypes.Playing);
        activity.SetState("In Multiplayer Lobby");
        activity.SetDetails("Waiting for players...");

        ActivitySecrets secrets = new ActivitySecrets();
        secrets.SetJoin(sessionJoinSecret);
        activity.SetSecrets(secrets);

        client.UpdateRichPresence(activity, result =>
        {
            statusText.text = result.Successful()
                ? "Rich Presence updated!"
                : "Failed to update Rich Presence!";
        });
    }

    private void OnActivityJoin(string secret)
    {
        if (secret == sessionJoinSecret)
        {
            Debug.Log("Friend wants to join the multiplayer session!");
            statusText.text = "Friend joining...";
            MultiplayerJoin(secret);
        }
        else
        {
            Debug.LogWarning("Received invalid join secret!");
        }
    }

    private void MultiplayerJoin(string secret)
    {
        Debug.Log($"Connecting friend with secret: {secret}");
    }

    private void OnApplicationQuit()
    {
        client?.Dispose();
    }

#else
    // ---------------- WebGL OAuth2 Methods ----------------
    private void WebGL_Login()
    {
        if (isLoggedIn)  
        {
            statusText.text = "Already logged in (WebGL).";
            return;
        }

        string clientIdStr = clientId.ToString();
        string redirectUri = "https://apgs.itch.io/rogue-slayers/redirect.html"; // must be added in Discord Dev Portal
        string oauthUrl = $"https://discord.com/api/oauth2/authorize?client_id={clientIdStr}&redirect_uri={UnityWebRequest.EscapeURL(redirectUri)}&response_type=token&scope=identify";

        Application.OpenURL(oauthUrl);
        statusText.text = "Opening Discord login...";
    }

    public void SetTokenFromRedirect(string token)
    {
        discordToken = token;
        isLoggedIn = true;
        statusText.text = "Logged in (WebGL)! Fetching user...";
        StartCoroutine(GetDiscordUser());
    }

    private IEnumerator<UnityWebRequestAsyncOperation> GetDiscordUser()
    {
        UnityWebRequest www = UnityWebRequest.Get("https://discord.com/api/users/@me");
        www.SetRequestHeader("Authorization", "Bearer " + discordToken);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            statusText.text = "User: " + www.downloadHandler.text;
            invitefriendpanel.SetActive(true);
        }
        else
        {
            statusText.text = "Failed to fetch user!";
        }
    }
#endif
}
