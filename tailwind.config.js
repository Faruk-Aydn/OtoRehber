/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    './OtoRehber/Views/**/*.cshtml',
    './OtoRehber/wwwroot/js/**/*.js',
  ],
  theme: {
    extend: {
      colors: {
        primary: '#2563eb', // Blue-600
        success: '#10b981', // Emerald-500
        danger: '#ef4444',  // Red-500
      },
    },
  },
  plugins: [
    require('@tailwindcss/typography'),
  ],
};
