using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class STBDie : MonoBehaviour
{
	public Animator DieAnimator;
	public Transform DieTransform;

	internal bool isRolling;

	private static readonly Vector3[] DieRotations = new[]
	{
		new Vector3(0f, 0f, -90f),
		new Vector3(0f, 0f, 0f),
		new Vector3(-90f, 0f, 0f),
		new Vector3(90f, 0f, 0f),
		new Vector3(0, 0f, 180f),
		new Vector3(0, 0f, 90f)
	};

	private void Start()
	{
		DieTransform.gameObject.SetActive(false);
	}

	public int Roll(KMAudio audio)
	{
		DieTransform.gameObject.SetActive(true);

		int result = Random.Range(0, 6);
		StartCoroutine(DoRoll(result, audio));
		return result + 1;
	}

	private IEnumerator DoRoll(int result, KMAudio audio)
	{
		isRolling = true;
		DieAnimator.SetTrigger("Roll");
		yield return new WaitForSecondsRealtime(Random.Range(0.5f, 2f));
		DieTransform.localRotation = Quaternion.Euler(DieRotations[result]);
		audio.PlaySoundAtTransform("kf_hrollers_give_die", DieTransform);
		DieAnimator.SetTrigger("Select");
		isRolling = false;
	}
}
