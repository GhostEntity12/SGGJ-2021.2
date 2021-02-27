using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
	public bool blocksInput;

	public abstract void Interact();
}
