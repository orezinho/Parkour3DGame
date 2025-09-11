using PlayFab;
using PlayFab.MultiplayerModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayFabLobbyManager : MonoBehaviour
{
    [Header("UI")]
    public Transform lobbyListContainer; // Content do ScrollView
    public GameObject lobbyButtonPrefab; // Prefab do botão

    private string currentLobbyId;
    private List<LobbySummary> lobbyList = new List<LobbySummary>();

    // Cria um lobby
    public void CreateLobby()
    {
        var request = new CreateLobbyRequest
        {
            MaxPlayers = 7,
            Owner = System.Guid.NewGuid().ToString(),
            LobbyName = "Sala_" + Random.Range(1000, 9999)
        };

        PlayFabMultiplayerAPI.CreateLobby(request, OnLobbyCreated, OnError);
    }

    // Lista lobbies e gera UI
    public void ListLobbies()
    {
        var request = new FindLobbiesRequest
        {
            Filter = "lobbyName ne null",
            OrderBy = "lobbyName asc"
        };

        PlayFabMultiplayerAPI.FindLobbies(request, OnLobbiesFound, OnError);
    }

    private void OnLobbiesFound(FindLobbiesResult result)
    {
        // limpa a lista antiga
        foreach (Transform child in lobbyListContainer)
        {
            Destroy(child.gameObject);
        }

        lobbyList = result.Lobbies;
        Debug.Log("?? Lobbies encontrados: " + lobbyList.Count);

        foreach (var lobby in lobbyList)
        {
            GameObject newButton = Instantiate(lobbyButtonPrefab, lobbyListContainer);

            // atualiza o texto
            TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
            buttonText.text = $"{lobby.LobbyName} ({lobby.CurrentPlayers}/{lobby.MaxPlayers})";

            // adiciona evento para entrar no lobby
            Button btn = newButton.GetComponent<Button>();
            string targetLobbyId = lobby.LobbyId; // precisa copiar a variável
            btn.onClick.AddListener(() => JoinLobby(targetLobbyId));
        }
    }

    // Entra no lobby
    public void JoinLobby(string lobbyId)
    {
        var request = new JoinLobbyRequest
        {
            LobbyId = lobbyId
        };

        PlayFabMultiplayerAPI.JoinLobby(request, OnLobbyJoined, OnError);
    }

    private void OnLobbyCreated(CreateLobbyResult result)
    {
        currentLobbyId = result.LobbyId;
        Debug.Log("? Lobby criado com sucesso! ID: " + currentLobbyId);
    }

    private void OnLobbyJoined(JoinLobbyResult result)
    {
        currentLobbyId = result.LobbyId;
        Debug.Log("? Entrou no lobby: " + currentLobbyId);
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError("? Erro: " + error.GenerateErrorReport());
    }
}