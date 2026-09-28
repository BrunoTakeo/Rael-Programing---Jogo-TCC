using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance { get; private set; }

    public static event Action OnLoginSuccess;

    private bool isInitialized = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await InitializeServices();
            SetupEvents();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        Debug.Log($"Unity Services State: {UnityServices.State}");
    }
    public async Task InitializeServices()
    {
        if (isInitialized) return;

        try
        {
            await UnityServices.InitializeAsync();
            SetupEvents();
            isInitialized = true;
            Debug.Log("Unity Services inicializado com sucesso!");
        }
        catch (Exception e)
        {
            Debug.LogError("Falha ao inicializar Unity Services: " + e.Message);
            Debug.LogException(e);
        }
    }
    public async Task<string> RegisterWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            if (!isInitialized) await InitializeServices();

            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

            Debug.Log("Cadastro realizado com sucesso!");

            // Dispara o mesmo evento do login → vai pro jogo
            OnLoginSuccess?.Invoke();

            return "";
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return e.Message;
        }
    }
    public async Task<string> LoginWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            OnLoginSuccess?.Invoke();
        }
        catch (AuthenticationException e)
        {
            if (e.ErrorCode == 51)
            {
                return "Acesso Negado, Token Invalido, Tenta o login De Novo";
            }
            Debug.LogException(e);
            return e.Message;
        }
        catch (RequestFailedException e)
        {
            Debug.LogException(e);
            return e.Message;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return e.Message;
        }
        return "";
    }
    public async Task LoginWithGoogleAsync()
    {
        if (!isInitialized)
            await InitializeServices();

        if (!isInitialized)
        {
            Debug.LogError("Unity Services não inicializado.");
            return;
        }

        try
        {
            Debug.Log("Iniciando login com Google...");

            // Se já estiver logado no Player Accounts, vai direto
            if (PlayerAccountService.Instance.IsSignedIn)
            {
                await SignInWithUnityToken();
                return;
            }

            // Escuta o evento de sucesso
            PlayerAccountService.Instance.SignedIn += OnPlayerAccountSignedIn;

            // Abre a janela de login
            await PlayerAccountService.Instance.StartSignInAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao iniciar login com Google:");
            Debug.LogException(e);

            // Remove o evento em caso de erro
            PlayerAccountService.Instance.SignedIn -= OnPlayerAccountSignedIn;
        }
    }

    // Chamado automaticamente quando o login no Player Accounts termina
    private async void OnPlayerAccountSignedIn()
    {
        // Remove o evento para não chamar várias vezes
        PlayerAccountService.Instance.SignedIn -= OnPlayerAccountSignedIn;

        await SignInWithUnityToken();
    }

    private async Task SignInWithUnityToken()
    {
        try
        {
            string accessToken = PlayerAccountService.Instance.AccessToken;

            if (string.IsNullOrEmpty(accessToken))
            {
                Debug.LogError("Access Token ainda está vazio!");
                return;
            }

            Debug.Log("Access Token recebido. Autenticando no Unity Authentication...");

            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);

            Debug.Log("Login com Google realizado com sucesso!");
            Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");

            // Entra no jogo
            OnLoginSuccess?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao autenticar com o token do Player Accounts:");
            Debug.LogException(e);
        }
    }
    private static void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken}");
            Debug.Log($"Player Name: {AuthenticationService.Instance.PlayerName}");
        };
        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log($"Player signed out");

        };
    }
}
