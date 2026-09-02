using Microsoft.AspNetCore.DataProtection;

namespace TravelTracker.Tests.Services;

public class AssistantActionEncryptionTests
{
    [Fact]
    public void CanonicalCommand_WhenKeyRingIsReopened_RemainsDecryptable()
    {
        var keyDirectory = new DirectoryInfo(Path.Combine(
            Path.GetTempPath(),
            "TravelTracker.Tests",
            Guid.NewGuid().ToString("N")));
        const string purpose = "TravelTracker.AssistantActions.CanonicalCommand.v1";
        var firstProvider = DataProtectionProvider.Create(
            keyDirectory,
            builder => builder.SetApplicationName("TravelTracker"));
        var ciphertext = firstProvider.CreateProtector(purpose).Protect("""{"locationName":"Buffalo House RV Park"}""");

        var restartedProvider = DataProtectionProvider.Create(
            keyDirectory,
            builder => builder.SetApplicationName("TravelTracker"));
        var plaintext = restartedProvider.CreateProtector(purpose).Unprotect(ciphertext);

        Assert.Equal("""{"locationName":"Buffalo House RV Park"}""", plaintext);
    }
}
