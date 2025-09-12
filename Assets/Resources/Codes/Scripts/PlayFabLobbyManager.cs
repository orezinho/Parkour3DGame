using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class PlayFabLobbySystem : MonoBehaviour
{
    [Header("UI")]
    public Transform lobbyListContainer;   // Content do ScrollView
    public GameObject lobbyButtonPrefab;   // Prefab do botão
    public TextMeshProUGUI statusText;     // Status na tela

    private static PlayFab.MultiplayerModels.EntityKey myEntityKey;
    private string currentConnectionString;
    private string currentLobbyId;

    // 1) Login
    void Start()
    {
        var req = new LoginWithCustomIDRequest
        {
            CustomId = System.Guid.NewGuid().ToString(),
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(req, OnLoginSuccess, OnLoginFailure);

        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            CreateLobby();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ListLobbies();
        }
    }



    void OnLoginSuccess(LoginResult res)
    {
        statusText.text = "Login feito!";
        myEntityKey = new PlayFab.MultiplayerModels.EntityKey
        {
            Id = res.EntityToken.Entity.Id,
            Type = res.EntityToken.Entity.Type
        };
    }

    void OnLoginFailure(PlayFabError err)
    {
        statusText.text = "Erro no login!";
        Debug.LogError(err.GenerateErrorReport());
    }

    // 2) Criar lobby -> Host
    public void CreateLobby()
    {
        string lobbyName = "Sala_" + Random.Range(1000, 9999);

        var req = new CreateLobbyRequest
        {
            MaxPlayers = 2,
            Owner = myEntityKey,
            AccessPolicy = AccessPolicy.Public,
            Members = new List<Member> { new Member { MemberEntity = myEntityKey } },
            // Nome pesquisável vai em SearchData (string_keyN)
            SearchData = new Dictionary<string, string> { { "string_key1", lobbyName } }
        };

        PlayFabMultiplayerAPI.CreateLobby(req, OnLobbyCreated, OnError);
    }

    void OnLobbyCreated(CreateLobbyResult res)
    {
        currentConnectionString = res.ConnectionString; // top-level no Create
        currentLobbyId = res.LobbyId;

        statusText.text = "Lobby criado!";
        Debug.Log($"Lobby criado. Id={currentLobbyId}, ConnStr={currentConnectionString}");

        // Vira HOST
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = "0.0.0.0"; // aceitar conexões
        transport.ConnectionData.Port = 7777;
        NetworkManager.Singleton.StartHost();
    }

    // 3) Listar lobbies
    public void ListLobbies()
    {
        var req = new FindLobbiesRequest
        {
            Filter = "lobby/memberCountRemaining gt 0",
            OrderBy = "lobby/memberCountRemaining desc"
        };
        PlayFabMultiplayerAPI.FindLobbies(req, OnLobbiesFound, OnError);
    }

    void OnLobbiesFound(FindLobbiesResult res)
    {
        foreach (Transform c in lobbyListContainer) Destroy(c.gameObject);

        foreach (var lobby in res.Lobbies)
        {
            var go = Instantiate(lobbyButtonPrefab, lobbyListContainer);

            string lobbyName = (lobby.SearchData != null &&
                                lobby.SearchData.TryGetValue("string_key1", out var name))
                               ? name : $"Lobby {lobby.LobbyId.Substring(0, 6)}";

            go.GetComponentInChildren<TMP_Text>().text =
                $"{lobbyName} ({lobby.CurrentPlayers}/{lobby.MaxPlayers})";

            string connectionString = lobby.ConnectionString; // é isso que se usa p/ Join
            go.GetComponent<Button>().onClick.AddListener(() => JoinLobby(connectionString));
        }

        statusText.text = "Lobbies carregados!";
    }

    // 4) Entrar no lobby -> JoinLobby usa ConnectionString
    public void JoinLobby(string connectionString)
    {
        var req = new JoinLobbyRequest
        {
            ConnectionString = connectionString,
            MemberEntity = myEntityKey
        };

        PlayFabMultiplayerAPI.JoinLobby(req, OnLobbyJoined, OnError);
    }

    // JoinLobbyResult NÃO tem Lobby completo; usa LobbyId p/ GetLobby
    void OnLobbyJoined(JoinLobbyResult res)
    {
        currentLobbyId = res.LobbyId;
        statusText.text = "Entrou no lobby, buscando dados...";

        var getReq = new GetLobbyRequest { LobbyId = currentLobbyId };
        PlayFabMultiplayerAPI.GetLobby(getReq, OnGetLobby, OnError);
    }

    // 5) GetLobby -> agora sim temos Lobby.ConnectionString
    void OnGetLobby(GetLobbyResult res)
    {
        currentConnectionString = res.Lobby.ConnectionString;
        statusText.text = "Lobby conectado!";
        Debug.Log($"GetLobby OK. Id={currentLobbyId}, ConnStr={currentConnectionString}");

        // Vira CLIENT
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = "127.0.0.1"; // provisório (LAN)
        transport.ConnectionData.Port = 7777;
        NetworkManager.Singleton.StartClient();
    }

    // Erros
    void OnError(PlayFabError err)
    {
        statusText.text = "Erro: " + err.ErrorMessage;
        Debug.LogError(err.GenerateErrorReport());
    }
}
