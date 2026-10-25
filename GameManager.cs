using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Ball Collection")]
    [SerializeField] private int targetBalls = 10;
    [SerializeField] private int collectedBalls = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text ballCounter;
    [SerializeField] private GameObject finishPanel;

    private bool levelFinished;

    private void Start()
    {
        collectedBalls = 0;
        levelFinished = false;

        if (finishPanel != null)
            finishPanel.SetActive(false);

        UpdateBallCounter();
    }

    public void CollectBall()
    {
        if (levelFinished)
            return;

        collectedBalls++;

        collectedBalls = Mathf.Min(
            collectedBalls,
            targetBalls
        );

        UpdateBallCounter();

        if (collectedBalls >= targetBalls)
            FinishLevel();
    }

    private void UpdateBallCounter()
    {
        if (ballCounter != null)
        {
            ballCounter.text =
                "BALLS: " + collectedBalls + " / " + targetBalls;
        }
    }

    private void FinishLevel()
    {
        levelFinished = true;

        if (finishPanel != null)
            finishPanel.SetActive(true);

        Debug.Log("LEVEL COMPLETE! All balls collected.");
    }
}

