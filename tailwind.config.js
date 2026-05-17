/** @type {import('tailwindcss').Config} */
module.exports = {
  mode: 'jit',
  content: [
    "./Views/**/*.cshtml",
    "./**/*.cshtml",
  ],
  theme: {
    extend: {
      colors: {
        // use your palette (from your project context)
        DEFAULT: '#005844', // This will create bg-brand, text-brand, etc.
        dark: '#004535',    // This will create bg-brand-dark, etc.
      },
      borderRadius: {
        'xl': '1rem',
      },
      // add other customizations if needed
    },
  },
  plugins: [],
}


