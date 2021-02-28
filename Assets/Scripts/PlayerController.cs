using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	public static PlayerController player;

	public float playerSpeed = 2;
	Rigidbody r;
	public bool canInput = true;
	Vector2 movementVector;

	Vector3 boxSize = new Vector3(0.75f, 0.25f, 0.5f);

	Interactable activeInteractable;

	Material m;

	public TextMeshProUGUI interactableText;

	private void Awake()
	{
		r = GetComponent<Rigidbody>();
		player = this;
		m = GetComponent<Renderer>().sharedMaterial;
	}

	void Update()
	{
		var interactablesInRange = Physics.OverlapBox(transform.position, boxSize, Quaternion.identity, 1 << 6, QueryTriggerInteraction.Collide);
		activeInteractable = interactablesInRange.Length switch
		{
			0 => null,
			1 => interactablesInRange[0].GetComponent<Interactable>(),
			_ => GetClosestInteractable(interactablesInRange),
		};

		if (activeInteractable is Warp)
		{
			interactableText.text = "E: Use";
		}
		if (activeInteractable is Examine)
		{
			interactableText.text = "E: Examine";
		}

		if (canInput)
		{
			movementVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
			if (movementVector.magnitude > 1) movementVector = movementVector.normalized;
			movementVector *= playerSpeed;

			if (activeInteractable)
			{
				if (Input.GetButtonDown("Interact"))
				{
					activeInteractable.Interact();
				}
			}
			interactableText.enabled = activeInteractable;
		}
		else
		{
			movementVector = Vector2.zero;
			interactableText.enabled = false;
		}
	}

	private void FixedUpdate()
	{
		if (movementVector.x > 0)
		{
			m.mainTextureScale = new Vector2(1, 1);
		}
		if (movementVector.x < 0)
		{
			m.mainTextureScale = new Vector2(-1, 1);
		}
		r.velocity = new Vector3(movementVector.x, 0, movementVector.y);
	}

	Interactable GetClosestInteractable(Collider[] interactables)
	{
		Interactable bestTarget = null;
		float closestDistanceSqr = Mathf.Infinity;
		Vector3 currentPosition = transform.position;
		foreach (Collider potentialTarget in interactables)
		{
			Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
			float dSqrToTarget = directionToTarget.sqrMagnitude;
			if (dSqrToTarget < closestDistanceSqr)
			{
				closestDistanceSqr = dSqrToTarget;
				bestTarget = potentialTarget.GetComponent<Interactable>();
			}
		}
		return bestTarget;
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawCube(transform.position, boxSize * 2);
	}

}
