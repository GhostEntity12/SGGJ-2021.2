using UnityEditor;
using UnityEngine;

public class Examine : Interactable
{
	public Dialogue examineScene;

	private void Reset()
	{
		base.Reset();
		blocksInput = true;
	}

	private void Awake()
	{
		DialogueManager.OnEndDialogueEvent += () => PlayerController.player.canInput = true;
	}

	public override void Interact()
	{
		if (blocksInput) PlayerController.player.canInput = false;
		FindObjectOfType<DialogueManager>().TriggerScene(examineScene);
	}

}
