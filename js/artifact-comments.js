import { onMounted, watch, computed, ref } from "vue"
import { classNames, isDate, toDate, fromXsdDuration, indexOfAny, map } from "@servicestack/client"
import {
    QueryArtifactComments,
    GetArtifactUserData,
    CreateArtifactCommentVote,
    DeleteArtifactCommentVote,
    DeleteArtifactComment,
    CreateArtifactCommentReport,
} from './dtos.mjs'
import { useClient } from './static.js'

const signInUrl = `/signin?return=${encodeURIComponent(location.pathname)}`

const ModalForm = {
    template: /*html*/`<div class="relative z-10" aria-labelledby="modal-title" role="dialog" aria-modal="true">
        <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"></div>
        <div class="fixed inset-0 z-10 overflow-y-auto">
            <div class="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
                <div class="relative transform overflow-hidden rounded-lg bg-white dark:bg-black text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-lg">
                    <slot></slot>
                </div>
            </div>
        </div>
    </div>`
}

const NewReport = {
    components: { ModalForm },
    template: /*html*/`<ModalForm class="z-30">
    <form @submit.prevent="submit">
        <div class="shadow overflow-hidden sm:rounded-md bg-white dark:bg-black">
            <div class="relative px-4 py-5 sm:p-6">
                <fieldset>
                    <legend :class="cls.form.legend">Report Comment</legend>

                    <error-summary :except="visibleFields" />

                    <div class="grid grid-cols-6 gap-6">
                        <div class="col-span-6">
                            <select-input id="type" label="Reason" v-model="postReport" :options="options" />
                        </div>
                        <div class="col-span-6">
                            <textarea-input id="description" v-model="description" placeholder="Please describe the issue for our moderation team to review" />
                        </div>
                    </div>
                </fieldset>
            </div>
            <div class="mt-4 px-4 py-3 bg-gray-50 dark:bg-gray-900 text-right sm:px-6">
                <div class="flex justify-end items-center">
                    <button :class="['mr-2',cls.buttons.secondary]" @click="$emit('done')">Cancel</button>
                    <button :class="cls.buttons.primary" type="submit">Submit</button>
                </div>
            </div>
        </div>
    </form>
</ModalForm>`,
    props: ['artifactCommentId'],
    emits:['done'],
    setup(props, { emit }) {
        const options = `Offensive,Spam,Nudity,Illegal,Other`.split(',')
        const visibleFields = ['PostReport', 'Description']
        const postReport = ref('')
        const description = ref('')
        let { apiVoid, error, loading } = useClient()

        function submit() {
            const { artifactCommentId } = props
            apiVoid(new CreateArtifactCommentReport({ artifactCommentId, postReport:options[postReport.value], description }))
                .then(r => emit('done'))
        }

        return {
            cls,
            options,
            visibleFields,
            postReport,
            description,
            submit,
        }
    }
}

