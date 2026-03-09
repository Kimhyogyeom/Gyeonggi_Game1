using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _hoverSprite;

    private Image _image;

    void Awake()
    {
        _image = GetComponent<Image>();
        if (_normalSprite == null)
            _normalSprite = _image.sprite;
    }

    void OnDisable()
    {
        transform.localScale = Vector3.one;
        if (_normalSprite != null)
            _image.sprite = _normalSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_hoverSprite != null)
            _image.sprite = _hoverSprite;
        transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_normalSprite != null)
            _image.sprite = _normalSprite;
        transform.localScale = Vector3.one;
    }
}
