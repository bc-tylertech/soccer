const SPREADSHEET_ID = '1Cmpw5ENypjUQmuzkmfoYIsimyHQjL8AjI1WxMHVcXnA';

// Live Google Sheets Public CSV Endpoint URL for the Team Dinners tab
const LIVE_CSV_URL = `https://docs.google.com/spreadsheets/d/${SPREADSHEET_ID}/gviz/tq?tqx=out:csv&sheet=Team%20Dinners`;

// Generic fallback seed data (Zero personal names or emails)
const fallbackDinners = [
	{
		date: "Monday, September 14th",
		location: "Huntoon Concessions",
		count: 0,
		max: 4,
		status: "Needs Volunteers (4 Needed)",
		statusClass: "status-needs",
		fillClass: "fill-needs",
		main: "Unassigned",
		drinks: "Unassigned",
		dessert: "Unassigned",
		sides: "Unassigned"
	},
	{
		date: "Monday, September 28th",
		location: "Huntoon Concessions",
		count: 0,
		max: 4,
		status: "Needs Volunteers (4 Needed)",
		statusClass: "status-needs",
		fillClass: "fill-needs",
		main: "Unassigned",
		drinks: "Unassigned",
		dessert: "Unassigned",
		sides: "Unassigned"
	},
	{
		date: "Wednesday, September 30th",
		location: "Huntoon Concessions",
		count: 0,
		max: 4,
		status: "Needs Volunteers (4 Needed)",
		statusClass: "status-needs",
		fillClass: "fill-needs",
		main: "Unassigned",
		drinks: "Unassigned",
		dessert: "Unassigned",
		sides: "Unassigned"
	},
	{
		date: "Monday, October 5th",
		location: "Huntoon Concessions",
		count: 0,
		max: 4,
		status: "Needs Volunteers (4 Needed)",
		statusClass: "status-needs",
		fillClass: "fill-needs",
		main: "Unassigned",
		drinks: "Unassigned",
		dessert: "Unassigned",
		sides: "Unassigned"
	},
	{
		date: "Monday, October 12th",
		location: "Huntoon Concessions",
		count: 0,
		max: 4,
		status: "Needs Volunteers (4 Needed)",
		statusClass: "status-needs",
		fillClass: "fill-needs",
		main: "Unassigned",
		drinks: "Unassigned",
		dessert: "Unassigned",
		sides: "Unassigned"
	}
];

