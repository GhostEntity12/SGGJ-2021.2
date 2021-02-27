using UnityEngine;

public class ChoiceBox : MonoBehaviour
{
    public DialogueManager dm;
    public ChoiceButton[] choices;

    public CanvasGroup canvasGroup;

    private void Awake()
    {
        dm = FindObjectOfType<DialogueManager>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ClickButton(ChoiceButton c)
    {
        dm.choiceBoxActive = false;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = canvasGroup.blocksRaycasts = (canvasGroup.alpha == 1);
        dm.TriggerScene(c.scene);
        ResetButtons();
    }

    public void ResetButtons()
    {
        foreach (ChoiceButton button in choices)
        {
            button.gameObject.SetActive(false);
        }
    }
}
