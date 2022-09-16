using UnityEngine;

/// <summary> A manager that handles general information about the game state. </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager s_singleton;
    [SerializeField]
    [Tooltip("The parent of all world objects")]
    private GameObject _world;
    [SerializeField]
    [Tooltip("The prefabs for world objects.")]
    private GameObject[] _worldObjectPrefabs;

    /// <summary> The singleton instance of the game manager. </summary>
    public static GameManager Singleton
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
    /// <summary> The parent of world objects. </summary>
    public GameObject World { get => _world; }
    /// <summary> The prefabs for world objects. </summary>
    public GameObject[] WorldObjectPrefabs { get => _worldObjectPrefabs; }

    private void Awake()
    {
        Singleton = this;
    }
}