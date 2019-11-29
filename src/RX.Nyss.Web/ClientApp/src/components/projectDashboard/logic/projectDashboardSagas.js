import { call, put, takeEvery, select } from "redux-saga/effects";
import * as consts from "./projectDashboardConstants";
import * as actions from "./projectDashboardActions";
import * as appActions from "../../app/logic/appActions";
import * as http from "../../../utils/http";
import { entityTypes } from "../../nationalSocieties/logic/nationalSocietiesConstants";
import dayjs from "dayjs";

export const projectDashboardSagas = () => [
  takeEvery(consts.OPEN_PROJECT_DASHBOARD.INVOKE, openProjectDashboard),
  takeEvery(consts.GET_PROJECT_DASHBOARD_DATA.INVOKE, getProjectDashboardData)
];

function* openProjectDashboard({ projectId }) {
  yield put(actions.openDashbaord.request());
  try {
    const project = yield call(openProjectDashboardModule, projectId);
    const filtersData = yield call(http.get, `/api/project/${projectId}/dashboard/filters`);

    const endDate = dayjs(new Date());


    const filters = (yield select(state => state.projectDashboard.filters)) ||
      {
        healthRiskId: null,
        area: null,
        startDate: endDate.add(-7, "day").format('YYYY-MM-DD'),
        endDate: endDate.format('YYYY-MM-DD'),
        groupingType: "Day"
      };

    yield call(getProjectDashboardData, { projectId, filters: filters })

    yield put(actions.openDashbaord.success(project.name, filtersData.value));
  } catch (error) {
    yield put(actions.openDashbaord.failure(error.message));
  }
};

function* getProjectDashboardData({ projectId, filters }) {
  yield put(actions.getDashboardData.request());
  try {
    const response = yield call(http.post, `/api/project/${projectId}/dashboard/data`, filters);
    yield put(actions.getDashboardData.success(
      filters,
      response.value.summary,
      response.value.reportsGroupedByDate,
      response.value.reportsGroupedByFeaturesAndDate,
      response.value.reportsGroupedByFeatures
    ));
  } catch (error) {
    yield put(actions.getDashboardData.failure(error.message));
  }
};

function* openProjectDashboardModule(projectId) {
  const project = yield call(http.getCached, {
    path: `/api/project/${projectId}/basicData`,
    dependencies: [entityTypes.project(projectId)]
  });

  yield put(appActions.openModule.invoke(null, {
    nationalSocietyId: project.value.nationalSociety.id,
    nationalSocietyName: project.value.nationalSociety.name,
    nationalSocietyCountry: project.value.nationalSociety.countryName,
    projectId: project.value.id,
    projectName: project.value.name
  }));

  return project.value;
}