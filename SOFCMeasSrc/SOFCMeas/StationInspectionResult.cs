namespace SOFCMeas
{
    internal sealed class StationInspectionResult
    {
        internal bool IsValid { get; set; }
        internal bool IsPass { get; set; }
        internal double PeakLoadKgf { get; set; }
        internal double LowerSpecKgf { get; set; }
        internal double UpperSpecKgf { get; set; }
        internal int InspectionCount { get; set; }
        internal int PassCount { get; set; }
        internal int FailCount { get; set; }
        internal double YieldPercent { get; set; }
        internal string ErrorMessage { get; set; }
    }
}
