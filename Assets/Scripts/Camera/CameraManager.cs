using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.VisualScripting;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

	[SerializeField] private CinemachineVirtualCamera[] allVirtualCameras;

	[Header("Controls for lerping the Y Damping during player jump/fall")]
	[SerializeField] private float fallPanAmount = 0.25f;
	[SerializeField] private float fallYPanTime = 0.35f;
	public float fallSpeedYDampingChangeThreshold = -15f;

	public bool IsLerpingYDamping { get; private set; }
	public bool LerpedFromPlayerFalling { get; set; }

	private Coroutine lerpYPanCoroutine;

	private CinemachineVirtualCamera currentCamera;
	private CinemachineFramingTransposer framingTransposer;

	private float normYPanAmount;

	private void Awake()
	{
		if (instance == null)
			instance = this;

		for (int i = 0; i < allVirtualCameras.Length; i++)
		{
			//set current active camera
			currentCamera = allVirtualCameras[i];

			//set the framing transposer
			framingTransposer = currentCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
		}

		//set the YDamping amount so it's based on the inspector value
		normYPanAmount = framingTransposer.m_YDamping;
	}

	#region Lerp the Y Damping

	public void LerpYDamping(bool isPlayerFalling)
	{
		lerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
	}

	private IEnumerator LerpYAction(bool isPlayerFalling)
	{
		IsLerpingYDamping = true;

		//grab the starting damping amount
		float startDampAmount = framingTransposer.m_YDamping;
		float endDampAmount = 0f;

		//determing the end damping amount
		if (isPlayerFalling)
		{
			endDampAmount = fallPanAmount;
			LerpedFromPlayerFalling = true;
		}
		else endDampAmount = normYPanAmount;

		//lerp the pan amount
		float elapsedTime = 0f;
		while (elapsedTime < fallYPanTime)
		{
			elapsedTime += Time.deltaTime;

			float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, (elapsedTime / fallYPanTime));
			framingTransposer.m_YDamping = lerpedPanAmount;

			yield return null;
		}

		IsLerpingYDamping = false;
	}
	#endregion
}
