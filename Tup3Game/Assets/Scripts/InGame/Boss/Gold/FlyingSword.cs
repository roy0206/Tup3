
using System;
using UnityEngine;
using DG.Tweening;

public class FlyingSword : MonoBehaviour
{
    [SerializeField] private float speed;
    private PlayerKnockBack player;
    BoxCollider2D bc;
    bool isStoped = false;
    bool isFixed = false;

    
    public float Timer { get; set; }
    private void OnEnable()
    {
        player = FindAnyObjectByType<PlayerKnockBack>();
        bc = GetComponent<BoxCollider2D>();
        bc.enabled = true;
        isStoped = false;
        isFixed = false;

        int randLength = UnityEngine.Random.Range(3, 4);
        float randAngle = UnityEngine.Random.Range(60f, 120f);
        Vector3 targetPos = transform.position + new Vector3(randLength * Mathf.Cos(randAngle *  Mathf.Deg2Rad), randLength * Mathf.Sin(randAngle *  Mathf.Deg2Rad));
        transform.DOMove(targetPos, 1f);
        Timer = 3;
    }

    private void Update()
    {
        CheckGround();
        Timer -= Time.deltaTime;
        var vec = player.transform.position - transform.position;
        if (!isFixed)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(vec), Time.deltaTime * speed);
        }
        if (Timer <= 0f && !isStoped)
        {
            isFixed = true;
            transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.World);
        }
    }

    void CheckGround()
    {
        if(isStoped) return;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.2f);
        foreach (var col in colliders)
        {
            if (col.gameObject.layer == LayerMask.GetMask("ground"))
            {
                isStoped = true;
                bc.enabled = false;
            }
        }
    }
    
}
