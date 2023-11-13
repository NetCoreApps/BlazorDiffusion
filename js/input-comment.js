import { ref, computed } from "vue"
import { CreateArtifactComment } from './dtos.mjs'
import { useClient } from './static.js'

export default {
    template: /*html*/`
        <div class="w-full">
            <div class="flex flex-col w-full border border-gray-700 rounded bg-gray-900 overflow-hidden">
                <textarea v-model="content" class="w-full h-24 m-0 border-none outline-none dark:bg-transparent" placeholder="Write a comment"></textarea>
                <div class="flex justify-between p-2 pl-4 bg-black items-center">
                    <div>
                        <a href="/docs/community-rules" target="_blank" class="text-sm text-gray-400">read the community rules</a>
                    </div>
                    <div>
                        <span class="mr-2 text-sm text-gray-400">{{ remainingChars }}</span>
                        <button @click="submit" :class="cls.buttons.primary" :disabled="loading">Post</button>
                    </div>
                </div>
            </div>
            <div class="flex flex-col">
                <error-summary :status='error' />
            </div>
        </div>
    `,
    props: ['artifactId','replyId'],
    emits: ['updated'],
    setup(props, { attrs, emit }) {

        let content = ref('')
        let remainingChars = computed(() => 280 - content.value.length)
        let { api, error, loading } = useClient()

        function submit() {
            const { artifactId, replyId } = props
            api(new CreateArtifactComment({ artifactId, replyId, content })).then(r => {
                if (r.succeeded) {
                    content.value = ''
                    emit('updated', r.response)
                }
            })
        }

        return {
            cls,
            loading,
            content,
            remainingChars,
            error,
            submit,
        }
    }
}
