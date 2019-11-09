using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MusicText : MonoBehaviour
{
    public Text Details;
    public CanvasGroup CanvasGroup;

    IEnumerator Sequence(string detailText)
    {
        yield return new WaitForSeconds(5);
        
        if (!string.IsNullOrEmpty(detailText))
            Details.text = detailText;

        for (float t = 0; t < 1; t += Time.deltaTime / 1.5f)
        {
            CanvasGroup.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }

        CanvasGroup.alpha = 1;
        yield return new WaitForSeconds(5);

        for (float t = 0; t < 1; t += Time.deltaTime / 1.5f)
        {
            CanvasGroup.alpha = Mathf.Lerp(1, 0, t);
            yield return null;
        }

        CanvasGroup.alpha = 0;
    }

    public void StartSequence(string detailText = null)
    {
        StartCoroutine(Sequence(detailText));
    }
}
