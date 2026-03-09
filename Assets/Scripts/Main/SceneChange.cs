using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChange : MonoBehaviour
{
    public static SceneChange Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image _fadeImage;
    [SerializeField] private float _fadeDuration = 0.5f;

    // _fadeImage가 Inspector에 할당되지 않았을 때 자동 생성
    private void EnsureFadeImage()
    {
        if (_fadeImage != null) return;

        // 자식에서 먼저 찾기
        _fadeImage = GetComponentInChildren<Image>(true);
        if (_fadeImage != null) return;

        // 없으면 자동 생성 (DontDestroyOnLoad 오브젝트 하위에)
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(this.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        _fadeImage = imgObj.AddComponent<Image>();
        _fadeImage.color = new Color(0, 0, 0, 0);
        _fadeImage.raycastTarget = false;

        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Debug.Log("[SceneChange] FadeImage 자동 생성됨. Inspector에 직접 할당하는 것을 권장합니다.");
    }

    [Header("Button")]
    [SerializeField] private Button _goIngameButton;
    [SerializeField] private Button _goIngame2Button;
    [SerializeField] private Button _goMainButton;

    [Header("Home Objects UI")]
    [SerializeField] private GameObject _homeParentObject;
    [SerializeField] private RawImage _homeRawImage;
    private bool _isTransitioning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureFadeImage();

        // 시작 시 투명하게
        if (_fadeImage != null)
        {
            Color c = _fadeImage.color;
            c.a = 0f;
            _fadeImage.color = c;
            _fadeImage.raycastTarget = false;
        }
    }

    void Start()
    {
        BindButton();
        UpdateBackButton(SceneManager.GetActiveScene().name == "Main");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (_isTransitioning) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            string current = SceneManager.GetActiveScene().name;
            if (current != "Main")
            {
                LoadScene("Main");
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindButton();

        bool isMain = scene.name == "Main";
        UpdateBackButton(isMain);

        if (_isTransitioning)
        {
            StartCoroutine(FadeInAfterLoad(isMain));
        }
        else if (_homeParentObject != null)
        {
            _homeParentObject.SetActive(isMain);
            _homeRawImage.enabled = isMain;
        }
    }

    private IEnumerator FadeInAfterLoad(bool showHome)
    {
        yield return null;
        yield return StartCoroutine(Fade(1f, 0f));
        _isTransitioning = false;

        // 메인으로 돌아올 때만 홈 UI 복구
        if (showHome && _homeParentObject != null)
        {
            _homeParentObject.SetActive(true);
            if (_homeRawImage != null) _homeRawImage.enabled = true;
        }
    }

    // Main씬이면 돌아가기 버튼 비활성화, Ingame이면 활성화
    private void UpdateBackButton(bool isMain)
    {
        if (_goMainButton != null)
            _goMainButton.gameObject.SetActive(!isMain);
    }

    private void BindButton()
    {
        if (_goIngameButton == null)
        {
            GameObject btnObj = GameObject.Find("ButtonGoIngame");
            if (btnObj != null)
                _goIngameButton = btnObj.GetComponent<Button>();
        }

        if (_goIngameButton != null)
        {
            _goIngameButton.onClick.RemoveAllListeners();
            _goIngameButton.onClick.AddListener(() => LoadScene("Ingame1"));
        }

        if (_goIngame2Button != null)
        {
            _goIngame2Button.onClick.RemoveAllListeners();
            _goIngame2Button.onClick.AddListener(() => LoadScene("Ingame2"));
        }

        if (_goMainButton != null)
        {
            _goMainButton.onClick.RemoveAllListeners();
            _goMainButton.onClick.AddListener(() => LoadScene("Main"));
        }
    }

    public void LoadScene(string sceneName)
    {
        Debug.Log($"[SceneChange] LoadScene 호출: {sceneName}, isTransitioning={_isTransitioning}");
        if (_isTransitioning) return;
        StartCoroutine(TransitionToScene(sceneName));
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        _isTransitioning = true;

        // 페이드 아웃과 씬 로딩을 동시에 시작
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // 페이드 아웃 (화면 검게)
        yield return StartCoroutine(Fade(0f, 1f));

        // 화면이 완전히 검어진 후 홈 UI 숨김 (인게임으로 갈 때)
        if (sceneName != "Main" && _homeParentObject != null)
        {
            _homeParentObject.SetActive(false);
            if (_homeRawImage != null) _homeRawImage.enabled = false;
        }

        // 씬 로딩 완료 대기 (90% = 준비 완료)
        while (asyncLoad.progress < 0.9f)
            yield return null;

        // 씬 활성화
        asyncLoad.allowSceneActivation = true;
    }

    private IEnumerator Fade(float from, float to)
    {
        Debug.Log($"[SceneChange] Fade({from} → {to}), _fadeImage={_fadeImage}");
        if (_fadeImage == null) yield break;

        _fadeImage.raycastTarget = true;
        float elapsed = 0f;
        Color c = _fadeImage.color;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _fadeDuration);
            c.a = Mathf.Lerp(from, to, t);
            _fadeImage.color = c;
            yield return null;
        }

        c.a = to;
        _fadeImage.color = c;

        if (to == 0f)
            _fadeImage.raycastTarget = false;
    }
}
