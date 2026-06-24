using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuHandler : MonoBehaviour
{
    [SerializeField] Button _playButton;
    [SerializeField] Button _exitButton;
    [SerializeField] TextMeshProUGUI _title;
    [SerializeField] TextMeshProUGUI _subtitle;
    [SerializeField] TextMeshProUGUI _play;
    [SerializeField] TextMeshProUGUI _exit;
    
    public static bool AllowLoad = false;
    void OnEnable()
    {
        InputSystem.actions["Exit"].performed += Exit;
    }

    void OnDisable()
    {
        InputSystem.actions["Exit"].performed -= Exit;
    }

    void Awake()
    {
        AllowLoad = false;
    }

    public void Play()
    {
        DisableButtons();
        StartCoroutine(FadeOut());
        StartCoroutine(LoadAsync());
    }

    void Exit(InputAction.CallbackContext ctx)
    {
        Application.Quit();
    }
    IEnumerator FadeOut()
    {
        var t = 0.0f;
        while (true)
        {
            _title.color = Color.Lerp(Color.white, Color.clear, t);
            _subtitle.color = Color.Lerp(Color.white, Color.clear, t);
            _play.color = Color.Lerp(Color.white, Color.clear, t);
            _exit.color = Color.Lerp(Color.white, Color.clear, t);
            t += Time.deltaTime * 0.5f;
            yield return null;
            if (t >= 1.0f)
            {
                break;
            }
        }

        AllowLoad = true;
    }

    void DisableButtons()
    {
        _exitButton.interactable = false;
        _playButton.interactable = false;
    }

    IEnumerator LoadAsync()
    {
        var load = SceneManager.LoadSceneAsync("Scenes/PlayScene");
        load.allowSceneActivation = false;
        while (!load.isDone)
        {
            if (load.progress >= 0.9f)
            {
                if (AllowLoad)
                {
                    load.allowSceneActivation = true;
                }

                yield return null;
            }
        }
    }
    public void Quit()
    {
        Application.Quit();
    }
}
