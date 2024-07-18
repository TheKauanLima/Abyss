using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
	private static DialogueManager instance;

	[Header("Dialogue UI")]
	[SerializeField] private GameObject dialoguePanel;
	[SerializeField] private TextMeshProUGUI dialogueText;

	private Story currentStory;
	private bool dialogueIsPlaying;

	private void Awake()
	{
		if (instance != null)
			Debug.LogWarning("Found more than one Dialogue Manager in the scene.");
		instance = this;
	}

	public static DialogueManager GetInstance()
	{
		return instance;
	}

	private void Start()
	{
		dialogueIsPlaying = false;
		dialoguePanel.SetActive(false);
	}

	private void Update()
	{
		//return right away if dialogue isn't playing
		if (!dialogueIsPlaying)
			return;

		//handle continuing to the next line in the dialogue when submit is pressed
		if (InputManager.SubmitWasPressed)
			ContinueStory();
	}

	public void EnterDialogueMode(TextAsset inkJSON)
	{
		currentStory = new Story(inkJSON.text);
		dialogueIsPlaying = true;
		dialoguePanel.SetActive(true);

		ContinueStory();
    }

	private void ExitDialogueMode()
	{
		dialogueIsPlaying = false;
		dialoguePanel.SetActive(false);
		dialogueText.text = string.Empty;
	}

	private void ContinueStory()
	{
		if (currentStory.canContinue)
			dialogueText.text = currentStory.Continue();
		else ExitDialogueMode();
	}
}
