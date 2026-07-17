using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonoUI : MonoBehaviour
{
    protected Canvas rootCanvas;
    protected Image image;
    protected Button button;
    [SerializeField] protected TMP_Text linkedText;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }
}
