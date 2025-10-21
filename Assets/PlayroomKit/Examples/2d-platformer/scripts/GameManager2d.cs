using System;
using System.Collections.Generic;
using Playroom;
using UnityEngine;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;
using TMPro;

public class GameManager2d : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string roomCode;

    [Header("Score and UI")]
    [SerializeField] private int score = 0;
    [SerializeField] private Transform scorePanel;
    [SerializeField] private GameObject scoreTextPrefab;

    private Dictionary<string, TextMeshProUGUI> playerScoreTexts = new();

    [SerializeField] private TextMeshProUGUI hellowWorldText;

    [Header("Player Count UI")]
    [SerializeField] private TextMeshProUGUI PlayerCountText;

    private static bool playerJoined;

    private static readonly List<PlayroomKit.Player> players = new();
    private static readonly List<GameObject> playerGameObjects = new();
    private static Dictionary<string, GameObject> PlayerDict = new();

    private PlayroomKit _playroomKit;
    [System.Serializable] public class ScoreData { public int value; } 
    [System.Serializable]public class TestDictionaryData{
        public string message;
        public string playerID;
        public int randomNumber;
        public NestedData nested;
    }
    [System.Serializable]public class NestedData{
        public string inner;
    }

    void Awake()
    {
        _playroomKit = new();
    }

    private void Initialize()
    {
        _playroomKit.InsertCoin(new InitOptions()
        {
            maxPlayersPerRoom = 4,
            defaultPlayerStates = new()
            {
                { "score", 0 },
            },
        }, () =>
        {
            _playroomKit.OnPlayerJoin(AddPlayer);
            print($"[Unity Log] isHost: {_playroomKit.IsHost()}");
        });
    }

    void Start()
    {
        Initialize();

        _playroomKit.RpcRegister("ShootBullet", HandleScoreUpdate, "You shot a bullet!");
        _playroomKit.RpcRegister("DisplayHelloWorldRPC", (data, caller) => DisplayHelloWorldRPC(data, caller), "Displays Hello World on all players' screens");
        _playroomKit.RpcRegister("ReceiveplayerRPCData", (data, caller) => ReceiveplayerRPCData(data), "Receives playerRPC data as JSON string");
        _playroomKit.RpcRegister("TestDictionaryRPC", (data, caller) => TestDictionaryRPC(data, caller), "Test Dictionary");
        
        void HandleScoreUpdate(string data, string caller)
        {
            var player = _playroomKit.GetPlayer(caller);
            Debug.Log($"Caller: {caller}, Player Name: {player?.GetProfile().name}, Data: {data}");

            if (PlayerDict.TryGetValue(caller, out GameObject playerObj))
            {
                var playerController = playerObj.GetComponent<PlayerController2d>();
                if (playerController != null)
                {
                try{  
                    var scoreData = JsonUtility.FromJson<ScoreData>(data);
                    playerController.scoreText.text = $"Score: {scoreData.value}";
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error deserializing score data: {ex.Message}");
                    playerController.scoreText.text = $"Score: {data}";
                }
                }
                else
                {
                    Debug.LogError($"PlayerController not found on GameObject for caller: {caller}");
                }
            }
            else
            {
                Debug.LogError($"No GameObject found for caller: {caller}");
            }
        }
    }

    private void Update()
    {
        if (playerJoined)
        {
            var myPlayer = _playroomKit.MyPlayer();
            var index = players.IndexOf(myPlayer);

            playerGameObjects[index].GetComponent<PlayerController2d>().Move();
            playerGameObjects[index].GetComponent<PlayerController2d>().Jump();

            players[index].SetState("pos", playerGameObjects[index].transform.position);
            ShootBullet(index);

            for (var i = 0; i < players.Count; i++)
            {
                if (players[i] != null && PlayerDict.TryGetValue(players[i].id, out GameObject playerObj))
                {
                    var pos = players[i].GetState<Vector3>("pos");
                    var color = players[i].GetState<Color>("color");
                    playerObj.transform.position = pos;
                    playerObj.GetComponent<SpriteRenderer>().color = color;
                }
            }

            
        }

        if (Input.GetKeyDown(KeyCode.H))
{
    _playroomKit.RpcCall("DisplayHelloWorldRPC", "Hello, World!", PlayroomKit.RpcMode.ALL, () =>
    {
        Debug.Log("RPC call to display 'Hello World' sent successfully.");
    });
}


        if (Input.GetKeyDown(KeyCode.P))
        {
            var myId = _playroomKit.MyPlayer()?.id ?? "unknown";
            TestDictionaryData testData = new TestDictionaryData
            {
            message = "Hello, World!",
            playerID = myId,
            randomNumber = UnityEngine.Random.Range(1, 100),
            nested = new NestedData { inner = "value" }
            };


            _playroomKit.RpcCall("TestDictionaryRPC", testData, PlayroomKit.RpcMode.ALL, () =>
        {
            Debug.Log("RPC call to send test dictionary sent successfully.");
        });
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            SendplayerRPCData();
        }
    }

    private void ShootBullet(int playerIndex)
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 playerPosition = playerGameObjects[playerIndex].transform.position;

            score = playerGameObjects[playerIndex].GetComponent<PlayerController2d>()
                .ShootBullet(playerPosition, 50f, score);

            _playroomKit.RpcCall("ShootBullet", score, PlayroomKit.RpcMode.ALL,
                () => { Debug.Log("Shooting bullet"); });
        }
    }

    
    public void AddPlayer(PlayroomKit.Player player)
    {
        Vector3 spawnPos = new(Random.Range(-4, 4), Random.Range(1, 5), 0);
        GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        player.SetState("color", player.GetProfile().color);

        PlayerDict[player.id] = playerObj;
        players.Add(player);
        playerGameObjects.Add(playerObj);

        string playerName = string.IsNullOrEmpty(player.GetProfile().name)
            ? $"Player {players.Count}"
            : player.GetProfile().name;

        GameObject scoreObj = Instantiate(scoreTextPrefab, scorePanel);
        TextMeshProUGUI scoreText = scoreObj.GetComponent<TextMeshProUGUI>();
        scoreText.text = $"{playerName}: 0";
        scoreText.color = player.GetProfile().color;

        playerScoreTexts[player.id] = scoreText;

        var controller = playerObj.GetComponent<PlayerController2d>();
        if (controller != null)
        {
            controller.scoreText = scoreText;
        }

        playerJoined = true;
        player.OnQuit(RemovePlayer);

        UpdatePlayerCountText();
        Debug.Log($" Added player: {playerName}");
    }

    private void RemovePlayer(string playerID)
{
    if (PlayerDict.TryGetValue(playerID, out GameObject player))
    {
        PlayerDict.Remove(playerID);
        players.Remove(players.Find(p => p.id == playerID));
        playerGameObjects.Remove(player);
        Destroy(player);

        if (playerScoreTexts.ContainsKey(playerID))
        {
            Destroy(playerScoreTexts[playerID].gameObject);
            playerScoreTexts.Remove(playerID);
        }
        UpdatePlayerCountText();
    }
    else
    {
        Debug.LogWarning("Player not found in dictionary");
    }
}

    private void UpdatePlayerCountText()
    {
        if (PlayerCountText != null)
        {
            PlayerCountText.text = $"Players: {players.Count}";
        }
    }

    private void DisplayHelloWorldRPC(string data, string caller)
    {
        if (hellowWorldText != null)
        {
            hellowWorldText.text = data;
            StartCoroutine(ClearHelloWorldAfterDelay(3f));
        }
    }

    private System.Collections.IEnumerator ClearHelloWorldAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (hellowWorldText != null)
        {
            hellowWorldText.text = "";
        }
    }

    private void SendplayerRPCData()
    {
        playerRPCClass playerRPCData = new()
        {
            playerRPCScore = 42,
            playerRPCString = "This is a playerRPC string!",
            someList = new List<string> { "Item1", "Item2", "Item3" }
        };

        Debug.Log($"$$$$ Sending playerRPC Data: {playerRPCData}");

        _playroomKit.RpcCall("ReceiveplayerRPCData", playerRPCData, PlayroomKit.RpcMode.ALL, () =>
        {
            Debug.Log("RPC call to send playerRPC data sent successfully.");
        });
    }

    private void ReceiveplayerRPCData(string jsonData)
    {
        Debug.Log($"$$$$ Received playerRPC Data: {jsonData}");
        try
        {
            playerRPCClass receivedData = JsonUtility.FromJson<playerRPCClass>(jsonData);
            if (receivedData != null)
            {
                Debug.Log($"Received playerRPC Score: {receivedData.playerRPCScore}");
                Debug.Log($"Received playerRPC String: {receivedData.playerRPCString}");
                Debug.Log($"Received List Count: {receivedData.someList?.Count ?? 0}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deserializing playerRPC data: {ex.Message}");
        }
    }

    private void TestDictionaryRPC(string data, string caller)
    {
    Debug.Log($"Test Dictionary RPC called by {caller} with data: {data}");
    try
    {
        var receivedData = JsonUtility.FromJson<TestDictionaryData>(data);
        Debug.Log($"[Dictionary Test] Message: {receivedData.message}");
        Debug.Log($"[Dictionary Test] PlayerID: {receivedData.playerID}");
        Debug.Log($"[Dictionary Test] Random: {receivedData.randomNumber}");
        Debug.Log($"[Dictionary Test] Nested: {receivedData.nested.inner}");
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error deserializing dictionary data: {ex.Message}");
    }
}
}

[System.Serializable]
public class playerRPCClass
{
    public int playerRPCScore;
    public string playerRPCString;
    public List<string> someList;
}
