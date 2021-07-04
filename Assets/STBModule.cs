using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class STBModule : MonoBehaviour
{
	public KMBombModule Module;
	public KMBombInfo BombInfo;
	public KMAudio Audio;

	public STBTile[] Tiles;
	public STBDie[] Dice;

	public KMSelectable RollButton;
	public Animator RollButtonAnimator;
	public KMSelectable ResetButton;
	public Animator ResetButtonAnimator;

	public TextMesh DisplayLabel;

	private int moduleId;
	private static int moduleIdCounter;

	private bool isActive;
	private bool isSolved;

	private bool CanInteract { get { return isActive && !isSolved; } }

	private int targetScore;

	private int lastDiceResult;
	private bool diceHaveBeenRolled;
	private bool isChoosingTileSum;
	private int currentTileSum;
	private int CurrentScore { get { return Tiles.Where(t => !t.isDown).Select(t => t.number).Sum(); } }

	private void Awake()
	{
		moduleId = ++moduleIdCounter;
	}

	private void Start()
	{
		targetScore = Random.Range(3, 21);
		Log("Target score: {0}", targetScore);

		DisplayLabel.text = "";

		for (int i = 0; i < Tiles.Length; i++) SetupTile(i);
		RollButton.OnInteract += Roll;
		ResetButton.OnInteract += Reset;

		Module.OnActivate += Activate;

		ResetRaw();
	}

	private void Activate()
	{
		isActive = true;
		DisplayLabel.text = targetScore.ToString();
	}

	private void SetupTile(int i)
	{
		STBTile tile = Tiles[i];
		tile.Init(i + 1);
		tile.Selectable.OnInteract += delegate
		{
			SelectTile(tile);
			return false;
		};
	}

	private void SelectTile(STBTile tile, bool forceSelect = false)
	{
		if (CanInteract && (isChoosingTileSum || forceSelect) && !tile.isDown)
		{
			tile.Selectable.AddInteractionPunch(0.5f);
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, tile.transform);

			tile.Flip(true);

			HandleTile(tile.number, forceSelect);
		}
	}

	private void SelectTile(int tileNum, bool forceSelect = false)
	{
		SelectTile(Tiles[tileNum - 1], forceSelect);
	}

	private void HandleTile(int num, bool forceSelect)
	{
		currentTileSum += num;
		if (currentTileSum > lastDiceResult && !forceSelect)
		{
			Log("Strike! Sum of flipped tiles ({0}) has exceeded the dice roll!", currentTileSum);
			Module.HandleStrike();
			ResetRaw();
		}
		else if (currentTileSum == lastDiceResult || forceSelect)
		{
			if (!forceSelect) Log("Sum of flipped tiles ({0}) is equivalent to the dice roll.", currentTileSum);
			isChoosingTileSum = false;
			if (CurrentScore < targetScore)
			{
				Log("Strike! Current score ({0}) has exceeded the target score!", CurrentScore);
				Module.HandleStrike();
				ResetRaw();
			}
			else if (CurrentScore == targetScore)
			{
				Log("Current score ({0}) is equivalent to the target score. Module {1}.", CurrentScore, forceSelect ? "autosolved" : "solved");
				Solve();
				if (forceSelect) SetCanRollDice(false);
			}
			else
			{
				SetCanRollDice(true);
				RollButtonAnimator.SetBool("IsWaiting", true);
			}
		}
		else SetCanRollDice(false);
	}

	private bool canRollDice;
	private bool canReset = true;

	private bool Roll()
	{
		if (CanInteract && canRollDice) StartCoroutine(RollDice());
		return false;
	}

	private void SetCanRollDice(bool canRoll)
	{
		canRollDice = canRoll;
		RollButtonAnimator.SetBool("IsHidden", !canRoll);
		RollButtonAnimator.SetBool("IsWaiting", false);
	}

	private IEnumerator RollDice()
	{
		RollButton.AddInteractionPunch();
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, RollButton.transform);

		SetCanRollDice(false);

		canReset = false;
		ResetButtonAnimator.SetBool("IsHidden", true);

		KMAudio.KMAudioRef rollSound = Audio.PlaySoundAtTransformWithRef("kf_dice_loop2", transform);

		lastDiceResult = Dice.Sum(d => d.Roll(Audio));
		Log("Dice roll result: {0}", lastDiceResult);

		yield return new WaitWhile(() => Dice.Any(d => d.isRolling));

		rollSound.StopSound();

		canReset = true;
		ResetButtonAnimator.SetBool("IsHidden", false);

		yield return new WaitForSecondsRealtime(1.5f);

		SetCanRollDice(true);

		diceHaveBeenRolled = true;
		isChoosingTileSum = true;
		currentTileSum = 0;
	}

	private bool Reset()
	{
		if (CanInteract)
		{
			ResetButton.AddInteractionPunch();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ResetButton.transform);

			ResetRaw();
		}
		return false;
	}

	private void ResetRaw()
	{
		if (canReset)
		{
			SetCanRollDice(true);
			if (diceHaveBeenRolled) isChoosingTileSum = true;
			else RollButtonAnimator.SetBool("IsWaiting", true);
			currentTileSum = 0;
			foreach (STBTile tile in Tiles) tile.Flip(false);
		}
	}

	private void Solve()
	{
		if (!isSolved)
		{
			Module.HandlePass();
			isSolved = true;
		}
	}

	private void Log(string format, params object[] args)
	{
		Debug.LogFormat("[Shut-the-Box #{0}] {1}", moduleId, string.Format(format, args));
	}





	private string TwitchHelpMessage = "!{0} roll | !{0} (flip/tiles/t) 1 2 3 | !{0} reset | Numbers can be in any format for the flip command, ex. \"1,2,3\" or \"123\" or even mixed like \"1,23 4|5\"";

	private IEnumerator ProcessTwitchCommand(string command)
	{
		string[] split = command.ToLowerInvariant().Split(' ');
		switch (split[0])
		{
			case "roll":
				Roll();
				yield return null;
				break;
			case "reset":
				Reset();
				yield return null;
				break;
			case "flip":
			case "tile":
			case "tiles":
			case "t":
				if (split.Length > 1)
				{
					foreach (char tileChar in split.Skip(1).Join("").Where(char.IsDigit).ToArray())
					{
						int tileNum;
						if (int.TryParse(tileChar.ToString(), out tileNum))
						{
							if (tileNum < 1 || tileNum > 9) yield return string.Format("sendtochaterror Tile number \"{0}\" is invalid.", tileNum);
							else SelectTile(tileNum);
						}
					}
				}
				break;
		}
	}

	private List<List<int>> RecursiveCombinationFinder(List<int> numbers, int target, List<int> partial)
	{
		List<List<int>> finalCombos = new List<List<int>>();

		int s = 0;
		foreach (int x in partial) s += x;

		if (s == target)
			finalCombos.Add(partial);

		if (s >= target)
			return finalCombos;

		for (int i = 0; i < numbers.Count; i++)
		{
			List<int> remaining = new List<int>();
			int n = numbers[i];
			for (int j = i + 1; j < numbers.Count; j++) remaining.Add(numbers[j]);

			List<int> partial_rec = new List<int>(partial) { n };
			finalCombos.AddRange(RecursiveCombinationFinder(remaining, target, partial_rec));
		}

		return finalCombos;
	}

	private List<List<int>> FindAllTileCombinations(List<int> numbers, int target)
	{
		return RecursiveCombinationFinder(numbers, target, new List<int>());
	}

	private static readonly List<int> CombinationFactors = Enumerable.Range(1, 9).ToList();

	private IEnumerator TwitchHandleForcedSolve()
	{
		yield return new WaitUntil(() => CanInteract && canReset);
		Reset();
		List<int> tilesToSelect = new List<int>(CombinationFactors);
		foreach (int tileNum in FindAllTileCombinations(CombinationFactors, targetScore).PickRandom()) tilesToSelect.Remove(tileNum);
		foreach (int tileNum in tilesToSelect) SelectTile(tileNum, true);
		yield break;
	}
}
