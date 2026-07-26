[System.Serializable]
public struct DialogueLine
{
    public string Text;
    public bool   Shake;
    public bool   Wave;
    public bool   Rainbow;
    public bool   Round;
    public bool   Bold;
    // 0 이하 = DialogueEffect Inspector 기본값 사용
    public float  FontSize;
    public float  ShakeIntensity;
    public float  ShakeSpeed;
    public float  WaveAmplitude;
    public float  WaveSpeed;
    public float  WaveFrequency;
    public float  RainbowSpeed;
    public float  RoundRadius;
    public float  RoundSpeed;
    public float  TypeSpeed;     // 초당 출력 글자 수, 0 이하 = 기본값 사용

    public DialogueLine(string text)
    {
        Text          = text;
        Shake         = false;
        Wave          = false;
        Rainbow       = false;
        Round         = false;
        Bold          = false;
        FontSize       = 0f;
        ShakeIntensity = 0f;
        ShakeSpeed     = 0f;
        WaveAmplitude  = 0f;
        WaveSpeed      = 0f;
        WaveFrequency  = 0f;
        RainbowSpeed   = 0f;
        RoundRadius    = 0f;
        RoundSpeed     = 0f;
        TypeSpeed      = 0f;
    }
}
