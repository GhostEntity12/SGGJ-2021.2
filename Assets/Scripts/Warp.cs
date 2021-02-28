using System.Collections;
using UnityEngine;

public class Warp : Interactable
{
    public bool fadeCamera;
	public Transform warpPoint;

    CanvasGroup canvasGroup;
    private void Start()
    {
        canvasGroup = GameObject.FindGameObjectWithTag("FadeCanvas").GetComponent<CanvasGroup>();
    }

    public override void Interact()
    {
        if (blocksInput) PlayerController.player.canInput = false;
        if (fadeCamera)
        {
            StartCoroutine(WarpPlayer());
        }
        else
        {
            PlayerController.player.transform.position = warpPoint.position;
        }
    }

	IEnumerator WarpPlayer()
	{
        // For keeping track of the fade
        float timeAtStart = Time.time;
        float timeSinceStart;
        float percentageComplete = 0;

        while (percentageComplete < 1) // Keeps looping until the lerp is complete
        {
            timeSinceStart = Time.time - timeAtStart;
            percentageComplete = timeSinceStart / 0.3f;

            float currentValue = Mathf.Lerp(0, 1, percentageComplete);

            canvasGroup.alpha = currentValue;
            yield return new WaitForEndOfFrame();
        }
        PlayerController.player.transform.position = warpPoint.position;

        yield return new WaitForSeconds(0.3f);

        // For keeping track of the fade
        timeAtStart = Time.time;
        timeSinceStart = 0;
        percentageComplete = 0;

        while (percentageComplete < 1) // Keeps looping until the lerp is complete
        {
            timeSinceStart = Time.time - timeAtStart;
            percentageComplete = timeSinceStart / 0.3f;

            float currentValue = Mathf.Lerp(1, 0, percentageComplete);

            canvasGroup.alpha = currentValue;
            yield return new WaitForEndOfFrame();
        }
        PlayerController.player.canInput = true;
    }
}
