<template>
	<p v-if="noPassword" class="noPassword">
		{{ $t('component.internal.defaultPasswords.noPassword') }}
	</p>
	<v-text-field
		v-else
		:append-inner-icon="visible ? 'visibility_off' : 'visibility'"
		:type="visible ? 'text' : 'password'"
		:readonly="true"
		@click:append-inner="visible = !visible"
		:value="value"
		density="compact"
		variant="plain"
	>
	</v-text-field>
</template>
<script setup lang="ts">
import { computed, ref } from 'vue';
const props = defineProps<{ value: string }>();
const visible = ref(false);

const noPassword = computed(() => props.value.trim() === '');
</script>

<style scoped lang="scss">
.noPassword {
	color: $grey-lighten-6;
	font-style: italic;
}
</style>
