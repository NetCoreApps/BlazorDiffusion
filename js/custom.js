const origScrollTo = window.scrollTo;
window.scrollTo = (x, y) => {
    const shouldSkip = true
    if (x === 0 && y === 0 && shouldSkip)
        return
    return origScrollTo.apply(this, arguments)
}

JS.getBreakpoints = function () {
    let resolutions = { '2xl': 1536, xl: 1280, lg: 1024, md: 768, sm: 640 }
    let sizes = Object.keys(resolutions)
    let w = document.body.clientWidth
    let o = {}
    sizes.forEach(res => o[res] = w > resolutions[res])
    return o
}
