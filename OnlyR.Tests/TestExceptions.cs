using System;
using System.Threading.Tasks;
using OnlyR.Core.Recorder;
using OnlyR.Exceptions;

namespace OnlyR.Tests;

public sealed class TestExceptions
{
    [Test]
    public async Task NoRecordingsExceptionDefaultConstructor()
    {
        var ex = new NoRecordingsException();
        await Assert.That(ex.Message).IsNotNull();
        await Assert.That(ex.Message).IsNotEmpty();
    }

    [Test]
    public async Task NoRecordingsExceptionWithMessage()
    {
        var ex = new NoRecordingsException("test");
        await Assert.That(ex.Message).IsEqualTo("test");
    }

    [Test]
    public async Task NoRecordingsExceptionWithInnerException()
    {
        var inner = new Exception("inner");
        var ex = new NoRecordingsException("msg", inner);
        await Assert.That(ex.Message).IsEqualTo("msg");
        await Assert.That(ex.InnerException).IsNotNull();
        await Assert.That(ex.InnerException!.Message).IsEqualTo("inner");
    }

    [Test]
    public async Task NoSpaceExceptionDefaultConstructor()
    {
        var ex = new NoSpaceException();
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task NoSpaceExceptionWithDriveLetter()
    {
        var ex = new NoSpaceException('C');
        await Assert.That(ex.Message).IsNotNull();
        await Assert.That(ex.Message).IsNotEmpty();
        await Assert.That(ex.Message).Contains("C");
    }

    [Test]
    public async Task NoSpaceExceptionWithMessage()
    {
        var ex = new NoSpaceException("disk full");
        await Assert.That(ex.Message).IsEqualTo("disk full");
    }

    [Test]
    public async Task NoSpaceExceptionWithInnerException()
    {
        var inner = new Exception("inner");
        var ex = new NoSpaceException("msg", inner);
        await Assert.That(ex.Message).IsEqualTo("msg");
        await Assert.That(ex.InnerException).IsNotNull();
    }

    [Test]
    public async Task NoDevicesExceptionDefaultConstructor()
    {
        var ex = new NoDevicesException();
        await Assert.That(ex.Message).IsNotNull();
        await Assert.That(ex.Message).IsNotEmpty();
    }
}
