using RunMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class GMPanel
{
    private TMP_InputField _naturalSeedInput;
    private TMP_InputField _naturalWidthInput;
    private TMP_InputField _naturalHeightInput;
    private TextMeshProUGUI _naturalParameterStatus;

    private void BuildNaturalMapParametersPage(Transform page)
    {
        AddHint(page, "设置下次自然地图生成的种子、宽度与高度。种子留空表示自动生成，0 也是有效种子；不修改当前地图。");
        NaturalMapParameters defaults = NaturalMapRuntime.DebugOverrides ?? new NaturalMapParameters();
        _naturalSeedInput = CreateNaturalParameterInput(page, "种子（留空自动）", "Input_NaturalSeed",
            NaturalMapRuntime.PreviewSeed.HasValue ? NaturalMapRuntime.PreviewSeed.Value.ToString() : "");
        _naturalWidthInput = CreateNaturalParameterInput(page, "地图宽度", "Input_NaturalWidth", defaults.widthTiles.ToString());
        _naturalHeightInput = CreateNaturalParameterInput(page, "地图高度", "Input_NaturalHeight", defaults.heightTiles.ToString());
        AddActionButton(page, "应用到下次生成", ApplyNaturalMapParameters);
        AddActionButton(page, "恢复自动种子与默认尺寸", ClearNaturalMapParameters);
        _naturalParameterStatus = CreateStatusLabel(page);
        _naturalParameterStatus.text = "应用后可从「生成地图」页预览，或正常派遣进局。";
    }

    private TMP_InputField CreateNaturalParameterInput(Transform page, string caption, string name, string value)
    {
        TextMeshProUGUI label = CreateStatusLabel(page);
        label.gameObject.name = name + "_Label";
        label.text = caption;
        label.GetComponent<LayoutElement>().preferredHeight = 28f;
        TMP_InputField input = CreateSeedInput(page);
        input.gameObject.name = name;
        input.SetTextWithoutNotify(value);
        return input;
    }

    private void ApplyNaturalMapParameters()
    {
        uint? seed = null;
        string rawSeed = _naturalSeedInput.text.Trim();
        uint parsedSeed;
        int width, height;
        if (rawSeed.Length > 0)
        {
            if (!uint.TryParse(rawSeed, out parsedSeed))
            {
                _naturalParameterStatus.text = "种子需为 0 ~ 4294967295 的整数，或留空。";
                return;
            }
            seed = parsedSeed;
        }
        if (!int.TryParse(_naturalWidthInput.text, out width) || !int.TryParse(_naturalHeightInput.text, out height)
            || width <= 0 || height <= 0)
        {
            _naturalParameterStatus.text = "宽度、高度需为正整数。";
            return;
        }
        try
        {
            NaturalMapRuntime.ConfigurePreview(seed, width, height);
            if (seed.HasValue)
            {
                _runMapSeed = seed.Value;
                if (_runMapSeedInput != null) _runMapSeedInput.SetTextWithoutNotify(seed.Value.ToString());
            }
            _naturalParameterStatus.text = "下次生成：种子 " + (seed.HasValue ? seed.Value.ToString() : "自动")
                + "，宽度 " + width + "，高度 " + height + "。";
        }
        catch (System.ArgumentException error) { _naturalParameterStatus.text = error.Message; }
    }

    private void ClearNaturalMapParameters()
    {
        NaturalMapRuntime.ClearPreview();
        NaturalMapParameters defaults = new NaturalMapParameters();
        _naturalSeedInput.SetTextWithoutNotify("");
        _naturalWidthInput.SetTextWithoutNotify(defaults.widthTiles.ToString());
        _naturalHeightInput.SetTextWithoutNotify(defaults.heightTiles.ToString());
        _naturalParameterStatus.text = "已恢复自动种子与默认地图尺寸。";
    }
}
