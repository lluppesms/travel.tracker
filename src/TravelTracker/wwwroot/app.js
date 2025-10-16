// JavaScript utilities for Travel Tracker

// Download a file from a data URL
window.downloadFile = function(filename, dataUrl) {
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
