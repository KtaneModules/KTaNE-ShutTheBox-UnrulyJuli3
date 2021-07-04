using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class STBTile : MonoBehaviour
{
	public TextMesh Label;
	public KMSelectable Selectable;
	public Animator TileAnimator;

	internal int number;
	internal bool isDown;

	public void Init(int num)
	{
		number = num;
		Label.text = number.ToString();
	}

	public void Flip(bool down)
	{
		TileAnimator.SetBool("IsDown", down);
		isDown = down;
	}
}
