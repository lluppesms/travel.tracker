// File download helper for Blazor
async function downloadFileFromStream(fileName, streamReference) {
    const arrayBuffer = await streamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    
    // Check if the File System Access API is supported (Chrome, Edge)
    if ('showSaveFilePicker' in window) {
        try {
            const options = {
                suggestedName: fileName,
                types: [
                    {
                        description: 'Data Files',
                        accept: fileName.endsWith('.json') 
                            ? { 'application/json': ['.json'] }
                            : { 'text/csv': ['.csv'] }
                    }
                ]
            };
            
            const fileHandle = await window.showSaveFilePicker(options);
            const writable = await fileHandle.createWritable();
            await writable.write(blob);
            await writable.close();
            return;
        } catch (err) {
            // User cancelled or browser doesn't support, fall back to regular download
            if (err.name !== 'AbortError') {
                console.log('Save file picker error:', err);
            }
        }
    }
    
    // Fallback for browsers that don't support File System Access API or if user cancelled
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}
