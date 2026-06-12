using CRM.Application.Abstractions;

namespace CRM.UnitTests.TestSupport;

/// <summary>
/// A reversible "hasher" used purely in unit tests. Prefixes the password so we can
/// assert that the password reaches the hasher, and reverses verification deterministically.
/// </summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public const string Prefix = "hashed::";
    public int VerifyCallCount { get; private set; }

    public string Hash(string password) => Prefix + password;

    public bool Verify(string password, string hash)
    {
        VerifyCallCount++;
        return hash == Prefix + password;
    }
}
