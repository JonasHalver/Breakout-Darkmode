using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField] DynamicPlayfieldBounds _playfieldBounds;
    List<(string name, Action<Action<bool>> init)> _initSteps = new ();
    bool _initializing;
    int _currentStepIndex;
    PowerUpManager _powerUpManager;

    void Awake()
    {
        Instance = this;
        _powerUpManager = FindAnyObjectByType<PowerUpManager>();
    }

    void OnEnable()
    {
        InputSystem.actions["Exit"].performed += ctx => SceneManager.LoadScene("Scenes/MainMenu");
    }

    void OnDisable()
    {
        InputSystem.actions["Exit"].performed -= ctx => SceneManager.LoadScene("Scenes/MainMenu");
    }

    void Start()
    {
        BuildInitializationList();
        StartNextInitialization();
    }
    void BuildInitializationList()
    {
        _initSteps.Clear();

        _initSteps.Add(("PlayfieldBounds", DynamicPlayfieldBounds.Initialize));
        _initSteps.Add(("BrickLayoutGenerator", BrickLayoutGenerator.Initialize));
    }
    void StartNextInitialization()
    {
        if (_initializing)
        {
            Debug.LogWarning("Initialization in progress, skipping step.");
            return;
        }

        if (_currentStepIndex >= _initSteps.Count)
        {
            Debug.Log("Combat scene initialization complete.");
            return;
        }

        _initializing = true;
        var step = _initSteps[_currentStepIndex];

        Debug.Log($"Initializing: {step.name}");

        step.init(success =>
        {
            _initializing = false;

            if (!success)
            {
                Debug.LogError($"Initialization failed: {step.name}");
                return;
            }

            if (_currentStepIndex == _initSteps.Count - 1)
            {
                Debug.Log("All systems initialized successfully.");
                return;
            }
            _currentStepIndex++;
            StartNextInitialization();
        });
    }
}
