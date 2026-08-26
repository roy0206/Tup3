using UnityEngine;
using DG.Tweening;

public class StartPlayerAnimation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.DORotate(new Vector3(0,0,65), 1f).SetLoops(10, LoopType.Yoyo).SetEase(Ease.Linear)
            .OnComplete(()=> transform.DORotate(new Vector3(0, 0, 66), 1).SetEase(Ease.InQuad));
    }
}
