using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceButton : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI text;
    public Dialogue scene;

    ChoiceBox choiceBox;

    private void Awake()
    {
        choiceBox = FindObjectOfType<ChoiceBox>();
        button = GetComponent<Button>();
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnClick() => choiceBox.ClickButton(this);
}
