using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> When clicked, selects a world file to use for the server. </summary>
public class WorldButton : MonoBehaviour
{
    private static List<WorldButton> s_worldButtons = new();
    private Button _button;
    private TextMeshProUGUI _text;
    private Image _image;
    private string _worldName;
    private bool _isSelected;

    /// <summary> Create a <see cref="WorldButton"/>. </summary>
    /// <param name="worldName"> The name of the world. </param>
    public static void Create(string worldName)
    {
        WorldButton button = Instantiate(UIManager.Singleton.WorldButtonPrefab, UIManager.Singleton.WorldParent).GetComponent<WorldButton>();
        button._worldName = worldName;
        button._text.text = worldName;
    }

    private void Awake()
    {
        s_worldButtons.Add(this);
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() => SelectWorld());
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _image = GetComponent<Image>();
    }

    private void SelectWorld()
    {
        _isSelected = !_isSelected;
        _image.color = _isSelected ? Color.cyan : Color.white;
        UIManager.Singleton.SetStartWorldButtonInteractable(_isSelected);
        if (_isSelected)
        {
            SaveManager.Singleton.Path = Application.dataPath + $"/SavedWorlds/{_worldName}.txt";
            foreach (WorldButton button in s_worldButtons)
            {
                if (button != this)
                {
                    button._isSelected = false;
                    _image.color = Color.white;
                }
            }
        }
    }
}