const Comment = {
    template: /*html*/`<div class="py-1 border-b border-gray-800">
        <div class="relative group py-2 px-2 hover:bg-gray-900 rounded-lg">
            <div class="hidden group-hover:block absolute top-2 right-2">
                <svg @click="showMenu=!showMenu" class="w-7 h-7 bg-gray-800 rounded cursor-pointer hover:bg-black" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><circle cx="64" cy="128" r="12" fill="currentColor"/><circle cx="192" cy="128" r="12" fill="currentColor"/><circle cx="128" cy="128" r="12" fill="currentColor"/></svg>
                <div v-if="showMenu" class="absolute -ml-20">
                    <div class="select-none rounded-md whitespace-nowrap bg-white dark:bg-black shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none" role="menu" aria-orientation="vertical" aria-labelledby="menu-button" tabindex="-1">
                        <div class="py-1" role="none">
                            <div @click="showDialog('Report')" class="flex cursor-pointer text-gray-700 dark:text-gray-300 dark:text-gray-300 dark:hover:bg-gray-800 px-4 py-2 text-sm" role="menuitem" tabindex="-1">
                                <svg class="mr-2 h-5 w-5 text-gray-400 group-hover:text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M3 3v1.5M3 21v-6m0 0l2.77-.693a9 9 0 0 1 6.208.682l.108.054a9 9 0 0 0 6.086.71l3.114-.732a48.524 48.524 0 0 1-.005-10.499l-3.11.732a9 9 0 0 1-6.085-.711l-.108-.054a9 9 0 0 0-6.208-.682L3 4.5M3 15V4.5"/></svg>
                                Report
                            </div>
                            <div @click="deleteComment" class="flex cursor-pointer text-gray-700 dark:text-gray-300 dark:text-gray-300 dark:hover:bg-gray-800 px-4 py-2 text-sm" role="menuitem" tabindex="-1">
                                <svg class="mr-2 h-5 w-5 text-gray-400 group-hover:text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M6 19a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V7H6v12M8 9h8v10H8V9m7.5-5l-1-1h-5l-1 1H5v2h14V4h-3.5Z"/></svg>
                                Delete
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="flex select-none">
                <img :src="comment.avatar ? 'https://cdn.diffusion.works' + comment.avatar : null || comment.profileUrl" class="w-6 h-6 rounded-full mr-2" />
                <div class="text-sm text-gray-300">
                    {{comment.handle || comment.displayName}} <span class="px-1">&#8226;</span> {{ timeAgo }}
                </div>
            </div>
            <div class="py-2 text-gray-50">
                {{comment.content}}
            </div>
            <div class="text-sm text-gray-300 flex justify-between h-6">
                <div class="flex items-center">
                    <svg v-if="hasUpVoted" class="w-4 h-4 cursor-pointer" @click="upVote" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><path fill="currentColor" d="M231.4 123.1a8 8 0 0 1-7.4 4.9h-40v80a16 16 0 0 1-16 16H88a16 16 0 0 1-16-16v-80H32a8 8 0 0 1-7.4-4.9a8.4 8.4 0 0 1 1.7-8.8l96-96a8.1 8.1 0 0 1 11.4 0l96 96a8.4 8.4 0 0 1 1.7 8.8Z"/></svg>
                    <svg v-else class="w-4 h-4 cursor-pointer" @click="upVote" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m12 3l9 7h-4.99L16 21H8V10H3l9-7Z"/></svg>
                    <span class="px-2 select-none">{{ comment.votes }}</span>
                    <svg v-if="hasDownVoted" class="w-4 h-4 cursor-pointer" @click="downVote" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><path fill="currentColor" d="m229.7 141.7l-96 96a8.1 8.1 0 0 1-11.4 0l-96-96a8.4 8.4 0 0 1-1.7-8.8A8 8 0 0 1 32 128h40V48a16 16 0 0 1 16-16h80a16 16 0 0 1 16 16v80h40a8 8 0 0 1 7.4 4.9a8.4 8.4 0 0 1-1.7 8.8Z"/></svg>
                    <svg v-else class="w-4 h-4 cursor-pointer" @click="downVote" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m12 21l9-7h-4.99L16 3H8v11H3l9 7Z"/></svg>
                </div>
                <div>
                    <button type="button" @click="showReply=!showReply" class="hidden group-hover:inline-flex items-center rounded-full rounded-tl-none border border-transparent bg-indigo-600 px-3 py-1 text-xs font-medium text-white shadow-sm hover:bg-indigo-700 focus:outline-none focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-indigo-500 focus:ring-offset-2 dark:ring-offset-black">
                        <svg class="-ml-0.5 mr-2 h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill="currentColor" d="M8 1C3.6 1 0 3.5 0 6.5c0 2 2 3.8 4 4.8c0 2.1-2 2.8-2 2.8c2.8 0 4.4-1.3 5.1-2.1H8c4.4 0 8-2.5 8-5.5S12.4 1 8 1z"/></svg>
                        reply
                    </button>
                </div>
            </div>
        </div>
        <div v-if="showReply">
            <div class="flex p-2">
                <div class="grow-0 flex flex-col">
                    <div class="grow-0">
                        <img :src="comment.avatar ? 'https://cdn.diffusion.works' + comment.avatar : null || comment.profileUrl" class="w-6 h-6 rounded-full mr-2" />
                    </div>
                    <div class="grow relative">
                        <span class="absolute top-1.5 left-2.5 -ml-px h-full w-[1px] bg-gray-800" aria-hidden="true"></span>
                    </div>
                </div>
                <div class="grow pl-1 relative">
                    <svg @click="showReply=false" class="absolute top-2 right-2 w-5 h-5 text-gray-700 hover:text-gray-500 cursor-pointer" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                        <path fill="currentColor" d="M6.4 19L5 17.6l5.6-5.6L5 6.4L6.4 5l5.6 5.6L17.6 5L19 6.4L13.4 12l5.6 5.6l-1.4 1.4l-5.6-5.6Z"/></svg>
                    <input-comment :artifact-id="comment.artifactId" :reply-id="comment.id" @updated="replyDone" />
                </div>
            </div>
        </div>
    </div>`,
    props: ['userData', 'comment', 'nested'],
    emits: ['voted', 'unvoted', 'showDialog', 'refresh'],
    setup(props, { emit }) {
        const { comment } = props
        const timeAgo = computed(() => Relative.from(comment.createdDate))
        const { api, apiVoid, error, loading } = useClient()
        const showMenu = ref(false)
        const showReply = ref(false)
        const replyDone = () => { showReply.value = false; emit('refresh');  }
        let auth = computed(() => AppData.Auth)
        let hasUpVoted = computed(() => props.userData.upVoted.indexOf(comment.id) >= 0)
        let hasDownVoted = computed(() => props.userData.downVoted.indexOf(comment.id) >= 0)

        function showDialog(dialog) {
            showMenu.value = false
            emit('showDialog', dialog, comment)
        }

        function vote(value) {
            if (auth.value) {
                if (!hasUpVoted.value && !hasDownVoted.value) {
                    comment.votes += value
                    apiVoid(new CreateArtifactCommentVote({
                        artifactCommentId: comment.id,
                        vote: value,
                    })).then(r => {
                        emit('voted', comment, value, r)
                        if (!r.succeeded) {
                            comment.votes -= value
                        }
                    })
                } else {
                    if (hasUpVoted.value) {
                        comment.votes += -1
                    }
                    if (hasDownVoted.value) {
                        comment.votes += 1
                    }
                    apiVoid(new DeleteArtifactCommentVote({ artifactCommentId: comment.id }))
                        .then(r => {
                            emit('unvoted', props.comment, value, r)
                            if (!r.succeeded) {
                                if (hasUpVoted.value) {
                                    comment.votes += 1
                                }
                                if (hasDownVoted.value) {
                                    comment.votes += -1
                                }
                            }
                        })
                }
            } else {
                location.href = signInUrl
            }
        }

        function deleteComment() {
            showMenu.value = false
            apiVoid(new DeleteArtifactComment({ id: comment.id }))
                .then(r => {
                    if (!r.error) {
                        emit('refresh')
                    }
                })
        }

        const upVote = () => vote(1)
        const downVote = () => vote(-1)

        return {
            showMenu,
            showReply,
            showDialog,
            replyDone,
            comment,
            timeAgo,
            upVote,
            downVote,
            hasUpVoted,
            hasDownVoted,
            deleteComment,
        }
    }
}

