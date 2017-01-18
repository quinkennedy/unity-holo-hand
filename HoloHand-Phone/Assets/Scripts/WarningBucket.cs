using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class WarningBucket {

    private Dictionary<object, string> warnings;
    private Text warningText;
    private Image warningImage;

    public WarningBucket(Text warningText, Image warningImage)
    {
        warnings = new Dictionary<object, string>();
        this.warningText = warningText;
        this.warningImage = warningImage;
    }

    private void displayWarnings()
    {
        if (warningText != null) {
            string displayText = string.Empty;
            Dictionary<object, string>.ValueCollection values = warnings.Values;
            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(displayText)){
                    displayText += System.Environment.NewLine;
                }
                displayText += value;
            }
            warningText.text = displayText;
        }
    }

    public void addWarning(object key, string warning)
    {
        if (warnings.Count == 0)
        {
            warningImage.enabled = true;
        }
        warnings.Add(key, warning);
        displayWarnings();
    }

    public bool hasWarning(object key)
    {
        return warnings.ContainsKey(key);
    }

    public void removeWarning(object key)
    {
        warnings.Remove(key);
        if (warnings.Count == 0)
        {
            warningImage.enabled = false;
        }
        displayWarnings();
    }
}
