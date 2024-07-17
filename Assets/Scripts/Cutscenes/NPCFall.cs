using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class NPCFall : MonoBehaviour
{
    private PlayableDirector playableDirector;
	[SerializeField] private Animator playerAnimator;

	private void Awake()
	{
		playableDirector = GetComponent<PlayableDirector>();
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
			collide.transform.parent.parent.GetComponent<PlayerMovement>().enabled = false;
			collide.transform.parent.parent.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			playerAnimator.SetBool("Running", false);
		}
	}

	private void EnableMovement()
	{
		GameObject.Find("Player").GetComponent<PlayerMovement>().enabled = true;
		GameObject.Find("LockPosition").GetComponent<CinemachineVirtualCamera>().enabled = false;
		GameObject.Find("Virtual Camera").GetComponent<CinemachineVirtualCamera>().enabled = true;
	}
}