const Thread = {
    components: { Comment },
    template: /*html*/`
    <div>
        <div v-for="(comment,index) in comments.filter(x => x.replyId == parentId)">
            <div :class="['flex', nested ? 'pl-1' : '']">
                <div v-if="nested" class="grow-0 flex flex-col">
                    <div class="w-6"></div>
                    <div class="grow relative">
                        <div class="">
                            <span class="absolute top-1 left-4 -ml-px h-full w-[1px] bg-gray-800" aria-hidden="true"></span>
                        </div>
                    </div>
                </div>
                <div class="grow relative">
                    <Comment :user-data="userData" :comment="comment" :nested="nested"
                            @voted="refreshUserData" @unvoted="refreshUserData" @show-dialog="showDialog" @refresh="refresh" />
                    <div class="">
                        <Thread :comments="comments" :parent-id="comment.id" 
                                @refresh="refresh" @refresh-user-data="refreshUserData" @show-dialog="showDialog" />
                    </div>
                </div>
            </div>
        </div>
    </div>
    `,
    props: ['comments','parentId'],
    emits: ['refresh','refreshUserData','showDialog'],
    setup(props, { emit }) {
        let { parentId } = props
        let userData = computed(() => AppData.UserArtifact)
        const refreshUserData = () => emit('refreshUserData')
        const showDialog = (dialog, comment) => emit('showDialog', dialog, comment)
        const refresh = () => emit('refresh')
        const nested = computed(() => parentId != null)
        return {
            userData,
            parentId,
            showDialog,
            nested,
            refresh,
            refreshUserData,
        }
    }
}
Thread.components['Thread'] = Thread

