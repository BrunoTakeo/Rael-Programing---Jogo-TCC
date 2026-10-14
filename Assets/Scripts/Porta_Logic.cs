using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Porta_Logic : MonoBehaviour, Interacao
{
    [Header("Destino")]
    [SerializeField] private Transform destino;           // Para onde o player vai
    [SerializeField] private Vector2 direcaoAoChegar;     // Direção que o player olha ao chegar

    public void Interact()
    {
        Debug.Log("Porta ativada: " + gameObject.name);
        TeleportarPlayer();
    }

    private void TeleportarPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("Player não encontrado! Verifique a Tag.");
            return;
        }

        // Teleporta o player
        playerObj.transform.position = destino.position;

        // Reseta movimento e define a direção
        PlayerControll player = playerObj.GetComponent<PlayerControll>();
        if (player != null)
        {
            player.ResetPlayer();

            Animator anim = playerObj.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetFloat("MoveX", direcaoAoChegar.x);
                anim.SetFloat("MoveY", direcaoAoChegar.y);
            }
        }

        Debug.Log("Teleportado para: " + destino.name);
    }
}
