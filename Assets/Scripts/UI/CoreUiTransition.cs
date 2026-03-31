using UnityEngine;
using DG.Tweening;
using System;

public class CoreUiTransition : MonoBehaviour
{
    [Header("Cấu hình dải màu")]
    public RectTransform leftStrip;
    public RectTransform rightStrip;

    [Header("Cấu hình đối tượng bay")]
    public RectTransform player2_Left;
    public RectTransform player1_Right;
    public RectTransform logo;

    [Header("Cài đặt Animation")]
    public float duration = 0.8f;
    public Ease easeInType = Ease.OutBack;
    public Ease easeOutType = Ease.InBack;
    public float offscreenDistanceX = 2500f;
    public float offscreenDistanceY = 2500f;

    [Header("Tự động chạy khi xuất hiện")]
    public bool playOnStart = true; // THÊM BIẾN NÀY ĐỂ TÙY CHỌN TỰ ĐỘNG CHẠY

    private Vector2 _leftStripOrig;
    private Vector2 _rightStripOrig;
    private Vector2 _player2Orig;
    private Vector2 _player1Orig;
    private Vector2 _logoOrig;

    private void Awake()
    {
        // Ghi nhớ lại toàn bộ tọa độ chuẩn bạn đã xếp trong Unity
        if (leftStrip) _leftStripOrig = leftStrip.anchoredPosition;
        if (rightStrip) _rightStripOrig = rightStrip.anchoredPosition;
        if (player2_Left) _player2Orig = player2_Left.anchoredPosition;
        if (player1_Right) _player1Orig = player1_Right.anchoredPosition;
        if (logo) _logoOrig = logo.anchoredPosition;
    }

    private void Start()
    {
        // NẾU TÍCH VÀO PLAY ON START -> TỰ ĐỘNG GIẤU ĐI RỒI BAY VÀO NGAY LẬP TỨC
        if (playOnStart)
        {
            SetInitialState();
            PlayInAnimation();
        }
    }

    public void SetInitialState()
    {
        if (leftStrip) leftStrip.anchoredPosition = _leftStripOrig + new Vector2(-offscreenDistanceX, 0);
        if (player2_Left) player2_Left.anchoredPosition = _player2Orig + new Vector2(-offscreenDistanceX, 0);

        if (rightStrip) rightStrip.anchoredPosition = _rightStripOrig + new Vector2(offscreenDistanceX, 0);
        if (player1_Right) player1_Right.anchoredPosition = _player1Orig + new Vector2(offscreenDistanceX, 0);

        if (logo) logo.anchoredPosition = _logoOrig + new Vector2(0, offscreenDistanceY);
    }

    public void PlayInAnimation()
    {
        float delay = 0.15f;

        if (leftStrip) leftStrip.DOAnchorPos(_leftStripOrig, duration).SetEase(easeInType);
        if (rightStrip) rightStrip.DOAnchorPos(_rightStripOrig, duration).SetEase(easeInType);

        if (player2_Left) player2_Left.DOAnchorPos(_player2Orig, duration).SetEase(easeInType).SetDelay(delay);
        if (player1_Right) player1_Right.DOAnchorPos(_player1Orig, duration).SetEase(easeInType).SetDelay(delay);
        if (logo) logo.DOAnchorPos(_logoOrig, duration).SetEase(easeInType).SetDelay(delay);
    }

    public void PlayOutAnimation(Action onComplete = null)
    {
        if (leftStrip) leftStrip.DOAnchorPos(_leftStripOrig + new Vector2(-offscreenDistanceX, 0), duration).SetEase(easeOutType);
        if (player2_Left) player2_Left.DOAnchorPos(_player2Orig + new Vector2(-offscreenDistanceX, 0), duration).SetEase(easeOutType);

        if (rightStrip) rightStrip.DOAnchorPos(_rightStripOrig + new Vector2(offscreenDistanceX, 0), duration).SetEase(easeOutType);
        if (player1_Right) player1_Right.DOAnchorPos(_player1Orig + new Vector2(offscreenDistanceX, 0), duration).SetEase(easeOutType);

        if (logo) logo.DOAnchorPos(_logoOrig + new Vector2(0, offscreenDistanceY), duration).SetEase(easeOutType)
            .OnComplete(() => {
                Destroy(gameObject);
                onComplete?.Invoke();
            });
    }
}