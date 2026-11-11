namespace RamosPartGenerator.Core.Services;

public enum ErpRegistration
{
    New,
    Registered,       // ERP 스냅샷에 있음
    PendingErp,       // Export 후 아직 ERP 스냅샷에 반영 전
    SuspectedMissing  // Export한 지 오래인데 ERP에 없음 → 등록 누락 의심
}

/// <summary>
/// PGM Export 이력과 ERP 스냅샷을 대조한 등록 상태 (2026-11-12, RCRAH086VP-GBGWG 누락 사고 후 도입).
/// Export만 됐다고 등록으로 보지 않고, 스냅샷 시점보다 충분히 전에 Export됐는데 ERP에 없으면 누락 의심으로 본다.
/// </summary>
public static class ErpRegistrationRules
{
    // ponytail: Export 후 ERP 등록까지 걸리는 시간 여유. 등록이 더 늦어지는 일이 잦으면 늘린다.
    public static readonly TimeSpan ReflectGrace = TimeSpan.FromDays(2);

    // ERP 스냅샷이 이보다 오래되면 "이미 등록된 파트를 신규로 내보내는" 반대 방향 실수 위험을 경고한다.
    public static readonly TimeSpan StaleSnapshot = TimeSpan.FromDays(7);

    public static ErpRegistration Classify(bool inErp, bool exported, DateTime? exportedAt, DateTime? snapshotAt)
    {
        if (inErp)
        {
            return ErpRegistration.Registered;
        }

        if (!exported)
        {
            return ErpRegistration.New;
        }

        if (snapshotAt is null)
        {
            return ErpRegistration.PendingErp; // ERP 미업로드 — 대조 불가
        }

        // 시각 없는 기존 이력(1.6.7 이전)은 언제 Export했는지 몰라 누락 의심으로 본다.
        return exportedAt is null || exportedAt.Value < snapshotAt.Value - ReflectGrace
            ? ErpRegistration.SuspectedMissing
            : ErpRegistration.PendingErp;
    }

    /// <summary>Export에서 제외할(이미 등록된 것으로 볼) 상태인지.</summary>
    public static bool TreatAsRegistered(ErpRegistration status) =>
        status is ErpRegistration.Registered or ErpRegistration.PendingErp;

    public static string Label(ErpRegistration status) => status switch
    {
        ErpRegistration.Registered => "등록됨",
        ErpRegistration.PendingErp => "ERP 반영 대기",
        ErpRegistration.SuspectedMissing => "누락 의심",
        _ => "신규"
    };
}
