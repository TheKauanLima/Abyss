using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class NPCFall : MonoBehaviour
{
    private PlayableDirector playableDirector;
	private Animator playerAnimator;
	private PlayerMovement playerMovement;

	private void Awake()
	{
		playableDirector = GetComponent<PlayableDirector>();
		playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();
		playerAnimator = GameObject.Find("Player").GetComponent<Animator>();
	}

	private void Update()
	{
		if (playableDirector.state != PlayState.Playing)
			EnableMovement();
	}

	private void OnTriggerExit2D(Collider2D collide)
	{
		if (collide.CompareTag("Player"))
		{
			playableDirector.enabled = true;
			playerMovement.enabled = false;
			collide.transform.parent.parent.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			playerAnimator.SetBool("Running", false);
		}
	}

	private void EnableMovement()
	{
		playerMovement.enabled = true;
		GameObject.Find("LockPosition").GetComponent<CinemachineVirtualCamera>().enabled = false;
		GameObject.Find("Virtual Camera").GetComponent<CinemachineVirtualCamera>().enabled = true;
	}
}
