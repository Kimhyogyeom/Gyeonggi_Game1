using UnityEngine;
using TMPro;
using System.Collections;

public class BounceTextReveal : MonoBehaviour
{
    [Header("텍스트 설정")]
    [SerializeField] private TextMeshProUGUI _textComponent;

    [Header("애니메이션 설정")]
    [Tooltip("글자 간 등장 간격 (초)")]
    [SerializeField] private float _charDelay = 0.08f;

    [Tooltip("통통 튀는 높이 (픽셀)")]
    [SerializeField] private float _bounceHeight = 20f;

    [Tooltip("바운스 지속 시간")]
    [SerializeField] private float _bounceDuration = 0.3f;

    [Header("재생 옵션")]
    [SerializeField] private bool _playOnEnable = true;

    private string _fullText;

    void OnEnable()
    {
        if (_playOnEnable)
        {
            Play();
        }
    }

    public void Play()
    {
        if (_textComponent == null)
            _textComponent = GetComponent<TextMeshProUGUI>();

        if (_textComponent == null) return;

        _fullText = _textComponent.text;
        StartCoroutine(RevealCoroutine());
    }

    public void Play(string text)
    {
        if (_textComponent == null)
            _textComponent = GetComponent<TextMeshProUGUI>();

        if (_textComponent == null) return;

        _fullText = text;
        _textComponent.text = _fullText;
        StartCoroutine(RevealCoroutine());
    }

    IEnumerator RevealCoroutine()
    {
        _textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = _textComponent.textInfo;
        int totalChars = textInfo.characterCount;

        // 모든 글자 투명하게
        for (int i = 0; i < totalChars; i++)
        {
            SetCharAlpha(i, 0);
        }

        // 한 글자씩 등장
        for (int i = 0; i < totalChars; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
            {
                yield return new WaitForSeconds(_charDelay);
                continue;
            }

            StartCoroutine(BounceChar(i));
            yield return new WaitForSeconds(_charDelay);
        }
    }

    IEnumerator BounceChar(int charIndex)
    {
        float elapsed = 0f;

        // 글자 보이게
        SetCharAlpha(charIndex, 255);

        TMP_TextInfo textInfo = _textComponent.textInfo;
        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        // 원래 위치 저장
        Vector3[] origVerts = textInfo.meshInfo[materialIndex].vertices;
        Vector3[] basePositions = new Vector3[4];
        for (int j = 0; j < 4; j++)
        {
            basePositions[j] = origVerts[vertexIndex + j];
        }

        while (elapsed < _bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _bounceDuration;

            // 바운스 커브: 위로 올라갔다 원래 위치로
            float bounce = Mathf.Sin(t * Mathf.PI) * (1f - t) * _bounceHeight;

            Vector3[] verts = textInfo.meshInfo[materialIndex].vertices;
            for (int j = 0; j < 4; j++)
            {
                verts[vertexIndex + j] = basePositions[j] + Vector3.up * bounce;
            }

            _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            yield return null;
        }

        // 원래 위치로 복원
        Vector3[] finalVerts = textInfo.meshInfo[materialIndex].vertices;
        for (int j = 0; j < 4; j++)
        {
            finalVerts[vertexIndex + j] = basePositions[j];
        }
        _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    void SetCharAlpha(int charIndex, byte alpha)
    {
        TMP_TextInfo textInfo = _textComponent.textInfo;
        if (charIndex >= textInfo.characterCount) return;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
        for (int j = 0; j < 4; j++)
        {
            colors[vertexIndex + j].a = alpha;
        }

        _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
}
