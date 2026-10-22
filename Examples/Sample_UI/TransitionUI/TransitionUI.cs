using System;
using System.Collections;
using DG.Tweening;
using Metal;
using Metal.BaseGame;
using Teo.AutoReference;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TransitionUI : BaseUI
{
    private static readonly int CircleRadius = Shader.PropertyToID(CIRLE_RADIUS);

    [SerializeField, GetInChildren, Name("Container")]
    private Image _image;

    [SerializeField, GetInChildren, Name("Logo")]
    private Image _logo;
    [SerializeField, GetInChildren, Name("BgText")]
    private TextMeshProUGUI _bgText;

    private Material _material;

    [Tooltip("tham số của shader")] private const string CIRLE_RADIUS = "_CircleRadius";

    protected UIManager UI => ServiceLocator.Game.Get<UIManager>();

    protected override bool IsUseAnim()
    {
        return false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnAfterSyncAttribute()
    {
        base.OnAfterSyncAttribute();
        _logo.gameObject.SetActive(false);
        _bgText.gameObject.SetActive(false);
    }

    // float transitionTime = 0.5f;
    private float _transitionTime = 0.7f;

    public void OnClose(Action callBack = null)
    {
        // GameAction.OnTrasitionClose?.Invoke();
        DoSetFloat(2, 0, _transitionTime, false, callBack);
    }

    public void OnOpen(Action callBack = null)
    {
        StartCoroutine(IEOnOpen(callBack));
    }

    private IEnumerator IEOnOpen(Action callBack = null)
    {
        yield return new WaitForSeconds(_transitionTime);
        DoSetFloat(0, 1, _transitionTime, true, callBack);
    }

    private void DoSetFloat(float startRadius, float endRadius, float duration = 0.5f, bool isOpen = true,
        Action callBack = null)
    {
        // SoundManager.Instance.PlaySFX(SoundType.Swipe);
        _material ??= _image.material;

        _material.SetFloat(CircleRadius, startRadius);
        _logo.gameObject.SetActive(false);
        _bgText.gameObject.SetActive(false);

        DOTween.To(
                () => _material.GetFloat(CircleRadius),
                r => _material.SetFloat(CircleRadius, r),
                endRadius,
                duration)
            .SetUpdate(UpdateType.Normal, true) // Vẫn chạy đc khi Time.timeScale = 0;
            .OnComplete(() =>
            {
                _logo.gameObject.SetActive(!isOpen);
                _bgText.gameObject.SetActive(!isOpen);
                callBack?.Invoke();
                if (isOpen)
                {
                    UI.Hide<TransitionUI>();
                }
            });
    }
}