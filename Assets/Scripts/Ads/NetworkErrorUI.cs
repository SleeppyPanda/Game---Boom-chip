using UnityEngine;
using UnityEngine.UI;
using System;

public class NetworkErrorUI : MonoBehaviour
{
    public static NetworkErrorUI Instance;

    [Header("UI Components")]
    [SerializeField] private Button retryButton;

    private CanvasGroup _canvasGroup;
    private Action _onRetryAction;
    private volatile bool _pendingShow = false;
    private volatile bool _pendingHide = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=green>[NetworkErrorUI] Instance initialized OK</color>");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Dùng CanvasGroup để ẩn/hiện, giữ GameObject luôn active để Update() chạy
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetVisible(false);

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }
    }

    public void Show(Action retryAction)
    {
        Debug.Log("<color=yellow>[NetworkErrorUI] Show() called</color>");
        _onRetryAction = retryAction;
        _pendingShow = true;
    }

    public void Hide()
    {
        _pendingHide = true;
        _onRetryAction = null;
    }

    private void Update()
    {
        if (_pendingShow)
        {
            _pendingShow = false;
            _pendingHide = false;
            SetVisible(true);
            Debug.Log("<color=green>[NetworkErrorUI] Panel shown on main thread</color>");
        }

        if (_pendingHide)
        {
            _pendingHide = false;
            SetVisible(false);
        }
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
        }
    }

    private void OnRetryClicked()
    {
        Action retry = _onRetryAction;
        Hide();
        retry?.Invoke();
    }
}
