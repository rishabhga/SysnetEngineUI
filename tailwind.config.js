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
                'nifi': {
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
            fontSize: {
                'xxs': '10px',
            }
        },
    },
    plugins: [],
}
