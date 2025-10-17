using System;
using System.Collections.Generic;
using Playroom;
using UnityEngine;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;
using TMPro;
using System.Threading;


public class GameManager2d : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private string roomCode;


    /// <summary>
    /// player scores and UI to display score of the game.
    /// </summary>
    [Header("Score and UI")]
    [SerializeField]
    private int score = 0;
    [SerializeField]
    private TextMeshProUGUI scoreTextPlayer1;
    [SerializeField]
    private TextMeshProUGUI scoreTextPlayer2;
    private TextMeshProUGUI selectedScoreText;
    [SerializeField]
    private TextMeshProUGUI hellowWorldText;
    // <summary>
    // To display current player count
    /// </summary>
    
    [Header("Player Count UI")]
    [SerializeField]
    private TextMeshProUGUI PlayerCountText;   

    private static bool playerJoined;

    /// <summary>
    /// List of players and their gameObjects.
    /// </summary>
    private static readonly List<PlayroomKit.Player> players = new();
    private static readonly List<GameObject> playerGameObjects = new();
    private static Dictionary<string, GameObject> PlayerDict = new();

    private PlayroomKit _playroomKit;
    [SerializeField]
    private string newData;


    void Awake()
    {
        _playroomKit = new();
    }

    /// <summary>
    /// Initialize PlayroomKit, starts multiplayer.
    /// </summary>
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

    /// <summary>
    /// Register the RPC method to update the score.
    /// </summary>
    void Start()
    {
        Initialize();

        _playroomKit.RpcRegister("ShootBullet", HandleScoreUpdate, "You shot a bullet!");
        _playroomKit.WaitForState("test", (s) => { Debug.LogWarning($"After waiting for test: {s}"); });

        // Register Rpc for hello world display
        _playroomKit.RpcRegister("DisplayHelloWorldRPC", (data, caller) => DisplayHelloWorldRPC(), "Displays Hello World on all players' screens");

        // Register Rpc for sending an d recieving custom data types 
        _playroomKit.RpcRegister("ReceiveCoolData", (data, caller) => ReceiveCoolData(data), "Receives cool data as JSON string");
    
    /// <summary>
    /// Update the Score UI of the player and sync.
    /// </summary>
    void HandleScoreUpdate(string data, string caller)
        {
            var player = _playroomKit.GetPlayer(caller);
            Debug.Log($"Caller: {caller}, Player Name: {player?.GetProfile().name}, Data: {data}");

            if (PlayerDict.TryGetValue(caller, out GameObject playerObj))
            {
                var playerController = playerObj.GetComponent<PlayerController2d>();
                if (playerController != null)
                {
                    playerController.scoreText.text = $"Score: {data}";
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

    /// <summary>
    /// Update the player position and sync.
    /// </summary>
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
                    if (playerGameObjects != null)
                    {
                        playerGameObjects[i].GetComponent<Transform>().position = pos;
                        playerGameObjects[i].GetComponent<SpriteRenderer>().color = color;
                    }
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            _playroomKit.RpcCall("DisplayHelloWorldRPC",null, PlayroomKit.RpcMode.ALL, () =>
            {
                Debug.Log("RPC call to display 'Hello World' sent successfully.");
            });
        }

        // Triggering an RPC to send custom data
        if (Input.GetKeyDown(KeyCode.C))
        {
            SendCoolData();
        } 

    }
    


    /// <summary>
    /// Shoot bullet and update the score.
    /// </summary>
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

    /// <summary>
    /// Adds the "player" to the game scene.
    /// </summary>
    public void AddPlayer(PlayroomKit.Player player)
    {
        var spawnPos = new Vector3(Random.Range(-4, 4), Random.Range(1, 5), 0);
        GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        player.SetState("color", player.GetProfile().color);

        PlayerDict.Add(player.id, playerObj);
        players.Add(player);
        playerGameObjects.Add(playerObj);

        // Assign score text based on player count

        if(players.Count == 1)
        {
            selectedScoreText= scoreTextPlayer1;
        }
        else if (players.Count == 2)
        {
            selectedScoreText= scoreTextPlayer2;
        }
        else if (players.Count ==  3)
        {
            selectedScoreText= Instantiate(scoreTextPlayer1, scoreTextPlayer1.transform.parent);
            selectedScoreText.name = "ScoreTextPlayer3";
            selectedScoreText.text = "Score: 0";

            RectTransform rectTransform = selectedScoreText.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - 50);
        }
        else if (players.Count == 4)
        {
            selectedScoreText= Instantiate(scoreTextPlayer2, scoreTextPlayer2.transform.parent);
            selectedScoreText.name = "ScoreTextPlayer4";
            selectedScoreText.text = "Score: 0";

            RectTransform rectTransform = selectedScoreText.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - 100);
        }

        playerObj.GetComponent<PlayerController2d>().scoreText = selectedScoreText;

        playerJoined = true;
        player.OnQuit(RemovePlayer);

        // Update player count UI
        UpdatePlayerCountText();  
    }

    /// <summary>
    /// Remove player from the game, called when the player leaves / closes the game.
    /// </summary>
    private static void RemovePlayer(string playerID)
    {
        if (PlayerDict.TryGetValue(playerID, out GameObject player))
        {
            PlayerDict.Remove(playerID);
            players.Remove(players.Find(p => p.id == playerID));
            playerGameObjects.Remove(player);
            Destroy(player);
            
            GameManager2d instance = FindObjectOfType<GameManager2d>();
            if (instance != null)
            {
                instance.UpdatePlayerCountText();
            }

        }
        else
        {
            Debug.LogWarning("Player is not in dictionary");
        }
    }

    /// <summary>
    /// Methode the player count text UI.
    /// </summary>
    private void UpdatePlayerCountText()
    {
        if (PlayerCountText != null)
        {
            PlayerCountText.text = $"Players: {players.Count}";
        }
        else
        {
            Debug.LogWarning("PlayerCountText is not assigned in the inspector.");
        }
    }



    // RPC method to display "Hello World" on all players' screens
    private void DisplayHelloWorldRPC()
    {
        foreach (var playerObj in playerGameObjects)
        {
            var playerController = playerObj.GetComponent<PlayerController2d>();
            if (playerController != null)
            {
                DisplayHelloWorld();
            }
            else
            {
                Debug.LogError("PlayerController2d component not found on player object.");
            }
        }
    }
    
    public void DisplayHelloWorld()
    {
        if (hellowWorldText != null)
        {
            hellowWorldText.text = "Hello, World!";
            StartCoroutine(ClearHelloWorldAfterDelay(3f));

        }
        else
        {
            Debug.LogWarning("hellowWorldText is not assigned in the inspector.");
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

    // < summary>
    // Methode to send custom data as JSON string
    // < /summary>

    private void SendCoolData()
    {
     IdkCoolClass coolData = new IdkCoolClass
     {
         coolScore = 42,
         coolString = "This is a cool string!",
         someList = new List<string> { "Item1", "Item2", "Item3" }
     } ;

        Debug.Log($"$$$$ Sending Cool Data: {coolData}");

        _playroomKit.RpcCall("ReceiveCoolData",coolData, PlayroomKit.RpcMode.ALL, () =>
        {
            Debug.Log("RPC call to send cool data sent successfully.");
        });
    
          _playroomKit.RpcCall("ReceiveCoolData", "test message", PlayroomKit.RpcMode.ALL, () =>
            {
                 Debug.Log("RPC call sent successfully.");
            });
    
      }

    //<summary>
    // Methode to receive custom data as JSON string
    //< /summary>
    private void ReceiveCoolData(string jsonData)
    {
        Debug.Log($"$$$$ Received Cool Data: {jsonData}");
        try
        {
            IdkCoolClass recievedData = JsonUtility.FromJson<IdkCoolClass>(jsonData);
            if (recievedData != null)
            {
                Debug.Log($"Received Cool Score: CoolScore = {recievedData.coolScore}");
                Debug.Log($"Received Cool String: {recievedData.coolString}");
                Debug.Log($"Received List Count :{recievedData.someList?.Count ?? 0} ");
            }
            else
            {
                Debug.LogError("Failed to deserialize received cool data.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deserializing cool data: {ex.Message}");
        }
        
    }
}



[System.Serializable]
public class IdkCoolClass
{
    public int coolScore;
    public string coolString;
    public List<string> someList;
}