import { computed, ComputedRef, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

export const useRouteSlug = (name?: ComputedRef<string>) => {
	const router = useRouter();
	const route = useRoute();

	const slug = computed(() => {
		if (!name?.value) {
			return null;
		}

		// Make name URL friendly
		const s = name.value
			.toLowerCase()
			.replace(/[åä]/gi, 'a')
			.replace(/ö/gi, 'o')
			.replace(/[^a-z0-9]+/g, '-')
			.replace(/^-+|-+$/g, '');

		return s;
	});

	watch(
		() => slug.value,
		(slug) => {
			if (slug !== null && route.params.slug !== slug) {
				router.replace({
					name: route.name,
					query: route.query,
					params: {
						...route.params,
						slug: slug,
					},
				});
			}
		},
		{ immediate: true }
	);
};
