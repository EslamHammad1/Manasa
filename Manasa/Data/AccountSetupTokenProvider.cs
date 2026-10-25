using Manasa.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Manasa.Data;

public sealed class AccountSetupTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<AccountSetupTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
    : DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider, options, logger);

public sealed class AccountSetupTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public const string ProviderName = nameof(AccountSetupTokenProvider);
    public const string Purpose = "AccountSetup";

    public AccountSetupTokenProviderOptions()
    {
        Name = ProviderName;
        TokenLifespan = TimeSpan.FromHours(1);
    }
}
