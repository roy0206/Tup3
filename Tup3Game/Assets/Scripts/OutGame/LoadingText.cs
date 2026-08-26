using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingText : MonoBehaviour
{
    TMP_Text text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<TMP_Text>();
        StartCoroutine(TextChange());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator TextChange()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            text.text = "Loading";
            yield return new WaitForSeconds(0.5f);
            text.text = "Loading.";
            yield return new WaitForSeconds(0.5f);
            text.text = "Loading..";
            yield return new WaitForSeconds(0.5f);
            text.text = "Loading...";
        }
    }
}
