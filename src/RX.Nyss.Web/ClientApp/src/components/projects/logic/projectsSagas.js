import { call, put, takeEvery, select } from "redux-saga/effects";
import * as consts from "./projectsConstants";
import * as actions from "./projectsActions";
import * as appActions from "../../app/logic/appActions";
import * as http from "../../../utils/http";
import { entityTypes } from "../../nationalSocieties/logic/nationalSocietiesConstants";
import { stringKeys } from "../../../strings";

export const projectsSagas = () => [
  takeEvery(consts.OPEN_PROJECTS_LIST.INVOKE, openProjectsList),
  takeEvery(consts.OPEN_PROJECT_CREATION.INVOKE, openProjectCreation),
  takeEvery(consts.OPEN_PROJECT_EDITION.INVOKE, openProjectEdition),
  takeEvery(consts.OPEN_PROJECT_OVERVIEW.INVOKE, openProjectOverview),
  takeEvery(consts.CREATE_PROJECT.INVOKE, createProject),
  takeEvery(consts.EDIT_PROJECT.INVOKE, editProject),
  takeEvery(consts.CLOSE_PROJECT.INVOKE, closeProject)
];

function* openProjectsList({ nationalSocietyId }) {
  yield put(actions.openList.request());
  try {
    yield openProjectsModule(nationalSocietyId);

    if (yield select(state => state.projects.listStale)) {
      yield call(getProjects, nationalSocietyId);
    }

    yield put(actions.openList.success());
  } catch (error) {
    yield put(actions.openList.failure(error.message));
  }
};

function* openProjectCreation({ nationalSocietyId }) {
  yield put(actions.openCreation.request());
  try {
    const formData = yield call(http.get, `/api/project/getFormData?nationalSocietyId=${nationalSocietyId}`);
    yield openProjectsModule(nationalSocietyId);
    yield put(actions.openCreation.success(formData.value.healthRisks, formData.value.timeZones));
  } catch (error) {
    yield put(actions.openCreation.failure(error.message));
  }
};

function* openProjectEdition({ nationalSocietyId, projectId }) {
  yield put(actions.openEdition.request());
  try {
    const response = yield call(http.get, `/api/project/${projectId}/get`);
    yield call(openProjectDashboardModule, projectId);
    yield put(actions.openEdition.success(response.value, response.value.formData.healthRisks, response.value.formData.timeZones));
  } catch (error) {
    yield put(actions.openEdition.failure(error.message));
  }
};

function* openProjectOverview({ projectId }) {
  yield put(actions.openOverview.request());
  try {
    const response = yield call(http.get, `/api/project/${projectId}/get`);
    yield call(openProjectDashboardModule, projectId);
    yield put(actions.openOverview.success(response.value, response.value.formData.healthRisks, response.value.formData.timeZones));
  } catch (error) {
    yield put(actions.openOverview.failure(error.message));
  }
};

function* createProject({ nationalSocietyId, data }) {
  yield put(actions.create.request());
  try {
    const response = yield call(http.post, `/api/project/add?nationalSocietyId=${nationalSocietyId}`, data);
    yield put(actions.create.success(response.value));
    yield put(actions.goToList(nationalSocietyId));
    yield put(appActions.showMessage(stringKeys.project.create.success));
  } catch (error) {
    yield put(actions.create.failure(error.message));
  }
};

function* editProject({ nationalSocietyId, projectId, data }) {
  yield put(actions.edit.request());
  try {
    const response = yield call(http.post, `/api/project/${projectId}/edit`, data);
    yield put(actions.edit.success(response.value));
    yield put(appActions.entityUpdated(entityTypes.project(projectId)));
    yield put(actions.goToOverview(nationalSocietyId, projectId));
  } catch (error) {
    yield put(actions.edit.failure(error.message));
  }
};

function* closeProject({ nationalSocietyId, projectId }) {
  yield put(actions.close.request(projectId));
  try {
    yield call(http.post, `/api/project/${projectId}/close?nationalSocietyId=${nationalSocietyId}`);
    yield put(actions.close.success(projectId));
    yield call(getProjects, nationalSocietyId);
    yield put(appActions.entityUpdated(entityTypes.project(projectId)));
  } catch (error) {
    yield put(actions.close.failure(projectId, error.message));
  }
};

function* getProjects(nationalSocietyId) {
  yield put(actions.getList.request());
  try {
    const response = yield call(http.get, `/api/project/list?nationalSocietyId=${nationalSocietyId}`);
    yield put(actions.getList.success(response.value));
  } catch (error) {
    yield put(actions.getList.failure(error.message));
  }
};

function* openProjectsModule(nationalSocietyId) {
  const nationalSociety = yield call(http.getCached, {
    path: `/api/nationalSociety/${nationalSocietyId}/get`,
    dependencies: [entityTypes.nationalSociety(nationalSocietyId)]
  });

  yield put(appActions.openModule.invoke(null, {
    nationalSocietyId: nationalSociety.value.id,
    nationalSocietyName: nationalSociety.value.name,
    nationalSocietyCountry: nationalSociety.value.countryName,
    nationalSocietyHeadManagerId: nationalSociety.value.headManagerId,
    nationalSocietyIsArchived: nationalSociety.value.isArchived
  }));
}

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
    projectName: project.value.name,
    projectIsClosed: project.value.isClosed
  }));

  return project.value;
}
