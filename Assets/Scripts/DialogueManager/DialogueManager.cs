using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    bool showingDialogue;
    bool isFading;
    bool isDisplayingText;
    [HideInInspector]
    public bool choiceBoxActive;
    IEnumerator displayDialogueCoroutine;

    [Header("UI")]
    CanvasGroup canvasGroup;
    [Tooltip("How quickly the UI fades in")]
    public float uiFadeInSpeed = 0.4f;
    [Space(15)]
    [Tooltip("The object which holds characters' names")]
    public TextMeshProUGUI nameBox;
    [Tooltip("The object which holds characters' dialogue")]
    public TextMeshProUGUI dialogueBox;
    [Tooltip("The object which holds characters' image")]
    public Image bust;
    [Tooltip("The Box which manages player choices")]
    public ChoiceBox choiceBox;

    [Header("Text Display Options")]
    [Tooltip("The length of time to wait between displaying characters")]
    public float delay = 0.05f;

    [Header("File")]
    [Tooltip("The scene to load")]
    public string sceneName;
    [Tooltip("Whether to clear the scene after it has run")]
    public bool clearAfterScene;

    [Header("Characters")]
    [Tooltip("The array that contains all of the characters")]
    public CharacterPortraitContainer[] characters;
    readonly Dictionary<string, CharacterPortraitContainer> characterDictionary = new Dictionary<string, CharacterPortraitContainer>();

    public Sprite defaultCharacterSprite;

    string[] fileLines;
    int currentLine;
    string characterName, characterDialogue, characterExpression;

    public VariableDump variableDump;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>(); // Gets the canvas group to deal with fading opacity
        foreach (var characterPortraits in characters) // Creates the dictionary
        {
            characterDictionary.Add(characterPortraits.name, characterPortraits);
        }
        ClearDialogueBox(); // Clears the dialogue box, just in case
    }

    /// <summary>
    /// Clears the dialogue box's name, dialogue and image
    /// </summary>
    void ClearDialogueBox()
    {
        bust.sprite = null;
        nameBox.text = string.Empty;
        dialogueBox.text = string.Empty;
    }

    /// <summary>
    /// Loads the given scene into an array of the scene's lines
    /// </summary>
    /// <param name="_sceneName">The name of the scene. Determines the file to grab. Scene dialogues should be named after the scene they take place in</param>
    public void LoadSceneTextFile(string _sceneName)
    {
        sceneName = _sceneName;
        var file = Resources.Load<TextAsset>($"Dialogue/{sceneName}");

        if (file == null)
        {
            Debug.LogError($"File \"{sceneName}\" not found!");
        }

        // Splits the input on its new lines
        fileLines = file.text.Split(
            new[] { "\r\n", "\r", "\n", Environment.NewLine },
            StringSplitOptions.None
            );

        currentLine = 0;
        ClearDialogueBox();
    }

    /// <summary>
    /// Loads the next line from fileLines
    /// </summary>
    public void LoadNewLine()
    {
        LoadNewLine:
        // Split the line into its components and store them
        string[] parsedText = fileLines[currentLine].Split('|');
        currentLine++;

        switch (parsedText[0].ToLower())
        {
            case "[choice]":    // Line represents a choice
                DisplayChoices(parsedText);
                break;
            case "[var]":       // Line represents setting a variable
                switch (parsedText[2].ToLower())
                {
                    case string s when s == "[set]" || s == "=":
                        SetVariable(parsedText[1], parsedText[3]);
                        break;
                    case string s when s == "[add]" || s == "+":
                        AddVariable(parsedText[1], parsedText[3]);
                        break;
                    case string s when s == "[subtract]" || s == "[sub]" || s == "-":
                        SubtractVariable(parsedText[1], parsedText[3]);
                        break;
                    case string s when s == "[append]" || s == "[app]" || s == "~":
                        AppendVariable(parsedText[1], parsedText[3]);
                        break;
                    default:
                        Debug.LogError($"Unknown variable operator \"{parsedText[2]}\".\nUse [Set]/=, [Add]/+, [Subtract]/[Sub]/- or [Append]/[App]/~.");
                        break;
                }
                if (currentLine == fileLines.Count())
                {
                    if (clearAfterScene) // Clears the scene if told to
                    {
                        sceneName = string.Empty;
                    }
                    StartCoroutine(FadeCanvas(uiFadeInSpeed, canvasGroup.alpha, 0)); // Fades out the UI
                    break;
                }
                else
                    // Here to prevent overflow from recursion.
                    goto LoadNewLine;
            default:            // Line represents dialogue
                characterName = parsedText[0];
                characterExpression = parsedText[1].ToLower();
                characterDialogue = parsedText[2];

                // Looks for substrings surrounded by braces and replaces them with 
                // variables of the same name, looked up in the variable dump
                MatchCollection variables = Regex.Matches(characterDialogue, @"{\S*}");
                foreach (Match variable in variables)
                {
                    string variableName = variable.ToString().Trim(new[] { '{', '}' });
                    FieldInfo field = variableDump.GetType().GetField(variableName);
                    if (field != null)
                    {
                        string value = field.GetValue(variableDump).ToString();
                        if (string.IsNullOrEmpty(value))
                        {
                            Debug.LogWarning($"Field \"{variableName}\" has no value");
                        }
                        else
                        {
                            characterDialogue = characterDialogue.Replace(variable.ToString(), value);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Field \"{variableName}\" not found");
                    }
                }

                // Clears the dialogue box
                dialogueBox.text = string.Empty;

                // Sets the name box
                nameBox.text = characterName;

                // Converts the expression string into the associated Sprite variable in the given character
                // Returns the unknown character sprite if no associated character is found
                try
                {
                    bust.sprite = (Sprite)characterDictionary[characterName].GetType().GetField(characterExpression).GetValue(characterDictionary[characterName]);
                }
                catch (KeyNotFoundException)
                {
                    bust.sprite = defaultCharacterSprite;
                }

                // Declare and then start the coroutine/IEnumerator so it can be stopped later
                displayDialogueCoroutine = DisplayDialogue(characterDialogue);
                StartCoroutine(displayDialogueCoroutine);
                break;
        }
    }

    void DisplayChoices(string[] parsedText)
    {
        choiceBoxActive = true;
        choiceBox.canvasGroup.alpha = 1;
        choiceBox.canvasGroup.interactable = choiceBox.canvasGroup.blocksRaycasts = (choiceBox.canvasGroup.alpha == 1);
        int choices = (parsedText.Count() - 1) / 2;
        for (int i = 1; i <= choices; i++)
        {
            choiceBox.choices[i - 1].gameObject.SetActive(true);
            choiceBox.choices[i - 1].text.text = parsedText[(2 * i) - 1];
            choiceBox.choices[i - 1].scene = parsedText[2 * i];
        }
    }

    /// <summary>
    /// Sets a variable in the given VariableDump
    /// </summary>
    /// <param name="parsedText"></param>
    void SetVariable(string variableName, string value)
    {
        FieldInfo field = variableDump.GetType().GetField(variableName);

        if (field != null)
        {
            switch (field.FieldType)
            {
                case Type t when t == typeof(string):
                    field.SetValue(variableDump, value);
                    break;
                case Type t when t == typeof(bool):
                    field.SetValue(variableDump, value.Equals("true"));// ? true : false);
                    break;
                case Type t when t == typeof(int):
                    if (int.TryParse(value, out int parsedInt))
                    {
                        field.SetValue(variableDump, parsedInt);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to int");
                    }
                    break;
                case Type t when t == typeof(float):
                    if (float.TryParse(value, out float parsedFloat))
                    {
                        field.SetValue(variableDump, parsedFloat);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to float");
                    }
                    break;
                default:
                    Debug.LogError($"Field type \"{field.FieldType}\" is not supported.");
                    break;
            }
        }
        else
        {
            Debug.LogError($"Field \"{variableName}\" not found");
        }
    }

    void AddVariable(string variableName, string value)
    {
        FieldInfo field = variableDump.GetType().GetField(variableName);

        if (field != null)
        {
            switch (field.FieldType)
            {
                case Type t when t == typeof(int):
                    if (int.TryParse(value, out int parsedInt))
                    {
                        field.SetValue(variableDump, (int)field.GetValue(variableDump) + parsedInt);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to int");
                    }
                    break;
                case Type t when t == typeof(float):
                    if (float.TryParse(value, out float parsedFloat))
                    {
                        field.SetValue(variableDump, (float)field.GetValue(variableDump) + parsedFloat);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to float");
                    }
                    break;
                default:
                    Debug.LogError($"[Add] only supports ints and floats! Field type \"{field.FieldType}\" is not supported.");
                    break;
            }
        }
        else
        {
            Debug.LogError($"Field \"{variableName}\" not found");
        }
    }

    void SubtractVariable(string variableName, string value)
    {
        FieldInfo field = variableDump.GetType().GetField(variableName);

        if (field != null)
        {
            switch (field.FieldType)
            {
                case Type t when t == typeof(int):
                    if (int.TryParse(value, out int parsedInt))
                    {
                        field.SetValue(variableDump, (int)field.GetValue(variableDump) - parsedInt);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to int");
                    }
                    break;
                case Type t when t == typeof(float):
                    if (float.TryParse(value, out float parsedFloat))
                    {
                        field.SetValue(variableDump, (float)field.GetValue(variableDump) - parsedFloat);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to float");
                    }
                    break;
                default:
                    Debug.LogError($"[Add] only supports ints and floats! Field type \"{field.FieldType}\" is not supported.");
                    break;
            }
        }
        else
        {
            Debug.LogError($"Field \"{variableName}\" not found");
        }
    }

    void AppendVariable(string variableName, string value)
    {
        FieldInfo field = variableDump.GetType().GetField(variableName);

        if (field != null)
        {
            if (field.FieldType == typeof(string))
            {
                field.SetValue(variableDump, field.GetValue(variableDump) + value);
            }
            else
            {
                Debug.LogError($"[Append] only supports strings! Field type \"{field.FieldType}\" is not supported.");
            }

        }
        else
        {
            Debug.LogError($"Field \"{variableName}\" not found");
        }
    }

    /// <summary>
    /// Displays a given string letter by letter
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <returns></returns>
    IEnumerator DisplayDialogue(string text)
    {
        isDisplayingText = true; // Marks the system as typing out letters. Used to determine what happens when pressing enter

        for (int i = 0; i < text.Length; i++) // Adds a letter to the textbox then waits the delay time
        {
            if (text[i] == '<') // If the letter is an opening tag character, autofill the rest of the tag
            {
                int indexOfClose = text.Substring(i).IndexOf('>');
                if (indexOfClose == -1)
                {
                    dialogueBox.text += text[i];
                    yield return new WaitForSeconds(delay);
                    continue;
                }
                dialogueBox.text += text.Substring(i, indexOfClose);
                i += indexOfClose - 1;
                continue;
            }

            dialogueBox.text += text[i];
            yield return new WaitForSeconds(delay);
        }

        isDisplayingText = false; // Marks the system as no longer typing out
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // Eats input if no matching file exists or if the UI is fading in/out or if the player is making a choice
            if (Resources.Load<TextAsset>($"Dialogue/{sceneName}") == null || isFading || choiceBoxActive)
                return;

            if (showingDialogue) // If dialogue boxes are visible
            {
                if (currentLine >= fileLines.Length && !isDisplayingText) // If there are no more lines and system is not typing out
                {
                    if (clearAfterScene) // Clears the scene if told to
                    {
                        sceneName = string.Empty;
                    }
                    StartCoroutine(FadeCanvas(uiFadeInSpeed, canvasGroup.alpha, 0)); // Fades out the UI
                    return;
                }
                if (isDisplayingText) // If the system is typing out
                {
                    StopCoroutine(displayDialogueCoroutine); // Stops the typing out
                    dialogueBox.text = characterDialogue; // Fills the textbox with the entirety of the character's line
                    isDisplayingText = false; // Marks the system as no longer typing out
                }
                else // If the system is not typing out
                {
                    LoadNewLine(); // Loads the next line
                }
            }
            else // If dialogue boxes are not visible
            {
                StartCoroutine(FadeCanvas(uiFadeInSpeed, canvasGroup.alpha, 1)); // Fades in the UI
            }
        }
    }

    /// <summary>
    /// Fades the canvas between opacities
    /// </summary>
    /// <param name="lerpTime">How long it takes for the canvas to fully fade</param>
    /// <param name="start">The starting opacity</param>
    /// <param name="end">The ending opacity</param>
    /// <returns></returns>
    IEnumerator FadeCanvas(float lerpTime, float start, float end)
    {
        if (end == 1) // If fading in
        {
            LoadSceneTextFile(sceneName);
        }

        // For keeping track of the fade
        float timeAtStart = Time.time;
        float timeSinceStart;
        float percentageComplete = 0;

        isFading = true; // Marks the UI as fading
        while (percentageComplete < 1) // Keeps looping until the lerp is complete
        {
            timeSinceStart = Time.time - timeAtStart;
            percentageComplete = timeSinceStart / lerpTime;

            float currentValue = Mathf.Lerp(start, end, percentageComplete);

            canvasGroup.alpha = currentValue;
            yield return new WaitForEndOfFrame();
        }
        isFading = false; // Marks the UI as no longer fading

        showingDialogue = !showingDialogue; // Toggles the representation of whether the UI is visible

        if (end == 1 && !isDisplayingText) // If fading in
        {
            LoadNewLine(); // Loads the next line
        }

        canvasGroup.interactable = canvasGroup.blocksRaycasts = (canvasGroup.alpha == 1);
    }
}