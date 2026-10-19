using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela de visualização de algoritmos de ordenação (no estilo do VisuAlgo):
/// retângulos com números que se movem, o código C# com a linha atual destacada
/// e controles de play/pausa, passo a passo e velocidade.
/// É criada e controlada pelo ChallengeManager.
/// </summary>
public class SortVisualizerView : MonoBehaviour
{
    // Layout (em pixels, dentro de uma área de 940 x 410)
    private const float AreaW = 560f;
    private const float AreaH = 236f;
    private const float CodeW = 360f;
    private const float BarBaseY = 30f;   // espaço embaixo das barras para o número da posição
    private const float MaxBars = 10;

    private static readonly Color BodyColor = new Color(0.93f, 0.95f, 1f, 1f);
    private static readonly Color MutedColor = new Color(0.60f, 0.66f, 0.78f, 1f);
    private static readonly Color PanelDark = new Color(0.04f, 0.05f, 0.08f, 1f);
    private static readonly Color CodeTextColor = new Color(0.62f, 0.95f, 0.70f, 1f);
    private static readonly Color LineHighlightColor = new Color(1f, 0.85f, 0.30f, 0.28f);
    private static readonly Color BarText = new Color(0.05f, 0.07f, 0.12f, 1f);

    private static readonly Color BarBlue = new Color(0.40f, 0.60f, 0.95f, 1f);
    private static readonly Color BarYellow = new Color(1.00f, 0.86f, 0.30f, 1f);
    private static readonly Color BarOrange = new Color(1.00f, 0.55f, 0.25f, 1f);
    private static readonly Color BarPurple = new Color(0.75f, 0.55f, 1.00f, 1f);
    private static readonly Color BarGreen = new Color(0.40f, 0.85f, 0.50f, 1f);

    private static readonly float[] Intervals = { 1.0f, 0.55f, 0.25f };
    private static readonly string[] SpeedNames = { "Lenta", "Normal", "Rápida" };

    private Font uiFont;
    private Font codeFont;

    private RectTransform barsRoot;
    private RectTransform codePanel;
    private Image lineHighlight;
    private Text descText;
    private Text counterText;
    private Text playLabel;
    private Text speedLabel;

    private readonly List<GameObject> barObjects = new List<GameObject>();
    private readonly List<GameObject> codeObjects = new List<GameObject>();
    private RectTransform[] barRects;
    private Image[] barImages;
    private float[] targetX;
    private int barCount;
    private float slotWidth;
    private float barWidth;

    private int[] values;
    private SortAlgorithm algorithm;
    private List<SortStep> steps;
    private int stepIndex;
    private bool playing;
    private float timer;
    private int speedIndex = 1;

    // ------------------------------------------------------------------
    // Construção
    // ------------------------------------------------------------------

    public void Build(float width, float height, Font ui, Font code)
    {
        uiFont = ui;
        codeFont = code;
        RectTransform root = GetComponent<RectTransform>();

        // Explicação do passo atual
        descText = ChallengeUI.NewText("Descricao", root, 21, TextAnchor.MiddleLeft, BodyColor, uiFont);
        ChallengeUI.Place(descText.rectTransform, width, height, 0f, 0f, width, 56f);

        // Área dos retângulos
        barsRoot = ChallengeUI.NewRect("Barras", root);
        ChallengeUI.Place(barsRoot, width, height, 0f, 64f, AreaW, AreaH);
        barsRoot.gameObject.AddComponent<Image>().color = PanelDark;

        // Painel do código
        codePanel = ChallengeUI.NewRect("Codigo", root);
        ChallengeUI.Place(codePanel, width, height, width - CodeW, 64f, CodeW, AreaH);
        codePanel.gameObject.AddComponent<Image>().color = PanelDark;

        RectTransform highlightRT = ChallengeUI.NewRect("LinhaAtual", codePanel);
        lineHighlight = highlightRT.gameObject.AddComponent<Image>();
        lineHighlight.color = LineHighlightColor;
        lineHighlight.raycastTarget = false;
        lineHighlight.gameObject.SetActive(false);

        // Controles
        Color neutral = new Color(0.17f, 0.23f, 0.36f, 1f);
        Color green = new Color(0.20f, 0.55f, 0.35f, 1f);
        Text label;

        Button restart = ChallengeUI.NewButton("Reiniciar", root, neutral, 44f, 20, TextAnchor.MiddleCenter, uiFont, out label);
        label.text = "Reiniciar";
        restart.onClick.AddListener(() => { ChallengeUI.ClearSelection(); Restart(); });
        ChallengeUI.Place(restart.GetComponent<RectTransform>(), width, height, 0f, 312f, 140f, 44f);

        Button back = ChallengeUI.NewButton("Anterior", root, neutral, 44f, 20, TextAnchor.MiddleCenter, uiFont, out label);
        label.text = "Anterior";
        back.onClick.AddListener(() => { ChallengeUI.ClearSelection(); StepBackManual(); });
        ChallengeUI.Place(back.GetComponent<RectTransform>(), width, height, 154f, 312f, 140f, 44f);

        Button play = ChallengeUI.NewButton("Play", root, green, 44f, 20, TextAnchor.MiddleCenter, uiFont, out playLabel);
        play.onClick.AddListener(() => { ChallengeUI.ClearSelection(); TogglePlay(); });
        ChallengeUI.Place(play.GetComponent<RectTransform>(), width, height, 308f, 312f, 140f, 44f);

        Button next = ChallengeUI.NewButton("Proximo", root, neutral, 44f, 20, TextAnchor.MiddleCenter, uiFont, out label);
        label.text = "Próximo";
        next.onClick.AddListener(() => { ChallengeUI.ClearSelection(); StepForwardManual(); });
        ChallengeUI.Place(next.GetComponent<RectTransform>(), width, height, 462f, 312f, 140f, 44f);

        Button speed = ChallengeUI.NewButton("Velocidade", root, neutral, 44f, 20, TextAnchor.MiddleCenter, uiFont, out speedLabel);
        speed.onClick.AddListener(() => { ChallengeUI.ClearSelection(); CycleSpeed(); });
        ChallengeUI.Place(speed.GetComponent<RectTransform>(), width, height, 616f, 312f, 210f, 44f);

        counterText = ChallengeUI.NewText("Contador", root, 18, TextAnchor.MiddleLeft, MutedColor, uiFont);
        ChallengeUI.Place(counterText.rectTransform, width, height, 0f, 366f, width, 30f);
    }

