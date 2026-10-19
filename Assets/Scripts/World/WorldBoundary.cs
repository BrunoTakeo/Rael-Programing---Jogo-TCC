using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define uma área retangular onde o jogador pode andar. O jogador não consegue dar um passo para fora dela.
///
/// Como usar: crie um GameObject vazio no CENTRO da área, adicione este componente e ajuste o Size.
/// Um retângulo vermelho aparece na Scene View mostrando o limite.
/// Cada ambiente (mundo, interior...) pode ter o seu próprio WorldBoundary: o jogador pode andar
/// se a posição de destino estiver dentro de QUALQUER um deles.
/// Se não existir nenhum WorldBoundary na cena, não há limite nenhum.
/// Não rotacione nem escale este objeto: só a posição e o Size contam.
/// </summary>
public class WorldBoundary : MonoBehaviour
{
    private static readonly List<WorldBoundary> all = new List<WorldBoundary>();

    [Tooltip("Largura e altura da área, em tiles (unidades do mundo).")]
    [SerializeField] private Vector2 size = new Vector2(28f, 20f);

    [SerializeField] private Color gizmoColor = new Color(1f, 0.25f, 0.25f, 1f);

    /// <summary>Retângulo da área, em coordenadas do mundo.</summary>
    public Rect Area
    {
        get
        {
            Vector2 center = transform.position;
            return new Rect(center - size * 0.5f, size);
        }
    }

    public bool Contains(Vector2 point)
    {
        return Area.Contains(point);
    }

    /// <summary>
    /// True se o ponto está dentro de alguma área (ou se não há nenhuma área cadastrada).
    /// </summary>
    public static bool IsInsideAnyArea(Vector3 worldPoint)
    {
        if (all.Count == 0)
            return true;

        Vector2 p = new Vector2(worldPoint.x, worldPoint.y);
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Contains(p))
                return true;
        }
        return false;
    }

    private void OnEnable()
    {
        if (!all.Contains(this))
            all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    // Garante que a lista não guarde sobras caso o "Enter Play Mode" sem reload de domínio esteja ligado.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(1f, size.x);
        size.y = Mathf.Max(1f, size.y);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
    }

    private void OnDrawGizmosSelected()
    {
        Color fill = gizmoColor;
        fill.a = 0.12f;
        Gizmos.color = fill;
        Gizmos.DrawCube(transform.position, new Vector3(size.x, size.y, 0f));
    }
}
