import React, { useState } from "react";

const RevisedFileUploader = () => {
    const [validationErrors, setValidationErrors] = useState([]);
    const [validationWarnings, setValidationWarnings] = useState([]);
    const [compareWarnings, setCompareWarnings] = useState([]);

    const handleValidate = async (event) => {
        event.preventDefault();
        console.log("Validate button clicked");
    
        try {
            const response = await fetch('http://localhost:5046/api/revisedfile/validate', {
                method: 'POST',
                body: new FormData(document.getElementById('revisedFileUploadForm')),
            });
    
            if (!response.ok) {
                console.error("API call failed:", response.statusText);
                return;
            }
    
            const data = await response.json();
            console.log("Validation results:", data);
    
            const fileInput = document.querySelector('input[name="revisedFile"]');
            const fileName = fileInput?.files?.[0]?.name || "the uploaded spreadsheet";
    
            if (data.errors.length === 0 && data.warnings.length === 0) {
                setValidationWarnings([`No errors or warnings found for ${fileName}.`]);
            } else {
                setValidationErrors(data.errors || []);
                setValidationWarnings(data.warnings || []);
            }
        } catch (error) {
            console.error("Error during validation:", error);
        }
    };
    
    

    const handleCompare = async () => {
        // Call backend API to compare the revised export file with the library
        const response = await fetch('http://localhost:5046/api/revisedfile/compare', {
            method: 'POST',
            body: new FormData(document.getElementById('revisedFileUploadForm')),
        });
        const data = await response.json();
        setCompareWarnings(data.compareWarnings);
    };

    return (
        <div>
            <h3>Upload Revised Export Spreadsheet</h3>
            <form id="revisedFileUploadForm">
                <input type="file" name="revisedFile" />
                <button type="button" onClick={handleValidate}>Validate</button>
                <button type="button" onClick={handleCompare}>Compare</button>
            </form>
            <div>
                {validationErrors.length > 0 && (
                    <div>
                        <h3>Validation Errors:</h3>
                        <ul>
                            {validationErrors.map((error, index) => (
                                <li key={index}>{error}</li>
                            ))}
                        </ul>
                    </div>
                )}
                {validationWarnings.length > 0 && (
                    <div>
                        <h3>Warnings:</h3>
                        <ul>
                            {validationWarnings.map((warning, index) => (
                                <li key={index}>{warning}</li>
                            ))}
                        </ul>
                    </div>
                )}
                {compareWarnings.length > 0 && (
                    <div>
                        <h3>Comparison Warnings:</h3>
                        <ul>
                            {compareWarnings.map((warning, index) => (
                                <li key={index} style={{ color: "red" }}>
                                    {warning}
                                </li>
                            ))}
                        </ul>
                    </div>
                )}
            </div>
        </div>
    );
};

export default RevisedFileUploader;
