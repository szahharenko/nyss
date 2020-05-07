import { appReducer } from "../components/app/logic/appReducer";
import { authReducer } from "../authentication/authReducer";
import { connectRouter } from 'connected-react-router'
import { combineReducers } from "redux";
import { nationalSocietiesReducer } from "../components/nationalSocieties/logic/nationalSocietiesReducer";
import { smsGatewaysReducer } from "../components/smsGateways/logic/smsGatewaysReducer";
import { projectsReducer } from "../components/projects/logic/projectsReducer";
import { requestReducer } from "../components/app/logic/requestReducer";
import { globalCoordinatorsReducer } from "../components/globalCoordinators/logic/globalCoordinatorsReducer";
import { healthRisksReducer } from "../components/healthRisks/logic/healthRisksReducer";
import { nationalSocietyUsersReducer } from "../components/nationalSocietyUsers/logic/nationalSocietyUsersReducer";
import { dataCollectorsReducer } from "../components/dataCollectors/logic/dataCollectorsReducer";
import { nationalSocietyConsentsReducer } from "../components/nationalSocietyConsents/logic/nationalSocietyConsentsReducer";
import { reportsReducer } from "../components/reports/logic/reportsReducer";
import { nationalSocietyReportsReducer } from "../components/nationalSocietyReports/logic/nationalSocietyReportsReducer";
import { nationalSocietyStructureReducer } from "../components/nationalSocietyStructure/logic/nationalSocietyStructureReducer";
import { projectDashboardReducer } from "../components/projectDashboard/logic/projectDashboardReducer";
import { alertsReducer } from "../components/alerts/logic/alertsReducer";
import { nationalSocietyDashboardReducer } from "../components/nationalSocietyDashboard/logic/nationalSocietyDashboardReducer";
import { translationsReducer } from "../components/translations/logic/translationsReducer";
import { organizationsReducer } from "../components/organizations/logic/organizationsReducer";
import { projectOrganizationsReducer } from "../components/projectOrganizations/logic/projectOrganizationsReducer";

export const createRootReducer = (history) => combineReducers({
  router: connectRouter(history),
  appData: appReducer,
  requests: requestReducer,
  auth: authReducer,
  nationalSocieties: nationalSocietiesReducer,
  nationalSocietyStructure: nationalSocietyStructureReducer,
  smsGateways: smsGatewaysReducer,
  organizations: organizationsReducer,
  projects: projectsReducer,
  projectDashboard: projectDashboardReducer,
  projectOrganizations: projectOrganizationsReducer,
  globalCoordinators: globalCoordinatorsReducer,
  healthRisks: healthRisksReducer,
  nationalSocietyUsers: nationalSocietyUsersReducer,
  dataCollectors: dataCollectorsReducer,
  nationalSocietyConsents: nationalSocietyConsentsReducer,
  reports: reportsReducer,
  nationalSocietyReports: nationalSocietyReportsReducer,
  nationalSocietyDashboard: nationalSocietyDashboardReducer,
  alerts: alertsReducer,
  translations: translationsReducer
});
