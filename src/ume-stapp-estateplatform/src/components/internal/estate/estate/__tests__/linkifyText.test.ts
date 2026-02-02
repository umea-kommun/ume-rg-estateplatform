import { describe, it, expect } from 'vitest';
import { linkify, LinkifyToken, LinkToken } from '../linkifyText';

function kinds(tokens: LinkifyToken[]) {
	return tokens.map((t) => t.type);
}

describe('linkifyText', () => {
	it('returns empty array for empty input', () => {
		expect(linkify('')).toEqual([]);
	});

	it('returns only a text token when there are no links', () => {
		expect(linkify('Just regular text')).toEqual([
			{ type: 'text', value: 'Just regular text' },
		]);
	});

	it('linkifies an email address', () => {
		const out = linkify('Contact me at test@example.com thanks');
		expect(kinds(out)).toEqual(['text', 'link', 'text']);

		const emailToken = out[1] as LinkToken;
		expect(emailToken.type).toBe('link');
		expect(emailToken.value).toBe('test@example.com');
		expect(emailToken.href).toBe('mailto:test@example.com');
	});

	it('linkifies a phone number', () => {
		const out = linkify('Call 070-123 45 67 now');
		expect(kinds(out)).toEqual(['text', 'link', 'text']);

		const phoneToken = out[1] as LinkToken;
		expect(phoneToken.type).toBe('link');
		expect(phoneToken.value).toBe('070-123 45 67');
		expect(phoneToken.href).toBe('tel:0701234567');
	});

	it('keeps a + in phone numbers', () => {
		const out = linkify('Call +4670 123 45 67');
		expect(kinds(out)).toEqual(['text', 'link']);

		const phoneToken = out[1] as LinkToken;
		expect(phoneToken.href).toBe('tel:+46701234567');
	});

	it('linkifies https URLs as-is', () => {
		const out = linkify('Go to https://example.com/path?q=1');
		expect(kinds(out)).toEqual(['text', 'link']);

		const urlToken = out[1] as LinkToken;
		expect(urlToken.value).toBe('https://example.com/path?q=1');
		expect(urlToken.href).toBe('https://example.com/path?q=1');
	});

	it('adds https:// to bare domains', () => {
		const out = linkify('See umea.se for info');
		expect(kinds(out)).toEqual(['text', 'link', 'text']);

		const urlToken = out[1] as LinkToken;
		expect(urlToken.value).toBe('umea.se');
		expect(urlToken.href).toBe('https://umea.se');
	});

	it('adds https:// to www. URLs', () => {
		const out = linkify('Visit www.example.com now');
		expect(kinds(out)).toEqual(['text', 'link', 'text']);

		const urlToken = out[1] as LinkToken;
		expect(urlToken.value).toBe('www.example.com');
		expect(urlToken.href).toBe('https://www.example.com');
	});

	it('supports multiple different links in the same string', () => {
		const input =
			'Call 070-123 45 67, email a@b.se, or visit umea.se/contact';
		const out = linkify(input);

		expect(out.filter((t) => t.type === 'link')).toHaveLength(3);

		const [phone, email, url] = out.filter(
			(t) => t.type === 'link'
		) as LinkToken[];

		expect(phone.value).toBe('070-123 45 67');
		expect(phone.href).toBe('tel:0701234567');

		expect(email.value).toBe('a@b.se');
		expect(email.href).toBe('mailto:a@b.se');

		expect(url.value).toBe('umea.se/contact');
		expect(url.href).toBe('https://umea.se/contact');
	});

	it('does not lose whitespace or punctuation between tokens', () => {
		const input = 'Email: test@example.com, thanks.';
		const out = linkify(input);

		expect(out).toHaveLength(3);
		expect(out[0]).toEqual({ type: 'text', value: 'Email: ' });
		expect((out[1] as LinkToken).href).toBe('mailto:test@example.com');
		expect(out[2]).toEqual({ type: 'text', value: ', thanks.' });
	});

	it('handles multiple URLs', () => {
		const input = 'a.com and b.com';
		const out = linkify(input);

		const links = out.filter((t) => t.type === 'link') as LinkToken[];
		expect(links).toHaveLength(2);

		expect(links[0].value).toBe('a.com');
		expect(links[0].href).toBe('https://a.com');
		expect(links[1].value).toBe('b.com');
		expect(links[1].href).toBe('https://b.com');
	});

	it('is case-insensitive for emails/domains', () => {
		const input = 'Mail Test@Example.COM and visit ExAmple.com';
		const out = linkify(input);

		const links = out.filter((t) => t.type === 'link') as LinkToken[];
		expect(links).toHaveLength(2);

		expect(links[0].value).toBe('Test@Example.COM');
		expect(links[0].href).toBe('mailto:Test@Example.COM');

		expect(links[1].value).toBe('ExAmple.com');
		expect(links[1].href).toBe('https://ExAmple.com');
	});

	it('does not treat a short number as phone', () => {
		const out = linkify('Year 2026 is here');
		expect(out).toEqual([{ type: 'text', value: 'Year 2026 is here' }]);
	});
});
