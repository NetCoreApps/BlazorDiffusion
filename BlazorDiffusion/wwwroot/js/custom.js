/* globals */

const origScrollTo = window.scrollTo;
window.scrollTo = (x, y) => {
    const shouldSkip = true
    if (x === 0 && y === 0 && shouldSkip)
        return
    return origScrollTo.apply(this, arguments)
}
function map(o, f) { return o == null ? null : f(o) }
function prerenderedPage() { return '' }

var cls = (function () {
    var button = 'inline-flex justify-center rounded-md border border-transparent py-2 px-4 text-sm font-medium shadow-sm focus:outline-none focus:ring-2 focus:ring-offset-2 '
    return {
        buttons: {
            primary: button + 'dark:ring-offset-black focus:ring-2 focus:ring-offset-2 text-white bg-indigo-600 hover:bg-indigo-700 focus:ring-indigo-500 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800',
            secondary: button + 'bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-400 dark:hover:text-white hover:bg-gray-50 dark:hover:bg-gray-700 focus:ring-indigo-500 dark:focus:ring-indigo-600 dark:ring-offset-black',
        },
        form: {
            legend: "text-base font-medium text-gray-900 dark:text-gray-100 text-center mb-4"
        },
    }
})();

/* becomes reactive in static.js */
var AppData = {
    init: false,
    Auth: null
}

var ApiBaseUrl = location.hostname === 'blazordiffusion.com'
    ? 'https://api.blazordiffusion.com'
    : location.origin

var client, Apps, Components;

const DiffusionBrand = `<a href="https://diffusion.works/" class="p-3 flex items-center">
    <span class="sr-only">Diffusion Works</span>
    <svg class="hidden sm:inline w-7 h-7" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20"><path fill="currentColor" d="M8.55 3.06c1.01.34-1.95 2.01-.1 3.13c1.04.63 3.31-2.22 4.45-2.86c.97-.54 2.67-.65 3.53 1.23c1.09 2.38.14 8.57-3.79 11.06c-3.97 2.5-8.97 1.23-10.7-2.66c-2.01-4.53 3.12-11.09 6.61-9.9zm1.21 6.45c.73 1.64 4.7-.5 3.79-2.8c-.59-1.49-4.48 1.25-3.79 2.8z"/></svg>
    <span class="ml-1 text-lg sm:text-xl leading-6 tracking-tight text-white">diffusion.works</span>
</a>`

if (document.referrer.startsWith('https://diffusion.works') || document.referrer.startsWith('https://localhost:5002')) {
    document.addEventListener('DOMContentLoaded', e => {
        document.querySelector('header a').outerHTML = DiffusionBrand
    })
}
