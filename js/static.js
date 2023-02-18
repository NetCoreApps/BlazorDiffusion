import { createApp, reactive, ref, unref, isRef, provide } from "vue"
import { JsonApiClient, $$, $1 } from "@servicestack/client"
import { Authenticate, GetAlbumUserData, ResponseError, ResponseStatus } from './dtos.mjs'
import ArtifactComments from '/js/artifact-comments.js'
import ArtifactInfo from '/js/artifact-info.js'
import ArtifactIcons from '/js/artifact-icons.js'
import ErrorSummary from '/js/error-summary.js'
import InputComment from '/js/input-comment.js'
import { SelectInput, TextareaInput } from '/js/form.js'

Components = createComponents({
    'artifact-comments': ArtifactComments,
    'artifact-info': ArtifactInfo,
    'artifact-icons': ArtifactIcons,
    'error-summary': ErrorSummary,
    'input-comment': InputComment,
    'select-input': SelectInput,
    'textarea-input': TextareaInput,
})
Apps = []

export function load() {
    console.log('load')
    $$('[data-component]').forEach(el => {
        if (el.__vue_app__) return
        let componentName = el.getAttribute('data-component')
        let component = componentName && Components[componentName]
        if (!component) {
            console.error(`Could not create component ${componentName}`)
            return
        }

        let propsStr = el.getAttribute('data-props')
        let props = propsStr && new Function(`return (${propsStr})`)() || {}

        const app = createApp(component, props)
        app.provide('AppData', AppData)
        Object.keys(Components).forEach(name => {
            app.component(name, Components[name])
        })
        app.mount(el)
        Apps.push(app)
    })
    loadUserData()
}

function loadUserData() {
    const albumId = map($1('[data-album]'), x => x.getAttribute('data-album'))
    if (albumId && albumId !== AppData.albumId) {
        console.log('loadUserData', albumId)
        client.api(new GetAlbumUserData({ albumId }))
            .then(r => {
                if (r.succeeded) {
                    AppData.UserAlbum = r.response
                    AppData.albumId = albumId
                }
            })
    }
}

export function init() {
    console.log('init')
    AppData = reactive(AppData)
    AppData.UserArtifact = { liked: false, upVoted: [], downVoted: [] }
    AppData.UserAlbum = { likedArtifacts: [] }
    client = JsonApiClient.create()
    load()

    client.api(new Authenticate())
        .then(api => {
            AppData.Auth = api.succeeded ? api.response : null
            AppData.init = true
            loadUserData()
        })
}

export function unrefs(o) {
    Object.keys(o).forEach(k => {
        const val = o[k]
        o[k] = isRef(val) ? unref(val) : val
    })
    return o
}

export function useClient() {
    const loading = ref(false)
    const error = ref()
    const response = ref()

    const setError = ({ message, errorCode, fieldName, errors }) => {
        if (!errorCode) errorCode = 'Exception'
        if (!errors) errors = []
        return error.value = fieldName
            ? new ResponseStatus({
                errorCode, message,
                errors: [new ResponseError({ fieldName, errorCode, message })]
            })
            : new ResponseStatus({ errorCode, message, errors })
    }

    const addFieldError = ({ fieldName, message, errorCode }) => {
        if (!errorCode) errorCode = 'Exception'
        if (!error.value) {
            setError({ fieldName, message, errorCode })
        } else {
            let copy = new ResponseStatus(error.value)
            copy.errors = [...(copy.errors || []).filter(x => x.fieldName.toLowerCase() != fieldName.toLowerCase()),
            new ResponseError({ fieldName, message, errorCode })]
            error.value = copy
        }
    }

    function api(request, args, method) {
        loading.value = true
        return client.api(unrefs(request))
            .then(api => {
                loading.value = false
                response.value = api.response
                error.value = api.error
                return api
            })
    }

    async function apiVoid(request, args, method) {
        loading.value = true
        return client.apiVoid(unrefs(request))
            .then(api => {
                loading.value = false
                response.value = api.response
                error.value = api.error
                return api
            })
    }

    let ctx = { setError, addFieldError, loading, error, api, apiVoid }
    provide('ApiState', ctx)
    return ctx
}

function createComponents(c) {
    c.signin = {
        template: /*html*/`<div :class="['transition-opacity', AppData.init ? 'opacity-100' : 'opacity-0']">
        <a v-if="auth" href="/profile?t=1" class="block mx-3 relative">
            <button type="button" class="max-w-xs rounded-full flex items-center text-sm focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-cyan-500 lg:p-2 lg:rounded-md lg:hover:bg-gray-50 dark:lg:hover:bg-gray-900 dark:ring-offset-black" id="user-menu-button" aria-expanded="false" aria-haspopup="true">
                <svg class="h-8 w-8 rounded-full text-cyan-600 hover:text-cyan-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a5 5 0 1 0 5 5a5 5 0 0 0-5-5zm0 8a3 3 0 1 1 3-3a3 3 0 0 1-3 3zm9 11v-1a7 7 0 0 0-7-7h-4a7 7 0 0 0-7 7v1h2v-1a5 5 0 0 1 5-5h4a5 5 0 0 1 5 5v1z"></path></svg>
                <span class="hidden ml-3 text-gray-700 dark:text-gray-300 text-sm font-medium lg:block"><span class="sr-only">Open user menu for </span>{{auth.displayName}}</span>
                <svg class="hidden flex-shrink-0 ml-1 h-5 w-5 text-gray-400 dark:text-gray-500 lg:block" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"></path></svg>
            </button>
        </a>
        <a v-else-if="AppData.init" href="/signin" class="m-2">
            <button class="rounded-md border py-2 px-4 text-sm font-medium shadow-sm focus:outline-none focus:ring-2 focus:ring-offset-2 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-400 dark:hover:text-white hover:bg-gray-50 dark:hover:bg-gray-700 focus:ring-indigo-500 dark:focus:ring-indigo-600 dark:ring-offset-black">
                Sign In
            </button>
        </a>
        </div>`,
        computed: {
            auth() {
                return map(AppData, x => x.Auth)
            },
        },
        data() {
            return {
                cls,
                AppData,
            }
        }
    }
    return c
}
