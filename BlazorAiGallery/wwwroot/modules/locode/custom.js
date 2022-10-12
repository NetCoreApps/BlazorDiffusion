/**: Extend locode App with custom JS **/

/** Custom [Format] method to style text with custom class
 * @param {*} val
 * @param {{cls:string}} [options] */
function presentFilesPreview(val, options) {
    let cls = options && options.cls || 'text-green-600'
    var result = `<div class="isolate flex -space-x-1 overflow-hidden">`
    for(var i = 0; i < val.length; i++) {
        result += `<img class="relative z-30 inline-block h-6 w-6 rounded-full ring-2 ring-white" style="margin-right:-1rem" src="${val[i].filePath}" alt="">`;
    }
    result += `</div>`;
    return result;
}