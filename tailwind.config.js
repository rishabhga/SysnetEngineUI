/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        './Views/**/*.cshtml',
        './Pages/**/*.cshtml',
        './Areas/**/Views/**/*.cshtml',
        './wwwroot/js/**/*.js'
    ],
    theme: {
        extend: {
            colors: {
                'brand': {
                    '50': '#f0f9ff',
                    '100': '#e0f2fe',
                    '200': '#bae6fd',
                    '300': '#7dd3fc',
                    '400': '#38bdf8',
                    '500': '#0ea5e9', // Main brand color
                    '600': '#0284c7',
                    '700': '#0369a1',
                    '800': '#075985',
                    '900': '#0c4a6e',
                },
                'surface': {
                    '50': '#f8fafc',
                    '100': '#f1f5f9',
                    '200': '#e2e8f0',
                    '300': '#cbd5e1',
                    '400': '#94a3b8',
                    '500': '#64748b',
                    '600': '#475569',
                    '700': '#334155',
                    '800': '#1e293b',
                    '900': '#0f172a',
                },
                'accent': {
                    'success': '#10b981',
                    'warning': '#f59e0b',
                    'error': '#ef4444',
                    'info': '#3b82f6',
                },
                'nifi': { // Keep legacy colors for compatibility
                    'bg': '#E3E8EB',
                    'canvas': '#FAFBFC',
                    'toolbar': '#728E9B',
                    'dark': '#004849',
                    'border': '#AABBC3',
                    'border-light': '#D0DADE',
                    'text': '#262626',
                    'text-light': '#728E9B',
                    'hover': '#C7D2D7',
                    'success': '#44A14B',
                    'error': '#BA554A',
                    'warning': '#F9C642',
                    'info': '#3B7BBF',
                }
            },
            boxShadow: {
                'glass': '0 8px 32px 0 rgba(31, 38, 135, 0.37)',
                'premium': '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
            },
            backdropFilter: {
                'none': 'none',
                'blur': 'blur(20px)',
            },
            fontSize: {
                'xxs': '10px',
            }
        },
    },
    plugins: [],
}