export default {
    components: { Thread, Comment, NewReport },
    template: /*html*/`
    <div :class="['mt-24 mx-auto flex flex-col w-full max-w-3xl transition-opacity', AppData.init ? 'opacity-100' : 'opacity-0']">
        <div v-if="auth" class="flex justify-center w-full">
            <input-comment :artifact-id="artifactId" @updated="refresh" />
        </div>
        <div v-else class="flex justify-center w-full">
            <div class="flex justify-between w-full max-w-2xl border border-gray-700 rounded bg-gray-900 overflow-hidden">
                <div class="p-2 pl-4 flex items-center">
                    <span class="text-gray-300">Sign in to leave a comment</span>
                </div>
                <div>
                    <a :href="signInUrl" :class="classNames(cls.buttons.secondary,'m-1')">Sign In</a>
                </div>
            </div>
        </div>
        <div v-if="comments.length" class="mt-8">
            <h2 class="text-xl border-b border-gray-800 py-2">{{ comments.length }} Comment{{ comments.length > 1 ? 's' : '' }}</h2>
            <Thread :comments="comments" :parent-id="null"
                    @refresh="refresh" @refresh-user-data="refreshUserData" @show-dialog="showDialog" />
        </div>
        <error-summary :status='error' />
        <NewReport v-if="show=='Report'" :artifact-comment-id="showTarget.id" @done="show=''" />
    </div>
    `,
    props: ['artifactId'],
    setup(props) {

        let { artifactId } = props
        let { api, loading, error } = useClient()
        let comments = ref([])
        let userData = computed(() => AppData.UserArtifact)
        let auth = computed(() => AppData.Auth)
        let show = ref('')
        let showTarget = ref(null)

        function showDialog(dialog,comment) {
            show.value = dialog
            showTarget.value = comment
        }

        function refreshUserData() {
            if (auth.value) {
                api(new GetArtifactUserData({ artifactId }))
                    .then(r => AppData.UserArtifact = r.response)
            }
        }

        function refresh() {
            api(new QueryArtifactComments({ artifactId }))
                .then(r => {
                    comments.value = r.response.results
                })
            refreshUserData()
        }

        onMounted(() => refresh())
        watch(() => AppData.Auth, refresh)

        return {
            AppData,
            cls,
            classNames,
            signInUrl,
            comments,
            userData,
            auth,
            error,
            loading,
            refresh,
            refreshUserData,
            show,
            showDialog,
            showTarget,
        }
    }
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

