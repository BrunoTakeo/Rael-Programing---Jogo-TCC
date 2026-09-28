using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Login_UI : MonoBehaviour
{
    [SerializeField] private UIDocument uIDocument;

    [SerializeField] private GameObject loginUI;

    [SerializeField] private GameObject gameRoot;

    private Button loginButton;
    private Button registerButton;
    private Button googleButton;
    private Button cadastrarButton;         // botão "Cadastrar" da tela de Cadastro
    private Button voltarLoginButton;       // botão "Voltar - Login"

    private TextField userNameField;
    private TextField passwordField;

    private TextField cadastroNomeField;
    private TextField cadastroEmailField;
    private TextField cadastroSenhaField;

    private VisualElement loginPanel;
    private VisualElement cadastroPanel;

    private void OnEnable()
    {
        // Escuta quando o login der certo
        AuthenticationManager.OnLoginSuccess += GoToGame;
    }

    private void OnDisable()
    {
        AuthenticationManager.OnLoginSuccess -= GoToGame;
    }
    private void Awake()
    {
        var root = uIDocument.rootVisualElement;

        loginPanel = root.Q<VisualElement>("Login");
        cadastroPanel = root.Q<VisualElement>("Cadastro");

        loginButton = root.Q<Button>("loginButton");
        registerButton = root.Q<Button>("registerButton");
        userNameField = root.Q<TextField>("nomeTextField");
        passwordField = root.Q<TextField>("passwordTextField");
        googleButton = root.Q<Button>("googleButton");

        cadastroNomeField = root.Q<TextField>("cadastroNome");
        cadastroEmailField = root.Q<TextField>("cadastroEmail");
        cadastroSenhaField = root.Q<TextField>("cadastroSenha");

        // Botões da tela de Cadastro
        cadastrarButton = root.Q<Button>("cadastrarButton");   // botão "Cadastrar"
        voltarLoginButton = root.Q<Button>("backButton"); // botão "Voltar - Login"

        // Eventos
        if (loginButton != null)
            loginButton.clicked += LoginButtonClick;

        if (registerButton != null)
            registerButton.clicked += ShowCadastroPanel;

        if (googleButton != null)
            googleButton.clicked += GoogleButtonClick;

        if (cadastrarButton != null)
            cadastrarButton.clicked += RegisterButtonClick;

        if (voltarLoginButton != null)
            voltarLoginButton.clicked += ShowLoginPanel;
    }

    private void ShowCadastroPanel()
    {
        loginPanel.style.display = DisplayStyle.None;
        cadastroPanel.style.display = DisplayStyle.Flex;
    }

    private void ShowLoginPanel()
    {
        cadastroPanel.style.display = DisplayStyle.None;
        loginPanel.style.display = DisplayStyle.Flex;
    }
    private async void RegisterButtonClick()
    {
        string nome = cadastroNomeField?.value.Trim();
        string email = cadastroEmailField?.value.Trim();
        string senha = cadastroSenhaField?.value;

        if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
        {
            Debug.LogWarning("Preencha todos os campos!");
            return;
        }

        string erro = await AuthenticationManager.Instance.RegisterWithUsernamePasswordAsync(nome, senha);

        if (!string.IsNullOrEmpty(erro))
        {
            Debug.LogError("Erro no cadastro: " + erro);
        }
        // Se der sucesso, o evento OnLoginSuccess já é disparado
        // e o método GoToGame() é chamado automaticamente
    }

    private async void LoginButtonClick()
    {
        var errorText = await AuthenticationManager.Instance.LoginWithUsernamePasswordAsync(
            userNameField.value,
            passwordField.value
        );
        Debug.Log(errorText);
    }
    private async void GoogleButtonClick()
    {
        if (AuthenticationManager.Instance == null)
        {
            Debug.LogError("AuthenticationManager não encontrado na cena!");
            return;
        }

        await AuthenticationManager.Instance.LoginWithGoogleAsync();
    }
    private void GoToGame()
    {

        // Esconde a UI de login
        if (loginUI != null)
            loginUI.SetActive(false);

        // Mostra o jogo
        if (gameRoot != null)
            gameRoot.SetActive(true);

        // === RESET DO PERSONAGEM ===
        PlayerControll player = FindObjectOfType<PlayerControll>();
        if (player != null)
        {
            player.ResetPlayer();
        }

        // Garante que o GameController está no estado correto
        GameController gameController = FindObjectOfType<GameController>();
        if (gameController != null)
        {
            gameController.SetGameState(GameState.FreeRoam);
        }
    }
}
