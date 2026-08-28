using System;
using UnityEngine;
using DG.Tweening;

public class SwordTrap : MonoBehaviour
{
    private void OnEnable()
    {
        /*transform.DOMoveY(-1, 0.5f).SetEase(Ease.InQuad);
        DOVirtual.DelayedCall(3f,
            () =>
            {
                transform.DOMoveY(-3, 0.5f).SetEase(Ease.InQuad)
                    .OnComplete(() => PoolManager.Instance.Release(gameObject));
            });*/
    }
}
