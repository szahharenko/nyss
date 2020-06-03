import React from "react";
import PropTypes from "prop-types";
import { createFieldComponent } from "./FieldBase";
import TextField from '@material-ui/core/TextField';
import Autocomplete from '@material-ui/lab/Autocomplete';

const filter = (options, params) => {
  if (params.inputValue !== '') {
    const filtered = options.filter(o => o.title.toLowerCase().indexOf(params.inputValue.toLowerCase()) > -1);

    filtered.push({
      inputValue: params.inputValue,
      title: `Add "${params.inputValue}"`,
    });

    return filtered;
  }

  return options;
}

const AutocompleteTextInput = ({ error, name, label, value, options, freeSolo, autoSelect, controlProps, autoWidth, disabled, type }) => {
  return (
    <Autocomplete
      name={name}
      value={value}
      freeSolo={freeSolo}
      autoSelect={autoSelect}
      disabled={disabled}
      options={options}
      filterOptions={(options, params) => filter(options, params)}
      getOptionLabel={(option) => {
        // Value selected with enter, right from the input
        if (typeof option === 'string') {
          return option;
        }
        // Add "xxx" option created dynamically
        if (option.inputValue) {
          return option.inputValue;
        }
        // Regular option
        return option.title;
      }}
      renderOption={option => option.title}
      {...controlProps}
      renderInput={(params) => (
        <TextField
          {...params}
          label={label}
          error={!!error}
          helperText={error}
          fullWidth={autoWidth ? false : true}
          InputLabelProps={{ shrink: true }}
          type={type}
        />
      )}
    />
  );
};

AutocompleteTextInput.propTypes = {
  controlProps: PropTypes.object,
  name: PropTypes.string
};

export const AutocompleteTextInputField = createFieldComponent(AutocompleteTextInput);
export default AutocompleteTextInputField;