using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthView : MonoUI
{
     [SerializeField] GameObject healthSubject;
     private Image red;
     private void Start()
     {
          red = transform.GetChild(0).GetComponent<Image>();
          if (red == null)
          {
               Debug.LogWarning("HealthView has no Image component!");
               return;
          }

          if (healthSubject == null || !healthSubject.TryGetComponent(out IHealthUIEvent Event))
          {
               Debug.LogWarning("HealthView has no health subject component!");
               return;
          }
          
          Event.OnHealthChanged += UpdateHealthUI;
     }

     public void UpdateHealthUI(float current, float max)
     {
          red.fillAmount = current/max;
     }
     
}
