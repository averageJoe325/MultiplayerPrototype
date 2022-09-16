using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using RiptideNetworking;
using RiptideNetworking.Utils;
using UnityEngine;

/// <summary> The type of message being sent from the server to the client. </summary>
public enum ServerToClientId : ushort
{
    CreateWorldObject,
    DestroyWorldObject,
    MoveAndRotateWorldObject,
    UpdateHealthWorldObject,
    UpdateStatusEffectsWorldObject,
    UpdateInventoryWorldObject,
}

/// <summary> The type of message being sent from the client to the server. </summary>
public enum ClientToServerId : ushort
{
    Username,
}

/// <summary> A manager that can handle both server startup and shutdown and client connection and disconnection. </summary>
public class NetworkManager : MonoBehaviour
{
    /// <summary> The localhost ip address which serves as the default ip adress. </summary>
    public const string DEFAULT_ADDRESS = "127.0.0.1";
    /// <summary> The default port. </summary>
    public const ushort DEFAULT_PORT = 7777;
    /// <summary> The default maximum number of clients. </summary>
    public const ushort DEFAULT_MAX_CLIENTS = 20;
    private static NetworkManager s_singleton;

    /// <summary>The singleton instance of the network manager.</summary>
    public static NetworkManager Singleton
    {
        get
        {
            return s_singleton;
        }
        private set
        {
            if (s_singleton == null)
                s_singleton = value;
            else if (s_singleton != value)
                Destroy(value);
        }
    }
    /// <summary>The local server.</summary>
    public Server Server { get; private set; } = new();
    /// <summary> The local client. </summary>
    public Client Client { get; private set; } = new();

    // Sets up the singleton and initializes the Riptide logger.
    private void Awake()
    {
        Singleton = this;
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
    }

    private void Start()
    {
        Server.ClientConnected += OnClientConnected;
        Client.Connected += OnConnected;
        Client.ConnectionFailed += OnConnectionFailed;
        Client.Disconnected += OnDisconnected;
    }

    // Ticks the server and client.
    private void FixedUpdate()
    {
        Server.Tick();
        Client.Tick();
    }

    // Shuts down the server and client on quit.
    private void OnApplicationQuit()
    {
        if (Client.IsConnected)
            DisconnectClient();
        if (Server.IsRunning)
            StopServer();
    }

    private void ClearWorldObjectDict()
    {
        List<WorldObject> worldObjects = new();
        foreach (WorldObject worldObject in WorldObject.WorldObjectDict.Values)
            worldObjects.Add(worldObject);
        foreach (WorldObject worldObject in worldObjects)
            worldObject.DestroyWorldObject();
    }


    #region Startup and Shutdown
    /// <summary> Starts the server. </summary>
    /// <param name="port"> The port the server is started on. </param>
    /// <param name="maxClients"> The maximum number of clients that can be connected at a time. </param>
    public void StartServer(ushort port = DEFAULT_PORT, ushort maxClients = DEFAULT_MAX_CLIENTS)
    {
        Server.Start(port, maxClients);
        UIManager.Singleton.ActivatePanel(-1);
    }

    /// <summary> Attempts to connect the client to the server. </summary>
    /// <param name="address"> The address of the server. </param>
    public void ConnectClient(string address = DEFAULT_ADDRESS)
    {
        ushort port = DEFAULT_PORT;
        int colonPos = address.IndexOf(':');
        if (colonPos != -1)
            ushort.TryParse(address.Substring(colonPos) + 1, out port);
        address = colonPos == -1 ? address : address.Substring(0, colonPos);
        if (!Regex.IsMatch(address, @"([0-9]{1,3}\.){3}[0-9]{1,3}"))
            address = DEFAULT_ADDRESS;
        address += ":" + port;
        Client.Connect(address);
    }

    /// <summary> Stops the server. </summary>
    public void StopServer()
    {
        SaveManager.Singleton.WorldToFile();
        Server.Stop();
    }

    /// <summary> Disconnects the client. </summary>
    public void DisconnectClient()
    {
        Client.Disconnect();
        ClearWorldObjectDict();
    }
    #endregion


    #region Event Handlers and Messages
    // Either runs OnHostConnected or sends data about all world objects to the new client.
    private void OnClientConnected(object sender, ServerClientConnectedEventArgs eventArgs)
    {
        if (Client.Id != eventArgs.Client.Id)
        {
            foreach (int id in WorldObject.WorldObjectDict.Keys)
            {
                WorldObject worldObject = WorldObject.WorldObjectDict[id];
                if (worldObject.Type == ObjectType.Player)
                {
                    PlayerObject playerObject = (PlayerObject)worldObject;
                    PlayerObject.SendPlayerCreate(playerObject.ClientId, playerObject.Username, playerObject.transform.position, playerObject.transform.rotation, id);
                }
                else
                    WorldObject.SendCreate(worldObject.Type, worldObject.transform.position, worldObject.transform.rotation, id);
            }
        }
    }

    // Disables all panels and sends username message.
    // Message format: string, string, string, string.
    private void OnConnected(object sender, EventArgs eventArgs)
    {
        UIManager.Singleton.ActivatePanel(-1);
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerId.Username);
        message.AddString(UIManager.Singleton.Username);
        LoginInfo info = new LoginInfo(UIManager.Singleton.Password);
        message.AddString(info.EncText);
        message.AddString(info.Salt);
        message.AddString(info.InitVector);
        Client.Send(message);
    }

    private void OnConnectionFailed(object sender, EventArgs eventArgs)
    {
        UIManager.Singleton.ErrorText.text = "Connection to the server failed.";
    }

    // Removes all world objects and resets the static variables of the world object script.
    private void OnDisconnected(object sender, EventArgs eventArgs)
    {
        ClearWorldObjectDict();
        UIManager.Singleton.ActivatePanel(PanelIds.Start);
        UIManager.Singleton.ErrorText.text = "You have been disconnected from the server.";
    }

    // Spawns the player for every client that connects.
    // Message format: string, string, string, string.
    [MessageHandler((ushort)ClientToServerId.Username)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Message handlers are not called in the script.")]
    private static void SpawnPlayer(ushort clientId, Message message)
    {
        string username = message.GetString();
        if (string.IsNullOrEmpty(username))
            username = clientId == Singleton.Client.Id ? "Host" : "Guest " + clientId;
        PlayerObject.Create(clientId, username, Vector3.zero, Quaternion.identity,
            loginInfo: new LoginInfo(message.GetString(), message.GetString(), message.GetString()));
        // TODO: How do we deal w/ both new and existing users while making sure the plaintext password is never sent to server?
    }
    #endregion
}