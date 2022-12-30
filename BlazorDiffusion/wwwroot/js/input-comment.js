import { CreateArtifactComment } from './dtos.mjs'

export default {
    template: `
        <div class="w-full">
            <div class="group flex flex-col w-full border border-gray-700 rounded bg-gray-900 overflow-hidden">
                <textarea v-model="content" class="w-full h-24 m-0 border-none outline-none dark:bg-transparent group-focus:bg-gray-900" placeholder="Write a comment"></textarea>
                <div class="flex justify-between p-2 pl-4 bg-black items-center">
                    <div>
                        <a href="/docs/community-rules" target="_blank" class="text-sm text-gray-400">read the community rules</a>
                    </div>
                    <div>
                        <button @click="submit" :class="cls.buttons.primary" :disabled="loading">Post</button>
                    </div>
                </div>
            </div>
            <div class="flex flex-col">
                <error-summary :status='status' />
            </div>
        </div>
    `,
    props: ['artifactId'],
    emits: ['updated'],
    methods: {
        submit() {
            this.loading = true
            var { artifactId, content } = this
            client.api(new CreateArtifactComment({ artifactId, content })).then(api => {
                this.loading = false
                this.content = ''
                if (api.succeeded) {
                    this.$emit('updated', api.response)
                } else {
                    this.status = api.error
                }
            })
        },
    },
    data() {
        return {
            cls,
            loading: false,
            content: '',
            status: null,
        }
    },
}