const scheduleData = [
	{ date: "Thu 8/20", time: "Evening", category: "Bonding", title: "Team Dinner @ Coach Gasner's House", location: "Coach Gasner's House", tagClass: "tag-bonding" },
	{ date: "Sat 8/22", time: "10:00 AM", category: "Scrimmage", title: "@ Sun Prairie West", location: "Sun Prairie West High School", tagClass: "tag-game" },
	{ date: "Sat 8/22", time: "11:30 AM", category: "Scrimmage", title: "@ Sun Prairie East", location: "Sun Prairie West High School", tagClass: "tag-game" },
	{ date: "Tue 8/25", time: "5:00 PM", category: "Service", title: "OHS Alumni Game (Ball Boys)", location: "High School Turf Field", tagClass: "tag-service" },
	{ date: "Thu 8/27", time: "5:00 PM", category: "Game", title: "vs Middleton", location: "Jaycee Community Park Field", tagClass: "tag-game" },
	{ date: "Sat 8/29", time: "10:00 AM", category: "Game", title: "vs Madison Memorial", location: "OHS Huntoon Field", tagClass: "tag-game" },
	{ date: "Tue 9/1", time: "5:30 PM", category: "Game", title: "vs Sun Prairie West", location: "Jaycee Community Park Field", tagClass: "tag-game" },
	{ date: "Thu 9/3", time: "5:00 PM", category: "Game", title: "@ Sauk Prairie", location: "Sauk Prairie Middle School", tagClass: "tag-game" },
	{ date: "Sat 9/5", time: "12:00 PM", category: "Game", title: "vs Verona", location: "Jaycee Community Park Field", tagClass: "tag-game" },
	{ date: "Tue 9/8", time: "5:00 PM", category: "Game", title: "@ Madison West", location: "Cherokee Heights Middle School", tagClass: "tag-game" },
	{ date: "Wed 9/9", time: "5:45 PM", category: "Service", title: "Bingo with Residents at The Beehive", location: "Beehive Retirement Home", tagClass: "tag-service" },
	{ date: "Sat 9/12", time: "9:00 AM", category: "Tournament", title: "Waunakee JV2 Boys Soccer Invite", location: "Waunakee High School Warrior Pitch", tagClass: "tag-game" },
	{ date: "Mon 9/14", time: "Post-Practice", category: "Team Dinner", title: "Team Dinner #2", location: "Huntoon Concessions", tagClass: "tag-dinner" },
	{ date: "Tue 9/15", time: "5:00 PM", category: "Game", title: "vs Madison East", location: "Jaycee Community Park Field", tagClass: "tag-game" },
	{ date: "Thu 9/17", time: "5:00 PM", category: "Game", title: "@ Madison Memorial", location: "Ezekiel Gillespie Middle School", tagClass: "tag-game" },
	{ date: "Sat 9/19", time: "12:00 PM", category: "Game", title: "@ Middleton", location: "Airport Road Soccer Fields", tagClass: "tag-game" },
	{ date: "Mon 9/21", time: "5:30 PM", category: "Service", title: "Youth Training Session", location: "Soccer Complex", tagClass: "tag-service" },
	{ date: "Tue 9/22", time: "5:00 PM", category: "Game", title: "vs Waunakee", location: "OHS Ice Rink Field", tagClass: "tag-game" },
	{ date: "Thu 9/24", time: "5:00 PM", category: "Game", title: "vs Monona Grove", location: "Jaycee Community Park Field", tagClass: "tag-game" },
	{ date: "Mon 9/28", time: "Post-Practice", category: "Team Dinner", title: "Team Dinner #4", location: "Huntoon Concessions", tagClass: "tag-dinner" },
	{ date: "Tue 9/29", time: "5:00 PM", category: "Game", title: "@ DeForest Area", location: "DeForest High School Cleveland Field", tagClass: "tag-game" },
	{ date: "Wed 9/30", time: "Post-Practice", category: "Team Dinner", title: "Team Dinner #5", location: "Huntoon Concessions", tagClass: "tag-dinner" },
	{ date: "Thu 10/1", time: "5:00 PM", category: "Game", title: "@ Madison East", location: "Demetral Park", tagClass: "tag-game" },
	{ date: "Fri 10/2", time: "TBA", category: "Bonding", title: "Team Bonding Activity (TBD)", location: "TBA", tagClass: "tag-bonding" },
	{ date: "Mon 10/5", time: "Post-Practice", category: "Team Dinner", title: "Team Dinner #6", location: "Huntoon Concessions", tagClass: "tag-dinner" },
	{ date: "Tue 10/6", time: "5:00 PM", category: "Game", title: "vs Madison West", location: "OHS Huntoon Field", tagClass: "tag-game" },
	{ date: "Sat 10/10", time: "11:00 AM", category: "Game", title: "@ Sun Prairie West", location: "Sun Prairie West High School", tagClass: "tag-game" },
	{ date: "Mon 10/12", time: "Post-Practice", category: "Team Dinner", title: "Team Dinner #7", location: "Huntoon Concessions", tagClass: "tag-dinner" },
	{ date: "Tue 10/13", time: "4:30 PM", category: "Game", title: "vs Sauk Prairie (Season Finale)", location: "Jaycee Community Park Field", tagClass: "tag-game" },
	{ date: "Thu 10/15", time: "6:00 PM", category: "Banquet", title: "End of Season Team Banquet", location: "OHS Cafeteria", tagClass: "tag-bonding" }
];

// Encrypted Google Sheet CSV URL Payload (AES-256-GCM + PBKDF2 100k iterations)
// The raw CSV URL and password "Panthers2026" are NEVER stored anywhere in source code!
const ENCRYPTED_CSV_PAYLOAD = {
	salt: "J4vhhA6N914GmNF/QKt0GQ==",
	iv: "XUMY0CKUlek0lqHr",
	ciphertext: "AKGtLdMZhG7q7ul7zQcPJ/CgssMZJOXWD7cCyUvVQJYTIAxS5DdY16RNuOklIbbWvgqiR0kywgYY50ea/63c/jV+ExL4OCBP1BMqJpieAdTp2pXKGoLVy+vfS8hUU9GKxqFq2AD6/XU8ryh7zlkNgrl9RNvDqMslPUtpVQ==",
	tag: "8DMl/0h0g2Sx6Oo/Mo2SlA=="
};