    // ------------------------------------------------------------------
    // API usada pelo ChallengeManager
    // ------------------------------------------------------------------

    public string AlgorithmName
    {
        get { return SortStepGenerator.DisplayName(algorithm); }
    }

    public void Show(SortVisualizationData data)
    {
        List<int> source = (data != null && data.values != null && data.values.Count >= 2)
            ? data.values
            : new List<int> { 5, 3, 8, 1, 9, 2 };

        int n = Mathf.Min(source.Count, (int)MaxBars);
        values = new int[n];
        for (int i = 0; i < n; i++)
            values[i] = Mathf.Max(1, source[i]);

        algorithm = data != null ? data.algorithm : SortAlgorithm.BubbleSort;
        steps = SortStepGenerator.Generate(algorithm, values);

        BuildBars();
        BuildCode();

        stepIndex = 0;
        ApplyStep(true);

        playing = true;
        timer = 0f;
        speedIndex = 1;
        UpdatePlayLabel();
        UpdateSpeedLabel();
    }

    public void Pause()
    {
        playing = false;
    }

    public void TogglePlay()
    {
        if (steps == null)
            return;

        if (stepIndex >= steps.Count - 1)
        {
            Restart(); // terminou: Play recomeça
            return;
        }

        playing = !playing;
        timer = 0f;
        UpdatePlayLabel();
    }

    public void Restart()
    {
        if (steps == null)
            return;

        stepIndex = 0;
        ApplyStep(true);
        playing = true;
        timer = 0f;
        UpdatePlayLabel();
    }

    public void StepForwardManual()
    {
        playing = false;
        StepForward();
        UpdatePlayLabel();
    }

    public void StepBackManual()
    {
        playing = false;
        if (steps != null && stepIndex > 0)
        {
            stepIndex--;
            ApplyStep(false);
        }
        UpdatePlayLabel();
    }

    public void CycleSpeed()
    {
        speedIndex = (speedIndex + 1) % Intervals.Length;
        UpdateSpeedLabel();
    }

    // ------------------------------------------------------------------
    // Animação
    // ------------------------------------------------------------------

    private void Update()
    {
        if (barRects == null)
            return;

        float dt = Time.unscaledDeltaTime;

        // Os retângulos deslizam suavemente até a posição do passo atual.
        float k = Mathf.Min(1f, 14f * dt);
        for (int id = 0; id < barRects.Length; id++)
        {
            Vector2 p = barRects[id].anchoredPosition;
            p.x = Mathf.Lerp(p.x, targetX[id], k);
            barRects[id].anchoredPosition = p;
        }

        if (playing)
        {
            timer += dt;
            if (timer >= Intervals[speedIndex])
            {
                timer = 0f;
                bool moved = StepForward();
                if (!moved || stepIndex >= steps.Count - 1)
                    playing = false;
                UpdatePlayLabel();
            }
        }
    }

    private bool StepForward()
    {
        if (steps == null || stepIndex >= steps.Count - 1)
            return false;

        stepIndex++;
        ApplyStep(false);
        return true;
    }

