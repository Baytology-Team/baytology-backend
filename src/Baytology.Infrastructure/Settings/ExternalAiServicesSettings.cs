namespace Baytology.Infrastructure.Settings;

public class ExternalAiServicesSettings
{
    public bool ChatbotEnabled { get; set; }
    public string ChatbotBaseUrl { get; set; } = string.Empty;
    public bool RecommendationEnabled { get; set; }
    public string RecommendationBaseUrl { get; set; } = string.Empty;
    public bool VoiceRecognitionEnabled { get; set; }
    public string VoiceRecognitionBaseUrl { get; set; } = string.Empty;
    public bool ImageSearchEnabled { get; set; }
    public string ImageSearchBaseUrl { get; set; } = string.Empty;
    public bool AllowUntrustedCertificates { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
