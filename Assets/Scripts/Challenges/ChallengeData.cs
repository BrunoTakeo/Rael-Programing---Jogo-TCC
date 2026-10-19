using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public enum QuestionType
{
    MultipleChoice, // o jogador escolhe uma alternativa
    FillBlank       // o jogador digita a resposta (completar o código)
}

/// <summary>Uma pergunta dentro de um desafio.</summary>
[Serializable]
public class ChallengeQuestion
{
    public QuestionType type;

    [TextArea(2, 4)]
    [Tooltip("O enunciado da pergunta.")]
    public string prompt;

    [TextArea(2, 10)]
    [Tooltip("Trecho de código mostrado numa caixa escura (opcional). Use ___ para marcar a lacuna.")]
    public string code;

    [Header("Múltipla escolha")]
    [Tooltip("Alternativas (no máximo 6).")]
    public List<string> options = new List<string>();

    [Tooltip("Índice da alternativa correta. 0 = primeira, 1 = segunda...")]
    public int correctOption;

    [Header("Completar o código")]
    [Tooltip("Respostas aceitas. Espaços são ignorados: '==' e '= =' valem o mesmo.")]
    public List<string> acceptedAnswers = new List<string>();

    [Tooltip("Se marcado, 'Rael' e 'rael' valem o mesmo. Desmarcado, diferencia maiúsculas de minúsculas.")]
    public bool ignoreCase;

    [Header("Ajuda")]
    [TextArea(1, 3)]
    [Tooltip("Dica mostrada quando o jogador clica em 'Dica' (opcional).")]
    public string hint;

    [TextArea(1, 4)]
    [Tooltip("Explicação mostrada depois que o jogador acerta (opcional).")]
    public string explanation;

    /// <summary>Confere uma resposta digitada (para perguntas FillBlank).</summary>
    public bool IsCorrectText(string answer)
    {
        if (acceptedAnswers == null || answer == null)
            return false;

        string given = Normalize(answer, ignoreCase);
        if (given.Length == 0)
            return false;

        for (int i = 0; i < acceptedAnswers.Count; i++)
        {
            if (Normalize(acceptedAnswers[i], ignoreCase) == given)
                return true;
        }
        return false;
    }

    private static string Normalize(string text, bool lower)
    {
        string result = Regex.Replace(text ?? "", @"\s+", "");
        return lower ? result.ToLowerInvariant() : result;
    }
}

/// <summary>Configura a tela extra que anima um algoritmo de ordenação (opcional).</summary>
[Serializable]
public class SortVisualizationData
{
    [Tooltip("Marque para mostrar a tela de visualização entre a aula e as perguntas.")]
    public bool enabled;

    public SortAlgorithm algorithm;

    [Tooltip("Números que serão ordenados (de 2 a 10 valores, de preferência entre 1 e 99).")]
    public List<int> values = new List<int> { 5, 3, 8, 1, 9, 2 };
}

/// <summary>
/// Um desafio/lição de programação: um texto explicando o assunto seguido de perguntas.
/// Crie pelo menu: botão direito na pasta Project > Create > Rael > Desafio de Programação.
/// </summary>
[CreateAssetMenu(fileName = "Novo Desafio", menuName = "Rael/Desafio de Programação")]
public class ChallengeData : ScriptableObject
{
    [Tooltip("Identificador único, usado para salvar o progresso. Se ficar vazio, usa o nome do arquivo.")]
    [SerializeField] private string id;

    [SerializeField] private string title = "Novo Desafio";

    [TextArea(5, 14)]
    [Tooltip("A parte 'aula': o texto que explica o assunto antes das perguntas.")]
    [SerializeField] private string lesson;

    [Header("Tela extra: visualização de ordenação (opcional)")]
    [SerializeField] private SortVisualizationData visualization = new SortVisualizationData();

    [SerializeField] private List<ChallengeQuestion> questions = new List<ChallengeQuestion>();

    public string Id => string.IsNullOrEmpty(id) ? name : id;
    public string Title => title;
    public string Lesson => lesson;
    public List<ChallengeQuestion> Questions => questions;
    public SortVisualizationData Visualization => visualization;

    private string PrefKey => "desafio_concluido_" + Id;

    /// <summary>True se o jogador já concluiu este desafio.</summary>
    public bool IsCompleted => PlayerPrefs.GetInt(PrefKey, 0) == 1;

    public void MarkCompleted()
    {
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();
    }

    [ContextMenu("Resetar progresso deste desafio")]
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.Save();
    }

    private void OnValidate()
    {
        if (questions == null)
            return;

        for (int i = 0; i < questions.Count; i++)
        {
            ChallengeQuestion q = questions[i];
            if (q == null || q.options == null)
                continue;

            if (q.options.Count > 6)
                Debug.LogWarning("Desafio '" + name + "', pergunta " + (i + 1) + ": só as 6 primeiras alternativas aparecem.");

            if (q.type == QuestionType.MultipleChoice)
                q.correctOption = Mathf.Clamp(q.correctOption, 0, Mathf.Max(0, q.options.Count - 1));
        }
    }

#if UNITY_EDITOR
    /// <summary>Usado pelo menu do editor para criar desafios de exemplo.</summary>
    public void EditorSetup(string newId, string newTitle, string newLesson, List<ChallengeQuestion> newQuestions)
    {
        id = newId;
        title = newTitle;
        lesson = newLesson;
        questions = newQuestions;
    }

    public void EditorSetVisualization(SortAlgorithm algorithm, List<int> values)
    {
        visualization = new SortVisualizationData();
        visualization.enabled = true;
        visualization.algorithm = algorithm;
        visualization.values = values;
    }
#endif
}