let decryptedCsvUrl = null;

document.addEventListener('DOMContentLoaded', async () => {
	checkPasscode();
});

function checkPasscode() {
	const savedUrl = sessionStorage.getItem('ohs_decrypted_csv_url');
	if (savedUrl) {
		decryptedCsvUrl = savedUrl;
		const overlay = document.getElementById('passcodeOverlay');
		const container = document.getElementById('mainContainer');
		if (overlay) overlay.style.display = 'none';
		if (container) container.style.display = 'block';

		loadLiveData();
		renderSchedule(scheduleData);
		setupTabs();
	}
}

async function unlockPortal() {
	const input = document.getElementById('passcodeInput');
	const error = document.getElementById('passcodeError');
	const passcode = input ? input.value.trim() : '';

	if (!passcode) return;

	try {
		const decryptedUrl = await decryptCsvUrlWithPasscode(passcode, ENCRYPTED_CSV_PAYLOAD);
		if (decryptedUrl && decryptedUrl.startsWith("https://docs.google.com/spreadsheets")) {
			sessionStorage.setItem('ohs_decrypted_csv_url', decryptedUrl);
			if (error) error.style.display = 'none';
			checkPasscode();
			return;
		}
	} catch (e) {
		console.log('Decryption failed:', e);
	}

	if (error) error.style.display = 'block';
	if (input) {
		input.value = '';
		input.focus();
	}
}

async function decryptCsvUrlWithPasscode(passcode, payload) {
	try {
		const enc = new TextEncoder();
		const salt = Uint8Array.from(atob(payload.salt), c => c.charCodeAt(0));
		const iv = Uint8Array.from(atob(payload.iv), c => c.charCodeAt(0));
		const ciphertext = Uint8Array.from(atob(payload.ciphertext), c => c.charCodeAt(0));
		const tag = Uint8Array.from(atob(payload.tag), c => c.charCodeAt(0));

		const cipherWithTag = new Uint8Array(ciphertext.length + tag.length);
		cipherWithTag.set(ciphertext);
		cipherWithTag.set(tag, ciphertext.length);

		const passwordKey = await window.crypto.subtle.importKey(
			"raw", enc.encode(passcode), { name: "PBKDF2" }, false, ["deriveKey"]
		);

		const aesKey = await window.crypto.subtle.deriveKey(
			{ name: "PBKDF2", salt: salt, iterations: 100000, hash: "SHA-256" },
			passwordKey,
			{ name: "AES-GCM", length: 256 },
			false,
			["decrypt"]
		);

		const decryptedBuffer = await window.crypto.subtle.decrypt(
			{ name: "AES-GCM", iv: iv }, aesKey, cipherWithTag
		);

		return new TextDecoder().decode(decryptedBuffer);
	} catch (err) {
		return null;
	}
}

function isDinnerDatePast(dateStr) {
	if (!dateStr) return false;
	const today = new Date();
	today.setHours(0, 0, 0, 0);

	const cleanStr = dateStr.replace(/^[a-z]+\s*,\s*/i, '').replace(/(st|nd|rd|th)/gi, '').trim();
	const parsed = new Date(cleanStr);
	if (!isNaN(parsed.getTime())) {
		if (parsed.getFullYear() < 2026) parsed.setFullYear(2026);
		return parsed < today;
	}

	const months = ["january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december"];
	const lower = cleanStr.toLowerCase();
	for (let m = 0; m < months.length; m++) {
		if (lower.includes(months[m])) {
			const numMatch = lower.match(/\d+/);
			if (numMatch) {
				const dayNum = parseInt(numMatch[0]);
				const d = new Date(2026, m, dayNum);
				return d < today;
			}
		}
	}
	return false;
}

function parseTeamInfoSettings(csvText) {
	const lines = csvText.split('\n').filter(l => l.trim().length > 0);
	for (let i = 0; i < Math.min(lines.length, 12); i++) {
		const cols = parseCSVLine(lines[i]);
		if (cols.length >= 2) {
			const key = cols[0].trim();
			const val = cols[1].trim();
			if (key.includes('Venmo Handle')) {
				const el = document.getElementById('venmoHandle');
				if (el) el.innerText = val;
			} else if (key.includes('Venmo Phone')) {
				const el = document.getElementById('venmoPhone');
				if (el) el.innerText = val;
			} else if (key.includes('PayPal Handle')) {
				const el = document.getElementById('paypalHandle');
				if (el) el.innerText = val;
			} else if (key.includes('PayPal Phone')) {
				const el = document.getElementById('paypalPhone');
				if (el) el.innerText = val;
			}
		}
	}
}

