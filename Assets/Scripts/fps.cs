using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    public TextMeshProUGUI text;
    float timer;

    void Update()
    {
        // Update once per 0.2 seconds to avoid flicker
        timer += Time.unscaledDeltaTime;
        if (timer >= 0.2f)
        {
            float fps = 1f / Time.unscaledDeltaTime;
            text.text = $"{fps:F0} FPS";
            timer = 0f;
        }
    }
}
