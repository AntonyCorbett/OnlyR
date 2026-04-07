using System.Threading.Tasks;
using OnlyR.Services.Options;

namespace OnlyR.Tests;

public sealed class TestCommandLineService
{
    [Test]
    public async Task ConstructorDoesNotThrow()
    {
        var svc = new CommandLineService();
        await Assert.That(svc).IsNotNull();
    }

    [Test]
    public async Task DefaultNoGpuIsFalse()
    {
        var svc = new CommandLineService();
        await Assert.That(svc.NoGpu).IsFalse();
    }

    [Test]
    public async Task DefaultOptionsIdentifierIsNull()
    {
        var svc = new CommandLineService();
        await Assert.That(svc.OptionsIdentifier).IsNull();
    }

    [Test]
    public async Task DefaultNoSettingsIsFalse()
    {
        var svc = new CommandLineService();
        await Assert.That(svc.NoSettings).IsFalse();
    }

    [Test]
    public async Task DefaultNoFolderIsFalse()
    {
        var svc = new CommandLineService();
        await Assert.That(svc.NoFolder).IsFalse();
    }

    [Test]
    public async Task DefaultNoSaveIsFalse()
    {
        var svc = new CommandLineService();
        await Assert.That(svc.NoSave).IsFalse();
    }

    [Test]
    public async Task DefaultNoCopyIsFalse()
    {
        var svc = new CommandLineService();
        await Assert.That(svc.NoCopy).IsFalse();
    }
}
