// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/utils/linkifyText.ts @ 84b4a5dc
export type LinkToken = {
	type: 'link';
	value: string;
	href: string;
	kind: 'url' | 'email' | 'phone';
};

export type TextToken = {
	type: 'text';
	value: string;
};

export type LinkifyToken = LinkToken | TextToken;

const RE = {
	// email first so it doesn't get eaten by url
	email: /(?<email>[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,})/i,
	// url: supports https://, www., or bare domain like umea.se/path
	url: /(?<url>(?:https?:\/\/)?(?:www\.)?[a-z0-9-]+(?:\.[a-z0-9-]+)+(?:\/[^\s<]*)?)/i,
	// phone:
	phone: /(?<phone>\+?\d[\d\s().-]{6,}\d)/,
};

const MASTER = new RegExp(
	`${RE.email.source}|${RE.url.source}|${RE.phone.source}`,
	'gi'
);

function normalizeUrl(raw: string) {
	if (/^https?:\/\//i.test(raw)) return raw;
	if (/^www\./i.test(raw)) return `https://${raw}`;
	return `https://${raw}`;
}

function normalizePhone(raw: string) {
	return raw.replace(/[^\d+]/g, '');
}

function toLinkifyToken(
	raw: string,
	groups?: Record<string, string>
): LinkifyToken {
	if (groups?.email) {
		return {
			type: 'link',
			kind: 'email',
			value: raw,
			href: `mailto:${groups.email}`,
		};
	}
	if (groups?.url) {
		const href = normalizeUrl(groups.url);
		return { type: 'link', kind: 'url', value: raw, href };
	}
	if (groups?.phone) {
		const tel = normalizePhone(groups.phone);
		return { type: 'link', kind: 'phone', value: raw, href: `tel:${tel}` };
	}
	return { type: 'text', value: raw };
}

export function linkify(input: string): LinkifyToken[] {
	const LinkifyTokens: LinkifyToken[] = [];
	let last = 0;

	for (const m of input.matchAll(MASTER)) {
		const start = m.index ?? 0;
		const raw = m[0];
		const groups = m.groups as Record<string, string> | undefined;

		if (start > last) {
			LinkifyTokens.push({
				type: 'text',
				value: input.slice(last, start),
			});
		}
		LinkifyTokens.push(toLinkifyToken(raw, groups));

		last = start + raw.length;
	}

	if (last < input.length) {
		LinkifyTokens.push({ type: 'text', value: input.slice(last) });
	}
	return LinkifyTokens;
}
