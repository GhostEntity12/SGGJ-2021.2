using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public abstract class Interactable : MonoBehaviour
{
	public bool blocksInput;

	protected virtual void Reset()
	{
		gameObject.layer = 6;
	}

	public abstract void Interact();
}
