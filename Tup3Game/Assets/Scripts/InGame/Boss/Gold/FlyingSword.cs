
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
        float randAngle = UnityEngine.Random.Range(30f, 150f);
        Vector3 targetPos = transform.position + new Vector3(randLength * Mathf.Cos(randAngle *  Mathf.Deg2Rad), randLength * Mathf.Sin(randAngle *  Mathf.Deg2Rad));
        transform.DOMove(targetPos, 1f);
        Timer = UnityEngine.Random.Range(3f, 5f);
    }

    private void Update()
    {
        
    }

    private void FixedUpdate()
    {

        Timer -= Time.fixedDeltaTime;
        var vec = player.transform.position - transform.position;
        if (!isFixed)
        {
            var angle = Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg;
            if (angle > transform.eulerAngles.y)
            {
                transform.rotation = Quaternion.Euler(0,0, Mathf.Lerp(transform.eulerAngles.z,angle, Time.fixedDeltaTime));
            }
            else
            {
                transform.rotation = Quaternion.Euler(0,0, Mathf.Lerp(angle, transform.eulerAngles.z, Time.fixedDeltaTime));
            }

        }
        if (Timer <= 0f && !isStoped)
        {
            CheckGround();
            isFixed = true;
            transform.Translate(Vector2.right * speed * Time.fixedDeltaTime, Space.Self);
        }
    }

    void CheckGround()
    {
        if(isStoped) return;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.2f);
        foreach (var col in colliders)
        {
            if (col.gameObject.layer == 6)
            {
                isStoped = true;
                bc.enabled = false;
            }
        }
    }
    
}
