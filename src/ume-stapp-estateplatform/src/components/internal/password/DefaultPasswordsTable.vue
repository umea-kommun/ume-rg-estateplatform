<template>
	<v-data-table
		v-model:sort-by="sortBy"
		:items="props.items"
		class="mt-3"
		items-per-page="-1"
		hide-default-footer
		hide-no-data
	>
		<!-- Table headers -->
		<template v-slot:headers>
			<base-table-header
				v-model:sortBy="sortBy"
				:headers="headers"
				default-order="asc"
			/>
		</template>

		<!-- Password item -->
		<template v-slot:item="{ item }">
			<tr class="password-item">
				<td class="name">
					{{ item.name }}
				</td>
				<td class="date-of-birth" :label="headers[1].title">
					{{ item.dateOfBirth }}
				</td>
				<td class="email" :label="headers[2].title">
					{{ item.email }}
				</td>
				<td class="password" :label="headers[3].title">
					<PasswordTextField :value="item.defaultPassword" />
				</td>
			</tr>
		</template>
	</v-data-table>
</template>

<script setup lang="ts">
import BaseTableHeader from '@/components/base/baseTable/BaseTableHeader.vue';
import { ref, watch } from 'vue';
import PasswordTextField from './PasswordTextField.vue';
import { ISortBy, ITableHeader } from '@/models/Interfaces';
import { useI18n } from 'vue-i18n';
import { IPasswordDefaultAssignment } from '@/models/password/Interfaces';

const { t } = useI18n();
const sortBy = ref<ISortBy[]>([{ key: 'name', order: 'asc' }]);

const props = defineProps<{
	items: IPasswordDefaultAssignment[];
}>();

const emit = defineEmits(['update:sortBy']);

watch(sortBy, async (newValue) => {
	emit('update:sortBy', newValue);
});

const headers: ITableHeader[] = [
	{
		title: t('component.internal.defaultPasswords.headers.name'),
		align: 'start',
		key: 'name',
	},
	{
		title: t('component.internal.defaultPasswords.headers.dateOfBirth'),
		align: 'start',
		key: 'dateOfBirth',
	},
	{
		title: t('component.internal.defaultPasswords.headers.email'),
		align: 'start',
		key: 'email',
	},
	{
		title: t('component.internal.defaultPasswords.headers.password'),
		align: 'start',
		key: 'password',
		sortable: false,
	},
];
</script>

<style scoped lang="scss">
.password {
	min-width: 200px;
	max-height: fit-content;
}

@media only screen and (max-width: 800px) {
	:deep(table) {
		width: 100%;

		thead {
			display: none;
		}
	}
	.password-item {
		display: flex;
		flex-wrap: wrap;

		border: solid 1px $grey-lighten-3;
		border-radius: $border-radius;
		background-color: $grey-lighten-2;
		margin-bottom: 2%;
		padding: 1%;
		padding-bottom: 3%;

		td {
			display: flex;
			flex-direction: column;
			background-color: transparent;
			border-bottom: none !important;
			min-width: 200px;

			&:before {
				content: attr(label);
				font-weight: bold;
				width: 100%;
			}
		}
		.name {
			font-weight: bold;
			font-size: 150%;
			height: 10%;
			width: 100%;
		}
	}
}
</style>
