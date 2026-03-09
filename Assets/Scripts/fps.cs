using System;
using TMPro;
using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    public TextMeshProUGUI text;
    float timer;

	public string extraOutput = "";

	private string elapsedTimeString() {
		float elapsed = Time.time;
		TimeSpan t = TimeSpan.FromSeconds(elapsed);
		return $"Simulation Time {t:hh\\:mm\\:ss}";
	}


    void Update()
    {
        // Update once per 0.2 seconds to avoid flicker
        timer += Time.unscaledDeltaTime;
        if (timer >= 0.2f)
        {
            float fps = 1f / Time.unscaledDeltaTime;
			string outputText = $"{fps:F0} FPS";
			outputText += "\n";
			outputText += elapsedTimeString();
			outputText += "\n";
			outputText += extraOutput;

            text.text = outputText;

            timer = 0f;
        }
    }
}
