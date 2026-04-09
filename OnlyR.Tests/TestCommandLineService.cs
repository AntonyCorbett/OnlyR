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

    [Test]
    public async Task ParseNoGpuFlag()
    {
        var svc = new CommandLineService();
        svc.Parse(["app.exe", "--nogpu"]);
        await Assert.That(svc.NoGpu).IsTrue();
    }

    [Test]
    public async Task ParseIdParameter()
    {
        var svc = new CommandLineService();
        svc.Parse(["app.exe", "--id", "myIdentifier"]);
        await Assert.That(svc.OptionsIdentifier).IsEqualTo("myIdentifier");
    }

    [Test]
    public async Task ParseNoSettingsFlag()
    {
        var svc = new CommandLineService();
        svc.Parse(["app.exe", "--nosettings"]);
        await Assert.That(svc.NoSettings).IsTrue();
    }

    [Test]
    public async Task ParseNoFolderFlag()
    {
        var svc = new CommandLineService();
        svc.Parse(["app.exe", "--nofolder"]);
        await Assert.That(svc.NoFolder).IsTrue();
    }

    [Test]
    public async Task ParseNoSaveFlag()
    {
        var svc = new CommandLineService();
        svc.Parse(["app.exe", "--nosave"]);
        await Assert.That(svc.NoSave).IsTrue();
    }

    [Test]
    public async Task ParseMultipleFlags()
    {
        var svc = new CommandLineService();
        svc.Parse(["app.exe", "--nogpu", "--nosettings", "--id", "test123"]);
        await Assert.That(svc.NoGpu).IsTrue();
        await Assert.That(svc.NoSettings).IsTrue();
        await Assert.That(svc.OptionsIdentifier).IsEqualTo("test123");
    }
}
