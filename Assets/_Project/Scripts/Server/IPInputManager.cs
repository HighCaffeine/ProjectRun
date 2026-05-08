using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class IPInputManager : GenericSingleton<IPInputManager>
{
    public TMP_InputField[] ipInputs = new TMP_InputField[4];

    void Start()
    {
        for (int i = 0; i < ipInputs.Length; i++)
        {
            int index = i;
            ipInputs[i].onValueChanged.AddListener((text) => OnInputValueChanged(index, text));
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
        {
            string clipboard = GUIUtility.systemCopyBuffer;

            if (!string.IsNullOrEmpty(clipboard) && clipboard.Contains("."))
            {
                string[] parts = clipboard.Split('.');

                for (int i = 0; i < Mathf.Min(parts.Length, 4); i++)
                {
                    string cleanNum = Regex.Replace(parts[i], "[^0-9]", "");

                    if (cleanNum.Length > 3) cleanNum = cleanNum.Substring(0, 3);

                    ipInputs[i].SetTextWithoutNotify(cleanNum);
                }

                int lastIndex = Mathf.Min(parts.Length - 1, 3);
                ipInputs[lastIndex].Select();
                ipInputs[lastIndex].caretPosition = ipInputs[lastIndex].text.Length;
            }
        }
    }

    void OnInputValueChanged(int index, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (text.EndsWith(".") || text.EndsWith(" ") || text.EndsWith("\t"))
        {
            string cleanText = text.Substring(0, text.Length - 1);
            ipInputs[index].SetTextWithoutNotify(cleanText);

            if (index < 3) ipInputs[index + 1].Select();
            return;
        }

        if (text.Length >= 3 && index < 3)
        {
            if (text.Length > 3)
            {
                ipInputs[index].SetTextWithoutNotify(text.Substring(0, 3));
            }
            ipInputs[index + 1].Select();
        }
    }

    public string GetFullIP()
    {
        string ip1 = string.IsNullOrEmpty(ipInputs[0].text) ? "0" : ipInputs[0].text;
        string ip2 = string.IsNullOrEmpty(ipInputs[1].text) ? "0" : ipInputs[1].text;
        string ip3 = string.IsNullOrEmpty(ipInputs[2].text) ? "0" : ipInputs[2].text;
        string ip4 = string.IsNullOrEmpty(ipInputs[3].text) ? "0" : ipInputs[3].text;

        return $"{ip1}.{ip2}.{ip3}.{ip4}";
    }

    public void SetIP(string fullIp)
    {
        string[] parts = fullIp.Split('.');
        if (parts.Length == 4)
        {
            for (int i = 0; i < 4; i++) ipInputs[i].text = parts[i];
        }
    }
}