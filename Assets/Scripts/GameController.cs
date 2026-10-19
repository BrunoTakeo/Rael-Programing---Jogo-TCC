using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { FreeRoam, Dialog, Challenge, Transition }

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [SerializeField] private PlayerControll playerControll;

    private GameState state;
    public GameState State => state;

    private void Awake()
    {
        Instance = this;

        if (playerControll == null)
        {
            playerControll = FindObjectOfType<PlayerControll>();
            if (playerControll == null)
            {
                Debug.LogError("PlayerControll não foi encontrado na cena!");
            }
        }
    }
    private void Start()
    {
        DialogManager.Instance.OnShowDialog += () =>
        {
            state = GameState.Dialog;
        };
        DialogManager.Instance.OnHideDialog += () =>
        {
            if (state == GameState.Dialog)
            {
                state = GameState.FreeRoam;
            }
        };
    }
    private void Update()
    {
        if (state == GameState.FreeRoam)
        {
            playerControll.HandleUpdate();

        }
        else if (state == GameState.Dialog)
        {
            DialogManager.Instance.HandleUpdate();

        }
        else if (state == GameState.Challenge)
        {
            if (ChallengeManager.Instance != null)
            {
                ChallengeManager.Instance.HandleUpdate();
            }
        }
    }
    public void SetGameState(GameState newState)
    {
        state = newState;
    }
}
