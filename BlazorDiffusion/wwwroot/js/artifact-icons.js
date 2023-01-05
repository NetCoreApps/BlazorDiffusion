import { computed } from 'https://unpkg.com/vue@3/dist/vue.esm-browser.js'
import { useClient } from './static.js'
import { CreateArtifactLike, DeleteArtifactLike } from './dtos.mjs'

export default {
    template: /*html*/`
<div :class="['flex justify-between p-4 transition-opacity', AppData.init ? 'opacity-100' : 'opacity-0']">
    <div>
        <svg v-if="hasLiked" @click.prevent="unlikeArtifact" class="w-4 h-4 sm:w-6 sm:h-6 text-red-600 hover:text-red-400 cursor-pointer" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
            <title>undo like</title>
            <path fill="currentColor" d="M2 8.4A5.4 5.4 0 0 1 7.5 3A5.991 5.991 0 0 1 12 5a5.991 5.991 0 0 1 4.5-2A5.4 5.4 0 0 1 22 8.4c0 5.356-6.379 9.4-10 12.6C8.387 17.773 2 13.76 2 8.4Z" />
        </svg>
        <svg v-else @click.prevent="likeArtifact" class="w-4 h-4 sm:w-6 sm:h-6 text-cyan-600 hover:text-cyan-400 cursor-pointer" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
            <title>like image</title>
            <path fill="currentColor" d="M12 21c-.645-.572-1.374-1.167-2.145-1.8h-.01c-2.715-2.22-5.792-4.732-7.151-7.742c-.446-.958-.683-2-.694-3.058A5.39 5.39 0 0 1 7.5 3a6.158 6.158 0 0 1 3.328.983A5.6 5.6 0 0 1 12 5c.344-.39.738-.732 1.173-1.017A6.152 6.152 0 0 1 16.5 3A5.39 5.39 0 0 1 22 8.4a7.422 7.422 0 0 1-.694 3.063c-1.359 3.01-4.435 5.521-7.15 7.737l-.01.008c-.772.629-1.5 1.224-2.145 1.8L12 21ZM7.5 5a3.535 3.535 0 0 0-2.5.992A3.342 3.342 0 0 0 4 8.4c.011.77.186 1.53.512 2.228A12.316 12.316 0 0 0 7.069 14.1c.991 1 2.131 1.968 3.117 2.782c.273.225.551.452.829.679l.175.143c.267.218.543.444.81.666l.013-.012l.006-.005h.006l.009-.007h.01l.018-.015l.041-.033l.007-.006l.011-.008h.006l.009-.008l.664-.545l.174-.143c.281-.229.559-.456.832-.681c.986-.814 2.127-1.781 3.118-2.786a12.298 12.298 0 0 0 2.557-3.471c.332-.704.51-1.472.52-2.25A3.343 3.343 0 0 0 19 6a3.535 3.535 0 0 0-2.5-1a3.988 3.988 0 0 0-2.99 1.311L12 8.051l-1.51-1.74A3.988 3.988 0 0 0 7.5 5Z" />
        </svg>
    </div>
    <a :href="downloadArtifact" class="" target="_blank" title="Download">
        <svg class="w-4 h-4 sm:w-6 sm:h-6 text-cyan-600 hover:text-cyan-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
            <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M6 20h12M12 4v12m0 0l3.5-3.5M12 16l-3.5-3.5" />
        </svg>
    </a>
</div>
    `,
    props: ['artifactId','artifactRefId'],
    setup(props) {
        const hasLiked = computed(() => AppData.UserAlbum.likedArtifacts.indexOf(artifactId) >= 0)
        const { api, apiVoid, error, loading } = useClient()
        const { artifactId } = props
        const downloadArtifact = computed(() => ApiBaseUrl + '/download/artifact/' + props.artifactRefId)

        const addLike = () => AppData.UserAlbum.likedArtifacts.push(artifactId)
        const removeLike = () => AppData.UserAlbum.likedArtifacts = AppData.UserAlbum.likedArtifacts.filter(x => x != artifactId)

        function unlikeArtifact() {
            removeLike()
            apiVoid(new DeleteArtifactLike({ artifactId }))
                .then(r => {
                    if (!r.succeeded) {
                        addLike()
                    }
                })
        }

        function likeArtifact() {
            addLike()
            api(new CreateArtifactLike({ artifactId }))
                .then(r => {
                    if (!r.succeeded) {
                        removeLike()
                    }
                })
        }

        return {
            AppData,
            hasLiked,
            downloadArtifact,
            unlikeArtifact,
            likeArtifact,
        }
    }
}
