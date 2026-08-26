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
        if (PauseManager.IsPaused) return;

        speed += accel * Time.deltaTime;
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }
}
