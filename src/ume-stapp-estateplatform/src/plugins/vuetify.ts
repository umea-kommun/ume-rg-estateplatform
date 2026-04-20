// Styles
import 'vuetify/styles';
import 'material-design-icons-iconfont/dist/material-design-icons.css';
import { aliases, md } from 'vuetify/iconsets/md';

// Vuetify
import { createVuetify } from 'vuetify';

const roundedDefault = { rounded: 'lg' };
const inputDefault = {
	...roundedDefault,
	variant: 'outlined',
	hideDetails: true,
};

// Themes: https://next.vuetifyjs.com/en/features/theme/
export default createVuetify({
	icons: {
		defaultSet: 'md',
		aliases,
		sets: {
			md,
		},
	},
	theme: {
		defaultTheme: 'umeaTheme',
		themes: {
			umeaTheme: {
				colors: {
					background: '#f2f2f2',
					primary: '#006e1e', // Björk, darker version, Umeåkommun
					secondary: '#00a01e', // Björk, lighter version, Umeå kommun
					accent: '#e4b1c2', // Rosa, Umeå kommun
					error: '#DB1814', // Red
					info: '#424242', // Grey-darken-3 from Material design
					success: '#006e1e', // Green
					warning: '#FFC107', // Orange
				},
			},
		},
	},
	defaults: {
		VAlert: roundedDefault,
		VAutocomplete: inputDefault,
		VSelect: inputDefault,
		VBtn: roundedDefault,
		VCard: roundedDefault,
		VTextField: inputDefault,
	},
});
