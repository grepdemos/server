﻿#nullable enable

using Bit.Core.AdminConsole.Entities;
using Bit.Core.AdminConsole.Enums;
using Bit.Core.Auth.Enums;
using Bit.Core.Auth.Repositories;

namespace Bit.Core.AdminConsole.OrganizationFeatures.Policies.PolicyValidators;

public class RequireSsoPolicyValidator : IPolicyValidator
{
    private readonly ISsoConfigRepository _ssoConfigRepository;

    public RequireSsoPolicyValidator(ISsoConfigRepository ssoConfigRepository)
    {
        _ssoConfigRepository = ssoConfigRepository;
    }

    public PolicyType Type => PolicyType.RequireSso;
    public IEnumerable<PolicyType> RequiredPolicies => [PolicyType.SingleOrg];

    public async Task<string> ValidateAsync(Policy? currentPolicy, Policy modifiedPolicy)
    {
        if (modifiedPolicy is not { Enabled: true })
        {
            return await ValidateDisableAsync(modifiedPolicy);
        }

        return "";
    }

    private async Task<string> ValidateDisableAsync(Policy policy)
    {
        // Do not allow this policy to be disabled if Key Connector or TDE are being used
        var ssoConfig = await _ssoConfigRepository.GetByOrganizationIdAsync(policy.OrganizationId);
        return ssoConfig?.GetData().MemberDecryptionType switch
        {
            MemberDecryptionType.KeyConnector => "Key Connector is enabled.",
            MemberDecryptionType.TrustedDeviceEncryption => "Trusted device encryption is on and requires this policy.",
            _ => ""
        };
    }
}
