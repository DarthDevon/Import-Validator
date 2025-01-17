import React from "react";
import './PageStyle.css';
import FileUploader from './Components/FileUploader';
import RevisedFileUploader from './Components/RevisedFileUploader'; // Ensure this is correct

function App() {
    return (
        <div>
            <h1>CSV Validator</h1>
            <FileUploader />
        
            {/* RevisedFileUploader component temporarily removed */}
            {/* <RevisedFileUploader /> */}
        </div>
    );
}

export default App;

