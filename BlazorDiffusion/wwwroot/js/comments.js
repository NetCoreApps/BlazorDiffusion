var { $1 } = exports

function map(o, f) { return o == null ? null : f(o) }

let ArtifactId = map($1('[data-artifact]'), x => parseInt(x.getAttribute('data-artifact')))

export default {
    template: `<div></div>`,
    data() {
        return { count: ArtifactId || 0 }
    },
}
