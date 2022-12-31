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