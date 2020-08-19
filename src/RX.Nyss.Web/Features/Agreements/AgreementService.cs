﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RX.Nyss.Common.Services;
using RX.Nyss.Common.Utils.DataContract;
using RX.Nyss.Data;
using RX.Nyss.Data.Concepts;
using RX.Nyss.Data.Models;
using RX.Nyss.Data.Queries;
using RX.Nyss.Web.Features.Agreements.Dto;
using RX.Nyss.Web.Features.NationalSocieties.Dto;
using RX.Nyss.Web.Services.Authorization;
using static RX.Nyss.Common.Utils.DataContract.Result;

namespace RX.Nyss.Web.Features.Agreements
{
    public interface IAgreementService
    {
        Task<Result<PendingConsentDto>> GetPendingAgreementDocuments();
        Task<Result> AcceptAgreement(string languageCode);
        Task<Result<AgreementsStatusesDto>> GetPendingAgreements();
    }

    public class AgreementService : IAgreementService
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly INyssContext _nyssContext;
        private readonly IGeneralBlobProvider _generalBlobProvider;
        private readonly IDataBlobService _dataBlobService;

        public AgreementService(IAuthorizationService authorizationService, INyssContext nyssContext, IGeneralBlobProvider generalBlobProvider, IDataBlobService dataBlobService)
        {
            _authorizationService = authorizationService;
            _nyssContext = nyssContext;
            _generalBlobProvider = generalBlobProvider;
            _dataBlobService = dataBlobService;
        }

        public async Task<Result> AcceptAgreement(string languageCode)
        {
            var identityUserName = _authorizationService.GetCurrentUserName();

            var user = await _nyssContext.Users.FilterAvailable()
                .Include(u => u.ApplicationLanguage)
                .SingleOrDefaultAsync(u => u.EmailAddress == identityUserName);

            if (user == null)
            {
                return Error(ResultKey.User.Common.UserNotFound);
            }

            var (pendingSocieties, staleSocieties) = await GetPendingAndStaleNationalSocieties(user);
            var utcNow = DateTime.UtcNow;

            var consentDocumentFileName = Guid.NewGuid() + ".pdf";
            var sourceUri = _generalBlobProvider.GetPlatformAgreementUrl(languageCode);
            await _dataBlobService.StorePlatformAgreement(sourceUri, consentDocumentFileName);

            foreach (var staleSociety in staleSocieties)
            {
                var existingConsent = await _nyssContext.NationalSocietyConsents
                    .Where(consent => consent.NationalSocietyId == staleSociety.Id && consent.UserEmailAddress == user.EmailAddress && !consent.ConsentedUntil.HasValue).SingleAsync();
                existingConsent.ConsentedUntil = utcNow;

                await _nyssContext.NationalSocietyConsents.AddAsync(new NationalSocietyConsent
                {
                    ConsentedFrom = utcNow,
                    NationalSocietyId = staleSociety.Id,
                    UserEmailAddress = user.EmailAddress,
                    UserPhoneNumber = user.PhoneNumber,
                    ConsentDocument = consentDocumentFileName
                });
            }

            foreach (var nationalSociety in pendingSocieties)
            {
                if (user.Role == Role.Manager || user.Role == Role.TechnicalAdvisor)
                {
                    var ns = await _nyssContext.NationalSocieties
                        .Include(society => society.DefaultOrganization)
                        .Where(society => society.Id == nationalSociety.Id).FirstOrDefaultAsync();

                    ns.DefaultOrganization.PendingHeadManager = null;
                    ns.DefaultOrganization.HeadManager = user;
                }

                await _nyssContext.NationalSocietyConsents.AddAsync(new NationalSocietyConsent
                {
                    ConsentedFrom = utcNow,
                    NationalSocietyId = nationalSociety.Id,
                    UserEmailAddress = user.EmailAddress,
                    UserPhoneNumber = user.PhoneNumber,
                    ConsentDocument = consentDocumentFileName
                });
            }

            await _nyssContext.SaveChangesAsync();

            return Success();
        }