    private float SlotX(int position)
    {
        return 10f + position * slotWidth + (slotWidth - barWidth) * 0.5f;
    }

    private void BuildBars()
    {
        for (int i = 0; i < barObjects.Count; i++)
            Destroy(barObjects[i]);
        barObjects.Clear();

        barCount = values.Length;
        slotWidth = (AreaW - 20f) / barCount;
        barWidth = Mathf.Min(slotWidth - 10f, 70f);

        int maxValue = 1;
        for (int i = 0; i < barCount; i++)
            maxValue = Mathf.Max(maxValue, values[i]);

        float maxBarH = AreaH - BarBaseY - 14f;
        float minBarH = 36f;

        barRects = new RectTransform[barCount];
        barImages = new Image[barCount];
        targetX = new float[barCount];

        for (int id = 0; id < barCount; id++)
        {
            RectTransform rt = ChallengeUI.NewRect("Barra" + id, barsRoot);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);

            float h = minBarH + ((float)values[id] / maxValue) * (maxBarH - minBarH);
            rt.sizeDelta = new Vector2(barWidth, h);
            rt.anchoredPosition = new Vector2(SlotX(id), BarBaseY);

            Image img = rt.gameObject.AddComponent<Image>();
            img.color = BarBlue;
            img.raycastTarget = false;

            Text value = ChallengeUI.NewText("Valor", rt, 22, TextAnchor.UpperCenter, BarText, uiFont);
            value.fontStyle = FontStyle.Bold;
            value.text = values[id].ToString();
            ChallengeUI.Stretch(value.rectTransform, 0f, 0f, 0f, 4f);

            barRects[id] = rt;
            barImages[id] = img;
            targetX[id] = SlotX(id);
            barObjects.Add(rt.gameObject);
        }

        // Números das posições (0, 1, 2...) embaixo de cada "casa"
        for (int p = 0; p < barCount; p++)
        {
            Text index = ChallengeUI.NewText("Pos" + p, barsRoot, 16, TextAnchor.MiddleCenter, MutedColor, uiFont);
            index.text = p.ToString();
            ChallengeUI.Place(index.rectTransform, AreaW, AreaH, SlotX(p) - 5f, AreaH - 26f, barWidth + 10f, 22f);
            barObjects.Add(index.gameObject);
        }
    }

    private void BuildCode()
    {
        for (int i = 0; i < codeObjects.Count; i++)
            Destroy(codeObjects[i]);
        codeObjects.Clear();

        string[] lines = SortStepGenerator.CodeLines(algorithm);
        for (int k = 0; k < lines.Length; k++)
        {
            Text t = ChallengeUI.NewText("Linha" + k, codePanel, 16, TextAnchor.MiddleLeft, CodeTextColor, codeFont);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = lines[k];
            ChallengeUI.Place(t.rectTransform, CodeW, AreaH, 10f, 8f + k * 24f, CodeW - 15f, 24f);
            codeObjects.Add(t.gameObject);
        }
    }

    private void ApplyStep(bool snapPositions)
    {
        SortStep s = steps[stepIndex];

        for (int pos = 0; pos < barCount; pos++)
        {
            int id = s.order[pos];
            targetX[id] = SlotX(pos);
            barImages[id].color = ColorFor(s, pos);

            if (snapPositions)
            {
                Vector2 p = barRects[id].anchoredPosition;
                p.x = targetX[id];
                barRects[id].anchoredPosition = p;
            }
        }

        descText.text = s.text;
        counterText.text = AlgorithmName + "   |   Passo " + (stepIndex + 1) + " de " + steps.Count;

        if (s.line >= 0 && s.line < codeObjects.Count)
        {
            lineHighlight.gameObject.SetActive(true);
            ChallengeUI.Place(lineHighlight.rectTransform, CodeW, AreaH, 4f, 8f + s.line * 24f, CodeW - 8f, 24f);
        }
        else
        {
            lineHighlight.gameObject.SetActive(false);
        }
    }

    private static Color ColorFor(SortStep s, int pos)
    {
        bool highlighted = pos == s.a || pos == s.b;

        if (highlighted && s.kind == SortStepKind.Swap)
            return BarOrange;
        if (highlighted && s.kind == SortStepKind.Compare)
            return BarYellow;
        if (pos == s.minPos)
            return BarPurple;
        if (s.sorted[pos])
            return BarGreen;
        return BarBlue;
    }

    private void UpdatePlayLabel()
    {
        if (playLabel == null)
            return;

        bool finished = steps != null && stepIndex >= steps.Count - 1;
        playLabel.text = finished ? "Repetir" : (playing ? "Pausar" : "Play");
    }

    private void UpdateSpeedLabel()
    {
        if (speedLabel != null)
            speedLabel.text = "Velocidade: " + SpeedNames[speedIndex];
    }
}
