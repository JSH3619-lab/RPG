using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Tests;

public class SpecEditServiceTests
{
    private static (SpecProvider Provider, SpecEditService Edit, string Dir) CreateOnTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ramos-spec-" + Guid.NewGuid().ToString("N"));
        var provider = new SpecProvider(dir); // 디스크에 파일 없음 -> 내장 리소스로 로드
        provider.Load();
        return (provider, new SpecEditService(provider), dir);
    }

    [Fact]
    public void SaveOptions_AddsCode_MaterializesReloadsAndSnapshots()
    {
        var (provider, edit, dir) = CreateOnTempDir();
        try
        {
            var updated = edit.GetOptions("bit").Append("24 - x24").ToArray();
            edit.SaveOptions("bit", updated);

            Assert.True(File.Exists(Path.Combine(dir, "shared.json")));
            Assert.Contains("24 - x24", provider.SharedSpec.CodeOptions["bit"]);
            // 편집 직전 baseline 스냅샷 1개가 남는다.
            Assert.Single(edit.ListBackups());
        }
        finally
        {
            SafeDelete(dir);
        }
    }

    [Fact]
    public void SaveOptions_Prunes_KeepsOnlyTwenty()
    {
        var (_, edit, dir) = CreateOnTempDir();
        try
        {
            for (var i = 0; i < 22; i++)
            {
                var updated = edit.GetOptions("bit").Append($"Z{i} - test{i}").ToArray();
                edit.SaveOptions("bit", updated);
                Thread.Sleep(2); // 타임스탬프(밀리초) 충돌 방지
            }

            Assert.Equal(20, edit.ListBackups().Count);
        }
        finally
        {
            SafeDelete(dir);
        }
    }

    [Fact]
    public void RestoreBackup_RevertsToChosenSnapshot()
    {
        var (provider, edit, dir) = CreateOnTempDir();
        try
        {
            edit.SaveOptions("bit", edit.GetOptions("bit").Append("24 - x24").ToArray());
            var baseline = edit.ListBackups().Single(); // 첫 저장 전 baseline

            edit.RestoreBackup(baseline.Id);

            Assert.DoesNotContain("24 - x24", provider.SharedSpec.CodeOptions["bit"]);
        }
        finally
        {
            SafeDelete(dir);
        }
    }

    [Fact]
    public void SaveOptions_RejectsDuplicateCode()
    {
        var (_, edit, dir) = CreateOnTempDir();
        try
        {
            var withDuplicate = edit.GetOptions("bit").Append("04 - dup").ToArray();
            var ex = Assert.Throws<InvalidOperationException>(() => edit.SaveOptions("bit", withDuplicate));
            Assert.Contains("중복", ex.Message);
        }
        finally
        {
            SafeDelete(dir);
        }
    }

    [Fact]
    public void SaveOptions_RejectsInvalidFormat()
    {
        var (_, edit, dir) = CreateOnTempDir();
        try
        {
            Assert.Throws<InvalidOperationException>(() => edit.SaveOptions("bit", new[] { "24x24" }));
        }
        finally
        {
            SafeDelete(dir);
        }
    }

    private static void SafeDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
        }
    }
}
