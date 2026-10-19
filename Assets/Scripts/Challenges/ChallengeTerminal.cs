using UnityEngine;

/// <summary>
/// Objeto (computador, quadro, mesa...) que inicia um desafio quando o jogador aperta Z olhando para ele.
///
/// Como usar: crie um GameObject, dê um sprite a ele (SpriteRenderer), adicione este componente
/// (ele já configura sozinho a layer "Interactable" e um BoxCollider2D de 1x1) e arraste um
/// ChallengeData para o campo "Challenge".
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ChallengeTerminal : MonoBehaviour, Interacao
{
    [SerializeField] private ChallengeData challenge;

    [Header("Feedback visual (opcional)")]
    [Tooltip("Cor aplicada ao sprite depois que o desafio é concluído.")]
    [SerializeField] private Color completedColor = new Color(0.65f, 1f, 0.70f, 1f);

    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;

    private void Reset()
    {
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0)
            gameObject.layer = layer;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            box.size = Vector2.one;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        ChallengeManager.OnChallengeCompleted += HandleCompleted;
    }

    private void OnDisable()
    {
        ChallengeManager.OnChallengeCompleted -= HandleCompleted;
    }

    private void Start()
    {
        RefreshVisual();
    }

    public void Interact()
    {
        if (challenge == null)
        {
            Debug.LogWarning("ChallengeTerminal '" + name + "': falta definir o Challenge.");
            return;
        }

        ChallengeManager.GetOrCreate().StartChallenge(challenge);
    }

    private void HandleCompleted(ChallengeData completed)
    {
        if (completed == challenge)
            RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null || challenge == null)
            return;

        spriteRenderer.color = challenge.IsCompleted ? completedColor : originalColor;
    }
}
