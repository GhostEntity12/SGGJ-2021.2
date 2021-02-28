using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AutoDialogue : MonoBehaviour
{
	public bool blocksInput;
	public bool singleFire;
	public Dialogue examineScene;

	private void Reset()
	{
		gameObject.layer = 6;
		blocksInput = true;
		GetComponent<BoxCollider>().isTrigger = true;
	}

	private void Start()
	{
		DialogueManager.OnEndDialogueEvent += () => PlayerController.player.canInput = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.GetComponent<PlayerController>())
		{
			if (blocksInput) PlayerController.player.canInput = false;
			FindObjectOfType<DialogueManager>().TriggerScene(examineScene);
		}
		if (singleFire) GetComponent<BoxCollider>().enabled = false;
	}
}
