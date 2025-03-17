using UnityEngine;

public class Graphics : MonoBehaviour
{
    public void SetQuality(int qualityindex)
    {
        QualitySettings.SetQualityLevel(qualityindex);
    }
}
