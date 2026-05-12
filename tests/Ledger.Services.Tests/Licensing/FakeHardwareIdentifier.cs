using Ledger.Services.Licensing;

namespace Ledger.Services.Tests.Licensing;

internal sealed class FakeHardwareIdentifier : IHardwareIdentifier
{
    private readonly string _id;
    public FakeHardwareIdentifier(string id) => _id = id;
    public string GetDeviceId() => _id;
}
