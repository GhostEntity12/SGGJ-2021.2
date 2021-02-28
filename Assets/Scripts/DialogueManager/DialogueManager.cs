using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    bool dialogueActive;
    [NonSerialized]
    public bool choiceBoxActive;
    Coroutine dialogueDisplayCoroutine;

    public KeyCode[] ProgressionKeys = new KeyCode[1] { KeyCode.Return };

    [Header("UI")]
    [Tooltip("The object which holds characters' names")]
    public TextMeshProUGUI nameBox;
    [Tooltip("The object which holds characters' dialogue")]
    public TextMeshProUGUI dialogueBox;
    [Tooltip("The object which holds characters' image")]
    public Image bust;
    [Tooltip("The Box which manages player choices")]
    public ChoiceBox choiceBox;
    [Space(15)]
    [Tooltip("How quickly the UI fades in")]
    public float uiFadeSpeed = 0.4f;
    CanvasGroup canvasGroup;

    [Header("Text Display Options")]
    [Tooltip("The length of time to wait between displaying characters")]
    public float delay = 0.05f;

    [Header("File")]
    [Tooltip("The scene to load")]
    public Dialogue sceneFile;

    [Header("Characters")]
    public Sprite defaultCharacterSprite;
    readonly Dictionary<string, CharacterPortraitContainer> characterDictionary = new Dictionary<string, CharacterPortraitContainer>();


    string[] fileLines;
    int currentLineIndex;
    string characterName, characterDialogue, characterExpression;

    public VariableDump variableDump;

    public delegate void OnEndDialogue();
    public static event OnEndDialogue OnEndDialogueEvent;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>(); // Gets the canvas group to deal with fading opacity
        foreach (CharacterPortraitContainer characterPortraits in Resources.LoadAll<CharacterPortraitContainer>("CharacterDialogueSprites")) // Creates the dictionary
        {
            characterDictionary.Add(characterPortraits.name, characterPortraits);
        }
        ClearDialogueBox(); // Clears the dialogue box, just in case
    }

    private void Update()
    {
        if (GetAnyKeyDown(ProgressionKeys) && dialogueActive) // If enter is pressed and the textboxes are visible
        {
            if (dialogueDisplayCoroutine != null) // If the system is currently typing out, finish and return
            {
                StopCoroutine(dialogueDisplayCoroutine); // Stops the typing out
                dialogueBox.text = characterDialogue; // Fills the textbox with the entirety of the character's line
                dialogueDisplayCoroutine = null;
                return;
            }
            else if (currentLineIndex >= fileLines.Length) // If there are no more lines
            {
                EndScene();
            }
            else
            {
                LoadNewLine(); // Loads the next line
            }
        }
    }

    public void TriggerScene(Dialogue scene)
    {
        sceneFile = scene;

        // Throws error no matching file exists
        if (sceneFile == null)
        {
            Debug.LogError($"Dialogue file \"{sceneFile}\" not found!");
            return;
        }

        Debug.Log($"<color=#5cd3e0>[Dialogue]</color> Starting dialogue {sceneFile.name}");
        ClearDialogueBox();
        StartCoroutine(FadeCanvas(uiFadeSpeed, 0, 1));

        // Splits the input on its new lines
        fileLines = sceneFile.dialogue.Split(
            new[] { "\r\n", "\r", "\n", Environment.NewLine },
            StringSplitOptions.None
            ).Where(l => l.Length > 0).ToArray();
        currentLineIndex = 0;
        LoadNewLine();
    }

    void EndScene()
    {
        OnEndDialogueEvent.Invoke();
        StartCoroutine(FadeCanvas(uiFadeSpeed, 1, 0));
    }

    /// <summary>
    /// Loads the next line from fileLines
    /// </summary>
    void LoadNewLine()
    {
        string line = fileLines[currentLineIndex];

        if (line[0] == '{')
        // Line represents setting/modifying a variable
        // Example line: { money add 10 }
        {
            ModifyVariable(line.Substring(1, line.Length - 2).Trim());
            currentLineIndex++;

            if (currentLineIndex >= fileLines.Length) return;
            line = fileLines[currentLineIndex];
        }
        else if (line[0] == '[')
        // Line(s) represents a choice
        // Example lines:
        // [
        //     Polite Greeting  : DIA_Scene1_PoliteGreeting
        //     Rude Greeting    : DIA_Scene1_RudeGreeting
        // ]
        {

            currentLineIndex++;
            List<string> optionsLines = new List<string>();
            while (fileLines[currentLineIndex][0] != ']')
            {
                optionsLines.Add(fileLines[currentLineIndex].Trim());
            }
            DisplayChoices(optionsLines);
            currentLineIndex++;

            // Update line to reflect new value or return if end of file
            if (currentLineIndex >= fileLines.Length) return;
            line = fileLines[currentLineIndex];
        }

        DisplayDialogue(line);
        currentLineIndex++;
    }

    void DisplayDialogue(string line)
    {
        // Clears the dialogue box
        dialogueBox.text = string.Empty;

        // Split the line into character info and dialogue
        string[] parsedText = line.Split(':');
        switch (parsedText[0].IndexOf('('))
        {
            case -1: // No expression defined
                characterName = parsedText[0].Trim();
                characterExpression = "neutral";
                break;
            default:
                characterName = parsedText[0].Substring(0, parsedText[0].IndexOf('(')).Trim();
                characterExpression = parsedText[0].Substring(parsedText[0].IndexOf('(') + 1, parsedText[0].IndexOf(')') - parsedText[0].IndexOf('(') - 1).ToLower();
                break;
        }

        characterDialogue = parsedText[1];

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
                    Debug.LogWarning($"Field \"{variableName}\" has no value");
                else
                    characterDialogue = characterDialogue.Replace(variable.ToString(), value);
            }
            else
            {
                Debug.LogError($"Field \"{variableName}\" not found");
            }
        }

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

        // Start the coroutine/IEnumerator
        dialogueDisplayCoroutine = StartCoroutine(PrintDialogue(characterDialogue));

    }

    void ModifyVariable(string command)
    {
        // Command structure:
        // [0: variable] [1: command] [2: value]
        string[] commandInfo = command.Split(' ');

        switch (commandInfo[1].ToLower())
        {
            case string s when s == "[set]":
                variableDump.SetVariable(commandInfo[0], commandInfo[2]);
                break;
            case string s when s == "[add]":
                variableDump.AddVariable(commandInfo[0], commandInfo[2]);
                break;
            case string s when s == "[sub]" || s == "[subtract]":
                variableDump.SubtractVariable(commandInfo[0], commandInfo[2]);
                break;
            case string s when s == "[app]" || s == "[append]":
                variableDump.AppendVariable(commandInfo[0], commandInfo[2]);
                break;
            default:
                Debug.LogError($"Unknown variable operator \"{commandInfo[1]}\".\nUse [Set], [Add], [Subtract]/[Sub] or [Append]/[App].");
                break;
        }
    }

    void DisplayChoices(List<string> options)
    {
        choiceBoxActive = true;
        choiceBox.canvasGroup.alpha = 1;
        choiceBox.canvasGroup.interactable = choiceBox.canvasGroup.blocksRaycasts = (choiceBox.canvasGroup.alpha == 1);
        for (int i = 0; i < options.Count; i++)
        {
            string[] optionInfo = options[i].Split(':');
            choiceBox.choices[i].gameObject.SetActive(true);
            choiceBox.choices[i].text.text = optionInfo[0].Trim();
            Dialogue scene = Resources.Load($"Dialogue/{options[1].Trim()}") as Dialogue;
            if (scene)
                choiceBox.choices[i].scene = scene;
            else
                Debug.LogError($"Dialogue file \"{sceneFile}\" not found!");
        }
    }

    /// <summary>
    /// Displays a given string letter by letter
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <returns></returns>
    IEnumerator PrintDialogue(string text)
    {
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
        dialogueDisplayCoroutine = null;
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
        // For keeping track of the fade
        float timeAtStart = Time.time;
        float timeSinceStart;
        float percentageComplete = 0;

        while (percentageComplete < 1) // Keeps looping until the lerp is complete
        {
            timeSinceStart = Time.time - timeAtStart;
            percentageComplete = timeSinceStart / lerpTime;

            float currentValue = Mathf.Lerp(start, end, percentageComplete);

            canvasGroup.alpha = currentValue;
            yield return new WaitForEndOfFrame();
        }

        dialogueActive = !dialogueActive; // Toggles the representation of whether the UI is visible

        canvasGroup.interactable = canvasGroup.blocksRaycasts = (canvasGroup.alpha == 1);
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

    bool GetAnyKeyDown(params KeyCode[] aKeys)
    {
        foreach (var key in aKeys)
            if (Input.GetKeyDown(key))
                return true;
        return false;
    }

#if UNITY_EDITOR
    [MenuItem("Dialogue/Convert Text Dialogue Files")]
    static void ConvertTextFiles()
    {
        TextAsset[] dialogueFiles = Resources.LoadAll<TextAsset>("Dialogue");
        foreach (TextAsset file in dialogueFiles)
        {
            Dialogue existingAsset = Resources.Load<Dialogue>($"Dialogue/{file.name}");
            if (existingAsset)
            {
                existingAsset.dialogue = file.text;
            }
            else
            {
                Dialogue asset = ScriptableObject.CreateInstance<Dialogue>();

                asset.dialogue = file.text;

                AssetDatabase.CreateAsset(asset, $"Assets/Resources/Dialogue/{file.name}.asset");
                AssetDatabase.SaveAssets();
            }
        }
    }
#endif
}