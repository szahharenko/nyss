﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RX.Nyss.Common;
using RX.Nyss.Data;
using RX.Nyss.Data.Concepts;
using RX.Nyss.Data.Models;
using RX.Nyss.Web.Configuration;
using RX.Nyss.Web.Features.Alerts.Dto;
using RX.Nyss.Web.Services;
using RX.Nyss.Web.Utils.DataContract;
using static RX.Nyss.Web.Utils.DataContract.Result;

namespace RX.Nyss.Web.Features.Alerts
{
    public interface IAlertReportService
    {
        Task<Result<AcceptReportResponseDto>> AcceptReport(int alertId, int reportId);
        Task<Result<DismissReportResponseDto>> DismissReport(int alertId, int reportId);
    }

    public class AlertReportService : IAlertReportService
    {
        private readonly IConfig _config;
        private readonly INyssContext _nyssContext;
        private readonly IAlertService _alertService;
        private readonly IQueueService _queueService;

        public AlertReportService(
            IConfig config,
            INyssContext nyssContext,
            IAlertService alertService,
            IQueueService queueService)
        {
            _config = config;
            _nyssContext = nyssContext;
            _alertService = alertService;
            _queueService = queueService;
        }

        public async Task<Result<AcceptReportResponseDto>> AcceptReport(int alertId, int reportId)
        {
            var alertReport = await _nyssContext.AlertReports
                .Include(ar => ar.Alert)
                .Include(ar => ar.Report)
                .Where(ar => ar.AlertId == alertId && ar.ReportId == reportId)
                .SingleAsync();

            if (!GetAlertHasStatusThatAllowsReportCrossChecks(alertReport))
            {
                return Error<AcceptReportResponseDto>(ResultKey.Alert.AcceptReport.WrongAlertStatus);
            }
            
            if (alertReport.Report.Status != ReportStatus.Pending)
            {
                return Error<AcceptReportResponseDto>(ResultKey.Alert.AcceptReport.WrongReportStatus);
            }

            alertReport.Report.Status = ReportStatus.Accepted;
            await _nyssContext.SaveChangesAsync();

            var response = new AcceptReportResponseDto
            {
                AssessmentStatus = await _alertService.GetAlertAssessmentStatus(alertId)
            };

            return Success(response);
        }

        private static bool GetAlertHasStatusThatAllowsReportCrossChecks(AlertReport alertReport) =>
            StatusConstants.AlertStatusesAllowingCrossChecks.Contains(alertReport.Alert.Status);

        public async Task<Result<DismissReportResponseDto>> DismissReport(int alertId, int reportId)
        {
            var alertReport = await _nyssContext.AlertReports
                .Include(ar => ar.Alert)
                .Include(ar => ar.Report)
                .Where(ar => ar.AlertId == alertId && ar.ReportId == reportId)
                .SingleAsync();

            if (!GetAlertHasStatusThatAllowsReportCrossChecks(alertReport))
            {
                return Error<DismissReportResponseDto>(ResultKey.Alert.DismissReport.WrongAlertStatus);
            }

            if (alertReport.Report.Status != ReportStatus.Pending)
            {
                return Error<DismissReportResponseDto>(ResultKey.Alert.DismissReport.WrongReportStatus);
            }

            alertReport.Report.Status = ReportStatus.Rejected;

            await DismissAlertReport(reportId);

            await _nyssContext.SaveChangesAsync();

            var response = new DismissReportResponseDto
            {
                AssessmentStatus = await _alertService.GetAlertAssessmentStatus(alertId)
            };

            return Success(response);
        }

        private Task DismissAlertReport(int reportId)
        {
            var message = new DismissReportMessage
            {
                ReportId = reportId
            };

            return _queueService.Send(_config.ServiceBusQueues.ReportDismissalQueue, message);
        }
    }
}