using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <summary> The ID for panels. </summary>
public enum PanelIds
{
    Start,
    Join,
    Create,
    Server,
}

/// <summary> A manager for all UI elements. </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager s_singleton;


    #region UI Elements
    [SerializeField]
    [Tooltip("All UI screens that are availible.")]
    private GameObject[] _panels;

    [Header("Start Panel")]
    [SerializeField]
    [Tooltip("Button for joining a server.")]
    private Button _joinButton;
    [SerializeField]
    [Tooltip("Button for creating a server.")]
    private Button _createButton;

    [Header("Join Panel")]
    [SerializeField]
    [Tooltip("Input field for the server address.")]
    private TMP_InputField _addressInput;
    [SerializeField]
    [Tooltip("Input field for the username.")]
    private TMP_InputField _usernameInput;
    [SerializeField]
    [Tooltip("Input field for the password.")]
    private TMP_InputField _passwordInput;
    [SerializeField]
    [Tooltip("Button for connecting to the server.")]
    private Button _connectButton;
    [SerializeField]
    [Tooltip("Button for going back to the start panel.")]
    private Button _backJoinButton;

    [Header("Create Panel")]
    [Tooltip("Parent of all world buttons.")]
    public Transform WorldParent;
    [Tooltip("Prefab for the buttons to select the world")]
    public GameObject WorldButtonPrefab;
    [SerializeField]
    [Tooltip("Holds text that says there are no available worlds")]
    private GameObject _noWorlds;
    [SerializeField]
    [Tooltip("Button for starting an existing world.")]
    private Button _startWorldButton;
    [SerializeField]
    [Tooltip("Button for creating a new world.")]
    private Button _createWorldButton;
    [SerializeField]
    [Tooltip("Button for going back to the start panel.")]
    private Button _backCreateButton;

    [Header("Server Panel")]
    [SerializeField]
    [Tooltip("Input field for the world name.")]
    private TMP_InputField _worldNameInput;
    [SerializeField]
    [Tooltip("Input field for the port.")]
    private TMP_InputField _portInput;
    [SerializeField]
    [Tooltip("Input field for the max number of players.")]
    private TMP_InputField _maxPlayersInput;
    [SerializeField]
    [Tooltip("Toggle to join as both server and client.")]
    private Toggle _clientToggle;
    [SerializeField]
    [Tooltip("Input field for the username of the host client.")]
    private TMP_InputField _hostNameInput;
    [Tooltip("Input field for the password of the host client.")]
    private TMP_InputField _hostPasswordInput;
    [SerializeField]
    [Tooltip("Button for starting the server.")]
    private Button _startButton;
    [SerializeField]
    [Tooltip("Button for going back to the create panel.")]
    private Button _backServerButton;

    [Header("Miscellaneous")]
    [SerializeField]
    [Tooltip("Displays errors to the user.")]
    private TextMeshProUGUI _errorText;
    #endregion


    /// <summary> The singleton instance of the UI manager. </summary>
    public static UIManager Singleton
    {
        get => s_singleton;
        private set
        {
            if (s_singleton == null)
                s_singleton = value;
            else if (s_singleton != value)
                Destroy(value);
        }
    }
    /// <summary> The username of the local client. </summary>
    public string Username { get; private set; }
    /// <summary> The password of the local client. </summary>
    public string Password { get; private set; }

    /// <summary> The text used to display errors. </summary>
    public TextMeshProUGUI ErrorText { get => _errorText; }

    private void Awake()
    {
        Singleton = this;
    }

    // Give functionality to all the components.
    private void Start()
    {
        _joinButton.onClick.AddListener(() => ActivatePanel(PanelIds.Join));
        _createButton.onClick.AddListener(() => ActivatePanel(PanelIds.Create));

        _connectButton.onClick.AddListener(() => Connect());
        _backJoinButton.onClick.AddListener(() => ActivatePanel(PanelIds.Start));

        _startWorldButton.onClick.AddListener(() => SelectWorld());
        _createWorldButton.onClick.AddListener(() => CreateWorld());
        _backCreateButton.onClick.AddListener(() => ActivatePanel(PanelIds.Start));

        _clientToggle.onValueChanged.AddListener((bool value) => _hostNameInput.gameObject.SetActive(value));
        _startButton.onClick.AddListener(() => StartWorld());
        _backServerButton.onClick.AddListener(() => ActivatePanel(PanelIds.Server));
    }

    /// <summary> Sets the desired panel active and all others inactive. If the panel is the create panel, reset the world list. </summary>
    /// <param name="panel"> The id of the panel to be activated. </param>
    public void ActivatePanel(int panel)
    {
        ErrorText.text = "";
        for (int i = 0; i < _panels.Length; i++)
            _panels[i].SetActive(panel == i);
        if (panel == (int)PanelIds.Create)
        {
            bool enableText = true;
            float height = -WorldParent.GetComponent<VerticalLayoutGroup>().spacing;
            foreach (string fileName in Directory.GetFiles(Application.dataPath + "/SavedWorlds"))
            {
                if (fileName == Application.dataPath + "/SavedWorlds/default.txt" || fileName.EndsWith(".txt.meta"))
                    continue;
                enableText = false;
                StringBuilder builder = new StringBuilder(fileName);
                builder.Remove(0, (Application.dataPath + "/SavedWorlds/").Length).Remove(builder.Length - 4, 4);
                WorldButton.Create(builder.ToString());
                height += WorldButtonPrefab.GetComponent<RectTransform>().sizeDelta.y + WorldParent.GetComponent<VerticalLayoutGroup>().spacing;
                WorldParent.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, height);
            }
            _noWorlds.SetActive(enableText);
        }
    }
    public void ActivatePanel(PanelIds panel) => ActivatePanel((int)panel);


    /// <summary> Set the start world buttons interactable state. </summary>
    /// <param name="state"> Whether or not the button is interactable. </param>
    public void SetStartWorldButtonInteractable(bool state)
    {
        _startWorldButton.interactable = state;
    }

    #region Button Methods
    // Connect to the server with the inputted address.
    private void Connect()
    {
        string address = _addressInput.text;
        Username = _usernameInput.text;
        Password = _passwordInput.text;
        if (string.IsNullOrEmpty(address))
            NetworkManager.Singleton.ConnectClient();
        else
            NetworkManager.Singleton.ConnectClient(address);
    }

    // Load the default world file to the save manager path and prepare to create a new world.
    private void CreateWorld()
    {
        _worldNameInput.gameObject.SetActive(true);
        SaveManager.Singleton.Path = Application.dataPath + "/SavedWorlds/default.txt";
        ActivatePanel(PanelIds.Server);
    }

    // Load the selected world file to the save manager path and prepare to create a new world.
    private void SelectWorld()
    {
        _worldNameInput.gameObject.SetActive(false);
        ActivatePanel(PanelIds.Server);
    }

    // Start the server and load up the world, creating a new file for new worlds.
    private void StartWorld()
    {
        bool isPortValid = ushort.TryParse(_portInput.text, out ushort port);
        bool isMaxPlayersValid = ushort.TryParse(_maxPlayersInput.text, out ushort maxClients);
        if (isPortValid && isMaxPlayersValid)
            NetworkManager.Singleton.StartServer(port, maxClients);
        else if (isPortValid)
            NetworkManager.Singleton.StartServer(port);
        else if (isMaxPlayersValid)
            NetworkManager.Singleton.StartServer(maxClients: maxClients);
        else
            NetworkManager.Singleton.StartServer();
        SaveManager.Singleton.FileToWorld();
        if (SaveManager.Singleton.Path == Application.dataPath + "/SavedWorlds/default.txt")
        {
            if (string.IsNullOrEmpty(_worldNameInput.text))
                _worldNameInput.text = "New World";
            string worldName = _worldNameInput.text;
            int i = 0;
            while (File.Exists(Application.dataPath + $"/SavedWorlds/{worldName}.txt"))
            {
                i++;
                worldName = _worldNameInput.text + i;
            }
            SaveManager.Singleton.Path = Application.dataPath + $"/SavedWorlds/{worldName}.txt";
            SaveManager.Singleton.WorldToFile();
        }
        if (_clientToggle.isOn)
        {
            Username = _hostNameInput.text;
            Password = _hostPasswordInput.text;
            if (isPortValid)
                NetworkManager.Singleton.ConnectClient(NetworkManager.DEFAULT_ADDRESS + ":" + port);
            else
                NetworkManager.Singleton.ConnectClient();
        }
    }
    #endregion
}