        public async Task<Result<AgreementsStatusesDto>> GetPendingAgreements()
        {
            if (!_authorizationService.IsCurrentUserInAnyRole(Role.GlobalCoordinator, Role.Manager, Role.TechnicalAdvisor, Role.Coordinator))
            {
                return Success(new AgreementsStatusesDto());
            }

            var identityUserName = _authorizationService.GetCurrentUserName();

            var userEntity = await _nyssContext.Users.FilterAvailable()
                .Include(x => x.ApplicationLanguage)
                .SingleOrDefaultAsync(u => u.EmailAddress == identityUserName);
            
            var (pending, stale) = await GetPendingAndStaleNationalSocieties(userEntity);
            return Success(new AgreementsStatusesDto
            {
                PendingSocieties = pending.Select(p => p.Name),
                StaleSocieties = stale.Select(p => p.Name)
            });
        }

        public async Task<Result<PendingConsentDto>> GetPendingAgreementDocuments()
        {
            var identityUserName = _authorizationService.GetCurrentUserName();

            var userEntity = await _nyssContext.Users.FilterAvailable()
                .Include(x => x.ApplicationLanguage)
                .SingleOrDefaultAsync(u => u.EmailAddress == identityUserName);

            if (userEntity == null)
            {
                return Error<PendingConsentDto>(ResultKey.User.Common.UserNotFound);
            }

            var (pending, stale) = await GetPendingAndStaleNationalSocieties(userEntity);

            if (!pending.Union(stale).Any())
            {
                return Error<PendingConsentDto>(ResultKey.Consent.NoPendingConsent);
            }

            var applicationLanguages = await _nyssContext.ApplicationLanguages.ToListAsync();
            var docs = applicationLanguages.Select(apl => new AgreementDocument
            {
                Language = apl.DisplayName,
                LanguageCode = apl.LanguageCode,
                AgreementDocumentUrl = _generalBlobProvider.GetPlatformAgreementUrl(apl.LanguageCode.ToLower())
            }).Where(d => d.AgreementDocumentUrl != null);

            var pendingSociety = new PendingConsentDto
            {
                AgreementDocuments = docs,
                PendingSocieties = pending.Select(ns => new PendingNationalSocietyConsentDto
                {
                    NationalSocietyName = ns.Name,
                    NationalSocietyId = ns.Id
                }).ToList(),
                StaleSocieties = stale.Select(ns => new PendingNationalSocietyConsentDto
                {
                    NationalSocietyName = ns.Name,
                    NationalSocietyId = ns.Id
                }).ToList()
            };

            return Success(pendingSociety);
        }

        private async Task<(List<NationalSociety> pendingSocieties, List<NationalSociety> staleSocieties)> GetPendingAndStaleNationalSocieties(User userEntity)
        {
            var applicableNationalSocieties = _nyssContext.NationalSocieties.Where(x =>
                (_authorizationService.IsCurrentUserInRole(Role.Coordinator) && x.NationalSocietyUsers.Any(y => y.UserId == userEntity.Id)) ||
                (_authorizationService.IsCurrentUserInAnyRole(Role.Manager, Role.TechnicalAdvisor) && (x.DefaultOrganization.HeadManager == userEntity || x.DefaultOrganization.PendingHeadManager == userEntity)));

            var activeAgreements = _nyssContext.NationalSocietyConsents.Where(nsc => !nsc.ConsentedUntil.HasValue && nsc.UserEmailAddress == userEntity.EmailAddress);
            var pendingNationalSocieties = await applicableNationalSocieties.Where(ns => activeAgreements.All(aa => aa.NationalSocietyId != ns.Id)).ToListAsync();

            var staleNationalSocieties = new List<NationalSociety>();
            if (activeAgreements.Any())
            {
                var agreementLastUpdatedTimeStamp = await _generalBlobProvider.GetPlatformAgreementLastModifiedDate(userEntity.ApplicationLanguage.LanguageCode);
                var staleAgreements = activeAgreements.Where(nsc => nsc.ConsentedFrom < agreementLastUpdatedTimeStamp);
                staleNationalSocieties.AddRange(await applicableNationalSocieties.Where(ns => staleAgreements.Any(sa => sa.NationalSocietyId == ns.Id)).ToListAsync());
            }

            return (pendingNationalSocieties, staleNationalSocieties);
        }
    }
}
