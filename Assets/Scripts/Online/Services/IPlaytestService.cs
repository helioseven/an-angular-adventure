using System.Threading;
using System.Threading.Tasks;

public class Eligibility
{
    public bool Allowed { get; }
    public string Reason { get; }
    public string RawJson { get; }

    public Eligibility(bool allowed, string reason, string rawJson)
    {
        Allowed = allowed;
        Reason = reason;
        RawJson = rawJson;
    }
}

public interface IPlaytestService
{
    Task<Eligibility> CheckEligibilityAsync(string steamIdOrDevId, CancellationToken ct = default);
}
