const origScrollTo = window.scrollTo;
window.scrollTo = (x, y) => {
    const shouldSkip = true
    if (x === 0 && y === 0 && shouldSkip)
        return
    return origScrollTo.apply(this, arguments)
}
