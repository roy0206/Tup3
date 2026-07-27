using System;
using UnityEngine;

public class SoilDrop : MonoBehaviour
{
    [SerializeField] private float accel;
    private float speed = 0;

    private void OnEnable()
    {
        speed = 0;
    }

    private void Update()
    {
        speed += accel * Time.deltaTime;
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }
}
