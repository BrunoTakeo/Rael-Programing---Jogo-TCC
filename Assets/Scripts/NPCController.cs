using UnityEngine;

public class NPCController : MonoBehaviour, Interacao
{
    [SerializeField] private Dialog dialog;
    [SerializeField] private Animator animator;

    [Header("Desafio (opcional)")]
    [Tooltip("Se preenchido, o terminal do desafio abre quando o diálogo termina.")]
    [SerializeField] private ChallengeData challenge;

    [Tooltip("Diálogo usado depois que o desafio já foi concluído. Vazio = repete o diálogo normal e oferece o desafio de novo.")]
    [SerializeField] private Dialog completedDialog;

    private bool playerInRange;
    private Transform playerTransform;

    private void Awake()
    {
        if (dialog == null)
            Debug.LogError("Dialog não foi atribuído para o NPC: " + gameObject.name);
    }

    private void OnDestroy()
    {
        if (DialogManager.Instance != null)
            DialogManager.Instance.OnHideDialog -= StartChallengeAfterDialog;
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    public void Interact()
    {

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                return;
            }
        }

        // Antes, um NPC sem Animator ficava mudo (o código saía aqui).
        // Agora ele só deixa de virar para o jogador e fala normalmente.
        if (animator != null)
        {
            TurnToPlayer();
        }

        Dialog toShow = dialog;
        bool offerChallenge = challenge != null;

        if (challenge != null && challenge.IsCompleted && completedDialog != null)
        {
            toShow = completedDialog;
            offerChallenge = false;
        }

        if (toShow != null)
        {
            if (offerChallenge)
            {
                // "-=" antes de "+=" evita ficar inscrito duas vezes se o jogador conversar de novo.
                DialogManager.Instance.OnHideDialog -= StartChallengeAfterDialog;
                DialogManager.Instance.OnHideDialog += StartChallengeAfterDialog;
            }

            StartCoroutine(DialogManager.Instance.ShowDialog(toShow));
        }
        else if (offerChallenge)
        {
            ChallengeManager.GetOrCreate().StartChallenge(challenge);
        }
    }

    // Chamado quando o diálogo fecha: abre o terminal do desafio.
    private void StartChallengeAfterDialog()
    {
        DialogManager.Instance.OnHideDialog -= StartChallengeAfterDialog;
        ChallengeManager.GetOrCreate().StartChallenge(challenge);
    }

    private void TurnToPlayer()
    {

        Vector2 diff = playerTransform.position - transform.position;

        float moveX = 0f;
        float moveY = 0f;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            moveX = diff.x > 0 ? 1f : -1f;
        }
        else
        {
            moveY = diff.y > 0 ? 1f : -1f;
        }

        animator.SetFloat("moveX", moveX);
        animator.SetFloat("moveY", moveY);

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerTransform = null;
        }
    }
}
