import React, { Fragment } from 'react';
import PropTypes from "prop-types";
import { connect } from "react-redux";
import { useLayout } from '../../utils/layout';
import * as nationalSocietiesActions from './logic/nationalSocietiesActions';
import Layout from '../layout/Layout';
import Typography from '@material-ui/core/Typography';
import { Loading } from '../common/loading/Loading';
import Button from '@material-ui/core/Button';
import Form from '../forms/form/Form';
import FormActions from '../forms/formActions/FormActions';
import ReadOnlyField from '../forms/ReadOnlyField';
import { useMount } from '../../utils/lifecycle';
import Grid from '@material-ui/core/Grid';
import { strings, stringKeys } from '../../strings';

const NationalSocietiesOverviewPageComponent = (props) => {
  useMount(() => {
    props.openOverview(props.match);
  });

  if (props.isFetching || !props.data) {
    return <Loading />;
  }

  return (
    <Fragment>
      <Form>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <ReadOnlyField
              label={strings(stringKeys.nationalSociety.form.name)}
              value={props.data.name}
            />
          </Grid>

          <Grid item xs={12}>
            <ReadOnlyField
              label={strings(stringKeys.nationalSociety.form.country)}
              value={props.data.countryName}
            />
          </Grid>

          <Grid item xs={12}>
            <ReadOnlyField
              label={strings(stringKeys.nationalSociety.form.contentLanguage)}
              value={props.data.contentLanguageName}
            />
          </Grid>
        </Grid>

        <FormActions>
          <Button variant="outlined" color="primary" onClick={() => props.openEdition(props.data.id)}>
            {strings(stringKeys.nationalSociety.edit)}
          </Button>
        </FormActions>
      </Form>

    </Fragment >
  );
}

NationalSocietiesOverviewPageComponent.propTypes = {
  getNationalSocieties: PropTypes.func,
  list: PropTypes.array
};

const mapStateToProps = state => ({
  isFetching: state.nationalSocieties.overviewFetching,
  data: state.nationalSocieties.overviewData
});

const mapDispatchToProps = {
  openOverview: nationalSocietiesActions.openOverview.invoke,
  openEdition: nationalSocietiesActions.goToEdition
};

export const NationalSocietiesOverviewPage = useLayout(
  Layout,
  connect(mapStateToProps, mapDispatchToProps)(NationalSocietiesOverviewPageComponent)
);
