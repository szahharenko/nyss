﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RX.Nyss.Data.Concepts;
using RX.Nyss.Web.Features.Authentication.Policies;
using RX.Nyss.Web.Features.NationalSociety.User.TechnicalAdvisor.Dto;
using RX.Nyss.Web.Utils;
using RX.Nyss.Web.Utils.DataContract;

namespace RX.Nyss.Web.Features.NationalSociety.User.TechnicalAdvisor
{
    [Route("api/nationalSociety/technicalAdvisor")]
    public class TechnicalAdvisorController
    {
        private readonly ITechnicalAdvisorService _technicalAdvisorService;

        public TechnicalAdvisorController(ITechnicalAdvisorService technicalAdvisorService)
        {
            _technicalAdvisorService = technicalAdvisorService;
        }

        /// <summary>
        /// Register a technical advisor.
        /// </summary>
        /// <param name="nationalSocietyId">The ID of the national society the technical advisor should be registered in</param>
        /// <param name="createTechnicalAdvisorRequestDto">The technical advisor to be created</param>
        /// <returns></returns>
        [HttpPost("/api/nationalSociety{nationalSocietyId:int}/technicalAdvisor/create")]
        [NeedsRole(Role.Administrator, Role.GlobalCoordinator, Role.DataManager, Role.DataManager), NeedsPolicy(Policy.NationalSocietyAccess)]
        public async Task<Result> CreateTechnicalAdvisor(int nationalSocietyId, [FromBody]CreateTechnicalAdvisorRequestDto createTechnicalAdvisorRequestDto) =>
            await _technicalAdvisorService.CreateTechnicalAdvisor(nationalSocietyId, createTechnicalAdvisorRequestDto);

        /// <summary>
        /// Get a technical advisor.
        /// </summary>
        /// <param name="technicalAdvisorId">The ID of the requested technical advisor</param>
        /// <returns></returns>
        [HttpGet("/api/nationalSociety/technicalAdvisor/{technicalAdvisorId:int}/get")]
        [NeedsRole(Role.Administrator, Role.GlobalCoordinator, Role.DataManager, Role.DataManager), NeedsPolicy(Policy.TechnicalAdvisorAccess)]
        public async Task<Result> Get(int technicalAdvisorId) =>
            await _technicalAdvisorService.GetTechnicalAdvisor(technicalAdvisorId);

        /// <summary>
        /// Update a technical advisor.
        /// </summary>
        /// /// <param name="technicalAdvisorId">The ID of the technical advisor to be edited</param>
        /// <param name="editTechnicalAdvisorRequestDto">The data used to update the specified technical advisor</param>
        /// <returns></returns>
        [HttpPost("/api/nationalSociety/technicalAdvisor/{technicalAdvisorId:int}/edit")]
        [NeedsRole(Role.Administrator, Role.GlobalCoordinator, Role.DataManager, Role.DataManager), NeedsPolicy(Policy.TechnicalAdvisorAccess)]
        public async Task<Result> Edit(int technicalAdvisorId, [FromBody]EditTechnicalAdvisorRequestDto editTechnicalAdvisorRequestDto) =>
            await _technicalAdvisorService.UpdateTechnicalAdvisor(technicalAdvisorId, editTechnicalAdvisorRequestDto);


        /// <summary>
        /// Delete a technical advisor.
        /// </summary>
        /// <param name="id">The ID of the technical advisor to be deleted</param>
        /// <returns></returns>
        [HttpGet("/api/nationalSociety/technicalAdvisor/{technicalAdvisorId:int}/delete")]
        [NeedsRole(Role.Administrator, Role.GlobalCoordinator, Role.DataManager, Role.DataManager), NeedsPolicy(Policy.TechnicalAdvisorAccess)]
        public async Task<Result> Delete(int technicalAdvisorId) =>
            await _technicalAdvisorService.DeleteTechnicalAdvisor(technicalAdvisorId);
    }
}

