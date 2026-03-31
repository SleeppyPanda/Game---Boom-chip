using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BottleController : MonoBehaviour
{
    [Header("Animation Objects")]
    public GameObject bottomSmoke;
    public GameObject topSmoke;
    public Image bottleImage;

    [Header("Settings")]
    public float smokeDuration = 0.6f;
    public float delayBeforeSpriteChange = 0.15f;

    private Coroutine lowerSmokeCoroutine;
    private Coroutine upperSmokeCoroutine;

    public void PlayLowerSmoke(Sprite newSprite)
    {
        if (bottomSmoke != null)
        {
            if (lowerSmokeCoroutine != null) StopCoroutine(lowerSmokeCoroutine);
            lowerSmokeCoroutine = StartCoroutine(LowerSmokeRoutine(newSprite));
        }
        else
        {
            if (bottleImage != null && newSprite != null)
                bottleImage.sprite = newSprite;
        }
    }

    private IEnumerator LowerSmokeRoutine(Sprite newSprite)
    {
        bottomSmoke.SetActive(false);
        bottomSmoke.SetActive(true);

        yield return new WaitForSeconds(delayBeforeSpriteChange);

        if (bottleImage != null && newSprite != null)
        {
            bottleImage.sprite = newSprite;
        }

        float remainingTime = smokeDuration - delayBeforeSpriteChange;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        bottomSmoke.SetActive(false);
        lowerSmokeCoroutine = null;
    }

    public void PlayUpperLand()
    {
        if (topSmoke != null)
        {
            if (upperSmokeCoroutine != null) StopCoroutine(upperSmokeCoroutine);
            upperSmokeCoroutine = StartCoroutine(UpperSmokeRoutine());
        }
    }

    private IEnumerator UpperSmokeRoutine()
    {
        topSmoke.SetActive(false);
        topSmoke.SetActive(true);

        yield return new WaitForSeconds(smokeDuration);

        topSmoke.SetActive(false);
        upperSmokeCoroutine = null;
    }

    public void InactiveUpperLand()
    {
        if (upperSmokeCoroutine != null) StopCoroutine(upperSmokeCoroutine);
        if (topSmoke != null) topSmoke.SetActive(false);
    }

    public void InactiveLowerSmoke()
    {
        if (lowerSmokeCoroutine != null) StopCoroutine(lowerSmokeCoroutine);
        if (bottomSmoke != null) bottomSmoke.SetActive(false);
    }

    public void ClearAllEffects()
    {
        StopAllCoroutines();
        lowerSmokeCoroutine = null;
        upperSmokeCoroutine = null;
        if (bottomSmoke != null) bottomSmoke.SetActive(false);
        if (topSmoke != null) topSmoke.SetActive(false);
    }
}