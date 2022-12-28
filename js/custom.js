const origScrollTo = window.scrollTo;
window.scrollTo = (x, y) => {
    const shouldSkip = true
    if (x === 0 && y === 0 && shouldSkip)
        return
    return origScrollTo.apply(this, arguments)
}
function map(o, f) { return o == null ? null : f(o) }

var { JsonServiceClient, Authenticate, $1, leftPart } = exports

let ApiBaseUrl = location.hostname === 'blazordiffusion.com'
    ? 'https://api.blazordiffusion.com'
    : location.origin

let client = new JsonServiceClient(ApiBaseUrl)
let AUTH = null

client.api(new Authenticate())
    .then(api => {
        if (api.succeeded) {
            AUTH = api.response
            map($1('#signin'), x => x.innerHTML = `<a href="/profile?t=1" class="block mx-3 relative">
                <button type="button" class="max-w-xs rounded-full flex items-center text-sm focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-cyan-500 lg:p-2 lg:rounded-md lg:hover:bg-gray-50 dark:lg:hover:bg-gray-900 dark:ring-offset-black" id="user-menu-button" aria-expanded="false" aria-haspopup="true">
                    <svg class="h-8 w-8 rounded-full text-cyan-600 hover:text-cyan-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a5 5 0 1 0 5 5a5 5 0 0 0-5-5zm0 8a3 3 0 1 1 3-3a3 3 0 0 1-3 3zm9 11v-1a7 7 0 0 0-7-7h-4a7 7 0 0 0-7 7v1h2v-1a5 5 0 0 1 5-5h4a5 5 0 0 1 5 5v1z"></path></svg>
                    <span class="hidden ml-3 text-gray-700 dark:text-gray-300 text-sm font-medium lg:block"><span class="sr-only">Open user menu for </span>${AUTH.displayName}</span>
                    <svg class="hidden flex-shrink-0 ml-1 h-5 w-5 text-gray-400 dark:text-gray-500 lg:block" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"></path></svg>
                </button></a>`)
        } else {
            AUTH = null
            map($1('#signin'), x => x.innerHTML = `<a href="/signin" class="m-2">
                <button class="rounded-md border py-2 px-4 text-sm font-medium shadow-sm focus:outline-none focus:ring-2 focus:ring-offset-2 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-400 dark:hover:text-white hover:bg-gray-50 dark:hover:bg-gray-700 focus:ring-indigo-500 dark:focus:ring-indigo-600 dark:ring-offset-black">
                    Sign In
                </button></a>`)
        }
    })
