using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public sealed class PlaytestService : IPlaytestService
{
    private readonly string _url;
    private readonly string _bearer; // ANON key or empty
    private readonly bool _devMock;
    private readonly string _devToken;

    public PlaytestService(string url, string bearer, bool devMock, string devToken)
    {
        _url = url;
        _bearer = bearer;
        _devMock = devMock;
        _devToken = devToken;
    }

    public async Task<Eligibility> CheckEligibilityAsync(
        string steamIdOrDevId,
        CancellationToken ct = default
    )
    {
        var payload = $"{{\"steamid\":\"{steamIdOrDevId}\"}}";
        using var req = new UnityWebRequest(_url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(_bearer))
            req.SetRequestHeader("Authorization", $"Bearer {_bearer}");

#if UNITY_EDITOR
        if (_devMock)
        {
            req.SetRequestHeader("X-Dev-Mock", "true");
            if (!string.IsNullOrEmpty(_devToken))
                req.SetRequestHeader("X-Dev-Token", _devToken);
        }
#endif

        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested)
                break;
            await Task.Yield();
        }

        if (req.result != UnityWebRequest.Result.Success)
            return new Eligibility(
                false,
                $"HTTP {req.responseCode}: {req.error}",
                req.downloadHandler.text
            );

        var text = req.downloadHandler.text ?? "";
        // Minimal rule: ok if function returned { ok: true, ... }
        bool allowed = text.Contains("\"ok\":true");
        string reason = allowed ? "OK" : "Function reported failure";
        return new Eligibility(allowed, reason, text);
    }
}