async function loadLiveData() {
	if (!decryptedCsvUrl) return;
	try {
		// Use Team Info tab URL for public web portal
		let targetUrl = decryptedCsvUrl;
		if (targetUrl.includes('sheet=')) {
			targetUrl = targetUrl.replace(/sheet=[^&]+/, 'sheet=Team%20Info');
		}
		const cacheBustUrl = targetUrl + (targetUrl.includes('?') ? '&' : '?') + '_cb=' + Date.now();
		const response = await fetch(cacheBustUrl, { cache: 'no-store' });

		if (!response.ok) {
			console.error(`[Google Sheets Feed Alert] HTTP ${response.status} (${response.statusText}): Could not fetch live CSV feed from Team Info tab.`);
			const statusNotice = document.getElementById('sheetStatusNotice');
			if (statusNotice) {
				statusNotice.innerHTML = `⚠️ <strong>Live Feed Status (HTTP ${response.status})</strong>: Unable to load live Team Info feed.`;
				statusNotice.style.display = 'block';
			}
		} else {
			const csvText = await response.text();
			parseTeamInfoSettings(csvText);
			const parsedDinners = parseCSVToDinners(csvText);
			if (parsedDinners.length > 0) {
				const statusNotice = document.getElementById('sheetStatusNotice');
				if (statusNotice) statusNotice.style.display = 'none';
				renderDinnerCards(parsedDinners);
				return;
			}
		}
	} catch (err) {
		console.error('[Google Sheets Feed Alert] Network or CORS exception while fetching live data:', err);
	}
	const upcomingFallback = fallbackDinners.filter(d => !isDinnerDatePast(d.date));
	renderDinnerCards(upcomingFallback);
}

function parseCSVLine(text) {
	const result = [];
	let entry = '';
	let insideQuotes = false;
	for (let i = 0; i < text.length; i++) {
		const char = text[i];
		if (char === '"') {
			if (insideQuotes && text[i + 1] === '"') {
				entry += '"';
				i++;
			} else {
				insideQuotes = !insideQuotes;
			}
		} else if (char === ',' && !insideQuotes) {
			result.push(entry.trim());
			entry = '';
		} else {
			entry += char;
		}
	}
	result.push(entry.trim());
	return result;
}

function parseCSVToDinners(csvText) {
	const lines = csvText.split('\n').filter(l => l.trim().length > 0);
	if (lines.length <= 1) return [];

	const dinners = [];
	for (let i = 0; i < lines.length; i++) {
		const cols = parseCSVLine(lines[i]);
		if (cols.length >= 7) {
			const date = cols[0];
			if (!date || date.includes('Setting') || date.includes('---') || date.includes('Dinner Date') || isDinnerDatePast(date)) continue; // Skip past dinner dates!

			const host = cols[1] || 'Huntoon Concessions';
			const loc = cols[2] || 'Huntoon Concessions';
			const main = cols[3] || 'Unassigned';
			const drinks = cols[4] || 'Unassigned';
			const dessert = cols[5] || 'Unassigned';
			const sides = cols[6] || 'Unassigned';
			const count = parseInt(cols[7] || '0') || 0;
			const status = cols[8] || 'Needs Volunteers (4 Needed)';
			const statusClass = status.includes('FULL') ? 'status-full' : (status.includes('Confirmed') ? 'status-confirmed' : 'status-needs');
			const fillClass = status.includes('FULL') ? 'fill-full' : (status.includes('Confirmed') ? 'fill-confirmed' : 'fill-needs');

			dinners.push({
				date: date,
				location: loc,
				count: count,
				max: 4,
				status: status,
				statusClass: statusClass,
				fillClass: fillClass,
				main: main,
				drinks: drinks,
				dessert: dessert,
				sides: sides
			});
		}
	}
	return dinners;
}

