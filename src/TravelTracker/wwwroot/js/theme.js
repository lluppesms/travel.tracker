// Theme Toggle Functionality
(function() {
    const THEME_KEY = 'travel-tracker-theme';
    
    // Get saved theme or default to light
    function getSavedTheme() {
        return localStorage.getItem(THEME_KEY) || 'light';
    }
    
    // Save theme preference
    function saveTheme(theme) {
        localStorage.setItem(THEME_KEY, theme);
    }
    
    // Apply theme to document
    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
    }
    
    // Toggle between light and dark theme
    function toggleTheme() {
        const currentTheme = getSavedTheme();
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        saveTheme(newTheme);
        applyTheme(newTheme);
        updateToggleButton(newTheme);
    }
    
    // Update toggle button icon
    function updateToggleButton(theme) {
        const button = document.getElementById('theme-toggle');
        if (button) {
            button.innerHTML = theme === 'light' ? '🌙' : '☀️';
            button.setAttribute('aria-label', `Switch to ${theme === 'light' ? 'dark' : 'light'} mode`);
        }
    }
    
    // Initialize theme on page load
    function initTheme() {
        const savedTheme = getSavedTheme();
        applyTheme(savedTheme);
        
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => updateToggleButton(savedTheme));
        } else {
            updateToggleButton(savedTheme);
        }
    }
    
    // Expose functions globally
    window.themeToggle = {
        init: initTheme,
        toggle: toggleTheme,
        getSavedTheme: getSavedTheme
    };
    
    // Auto-initialize
    initTheme();
})();
