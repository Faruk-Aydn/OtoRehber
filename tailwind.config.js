/** @type {import('tailwindcss').Config} */
const withVar = (v) => `rgb(var(${v}) / <alpha-value>)`;

module.exports = {
  darkMode: 'class',
  content: [
    './OtoRehber/Views/**/*.cshtml',
    './OtoRehber/wwwroot/js/**/*.js',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      colors: {
        // Marka rengi — kendinden emin, koyu bir mavi
        brand: {
          50: '#eff5ff',
          100: '#dbe8fe',
          200: '#bfd7fe',
          300: '#93bbfd',
          400: '#6096fa',
          500: '#3b76f6',
          600: '#2563eb', // ana ton
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
          950: '#172554',
        },
        // Tema-duyarlı yüzey / metin token'ları (app.src.css'teki CSS değişkenlerinden)
        surface: withVar('--surface'),
        'surface-2': withVar('--surface-2'),
        'surface-3': withVar('--surface-3'),
        border: withVar('--border'),
        content: withVar('--content'),
        'content-muted': withVar('--content-muted'),
        // Semantik + geriye dönük uyumluluk
        primary: '#2563eb',
        success: '#059669',
        warning: '#d97706',
        danger: '#dc2626',
      },
      borderColor: {
        DEFAULT: withVar('--border'),
      },
    },
  },
  plugins: [
    require('@tailwindcss/typography'),
  ],
};
