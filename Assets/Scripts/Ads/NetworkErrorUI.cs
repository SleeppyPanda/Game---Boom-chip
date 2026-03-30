using UnityEngine;
using UnityEngine.UI;
using System;

public class NetworkErrorUI : MonoBehaviour
{
    public static NetworkErrorUI Instance;

    [Header("UI Components")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button retryButton;

    private Action _onRetryAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }

        if (panel != null) panel.SetActive(false);

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }
    }

    public void Show(Action retryAction)
    {
        _onRetryAction = retryAction;
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        _onRetryAction = null;
    }

    private void OnRetryClicked()
    {
        Action retry = _onRetryAction;
        Hide();
        retry?.Invoke();
    }
}