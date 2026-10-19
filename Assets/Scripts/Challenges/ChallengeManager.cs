using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Executa um desafio de programação: mostra a aula, faz as perguntas, dá feedback e mostra o resultado.
///
/// A interface (Canvas, painel, botões...) é criada por código, então NÃO precisa montar nada na cena.
/// O ChallengeTerminal cria este objeto sozinho quando necessário. Se quiser trocar as fontes,
/// coloque este componente num GameObject da cena e arraste as fontes nos campos abaixo.
///
/// Ele funciona como o DialogManager: enquanto o desafio está aberto, o GameController fica no estado
/// Challenge e chama HandleUpdate() a cada frame.
/// </summary>
public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance { get; private set; }

    /// <summary>Disparado quando o jogador termina todas as perguntas de um desafio.</summary>
    public static event Action<ChallengeData> OnChallengeCompleted;

    [Header("Aparência (opcional)")]
    [Tooltip("Fonte do texto da interface. Vazio = fonte padrão da Unity.")]
    [SerializeField] private Font uiFont;

    [Tooltip("Fonte do código. Vazio = tenta usar Consolas/Courier do sistema.")]
    [SerializeField] private Font codeFont;

    private enum Phase { Lesson, Visual, Asking, Correct, Result }

    private const float PanelW = 1000f;
    private const float PanelH = 600f;
    private const int MaxOptions = 6;

    private static readonly Color PanelColor = new Color(0.09f, 0.11f, 0.17f, 0.98f);
    private static readonly Color TitleColor = new Color(1f, 0.85f, 0.40f, 1f);
    private static readonly Color BodyColor = new Color(0.93f, 0.95f, 1f, 1f);
    private static readonly Color MutedColor = new Color(0.60f, 0.66f, 0.78f, 1f);
    private static readonly Color CodeBgColor = new Color(0.04f, 0.05f, 0.08f, 1f);
    private static readonly Color CodeTextColor = new Color(0.62f, 0.95f, 0.70f, 1f);
    private static readonly Color OptionColor = new Color(0.17f, 0.23f, 0.36f, 1f);
    private static readonly Color PrimaryColor = new Color(0.20f, 0.55f, 0.35f, 1f);
    private static readonly Color HintButtonColor = new Color(0.35f, 0.30f, 0.50f, 1f);
    private static readonly Color CorrectColor = new Color(0.45f, 0.90f, 0.50f, 1f);
    private static readonly Color WrongColor = new Color(0.95f, 0.45f, 0.45f, 1f);
    private static readonly Color HintTextColor = new Color(1f, 0.85f, 0.45f, 1f);

    // ---- Interface ----
    private GameObject root;
    private RectTransform contentRT;
    private RectTransform visualRoot;
    private SortVisualizerView sortView;
    private Text titleText;
    private Text progressText;
    private Text bodyText;
    private Text codeText;
    private Text feedbackText;
    private Text footerText;
    private Text primaryLabel;
    private GameObject codeBox;
    private GameObject optionsRoot;
    private Button primaryButton;
    private Button hintButton;
    private InputField answerInput;
    private Button[] optionButtons;
    private Image[] optionImages;
    private Text[] optionLabels;

    // ---- Estado ----
    private ChallengeData current;
    private Phase phase;
    private int questionIndex;
    private int mistakes;

    public bool IsOpen { get; private set; }

    /// <summary>True enquanto qualquer desafio estiver na tela. Usado para travar o jogador.</summary>
    public static bool IsChallengeOpen
    {
        get { return Instance != null && Instance.IsOpen; }
    }

    private int openedFrame = -1;
    private int lastHandledFrame = -1;

    private ChallengeQuestion CurrentQuestion
    {
        get { return current.Questions[questionIndex]; }
    }

    // ------------------------------------------------------------------
    // Criação / ciclo de vida
    // ------------------------------------------------------------------

    /// <summary>Devolve o ChallengeManager da cena, criando um se ainda não existir.</summary>
    public static ChallengeManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        ChallengeManager found = FindObjectOfType<ChallengeManager>();
        if (found != null)
            return found;

        return new GameObject("ChallengeManager").AddComponent<ChallengeManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureFonts();
        BuildUI();
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // O desafio lê as próprias teclas, sem depender do estado do GameController.
    private void Update()
    {
        HandleUpdate();
    }

    private static GameController FindGameController()
    {
        GameController gc = GameController.Instance;
        if (gc == null)
            gc = FindObjectOfType<GameController>();
        return gc;
    }

    // ------------------------------------------------------------------
    // API pública
    // ------------------------------------------------------------------

    public void StartChallenge(ChallengeData data)
    {
        if (data == null || IsOpen)
            return;

        if (data.Questions == null || data.Questions.Count == 0)
        {
            Debug.LogWarning("O desafio '" + data.name + "' não tem nenhuma pergunta.");
            return;
        }

        current = data;
        questionIndex = 0;
        mistakes = 0;
        IsOpen = true;
        openedFrame = Time.frameCount;
        root.SetActive(true);
        ClearSelection();

        GameController gc = FindGameController();
        if (gc != null)
            gc.SetGameState(GameState.Challenge);

        if (!string.IsNullOrWhiteSpace(current.Lesson))
            ShowLesson();
        else if (HasVisualization())
            ShowVisual();
        else
            ShowQuestion();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        root.SetActive(false);
        ClearSelection();

        GameController gc = FindGameController();
        if (gc != null)
            gc.SetGameState(GameState.FreeRoam);
    }

    /// <summary>Chamado pelo GameController a cada frame enquanto o estado for Challenge.</summary>
    public void HandleUpdate()
    {
        if (!IsOpen)
            return;

        // Ignora o frame em que o desafio abriu (a tecla Z que fechou o diálogo ainda está "apertada")
        // e evita processar duas vezes no mesmo frame se alguém também chamar este método.
        if (Time.frameCount == openedFrame || Time.frameCount == lastHandledFrame)
            return;
        lastHandledFrame = Time.frameCount;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        switch (phase)
        {
            case Phase.Lesson:
            case Phase.Correct:
            case Phase.Result:
                if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    OnPrimary();
                }
                break;

            case Phase.Visual:
                if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OnPrimary();
                else if (Input.GetKeyDown(KeyCode.Space))
                    sortView.TogglePlay();
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                    sortView.StepForwardManual();
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                    sortView.StepBackManual();
                else if (Input.GetKeyDown(KeyCode.R))
                    sortView.Restart();
                break;

            case Phase.Asking:
                ChallengeQuestion q = CurrentQuestion;
                if (q.type == QuestionType.MultipleChoice)
                {
                    int count = Mathf.Min(q.options.Count, MaxOptions);
                    for (int i = 0; i < count; i++)
                    {
                        if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                        {
                            ChooseOption(i);
                            break;
                        }
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    SubmitAnswer();
                }
                break;
        }
    }

    // ------------------------------------------------------------------
    // Telas
    // ------------------------------------------------------------------

    private void ShowLesson()
    {
        HideVisual();
        phase = Phase.Lesson;

        titleText.text = current.Title;
        progressText.text = "Lição";
        bodyText.gameObject.SetActive(true);
        bodyText.text = current.Lesson;

        codeBox.SetActive(false);
        optionsRoot.SetActive(false);
        answerInput.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);
        hintButton.gameObject.SetActive(false);

        SetPrimary("Começar  [Z]", true);
        footerText.text = "[Esc] sair";
        Rebuild();
    }

    private bool HasVisualization()
    {
        return current != null && current.Visualization != null && current.Visualization.enabled;
    }

    private void ShowVisual()
    {
        phase = Phase.Visual;

        contentRT.gameObject.SetActive(false);
        visualRoot.gameObject.SetActive(true);
        sortView.Show(current.Visualization);

        titleText.text = current.Title;
        progressText.text = "Veja o algoritmo";
        hintButton.gameObject.SetActive(false);

        SetPrimary("Ir às perguntas  [Z]", true);
        footerText.text = "[Espaço] play/pausa  [Setas] passo  [R] reiniciar";
    }

    private void HideVisual()
    {
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(false);
        if (sortView != null)
            sortView.Pause();
        if (contentRT != null)
            contentRT.gameObject.SetActive(true);
    }

    private void ShowQuestion()
    {
        HideVisual();
        phase = Phase.Asking;
        ChallengeQuestion q = CurrentQuestion;
        bool multipleChoice = q.type == QuestionType.MultipleChoice;

        titleText.text = current.Title;
        progressText.text = "Pergunta " + (questionIndex + 1) + "/" + current.Questions.Count;

        bodyText.gameObject.SetActive(true);
        bodyText.text = q.prompt;

        bool hasCode = !string.IsNullOrWhiteSpace(q.code);
        codeBox.SetActive(hasCode);
        if (hasCode)
            codeText.text = q.code;

        feedbackText.gameObject.SetActive(false);
        hintButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(q.hint));

        optionsRoot.SetActive(multipleChoice);
        answerInput.gameObject.SetActive(!multipleChoice);

        if (multipleChoice)
        {
            for (int i = 0; i < MaxOptions; i++)
            {
                bool visible = q.options != null && i < q.options.Count;
                optionButtons[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;

                optionLabels[i].text = (i + 1) + ")  " + q.options[i];
                optionImages[i].color = OptionColor;
                optionButtons[i].interactable = true;
            }

            SetPrimary("", false);
            footerText.text = "Clique ou aperte [1]-[" + Mathf.Min(q.options.Count, MaxOptions) + "]   |   [Esc] sair";
        }
        else
        {
            answerInput.text = "";
            answerInput.interactable = true;

            SetPrimary("Confirmar  [Enter]", true);
            footerText.text = "Digite a resposta e aperte [Enter]   |   [Esc] sair";
            StartCoroutine(FocusInputNextFrame());
        }

        Rebuild();
    }

    private void ShowResult()
    {
        HideVisual();
        phase = Phase.Result;

        current.MarkCompleted();
        if (OnChallengeCompleted != null)
            OnChallengeCompleted(current);

        int stars = mistakes == 0 ? 3 : (mistakes <= 2 ? 2 : 1);
        int total = current.Questions.Count;

        titleText.text = current.Title;
        progressText.text = "Concluído!";

        string errors = mistakes == 1 ? "1 erro" : mistakes + " erros";
        bodyText.gameObject.SetActive(true);
        bodyText.text = "Desafio concluído!\n\nVocê respondeu corretamente as " + total + " perguntas, com " +
                        errors + " pelo caminho.\n\nEstrelas: " + new string('*', stars) +
                        new string('-', 3 - stars);

        codeBox.SetActive(false);
        optionsRoot.SetActive(false);
        answerInput.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);
        hintButton.gameObject.SetActive(false);

        SetPrimary("Fechar  [Z]", true);
        footerText.text = "";
        Rebuild();
    }

    // ------------------------------------------------------------------
    // Lógica das respostas
    // ------------------------------------------------------------------

    private void OnPrimary()
    {
        ClearSelection();

        switch (phase)
        {
            case Phase.Lesson:
                if (HasVisualization())
                {
                    ShowVisual();
                }
                else
                {
                    questionIndex = 0;
                    ShowQuestion();
                }
                break;

            case Phase.Visual:
                questionIndex = 0;
                ShowQuestion();
                break;

            case Phase.Asking:
                SubmitAnswer();
                break;

            case Phase.Correct:
                questionIndex++;
                if (questionIndex < current.Questions.Count)
                    ShowQuestion();
                else
                    ShowResult();
                break;

            case Phase.Result:
                Close();
                break;
        }
    }

    private void ChooseOption(int index)
    {
        ClearSelection();

        if (phase != Phase.Asking)
            return;

        ChallengeQuestion q = CurrentQuestion;
        if (q.type != QuestionType.MultipleChoice || index < 0 || index >= q.options.Count)
            return;

        if (index == q.correctOption)
        {
            OnCorrect();
        }
        else
        {
            // Desativa a alternativa errada para o jogador não repetir o mesmo chute.
            optionButtons[index].interactable = false;
            optionImages[index].color = new Color(0.45f, 0.18f, 0.18f, 1f);
            OnWrong();
        }
    }

    private void SubmitAnswer()
    {
        if (phase != Phase.Asking)
            return;

        ChallengeQuestion q = CurrentQuestion;
        if (q.type != QuestionType.FillBlank || string.IsNullOrWhiteSpace(answerInput.text))
            return;

        if (q.IsCorrectText(answerInput.text))
        {
            OnCorrect();
        }
        else
        {
            OnWrong();
            StartCoroutine(FocusInputNextFrame());
        }
    }

    private void OnCorrect()
    {
        phase = Phase.Correct;
        ChallengeQuestion q = CurrentQuestion;

        string message = "Correto!";
        if (!string.IsNullOrWhiteSpace(q.explanation))
            message += "\n" + q.explanation;
        ShowFeedback(message, CorrectColor);

        if (q.type == QuestionType.MultipleChoice)
        {
            for (int i = 0; i < MaxOptions; i++)
                optionButtons[i].interactable = false;

            optionImages[q.correctOption].color = new Color(0.20f, 0.50f, 0.28f, 1f);
        }
        else
        {
            answerInput.interactable = false;
            answerInput.DeactivateInputField();
        }

        hintButton.gameObject.SetActive(false);

        bool isLast = questionIndex >= current.Questions.Count - 1;
        SetPrimary(isLast ? "Finalizar  [Z]" : "Continuar  [Z]", true);
        footerText.text = "[Esc] sair";
        Rebuild();
    }

    private void OnWrong()
    {
        mistakes++;

        string message = "Ops, não foi dessa vez. Tente de novo!";
        if (!string.IsNullOrWhiteSpace(CurrentQuestion.hint))
            message += "  (Clique em Dica se precisar.)";
        ShowFeedback(message, WrongColor);
    }

    private void ShowHint()
    {
        ClearSelection();

        if (phase != Phase.Asking)
            return;

        ChallengeQuestion q = CurrentQuestion;
        if (string.IsNullOrWhiteSpace(q.hint))
            return;

        ShowFeedback("Dica: " + q.hint, HintTextColor);

        if (q.type == QuestionType.FillBlank)
            StartCoroutine(FocusInputNextFrame());
    }

    private void ShowFeedback(string message, Color color)
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = message;
        feedbackText.color = color;
        Rebuild();
    }

    private IEnumerator FocusInputNextFrame()
    {
        // Espera um frame para a tecla que acabou de ser apertada não "vazar" para dentro do campo.
        yield return null;

        if (IsOpen && phase == Phase.Asking && answerInput.gameObject.activeInHierarchy)
        {
            answerInput.ActivateInputField();
            answerInput.MoveTextEnd(false);
        }
    }

    private void SetPrimary(string label, bool visible)
    {
        primaryButton.gameObject.SetActive(visible);
        primaryLabel.text = label;
    }

    private void Rebuild()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
    }

    private static void ClearSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // ------------------------------------------------------------------
    // Construção da interface (tudo por código)
    // ------------------------------------------------------------------

    private void EnsureFonts()
    {
        if (uiFont == null)
        {
#if UNITY_2022_2_OR_NEWER
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        if (codeFont == null)
            codeFont = Font.CreateDynamicFontFromOSFont(new string[] { "Consolas", "Menlo", "Courier New", "DejaVu Sans Mono" }, 20);

        if (codeFont == null)
            codeFont = uiFont;
    }

    private void BuildUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Canvas
        root = new GameObject("ChallengeCanvas", typeof(RectTransform));
        root.transform.SetParent(transform, false);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // Fundo escurecido (também impede cliques "atravessarem" para o jogo)
        RectTransform dim = ChallengeUI.NewRect("Dim", root.transform);
        ChallengeUI.Stretch(dim, 0f, 0f, 0f, 0f);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        // Painel central
        RectTransform panel = ChallengeUI.NewRect("Panel", root.transform);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(PanelW, PanelH);
        panel.anchoredPosition = Vector2.zero;
        panel.gameObject.AddComponent<Image>().color = PanelColor;

        // Cabeçalho
        titleText = ChallengeUI.NewText("Title", panel, 30, TextAnchor.MiddleLeft, TitleColor, uiFont);
        titleText.fontStyle = FontStyle.Bold;
        Region(titleText.rectTransform, 30f, 18f, 700f, 48f);

        progressText = ChallengeUI.NewText("Progress", panel, 22, TextAnchor.MiddleRight, MutedColor, uiFont);
        Region(progressText.rectTransform, 730f, 18f, 240f, 48f);

        RectTransform divider = ChallengeUI.NewRect("Divider", panel);
        Region(divider, 30f, 72f, 940f, 2f);
        divider.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        // Conteúdo (as partes são empilhadas automaticamente)
        contentRT = ChallengeUI.NewRect("Content", panel);
        Region(contentRT, 30f, 90f, 940f, 410f);

        VerticalLayoutGroup layout = contentRT.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        bodyText = ChallengeUI.NewText("Body", contentRT, 22, TextAnchor.UpperLeft, BodyColor, uiFont);

        // Caixa de código
        RectTransform codeRT = ChallengeUI.NewRect("CodeBox", contentRT);
        codeBox = codeRT.gameObject;
        codeRT.gameObject.AddComponent<Image>().color = CodeBgColor;
        VerticalLayoutGroup codeLayout = codeRT.gameObject.AddComponent<VerticalLayoutGroup>();
        codeLayout.padding = new RectOffset(16, 16, 12, 12);
        codeLayout.childControlWidth = true;
        codeLayout.childControlHeight = true;
        codeLayout.childForceExpandWidth = true;
        codeLayout.childForceExpandHeight = false;
        codeText = ChallengeUI.NewText("Code", codeRT, 20, TextAnchor.UpperLeft, CodeTextColor, codeFont);

        // Alternativas
        RectTransform optionsRT = ChallengeUI.NewRect("Options", contentRT);
        optionsRoot = optionsRT.gameObject;
        VerticalLayoutGroup optionsLayout = optionsRT.gameObject.AddComponent<VerticalLayoutGroup>();
        optionsLayout.spacing = 8f;
        optionsLayout.childControlWidth = true;
        optionsLayout.childControlHeight = true;
        optionsLayout.childForceExpandWidth = true;
        optionsLayout.childForceExpandHeight = false;

        optionButtons = new Button[MaxOptions];
        optionImages = new Image[MaxOptions];
        optionLabels = new Text[MaxOptions];
        for (int i = 0; i < MaxOptions; i++)
        {
            int index = i; // cópia para o clique usar o índice certo
            Text label;
            Button button = ChallengeUI.NewButton("Option" + (i + 1), optionsRT, OptionColor, 46f, 22, TextAnchor.MiddleLeft, uiFont, out label);
            button.onClick.AddListener(() => ChooseOption(index));
            optionButtons[i] = button;
            optionImages[i] = button.GetComponent<Image>();
            optionLabels[i] = label;
        }

        // Campo de resposta
        answerInput = NewInputField(contentRT);

        // Feedback (acerto/erro/dica)
        feedbackText = ChallengeUI.NewText("Feedback", contentRT, 22, TextAnchor.UpperLeft, CorrectColor, uiFont);

        // Tela extra de visualização (começa escondida)
        visualRoot = ChallengeUI.NewRect("Visual", panel);
        Region(visualRoot, 30f, 90f, 940f, 410f);
        sortView = visualRoot.gameObject.AddComponent<SortVisualizerView>();
        sortView.Build(940f, 410f, uiFont, codeFont);
        visualRoot.gameObject.SetActive(false);

        // Rodapé
        footerText = ChallengeUI.NewText("Footer", panel, 18, TextAnchor.MiddleLeft, MutedColor, uiFont);
        Region(footerText.rectTransform, 30f, 528f, 560f, 44f);

        Text hintLabel;
        hintButton = ChallengeUI.NewButton("HintButton", panel, HintButtonColor, 48f, 22, TextAnchor.MiddleCenter, uiFont, out hintLabel);
        hintLabel.text = "Dica";
        hintButton.onClick.AddListener(ShowHint);
        Region(hintButton.GetComponent<RectTransform>(), 600f, 526f, 150f, 48f);

        primaryButton = ChallengeUI.NewButton("PrimaryButton", panel, PrimaryColor, 48f, 22, TextAnchor.MiddleCenter, uiFont, out primaryLabel);
        primaryButton.onClick.AddListener(OnPrimary);
        Region(primaryButton.GetComponent<RectTransform>(), 760f, 526f, 210f, 48f);
    }

    /// <summary>Posiciona um elemento no painel: x/y contados a partir do canto superior esquerdo.</summary>
    private static void Region(RectTransform rt, float x, float y, float width, float height)
    {
        ChallengeUI.Place(rt, PanelW, PanelH, x, y, width, height);
    }

    private InputField NewInputField(Transform parent)
    {
        RectTransform rt = ChallengeUI.NewRect("AnswerInput", parent);

        Image background = rt.gameObject.AddComponent<Image>();
        background.color = new Color(0.04f, 0.05f, 0.08f, 1f);

        LayoutElement element = rt.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 52f;

        Text text = ChallengeUI.NewText("Text", rt, 22, TextAnchor.MiddleLeft, Color.white, codeFont);
        ChallengeUI.Stretch(text.rectTransform, 14f, 6f, 14f, 6f);

        Text placeholder = ChallengeUI.NewText("Placeholder", rt, 22, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.35f), codeFont);
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.text = "Digite a resposta aqui...";
        ChallengeUI.Stretch(placeholder.rectTransform, 14f, 6f, 14f, 6f);

        InputField field = rt.gameObject.AddComponent<InputField>();
        field.targetGraphic = background;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.lineType = InputField.LineType.SingleLine;
        field.characterLimit = 80;

        return field;
    }
}
