using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;

	[Header("Flip Rotation Stats")]
	[SerializeField] private float flipYRotationTime = 0.5f;

    private PlayerMovement player;

    private bool isFacingRight;

	private void Awake()
	{
		player = playerTransform.GetComponent<PlayerMovement>();
		isFacingRight = player.facingRight;
	}

	private void Update()
	{
		//make the cameraFollowObject follow the player's position
		transform.position = playerTransform.position;
	}

	public void CallTurn()
	{
		LeanTween.rotateY(gameObject, DetermineEndRotation(), flipYRotationTime).setEaseInOutSine();
	}

	private IEnumerator FlipYLerp()
	{
		float startRotation = transform.localEulerAngles.y;
		float endRotationAmount = DetermineEndRotation();
		float yRotation = 0;

		float elapsedTime = 0f;
		while (elapsedTime < flipYRotationTime)
		{
			elapsedTime += Time.deltaTime;

			//lerp the y rotation
			yRotation = Mathf.Lerp(startRotation, endRotationAmount, (elapsedTime / flipYRotationTime));
			transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

			yield return null;
		}
	}

	private float DetermineEndRotation()
	{
		isFacingRight = !isFacingRight;

		if (isFacingRight)
			return 180f;
		else return 0f;
	}
}