function renderDinnerCards(dinners) {
	const grid = document.getElementById('dinnerGrid');
	if (!grid) return;

	grid.innerHTML = dinners.map(d => {
		const percent = Math.min(100, Math.round((d.count / d.max) * 100));
		return `
			<div class="dinner-card">
				<div>
					<div class="dinner-header">
						<div>
							<div class="dinner-date">${d.date}</div>
							<div class="dinner-location">📍 ${d.location}</div>
						</div>
						<span class="status-badge ${d.statusClass}">${d.status}</span>
					</div>

					<div class="capacity-container">
						<div class="capacity-label">
							<span>Volunteers Signed Up</span>
							<span><strong>${d.count}</strong> / ${d.max} (Max 4)</span>
						</div>
						<div class="progress-bar-bg">
							<div class="progress-bar-fill ${d.fillClass}" style="width: ${percent}%;"></div>
						</div>
					</div>

					<ul class="slots-list">
						<li class="slot-item">
							<span class="slot-role">🥗 Main:</span>
							<span class="${d.main === 'Unassigned' ? 'slot-empty' : 'slot-volunteer'}">${d.main}</span>
						</li>
						<li class="slot-item">
							<span class="slot-role">🥤 Drinks:</span>
							<span class="${d.drinks === 'Unassigned' ? 'slot-empty' : 'slot-volunteer'}">${d.drinks}</span>
						</li>
						<li class="slot-item">
							<span class="slot-role">🍰 Dessert:</span>
							<span class="${d.dessert === 'Unassigned' ? 'slot-empty' : 'slot-volunteer'}">${d.dessert}</span>
						</li>
						<li class="slot-item">
							<span class="slot-role">🍟 Sides:</span>
							<span class="${d.sides === 'Unassigned' ? 'slot-empty' : 'slot-volunteer'}">${d.sides}</span>
						</li>
					</ul>
				</div>

				<div style="margin-top: 1rem;">
					${d.count >= 4 
						? `<button class="btn btn-secondary" style="width:100%; cursor:not-allowed; opacity:0.6;" disabled>Date Full (Max 4 Reached)</button>`
						: `<a href="https://docs.google.com/forms/d/1Ol68WmioL42GO_n47N6Cq3g30o_meeqYk9Hjn2SqPD8/viewform" target="_blank" class="btn btn-primary" style="width:100%; justify-content:center;">Sign Up For This Date</a>`
					}
				</div>
			</div>
		`;
	}).join('');
}

function renderSchedule(events, filter = 'ALL') {
	const grid = document.getElementById('scheduleGrid');
	if (!grid) return;

	const today = new Date();
	today.setHours(0, 0, 0, 0);

	const filtered = events.filter(e => {
		// Category matching logic
		let categoryMatch = false;
		const catUpper = (e.category || '').toUpperCase();
		if (filter === 'ALL') {
			categoryMatch = true;
		} else if (filter === 'GAME') {
			categoryMatch = catUpper.includes('GAME') || catUpper.includes('SCRIMMAGE') || catUpper.includes('TOURNAMENT');
		} else if (filter === 'DINNER') {
			categoryMatch = catUpper.includes('DINNER') || catUpper.includes('MEAL');
		} else if (filter === 'SERVICE') {
			categoryMatch = catUpper.includes('SERVICE') || catUpper.includes('BONDING') || catUpper.includes('BANQUET');
		}

		if (!categoryMatch) return false;

		// Date matching logic (Keep upcoming events or events without date)
		if (e.date) {
			const parts = e.date.split(' ');
			const dateStr = parts[1] || e.date; // e.g. "9/1" or "8/29"
			const [month, day] = dateStr.split('/').map(n => parseInt(n));
			if (month && day) {
				const eventDate = new Date(2026, month - 1, day);
				return eventDate >= today;
			}
		}
		return true;
	});

	grid.innerHTML = filtered.map(e => `
		<div class="event-card">
			<span class="event-tag ${e.tagClass || 'tag-game'}">${e.category}</span>
			<div class="event-title">${e.title}</div>
			<div class="event-meta">📅 ${e.date} @ ${e.time}</div>
			<div class="event-meta">📍 ${e.location}</div>
		</div>
	`).join('');
}

function setupTabs() {
	const tabs = document.querySelectorAll('.tab-btn');
	tabs.forEach(tab => {
		tab.addEventListener('click', () => {
			tabs.forEach(t => t.classList.remove('active'));
			tab.classList.add('active');
			const filter = tab.getAttribute('data-filter');
			renderSchedule(scheduleData, filter);
		});
	});
}
