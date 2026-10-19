using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Atalhos do editor para testar o sistema de desafios.
/// Este arquivo precisa ficar numa pasta chamada "Editor".
/// </summary>
public static class RaelTools
{
    private const string Folder = "Assets/Desafios";

    [MenuItem("Tools/Rael/Criar desafios de exemplo")]
    public static void CreateSampleChallenges()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "Desafios");

        ChallengeData first = CreateAsset("Desafio_01_Variaveis", "variaveis", "Variáveis", Variables());
        CreateAsset("Desafio_02_Condicionais", "condicionais", "Condicionais (if / else)", Conditionals());
        CreateAsset("Desafio_03_Lacos", "lacos", "Laços de repetição", Loops());
        CreateAsset("Desafio_04_CSharp_Basico", "csharp_basico", "C# Básico: tipos e variáveis", CSharpBasics());
        CreateAsset("Desafio_05_BubbleSort", "bubble_sort", "Ordenação: Bubble Sort (C#)", BubbleSortChallenge(),
            SortAlgorithm.BubbleSort, new[] { 5, 3, 8, 1, 9, 2 });
        CreateAsset("Desafio_06_SelectionSort", "selection_sort", "Ordenação: Selection Sort (C#)", SelectionSortChallenge(),
            SortAlgorithm.SelectionSort, new[] { 6, 2, 9, 4, 1, 7 });
        CreateAsset("Desafio_07_InsertionSort", "insertion_sort", "Ordenação: Insertion Sort (C#)", InsertionSortChallenge(),
            SortAlgorithm.InsertionSort, new[] { 7, 3, 9, 4, 6, 1 });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (first != null)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = first;
        }

        Debug.Log("Desafios de exemplo criados em " + Folder + ". Arraste um deles para o campo 'Challenge' de um ChallengeTerminal.");
    }

    [MenuItem("Tools/Rael/Resetar progresso dos desafios")]
    public static void ResetProgress()
    {
        string[] guids = AssetDatabase.FindAssets("t:ChallengeData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ChallengeData data = AssetDatabase.LoadAssetAtPath<ChallengeData>(path);
            if (data != null)
                data.ResetProgress();
        }

        Debug.Log("Progresso de " + guids.Length + " desafio(s) resetado.");
    }

    // ------------------------------------------------------------------

    private static ChallengeData CreateAsset(string fileName, string id, string title, ChallengeContent content,
        SortAlgorithm? sortAlgorithm = null, int[] sortValues = null)
    {
        string path = Folder + "/" + fileName + ".asset";

        // Não sobrescreve um desafio que você já tenha editado.
        ChallengeData existing = AssetDatabase.LoadAssetAtPath<ChallengeData>(path);
        if (existing != null)
            return existing;

        ChallengeData data = ScriptableObject.CreateInstance<ChallengeData>();
        data.EditorSetup(id, title, content.lesson, content.questions);
        if (sortAlgorithm.HasValue && sortValues != null)
            data.EditorSetVisualization(sortAlgorithm.Value, new List<int>(sortValues));
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private struct ChallengeContent
    {
        public string lesson;
        public List<ChallengeQuestion> questions;
    }

    private static ChallengeQuestion Choice(string prompt, string code, string[] options, int correct, string explanation)
    {
        ChallengeQuestion q = new ChallengeQuestion();
        q.type = QuestionType.MultipleChoice;
        q.prompt = prompt;
        q.code = code;
        q.options = new List<string>(options);
        q.correctOption = correct;
        q.explanation = explanation;
        return q;
    }

    private static ChallengeQuestion Fill(string prompt, string code, string[] answers, bool ignoreCase, string hint, string explanation)
    {
        ChallengeQuestion q = new ChallengeQuestion();
        q.type = QuestionType.FillBlank;
        q.prompt = prompt;
        q.code = code;
        q.acceptedAnswers = new List<string>(answers);
        q.ignoreCase = ignoreCase;
        q.hint = hint;
        q.explanation = explanation;
        return q;
    }

    private static ChallengeContent Variables()
    {
        ChallengeContent c = new ChallengeContent();
        c.lesson =
            "Uma variável é como uma caixinha com um nome, onde guardamos um valor para usar depois.\n\n" +
            "Em Python, criamos uma variável com o sinal de igual (=):\n\n" +
            "    vida = 100\n" +
            "    nome = \"Rael\"\n\n" +
            "O nome da variável fica à esquerda e o valor à direita. Depois, basta usar o nome para ler o valor guardado.";

        c.questions = new List<ChallengeQuestion>
        {
            Choice("Qual linha cria uma variável chamada vida com o valor 100?", "",
                new[] { "vida = 100", "100 = vida", "vida == 100", "criar vida 100" }, 0,
                "O nome vai à esquerda e o valor à direita do =. O == serve para comparar, não para guardar."),

            Fill("Complete o código para guardar o número 5 na variável pontos.",
                "pontos ___ 5",
                new[] { "=" }, false,
                "Qual símbolo usamos para guardar um valor dentro de uma variável?",
                "Isso! O = guarda o valor 5 dentro de pontos."),

            Fill("O que o código abaixo mostra na tela?",
                "nome = \"Rael\"\nprint(nome)",
                new[] { "Rael" }, true,
                "O print mostra o valor guardado na variável, não o nome dela.",
                "print(nome) mostra o valor guardado em nome: Rael.")
        };
        return c;
    }

    private static ChallengeContent Conditionals()
    {
        ChallengeContent c = new ChallengeContent();
        c.lesson =
            "Com o if, o programa toma decisões: o bloco só roda se a condição for verdadeira. " +
            "O else roda quando a condição é falsa.\n\n" +
            "    if vida > 0:\n" +
            "        print(\"Vivo\")\n" +
            "    else:\n" +
            "        print(\"Game Over\")\n\n" +
            "Repare nos dois-pontos (:) no fim do if e do else, e no recuo (espaços) das linhas de dentro do bloco.";

        c.questions = new List<ChallengeQuestion>
        {
            Choice("O que o código abaixo mostra na tela?",
                "vida = 0\nif vida > 0:\n    print(\"Vivo\")\nelse:\n    print(\"Game Over\")",
                new[] { "Vivo", "Game Over", "Nada", "Dá erro" }, 1,
                "vida vale 0, então vida > 0 é falso e o else é executado."),

            Fill("Complete para verificar se vida é igual a 0.",
                "if vida ___ 0:",
                new[] { "==" }, false,
                "Para comparar dois valores usamos dois sinais de igual.",
                "O == compara dois valores. Um único = guardaria um valor."),

            Choice("Qual palavra executa um bloco quando a condição do if é falsa?", "",
                new[] { "else", "then", "stop", "otherwise" }, 0,
                "O else cobre o caso contrário ao do if.")
        };
        return c;
    }

    private static ChallengeContent Loops()
    {
        ChallengeContent c = new ChallengeContent();
        c.lesson =
            "Um laço (loop) repete um bloco de código sem precisar escrever tudo de novo.\n\n" +
            "    for i in range(3):\n" +
            "        print(\"Oi\")\n\n" +
            "O range(3) faz o bloco repetir 3 vezes. Já o while repete enquanto uma condição for verdadeira:\n\n" +
            "    while vida > 0:\n" +
            "        vida = vida - 1";

        c.questions = new List<ChallengeQuestion>
        {
            Choice("Quantas vezes a palavra Oi aparece na tela?",
                "for i in range(4):\n    print(\"Oi\")",
                new[] { "3", "4", "5", "Nenhuma" }, 1,
                "range(4) repete o bloco 4 vezes."),

            Fill("Complete a primeira palavra do laço.",
                "___ i in range(5):",
                new[] { "for" }, false,
                "Tem 3 letras e começa com f.",
                "for i in range(5) repete o bloco 5 vezes."),

            Choice("Qual laço repete enquanto uma condição for verdadeira?", "",
                new[] { "for", "while", "if", "def" }, 1,
                "O while testa a condição a cada volta e só para quando ela fica falsa.")
        };
        return c;
    }

    private static ChallengeContent CSharpBasics()
    {
        ChallengeContent c = new ChallengeContent();
        c.lesson =
            "C# é uma linguagem tipada: ao criar uma variável, dizemos qual é o tipo dela.\n\n" +
            "    int vida = 100;\n" +
            "    float velocidade = 2.5f;\n" +
            "    string nome = \"Rael\";\n" +
            "    bool vivo = true;\n\n" +
            "int = número inteiro | float = número com vírgula (termina com f) | string = texto entre aspas duplas | bool = true ou false.\n\n" +
            "Toda instrução termina com ponto e vírgula (;). Para mostrar algo na tela: Console.WriteLine(nome); (na Unity: Debug.Log(nome);).";

        c.questions = new List<ChallengeQuestion>
        {
            Choice("Qual tipo guarda o número inteiro 100?", "",
                new[] { "int", "string", "bool", "float" }, 0,
                "int guarda números inteiros, como 100."),

            Fill("Complete com o tipo certo para guardar um texto.",
                "___ nome = \"Rael\";",
                new[] { "string" }, false,
                "É o tipo usado para texto. Escreva em minúsculas.",
                "string guarda texto, sempre entre aspas duplas."),

            Choice("Qual linha declara corretamente uma variável float?", "",
                new[] { "float v = 2.5f;", "float v = 2.5;", "Float v = 2.5f;", "float v = \"2.5f\";" }, 0,
                "Em C#, um float precisa do f no final (2.5f); sem ele o número é double. E o tipo se escreve em minúsculas."),

            Fill("O que falta no fim da linha?",
                "int pontos = 10___",
                new[] { ";" }, false,
                "Toda instrução em C# termina com um símbolo especial.",
                "O ponto e vírgula (;) encerra a instrução."),

            Fill("Qual valor aparece na tela?",
                "int a = 3;\nint b = 4;\nConsole.WriteLine(a + b);",
                new[] { "7" }, false,
                "Some os valores guardados em a e b.",
                "a + b = 3 + 4 = 7."),

            Choice("O que o código abaixo mostra na tela?",
                "int vida = 0;\nif (vida > 0)\n{\n    Console.WriteLine(\"Vivo\");\n}\nelse\n{\n    Console.WriteLine(\"Game Over\");\n}",
                new[] { "Vivo", "Game Over", "Nada", "Dá erro" }, 1,
                "vida vale 0, então vida > 0 é falso e o bloco do else é executado. Em C#, a condição do if fica entre parênteses.")
        };
        return c;
    }

    private static ChallengeContent BubbleSortChallenge()
    {
        ChallengeContent c = new ChallengeContent();
        c.lesson =
            "Ordenar é colocar os valores numa ordem, por exemplo do menor para o maior.\n\n" +
            "O Bubble Sort compara dois vizinhos de cada vez e troca os que estão fora de ordem. " +
            "A cada volta completa, o maior valor \"borbulha\" até o final do vetor.\n\n" +
            "Na próxima tela você vai ver o algoritmo funcionando, com o código em C# ao lado.";

        c.questions = new List<ChallengeQuestion>
        {
            Choice("No Bubble Sort, o que acontece quando dois vizinhos estão fora de ordem?", "",
                new[] { "Eles trocam de lugar", "O menor é apagado", "O vetor inteiro é invertido", "Nada, o algoritmo continua" }, 0,
                "É a troca de vizinhos que vai empurrando os maiores valores para o final."),

            Fill("O vetor é { 5, 3, 8 }. Como ele fica depois de trocar só os dois primeiros? Digite separado por vírgulas.",
                "int[] v = { 5, 3, 8 };",
                new[] { "3,5,8" }, false,
                "Troque o 5 de lugar com o 3 e deixe o 8 onde está.",
                "Depois da troca, o vetor fica { 3, 5, 8 }."),

            Choice("Depois da primeira volta completa em { 5, 3, 8, 1 }, qual valor já está na posição final (a última)?", "",
                new[] { "8", "1", "5", "3" }, 0,
                "O maior valor borbulha até o fim: 5,3,8,1 vira 3,5,1,8."),

            Fill("Complete a condição que troca vizinhos fora de ordem (ordem crescente).",
                "if (v[j] ___ v[j + 1])\n    Trocar(v, j, j + 1);",
                new[] { ">" }, false,
                "Troca quando o da esquerda é MAIOR que o da direita.",
                "Se v[j] > v[j + 1], o da esquerda é maior e precisa ir para a direita."),

            Choice("Se o vetor já está ordenado, quantas trocas o Bubble Sort faz?", "",
                new[] { "0", "1", "n", "n vezes n" }, 0,
                "Nenhum par está fora de ordem, então nenhuma troca acontece (ele só compara).")
        };
        return c;
    }

    private static ChallengeContent SelectionSortChallenge()
    {
        ChallengeContent c = new ChallengeContent();
        c.lesson =
            "O Selection Sort (ordenação por seleção) procura o MENOR valor da parte que ainda não está ordenada " +
            "e o coloca na primeira posição dessa parte. Depois repete para a posição seguinte.\n\n" +
            "Na próxima tela, repare na barra roxa: ela marca o menor valor encontrado até agora.";

        c.questions = new List<ChallengeQuestion>
        {
            Choice("O que o Selection Sort faz a cada volta?", "",
                new[] { "Acha o menor valor da parte não ordenada e o coloca no lugar certo", "Troca sempre os dois primeiros valores", "Embaralha o vetor", "Remove o maior valor" }, 0,
                "Cada volta fixa mais um valor na posição definitiva."),

            Fill("Em { 4, 2, 7 }, qual é o menor valor que ele encontra na primeira volta?",
                "int[] v = { 4, 2, 7 };",
                new[] { "2" }, false,
                "Olhe todos os valores e ache o menor.",
                "O menor valor do vetor é 2."),

            Choice("Depois da primeira volta em { 4, 2, 7, 1 }, como fica o vetor?", "",
                new[] { "1, 2, 7, 4", "2, 4, 7, 1", "1, 4, 7, 2", "4, 2, 1, 7" }, 0,
                "O menor valor (1) troca de lugar com o primeiro (4): { 1, 2, 7, 4 }.")
        };
        return c;
    }

    private static ChallengeContent InsertionSortChallenge()
    {
        ChallengeContent c = new ChallengeContent();
        c.lesson =
            "O Insertion Sort funciona como ordenar cartas na mão: você pega uma carta de cada vez e a desliza " +
            "para trás até achar o lugar certo.\n\n" +
            "A parte da esquerda do vetor fica sempre ordenada (em verde) e cresce a cada passo.";

        c.questions = new List<ChallengeQuestion>
        {
            Choice("Qual parte do vetor o Insertion Sort mantém sempre ordenada?", "",
                new[] { "A parte da esquerda", "A parte da direita", "Só o meio", "Nenhuma" }, 0,
                "Ele vai aumentando a parte ordenada da esquerda, uma posição por vez."),

            Fill("Complete a troca com o vizinho da esquerda.",
                "while (j > 0 && v[j-1] > v[j])\n{\n    Trocar(v, j, ___);\n    j--;\n}",
                new[] { "j-1" }, false,
                "O vizinho da esquerda está uma posição antes de j.",
                "Trocar(v, j, j-1) troca o valor com o vizinho da esquerda."),

            Choice("Em { 3, 5, 4 }, ao inserir o 4, com quem ele troca de lugar?", "",
                new[] { "Com o 5", "Com o 3", "Com ninguém", "Com os dois ao mesmo tempo" }, 0,
                "O 5 é maior que o 4, então trocam. Depois o 3 é menor que o 4 e ele para.")
        };
        return c;
    }
}
