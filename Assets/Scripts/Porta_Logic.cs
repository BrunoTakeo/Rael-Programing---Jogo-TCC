using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Porta_Logic : MonoBehaviour, Interacao
{
    [Header("Destino")]
    [SerializeField] private Transform destino;           
    [SerializeField] private Vector2 direcaoAoChegar;     

    [Tooltip("Collider que limita a câmera na área de destino (ex.: B1 para o interior, T1 para o mundo). Vazio = não troca.")]
    [SerializeField] private Collider2D limiteCameraDestino;

    [Header("Transição")]
    [SerializeField] private float duracaoFade = 0.25f;

    [Header("Trava (opcional)")]
    [Tooltip("Se preenchido, a porta só abre depois que o jogador concluir este desafio.")]
    [SerializeField] private ChallengeData desafioNecessario;

    [Tooltip("Diálogo mostrado quando a porta está trancada.")]
    [SerializeField] private Dialog dialogoTrancada;

    private bool ocupada;

    private void Reset()
    {
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0)
            gameObject.layer = layer;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            box.size = Vector2.one;
    }

    public void Interact()
    {
        if (ocupada)
            return;

        Debug.Log("Porta ativada: " + gameObject.name);

        if (destino == null)
        {
            Debug.LogWarning("Porta '" + name + "': falta definir o Destino.");
            return;
        }

        if (desafioNecessario != null && !desafioNecessario.IsCompleted)
        {
            if (dialogoTrancada != null && DialogManager.Instance != null)
                StartCoroutine(DialogManager.Instance.ShowDialog(dialogoTrancada));
            else
                Debug.Log("Porta trancada: conclua o desafio '" + desafioNecessario.Title + "'.");
            return;
        }

        StartCoroutine(TeleportarPlayer());
    }

    private IEnumerator TeleportarPlayer()
    {
        ocupada = true;

        GameController gc = GameController.Instance;
        if (gc != null)
            gc.SetGameState(GameState.Transition);

        ScreenFader fader = ScreenFader.Instance;
        yield return fader.Fade(1f, duracaoFade);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        PlayerControll player = playerObj != null ? playerObj.GetComponent<PlayerControll>() : null;

        if (player == null)
        {
            Debug.LogError("Player não encontrado! Verifique a Tag e o PlayerControll.");
        }
        else
        {
            Vector3 antes = player.transform.position;

            player.TeleportTo(destino.position, direcaoAoChegar);
            TrocarCamera(player.transform, player.transform.position - antes);

            Debug.Log("Teleportado para: " + destino.name);
        }

        yield return null;
        yield return fader.Fade(0f, duracaoFade);

        if (gc != null)
            gc.SetGameState(GameState.FreeRoam);

        ocupada = false;
    }

    private void TrocarCamera(Transform player, Vector3 deslocamento)
    {
        CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam == null)
            return;

        if (limiteCameraDestino != null)
        {
            CinemachineConfiner2D confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.m_BoundingShape2D = limiteCameraDestino;
                confiner.InvalidateCache();
            }
            else
            {
                Debug.LogWarning("Porta: a Virtual Camera não tem CinemachineConfiner2D; os limites da câmera não foram trocados.");
            }
        }

        vcam.OnTargetObjectWarped(player, deslocamento);
        vcam.PreviousStateIsValid = false;
    }

    private void OnDrawGizmos()
    {
        if (destino == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, destino.position);
        Gizmos.DrawWireCube(destino.position, new Vector3(0.8f, 0.8f, 0f));
    }
}
