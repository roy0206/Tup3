using UnityEngine;
using TMPro;
public class InteractionView : MonoUI
{
    public void SetFill(float ratio)
    {
        image.fillAmount = ratio;
    }

    public void Enable()
    {
        gameObject.SetActive(true);
        image.fillAmount = 0;
    }
    public void Disable()
    {
        gameObject.SetActive(false);
    }

    public void SetPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
    }
}