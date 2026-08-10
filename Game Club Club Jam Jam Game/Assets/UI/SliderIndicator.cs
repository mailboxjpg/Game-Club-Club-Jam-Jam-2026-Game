using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SliderIndicator : MonoBehaviour
{
    [SerializeField] private List<Slider> sliders = new List<Slider>();
    [SerializeField] private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
    [SerializeField] private TextType textType = TextType.Raw;
    [SerializeField] private string textFormat = "F2";
    [SerializeField] private Gradient progressGradient;
    [SerializeField] private bool disableAtZero;
    [SerializeField] private bool overrideTextColor;

    private enum TextType
    {
        Raw,
        Percent,
        Decimal,
        Division
    }
    private List<Image> _fillImages = new List<Image>();
    private bool _disabled;

    private void Awake()
    {
        GetFillImages();
    }

    private void GetFillImages()
    {
        _fillImages.Clear();
        foreach (Slider slider in sliders)
        {
            _fillImages.Add(slider.fillRect.GetComponent<Image>());
        }
    }

    public void UpdateUI(float numerator, float denominator)
    {
        if (sliders.Count != _fillImages.Count)
        {
            GetFillImages();
        }
        if (disableAtZero && numerator <= 0f)
        {
            foreach(Slider slider in sliders)
            {
                slider.gameObject.SetActive(false);
            }
            _disabled = true;
            return;
        }
        else if (_disabled)
        {
            foreach(Slider slider in sliders)
            {
                slider.gameObject.SetActive(true);
            }
            _disabled = false;
        }
        float percent = numerator / denominator;
        Color color = progressGradient.Evaluate(percent);
        if (sliders.Count > 0)
        {
            for (int i = 0; i < sliders.Count; i++)
            {
                sliders[i].value = percent;
                if (color != Color.clear)
                {
                    _fillImages[i].color = color;
                }
            }
        }
        if (texts.Count > 0)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                switch(textType)
                {
                    case TextType.Raw:
                        texts[i].text = numerator.ToString(textFormat);
                        break;
                    case TextType.Percent:
                        texts[i].text = (percent * 100f).ToString(textFormat) + "%";
                        break;
                    case TextType.Decimal:
                        texts[i].text = percent.ToString(textFormat);
                        break;
                    case TextType.Division:
                        texts[i].text = numerator.ToString(textFormat) + " / " + denominator.ToString(textFormat);
                        break;
                }
                if (color != Color.clear && overrideTextColor)
                {
                    texts[i].color = color;
                }
            }
        }
    }

    public void AddSlider(Slider slider)
    {
        sliders.Add(slider);
        _fillImages.Add(slider.fillRect.GetComponent<Image>());
    }

    public void AddText(TextMeshProUGUI text)
    {
        texts.Add(text);
    }
}
