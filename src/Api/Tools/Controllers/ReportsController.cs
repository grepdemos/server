﻿using Api.Tools.Models.Response;
using Bit.Api.Tools.Models.Response;
using Bit.Core.AdminConsole.Repositories;
using Bit.Core.Auth.UserFeatures.TwoFactorAuth.Interfaces;
using Bit.Core.Context;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Vault.Queries;
using Core.AdminConsole.OrganizationFeatures.OrganizationUsers.Interfaces;
using Core.Tools.Models.Data;
using Core.Tools.ReportFeatures.OrganizationReportMembers.Interfaces;
using Core.Tools.ReportFeatures.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.Tools.Controllers;

[Route("reports")]
[Authorize("Application")]
public class ReportsController : Controller
{
    private readonly IOrganizationUserUserDetailsQuery _organizationUserUserDetailsQuery;
    private readonly IGroupRepository _groupRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICurrentContext _currentContext;
    private readonly IOrganizationCiphersQuery _organizationCiphersQuery;
    private readonly IApplicationCacheService _applicationCacheService;
    private readonly ITwoFactorIsEnabledQuery _twoFactorIsEnabledQuery;
    private readonly IMemberAccessCipherDetailsQuery _memberAccessCipherDetailsQuery;

    public ReportsController(
        IOrganizationUserUserDetailsQuery organizationUserUserDetailsQuery,
        IGroupRepository groupRepository,
        ICollectionRepository collectionRepository,
        ICurrentContext currentContext,
        IOrganizationCiphersQuery organizationCiphersQuery,
        IApplicationCacheService applicationCacheService,
        ITwoFactorIsEnabledQuery twoFactorIsEnabledQuery,
        IMemberAccessCipherDetailsQuery memberAccessCipherDetailsQuery
    )
    {
        _organizationUserUserDetailsQuery = organizationUserUserDetailsQuery;
        _groupRepository = groupRepository;
        _collectionRepository = collectionRepository;
        _currentContext = currentContext;
        _organizationCiphersQuery = organizationCiphersQuery;
        _applicationCacheService = applicationCacheService;
        _twoFactorIsEnabledQuery = twoFactorIsEnabledQuery;
        _memberAccessCipherDetailsQuery = memberAccessCipherDetailsQuery;
    }

    /// <summary>
    /// Organization member information containing a list of cipher ids
    /// assigned
    /// </summary>
    /// <param name="orgId">Organzation Id</param>
    /// <returns>IEnumerable of MemberCipherDetailsResponseModel</returns>
    /// <exception cref="NotFoundException">If Access reports permission is not assigned</exception>
    [HttpGet("member-cipher-details/{orgId}")]
    public async Task<IEnumerable<MemberCipherDetailsResponseModel>> GetMemberCipherDetails(Guid orgId)
    {
        // Using the AccessReports permission here until new permissions  
        // are needed for more control over reports
        if (!await _currentContext.AccessReports(orgId))
        {
            throw new NotFoundException();
        }

        var memberCipherDetails = await GetMemberCipherDetails(new MemberAccessCipherDetailsRequest { OrganizationId = orgId });

        var responses = memberCipherDetails.Select(x => new MemberCipherDetailsResponseModel(x));

        return responses;
    }

    /// <summary>
    /// Access details for an organization member. Includes the member information,
    /// group collection assignment, and item counts
    /// </summary>
    /// <param name="orgId">Organization Id</param>
    /// <returns>IEnumerable of MemberAccessReportResponseModel</returns>
    /// <exception cref="NotFoundException">If Access reports permission is not assigned</exception>
    [HttpGet("member-access/{orgId}")]
    public async Task<IEnumerable<MemberAccessReportResponseModel>> GetMemberAccessReport(Guid orgId)
    {
        if (!await _currentContext.AccessReports(orgId))
        {
            throw new NotFoundException();
        }

        var memberCipherDetails = await GetMemberCipherDetails(new MemberAccessCipherDetailsRequest { OrganizationId = orgId });

        var responses = memberCipherDetails.Select(x => new MemberAccessReportResponseModel(x));

        return responses;
    }

    /// <summary>
    /// Contains the organization member info, the cipher ids associated with the member, 
    /// and details on their collections, groups, and permissions
    /// </summary>
    /// <param name="request">Request to the MemberAccessCipherDetailsQuery</param>
    /// <returns>IEnumerable of MemberAccessCipherDetails</returns>
    private async Task<IEnumerable<MemberAccessCipherDetails>> GetMemberCipherDetails(MemberAccessCipherDetailsRequest request)
    {
        var memberCipherDetails =
            await _memberAccessCipherDetailsQuery.GetMemberAccessCipherDetails(request);
        return memberCipherDetails;
    }
}
