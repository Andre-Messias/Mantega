using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputMapManager_Example : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The Input Action Asset containing the maps.")]
    [SerializeField] private InputActionAsset _inputActions;

    [Header("Starting State")]
    [Tooltip("The name of the map to enable on Start (e.g., 'Player').")]
    [SerializeField] private string _defaultMapName = string.Empty;

    private void Awake()
    {
        ValidateDependencies();
    }

    private void Start()
    {
        SwitchToMap(_defaultMapName);
    }

    /// <summary>
    /// Enables a specific map and disables all others in the asset.
    /// </summary>
    /// <param name="mapName">The exact name of the map in the Action Asset.</param>
    public void SwitchToMap(string mapName)
    {
        if (string.IsNullOrEmpty(mapName)) return;

        // Desabilita todos para garantir que apenas um esteja ativo
        _inputActions.Disable();

        InputActionMap targetMap = _inputActions.FindActionMap(mapName);

        if (targetMap != null)
        {
            targetMap.Enable();
            Debug.Log($"[InputMapManager] Switched to map: {mapName}");
        }
        else
        {
            Debug.LogError($"[InputMapManager] Map '{mapName}' not found in asset.");
        }
    }

    /// <summary>
    /// Enables a map by its name without disabling others.
    /// </summary>
    public void EnableMap(string mapName) => _inputActions.FindActionMap(mapName)?.Enable();

    /// <summary>
    /// Disables a map by its name.
    /// </summary>
    public void DisableMap(string mapName) => _inputActions.FindActionMap(mapName)?.Disable();

    private void ValidateDependencies()
    {
        if (_inputActions == null)
        {
            throw new ArgumentNullException(nameof(_inputActions), "InputActionAsset is missing on InputMapManager.");
        }
    }
}
