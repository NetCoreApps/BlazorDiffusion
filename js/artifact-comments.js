import { classNames, isDate, toDate, fromXsdDuration, indexOfAny } from 'https://unpkg.com/@servicestack/client/dist/servicestack-client.mjs'
import { QueryArtifactComments } from './dtos.mjs'

const Comment = {
    template: `<div class="py-1 border-b border-gray-800">
        <div class="py-4 px-2 hover:bg-gray-900 rounded-lg">
            <div class="flex">
                <img :src="comment.avatar ? 'https://cdn.diffusion.works' + comment.avatar : null || comment.profileUrl" class="w-6 h-6 rounded-full mr-2" />
                <div class="text-sm text-gray-300">
                    {{comment.handle || comment.displayName}} <span class="px-2">&#8226;</span> {{ relative }}
                </div>
            </div>
            <div class="text-lg py-2">
                {{comment.content}}
            </div>
            <div class="text-sm text-gray-300 flex items-center">
                <svg class="w-4 h-4 cursor-pointer" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m12 3l9 7h-4.99L16 21H8V10H3l9-7Z"/></svg>
                <span class="px-2">{{ votes }}</span>
                <svg class="w-4 h-4 cursor-pointer" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m12 21l9-7h-4.99L16 3H8v11H3l9 7Z"/></svg>
            </div>
        </div>
    </div>`,
    props: ['comment'],
    computed: {
        relative() {
            return Relative.from(this.comment.createdDate)
        },
        votes() {
            return this.comment.voteUpCount || 0 - this.comment.voteDownCount || 0
        }
    }
}

export default {
    components: { Comment },
    template: `
    <div class="mx-auto group flex flex-col w-full max-w-3xl">
        <div v-if="auth" class="flex justify-center w-full">
            <input-comment :artifact-id="artifactId" @updated="refresh" />
        </div>
        <div v-else class="flex justify-center w-full">
            <div class="flex justify-between w-full max-w-2xl border border-gray-700 rounded bg-gray-900 overflow-hidden">
                <div class="p-2 pl-4 flex items-center">
                    <span class="text-gray-300">Sign in to leave a comment</span>
                </div>
                <div>
                    <a :href="signIn" :class="classNames(cls.buttons.secondary,'m-1')">Sign In</a>
                </div>
            </div>
        </div>
        <div v-if="comments.length" class="mt-8">
            <h2 class="text-xl border-b border-gray-800 py-2">{{ comments.length }} Comment{{ comments.length ? 's' : '' }}</h2>
            <Comment v-for="(c,index) in comments" :comment="c" />
        </div>
        <error-summary :status='status' />
    </div>
    `,
    props: ['artifactId'],
    methods: {
        classNames,
        refresh() {
            client.api(new QueryArtifactComments({ artifactId: this.artifactId }))
                .then(api => {
                    if (api.succeeded) {
                        this.comments = api.response.results
                    } else this.status = api.error
                })
        },
    },
    computed: {
        auth() {
            return map(AppData, x => x.Auth)
        },
        signIn() {
            return `/signin?return=${encodeURIComponent(location.pathname)}`
        },
    },
    mounted() { 
        this.refresh()
    },
    data() {
        return {
            AppData,
            cls,
            status: null,
            comments: [],
        }
    },
}


var defaultFormats = { locale: map(navigator.languages, x => x[0]) || navigator.language || 'en' }
var Relative = (function () {
    let nowMs = () => new Date().getTime()

    let DateChars = ['/', 'T', ':', '-']
    /** @param {string|Date|number} val */
    function toRelativeNumber(val) {
        if (val == null) return NaN
        if (typeof val == 'number')
            return val
        if (isDate(val))
            return val.getTime() - nowMs()
        if (typeof val === 'string') {
            let num = Number(val)
            if (!isNaN(num))
                return num
            if (val[0] === 'P' || val.startsWith('-P'))
                return fromXsdDuration(val) * 1000 * -1
            if (indexOfAny(val, DateChars) >= 0)
                return toDate(val).getTime() - nowMs()
        }
        return NaN
    }
    let defaultRtf = new Intl.RelativeTimeFormat(defaultFormats.locale, {})
    let year = 24 * 60 * 60 * 1000 * 365
    let units = {
        year,
        month: year / 12,
        day: 24 * 60 * 60 * 1000,
        hour: 60 * 60 * 1000,
        minute: 60 * 1000,
        second: 1000
    }
    /** @param {number} elapsedMs
     *  @param {Intl.RelativeTimeFormat} [rtf] */
    function fromMs(elapsedMs, rtf) {
        for (let u in units) {
            if (Math.abs(elapsedMs) > units[u] || u === 'second')
                return (rtf || defaultRtf).format(Math.round(elapsedMs / units[u]), u)
        }
    }
    /** @param {string|Date|number} val
     *  @param {Intl.RelativeTimeFormat} [rtf] */
    function from(val, rtf) {
        let num = toRelativeNumber(val)
        if (!isNaN(num))
            return fromMs(num, rtf)
        console.error(`Cannot convert ${val}:${typeof val} to relativeTime`)
        return ''
    }
    /** @param {Date} d
     *  @param {Date} [from] */
    let fromDate = (d, from) =>
        fromMs(d.getTime() - (from ? from.getTime() : nowMs()))

    return {
        from,
        fromMs,
        fromDate,
    }
})();

