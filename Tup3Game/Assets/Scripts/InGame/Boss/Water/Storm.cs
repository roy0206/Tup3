using UnityEngine;

public class Storm : MonoBehaviour
{
    [SerializeField] private float pullPower = 3f;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private float delay;
    [SerializeField] private float scale;
    [SerializeField] private float damage;
    [SerializeField] private bool isAlive;
    [SerializeField] private Playermovement player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Update()
    {
            player.ApplyGravityPull(
                transform.position,
                pullPower
            );
    }


    void Start()
    {
        
    }
}
