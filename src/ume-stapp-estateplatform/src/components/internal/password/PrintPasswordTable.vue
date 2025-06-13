<template>
	<Teleport to="body">
		<div class="print-table-content">
			<img
				class="logo"
				height="50"
				:src="logoGreen"
				:alt="$t('app.nav.logo')"
			/>
			<div class="p-header">
				<h1 class="school-group">{{ school }} - {{ group }}</h1>
				<h4>
					{{ $t('component.internal.defaultPasswords.title') }}
				</h4>
			</div>
			<v-data-table
				v-model:sort-by="sortTableBy"
				:items="props.items"
				items-per-page="-1"
				hide-default-footer
				hide-no-data
				density="compact"
			>
				<!-- Table headers -->
				<template v-slot:headers>
					<base-table-header
						v-model:sortBy="sortTableBy"
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
						<td class="date-of-birth">
							{{ item.dateOfBirth }}
						</td>
						<td class="email">
							{{ item.email }}
						</td>
						<td class="password">
							{{ item.defaultPassword }}
						</td>
					</tr>
				</template>
			</v-data-table>
		</div>
	</Teleport>
</template>

<script setup lang="ts">
import BaseTableHeader from '@/components/base/baseTable/BaseTableHeader.vue';
import { ref, watch } from 'vue';
import { ISortBy, ITableHeader } from '@/models/Interfaces';
import { useI18n } from 'vue-i18n';
import { IPasswordDefaultAssignment } from '@/models/password/Interfaces';
import logoGreen from '@/assets/logo_green.png';

const { t } = useI18n();
const sortTableBy = ref<ISortBy[]>([{ key: 'name', order: 'asc' }]);

const props = defineProps<{
	items: IPasswordDefaultAssignment[];
	sortBy: Array<ISortBy>;
	school: string | null;
	group: string | null;
}>();

watch(
	() => props.sortBy,
	async () => {
		sortTableBy.value = props.sortBy;
	}
);

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
.print-table-content {
	position: absolute;
	min-height: 100vh;
	z-index: 100;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	padding: 40px 20px 20px 20px;
	background-color: white;
	display: none;
}
.p-header {
	margin-top: 30px;
}
.school-group {
	margin-bottom: 10px;
}

@page {
	margin-left: 19mm;
	margin-right: 19mm;
	margin-top: 19mm;
	margin-bottom: 30mm;
}

@media print {
	.print-table-content {
		display: block;
	}
}
</style>
