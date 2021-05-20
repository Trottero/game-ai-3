using UnityEngine;
using System.Collections;

public class FpsCounter : MonoBehaviour {

    string label = "";
    float count = 0;

    private float currentFps;
    private float lowFps = float.MaxValue;
    private float highFps = float.MinValue;
    private float avgFps;

    IEnumerator Start ()
    {
        Application.targetFrameRate = -1;
        GUI.depth = 2;

        while (true) {
            if (Time.timeScale == 1) {
                yield return new WaitForSeconds (0.1f);
                count += 1;
                currentFps = (Mathf.Round (1 / Time.deltaTime));

                if (currentFps < lowFps)
                {
                    lowFps = currentFps;
                }

                if (currentFps > highFps)
                {
                    highFps = currentFps;
                }

                avgFps += (currentFps - avgFps)/count;
            }
            yield return new WaitForSeconds (0.5f);
        }
    }

    void OnGUI ()
    {
        GUI.Label (new Rect (5, 5, 200, 100), "FPS: " + currentFps);
        GUI.Label (new Rect (5, 5 + 15, 200, 100), "Low FPS: " + lowFps);
        GUI.Label (new Rect (5, 5 + 15 + 15, 200, 100), "High FPS: " + highFps);
        GUI.Label (new Rect (5, 5 + 15 + 15 + 15, 200, 100), "Avg FPS: " + Mathf.RoundToInt(avgFps));
    }
}
