using System;
using System.Collections.Generic;

public enum SortAlgorithm
{
    BubbleSort,
    SelectionSort,
    InsertionSort
}

public enum SortStepKind
{
    Info,     // só uma explicação
    Compare,  // dois elementos sendo comparados
    Swap      // dois elementos trocando de lugar
}

/// <summary>Uma "foto" do algoritmo em um instante: a ordem dos elementos e o que está destacado.</summary>
public class SortStep
{
    public int[] order;      // order[posição] = id do elemento que está nessa posição
    public bool[] sorted;    // posições que já estão no lugar certo
    public int a = -1;       // posições destacadas (comparação ou troca)
    public int b = -1;
    public int minPos = -1;  // posição marcada em roxo (menor atual / elemento sendo inserido)
    public SortStepKind kind;
    public int line = -1;    // linha do código C# que está sendo executada
    public string text;      // explicação em português
}

/// <summary>
/// Calcula, de uma vez, todos os passos de um algoritmo de ordenação.
/// Assim a tela pode avançar, voltar e reiniciar sem recalcular nada.
/// </summary>
public static class SortStepGenerator
{
    public static string[] CodeLines(SortAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case SortAlgorithm.SelectionSort:
                return new string[]
                {
                    "for (int i = 0; i < n-1; i++)",
                    "{",
                    "  int min = i;",
                    "  for (int j = i+1; j < n; j++)",
                    "    if (v[j] < v[min]) min = j;",
                    "  Trocar(v, i, min);",
                    "}"
                };

            case SortAlgorithm.InsertionSort:
                return new string[]
                {
                    "for (int i = 1; i < n; i++)",
                    "{",
                    "  int j = i;",
                    "  while (j > 0 && v[j-1] > v[j])",
                    "  {",
                    "    Trocar(v, j, j-1);",
                    "    j--;",
                    "  }",
                    "}"
                };

            default: // BubbleSort
                return new string[]
                {
                    "for (int i = 0; i < n-1; i++)",
                    "  for (int j = 0; j < n-1-i; j++)",
                    "    if (v[j] > v[j+1])",
                    "      Trocar(v, j, j+1);"
                };
        }
    }

    public static string DisplayName(SortAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case SortAlgorithm.SelectionSort: return "Selection Sort";
            case SortAlgorithm.InsertionSort: return "Insertion Sort";
            default: return "Bubble Sort";
        }
    }

    public static List<SortStep> Generate(SortAlgorithm algorithm, int[] values)
    {
        switch (algorithm)
        {
            case SortAlgorithm.SelectionSort: return Selection(values);
            case SortAlgorithm.InsertionSort: return Insertion(values);
            default: return Bubble(values);
        }
    }

    // ------------------------------------------------------------------

    private static SortStep Snap(int[] order, bool[] sorted, int a, int b, int minPos, SortStepKind kind, int line, string text)
    {
        SortStep s = new SortStep();
        s.order = (int[])order.Clone();
        s.sorted = (bool[])sorted.Clone();
        s.a = a;
        s.b = b;
        s.minPos = minPos;
        s.kind = kind;
        s.line = line;
        s.text = text;
        return s;
    }

    private static int[] Identity(int n)
    {
        int[] order = new int[n];
        for (int i = 0; i < n; i++)
            order[i] = i;
        return order;
    }

    private static void SwapPositions(int[] order, int p, int q)
    {
        int t = order[p];
        order[p] = order[q];
        order[q] = t;
    }

    private static List<SortStep> Bubble(int[] v)
    {
        int n = v.Length;
        int[] order = Identity(n);
        bool[] sorted = new bool[n];
        List<SortStep> steps = new List<SortStep>();

        steps.Add(Snap(order, sorted, -1, -1, -1, SortStepKind.Info, -1,
            "Vetor inicial. O Bubble Sort compara vizinhos e troca os que estão fora de ordem."));

        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - 1 - i; j++)
            {
                int x = v[order[j]];
                int y = v[order[j + 1]];
                bool swap = x > y;

                steps.Add(Snap(order, sorted, j, j + 1, -1, SortStepKind.Compare, 2,
                    "Comparando " + x + " e " + y + ": " + x + " > " + y + "? " +
                    (swap ? "Sim, então troca." : "Não, ficam como estão.")));

                if (swap)
                {
                    SwapPositions(order, j, j + 1);
                    steps.Add(Snap(order, sorted, j, j + 1, -1, SortStepKind.Swap, 3,
                        "Trocando " + x + " e " + y + " de lugar."));
                }
            }

            sorted[n - 1 - i] = true;
            steps.Add(Snap(order, sorted, -1, -1, -1, SortStepKind.Info, 0,
                "Fim da volta: o maior valor restante (" + v[order[n - 1 - i]] + ") chegou à posição final."));
        }

        if (n > 0)
            sorted[0] = true;

        steps.Add(Snap(order, sorted, -1, -1, -1, SortStepKind.Info, -1, "Pronto! O vetor está ordenado."));
        return steps;
    }

    private static List<SortStep> Selection(int[] v)
    {
        int n = v.Length;
        int[] order = Identity(n);
        bool[] sorted = new bool[n];
        List<SortStep> steps = new List<SortStep>();

        steps.Add(Snap(order, sorted, -1, -1, -1, SortStepKind.Info, -1,
            "Vetor inicial. O Selection Sort procura o menor valor e o coloca na posição certa."));

        for (int i = 0; i < n - 1; i++)
        {
            int min = i;
            steps.Add(Snap(order, sorted, -1, -1, min, SortStepKind.Info, 2,
                "Posição " + i + ": por enquanto, o menor valor é " + v[order[min]] + "."));

            for (int j = i + 1; j < n; j++)
            {
                int x = v[order[j]];
                int m = v[order[min]];
                bool less = x < m;

                int shownMin = less ? j : min;
                steps.Add(Snap(order, sorted, j, -1, shownMin, SortStepKind.Compare, 4,
                    "Comparando " + x + " com o menor atual (" + m + "): " + x + " < " + m + "? " +
                    (less ? "Sim! Novo menor: " + x + "." : "Não.")));

                if (less)
                    min = j;
            }

            sorted[i] = true;
            if (min != i)
            {
                int left = v[order[i]];
                int smallest = v[order[min]];
                SwapPositions(order, i, min);
                steps.Add(Snap(order, sorted, i, min, -1, SortStepKind.Swap, 5,
                    "Trocando " + left + " (posição " + i + ") com o menor valor, " + smallest + "."));
            }
            else
            {
                steps.Add(Snap(order, sorted, -1, -1, -1, SortStepKind.Info, 5,
                    "O " + v[order[i]] + " já estava na posição certa, não precisa trocar."));
            }
        }

        if (n > 0)
            sorted[n - 1] = true;

        steps.Add(Snap(order, sorted, -1, -1, -1, SortStepKind.Info, -1, "Pronto! O vetor está ordenado."));
        return steps;
    }

    private static List<SortStep> Insertion(int[] v)
    {
        int n = v.Length;
        int[] order = Identity(n);
        bool[] sorted = new bool[n];
        List<SortStep> steps = new List<SortStep>();

        if (n > 0)
            sorted[0] = true;

        steps.Add(Snap(order, sorted, -1, -1, -1, SortStepKind.Info, -1,
            "Vetor inicial. O Insertion Sort mantém a parte da esquerda ordenada e vai inserindo um valor por vez."));

        for (int i = 1; i < n; i++)
        {
            steps.Add(Snap(order, sorted, -1, -1, i, SortStepKind.Info, 0,
                "Vamos inserir o " + v[order[i]] + " (posição " + i + ") na parte ordenada, à esquerda."));

            int j = i;
            while (j > 0)
            {
                int left = v[order[j - 1]];
                int cur = v[order[j]];
                bool swap = left > cur;

                steps.Add(Snap(order, sorted, j - 1, j, -1, SortStepKind.Compare, 3,
                    "Comparando " + left + " e " + cur + ": " + left + " > " + cur + "? " +
                    (swap ? "Sim, então troca." : "Não, o " + cur + " já está no lugar certo.")));

                if (!swap)
                    break;

                SwapPositions(order, j - 1, j);
                steps.Add(Snap(order, sorted, j - 1, j, -1, SortStepKind.Swap, 5,
                    "Trocando " + left + " e " + cur + "."));
                j--;
            }

            for (int k = 0; k <= i; k++)
                sorted[k] = true;

            steps.Add(Snap(order, sorted, -1, -1, -1, SortStepKind.Info, 0,
                "A parte ordenada agora vai da posição 0 até a " + i + "."));
        }

        steps.Add(Snap(order, sorted, -1, -1, -1, SortStepKind.Info, -1, "Pronto! O vetor está ordenado."));
        return steps;
    }
}
