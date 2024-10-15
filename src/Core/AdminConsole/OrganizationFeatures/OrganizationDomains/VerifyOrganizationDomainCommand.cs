﻿using Bit.Core.AdminConsole.OrganizationFeatures.OrganizationDomains.Interfaces;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Microsoft.Extensions.Logging;

namespace Bit.Core.AdminConsole.OrganizationFeatures.OrganizationDomains;

public class VerifyOrganizationDomainCommand : IVerifyOrganizationDomainCommand
{
    private readonly IOrganizationDomainRepository _organizationDomainRepository;
    private readonly IDnsResolverService _dnsResolverService;
    private readonly IEventService _eventService;
    private readonly ILogger<VerifyOrganizationDomainCommand> _logger;

    public VerifyOrganizationDomainCommand(
        IOrganizationDomainRepository organizationDomainRepository,
        IDnsResolverService dnsResolverService,
        IEventService eventService,
        ILogger<VerifyOrganizationDomainCommand> logger)
    {
        _organizationDomainRepository = organizationDomainRepository;
        _dnsResolverService = dnsResolverService;
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<OrganizationDomain> VerifyOrganizationDomainAsync(OrganizationDomain domain)
    {
        domain.SetLastCheckedDate();

        if (domain.VerifiedDate is not null)
        {
            await _organizationDomainRepository.ReplaceAsync(domain);
            throw new ConflictException("Domain has already been verified.");
        }

        var claimedDomain =
            await _organizationDomainRepository.GetClaimedDomainsByDomainNameAsync(domain.DomainName);

        if (claimedDomain.Any())
        {
            await _organizationDomainRepository.ReplaceAsync(domain);
            throw new ConflictException("The domain is not available to be claimed.");
        }

        try
        {
            if (await _dnsResolverService.ResolveAsync(domain.DomainName, domain.Txt))
            {
                domain.SetVerifiedDate();
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error verifying Organization domain: {domain}. {errorMessage}",
                domain.DomainName, e.Message);
        }

        await _organizationDomainRepository.ReplaceAsync(domain);

        await _eventService.LogOrganizationDomainEventAsync(domain,
            domain.VerifiedDate != null
                ? EventType.OrganizationDomain_Verified
                : EventType.OrganizationDomain_NotVerified);

        return domain;
    }
}
