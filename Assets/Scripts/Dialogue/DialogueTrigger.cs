using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
	[Header("Visual Trigger")]
	[SerializeField] private GameObject visualTrigger;

	[Header("Ink JSON")]
	[SerializeField] private TextAsset inkJSON;

	private bool playerInRange;

	private void Awake()
	{
		playerInRange = false;
		visualTrigger.SetActive(false);
	}

	private void Update()
	{
		if (playerInRange && !DialogueManager.GetInstance().dialogueIsPlaying)
		{
			visualTrigger.SetActive(true);
			if (InputManager.InteractWasPressed)
			{
				DialogueManager.GetInstance().EnterDialogueMode(inkJSON);
			}
		}
		else visualTrigger.SetActive(false);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.CompareTag("Player"))
			playerInRange = true;
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.CompareTag("Player"))
			playerInRange = false;
	}
}
