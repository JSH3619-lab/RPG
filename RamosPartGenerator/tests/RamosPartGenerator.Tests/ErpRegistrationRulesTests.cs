using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Tests;

public class ErpRegistrationRulesTests
{
    private static readonly DateTime Snapshot = new(2026, 11, 10, 9, 0, 0);

    [Theory]
    // ERP에 있으면 Export 이력과 상관없이 등록됨
    [InlineData(true, true, -10, ErpRegistration.Registered)]
    [InlineData(false, false, null, ErpRegistration.New)]
    // 스냅샷 이후·직전(여유 2일 안) Export → 반영 대기 (Export에서 제외)
    [InlineData(false, true, 1, ErpRegistration.PendingErp)]
    [InlineData(false, true, -1, ErpRegistration.PendingErp)]
    // 스냅샷보다 2일 넘게 전에 Export했는데 ERP에 없음 → 누락 의심 (RCRAH086VP-GBGWG 사례)
    [InlineData(false, true, -3, ErpRegistration.SuspectedMissing)]
    // Export 시각 없는 기존 이력도 누락 의심
    [InlineData(false, true, null, ErpRegistration.SuspectedMissing)]
    public void Classify(bool inErp, bool exported, int? exportedDaysFromSnapshot, ErpRegistration expected)
    {
        DateTime? exportedAt = exportedDaysFromSnapshot is null ? null : Snapshot.AddDays(exportedDaysFromSnapshot.Value);

        Assert.Equal(expected, ErpRegistrationRules.Classify(inErp, exported, exportedAt, Snapshot));
    }

    [Fact]
    public void Classify_NoErpSnapshot_ExportedIsPending()
    {
        Assert.Equal(ErpRegistration.PendingErp, ErpRegistrationRules.Classify(false, true, null, null));
    }

    [Theory]
    [InlineData(ErpRegistration.Registered, true)]
    [InlineData(ErpRegistration.PendingErp, true)]
    [InlineData(ErpRegistration.SuspectedMissing, false)]
    [InlineData(ErpRegistration.New, false)]
    public void TreatAsRegistered_OnlyRegisteredAndPendingAreExcluded(ErpRegistration status, bool expected)
    {
        Assert.Equal(expected, ErpRegistrationRules.TreatAsRegistered(status));
    }
}
