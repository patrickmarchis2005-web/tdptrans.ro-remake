namespace TdpTrans.Services
{
    public interface IAiSuspiciousActivityDetector
    {
        Task<AiSuspiciousActivityAssessment?> Assess(int userId);
        void InvalidateModel();
    }

    public sealed class AiSuspiciousActivityAssessment
    {
        public bool ShouldFlag { get; init; }
        public double Probability { get; init; }
        public int RiskScore { get; init; }
        public string Details { get; init; } = string.Empty;
    }
}
