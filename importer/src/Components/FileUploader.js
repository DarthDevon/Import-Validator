import React, { useState } from 'react';

const FileUploader = () => {
  const [importFile, setImportFile] = useState(null);
  const [libraryFile, setLibraryFile] = useState(null);
  const [message, setMessage] = useState('');
  const [validationWarnings, setValidationWarnings] = useState([]);
  const [compareWarnings, setCompareWarnings] = useState([]);
  const [errors, setErrors] = useState([]);

  const handleImportFileChange = (event) => {
    setImportFile(event.target.files[0]);
    setMessage('');
    setValidationWarnings([]);
    setCompareWarnings([]);
    setErrors([]);
  };

  const handleLibraryFileChange = (event) => {
    setLibraryFile(event.target.files[0]);
    setMessage('');
  };

  const handleValidate = async () => {
    if (!importFile) {
      setMessage('Please select an import file to validate.');
      return;
    }

    const formData = new FormData();
    formData.append('file', importFile);

    try {
      const response = await fetch('http://localhost:5046/api/fileupload/upload', {
        method: 'POST',
        body: formData,
      });
      const data = await response.json();

      if (data.errors?.length > 0 || data.warnings?.length > 0) {
        setErrors(data.errors);
        setValidationWarnings(data.warnings);
        setMessage('Validation completed with issues. Review errors and warnings.');
      } else {
        setMessage('This file has been formatted properly!');
        setValidationWarnings([]);
        setErrors([]);
      }
    } catch (error) {
      console.error('Error validating file:', error);
      setMessage('An error occurred during file validation.');
    }
  };

  const handleCompare = async () => {
    if (!importFile) {
      setMessage('Please upload an import spreadsheet to compare.');
      return;
    }

    if (!libraryFile) {
      setMessage('Please upload a library spreadsheet to compare.');
      return;
    }

    const formData = new FormData();
    formData.append('importFile', importFile);
    formData.append('libraryFile', libraryFile);

    try {
      const response = await fetch('http://localhost:5046/api/fileupload/compare', {
        method: 'POST',
        body: formData,
      });
      const data = await response.json();

      if (data.warnings?.length > 0) {
        setCompareWarnings(data.warnings);
        setMessage('Comparison completed with warnings. Review them below.');
      } else {
        setMessage('No duplicates detected.');
        setCompareWarnings([]);
      }
    } catch (error) {
      console.error('Error comparing files:', error);
      setMessage('An error occurred during file comparison.');
    }
  };

  return (
    <div className="container">
      <div className="column">
        <h2>Upload Import Spreadsheet</h2>
        <input type="file" accept=".csv" onChange={handleImportFileChange} />
        <button onClick={handleValidate} className="button">
          Validate
        </button>
      </div>

      <div className="column">
        <h2>Upload Library Spreadsheet</h2>
        <input type="file" accept=".csv" onChange={handleLibraryFileChange} />
        <button onClick={handleCompare} className="button" disabled={!importFile || !libraryFile}>
          Compare
        </button>
      </div>

      {message && <p className="success-message">{message}</p>}
      {errors.length > 0 && (
        <div className="error-messages">
          <h3>Validation Errors:</h3>
          <ul>
            {errors.map((error, index) => (
              <li key={index}>{error}</li>
            ))}
          </ul>
        </div>
      )}
      {(validationWarnings.length > 0 || compareWarnings.length > 0) && (
        <div className="warning-messages">
          <h3>Warnings:</h3>
          <ul>
            {validationWarnings.map((warning, index) => (
              <li key={`validation-${index}`}>{warning}</li>
            ))}
            {compareWarnings.map((warning, index) => (
              <li key={`compare-${index}`} style={{ color: 'red' }}>
                Item Duplication Warning: {warning}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};

export default FileUploader;
