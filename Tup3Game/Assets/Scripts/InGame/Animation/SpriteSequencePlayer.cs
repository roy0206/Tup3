using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSequencePlayer : MonoBehaviour
{
    [SerializeField] private List<Sprite> sprites = new List<Sprite>();
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private bool loop;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (sprites.Count > 0) StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        var interval = new WaitForSeconds(1f / frameRate);
        do
        {
            foreach (var sprite in sprites)
            {
                spriteRenderer.sprite = sprite;
                yield return interval;
            }
        } while (loop);
    }
}
