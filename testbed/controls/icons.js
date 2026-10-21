// Small line icons, one per act. Drawn in the current colour.
const P = {
  eye: '<path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>',
  loop: '<path d="M20 12a8 8 0 1 1-2.6-5.9"/><path d="M20 4v4.5h-4.5"/><circle cx="12" cy="12" r="2"/>',
  coin: '<circle cx="12" cy="12" r="9"/><path d="M12 7v10M9.5 14.5c0 1 1 1.7 2.5 1.7s2.5-.7 2.5-1.7-1-1.5-2.5-1.9-2.5-.9-2.5-1.9 1-1.7 2.5-1.7 2.5.7 2.5 1.7"/>',
  phone: '<path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2z"/>',
  talk: '<path d="M4 5h16v11H10l-5 4v-4H4z"/><path d="M8 9.5h8M8 12.5h5"/>',
  hand: '<path d="M7 12V6.5a1.5 1.5 0 0 1 3 0V11M10 11V4.5a1.5 1.5 0 0 1 3 0V11M13 11V5.5a1.5 1.5 0 0 1 3 0V13M16 10.5a1.5 1.5 0 0 1 3 0V15a6 6 0 0 1-6 6h-1a6 6 0 0 1-5-3l-3-5 1.6-1L7 14"/>',
  door: '<path d="M6 21V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v17M3 21h18"/><circle cx="14.5" cy="12.5" r=".8"/>',
  knock: '<path d="M12 2.5l2.2 5.3 5.3-2.3-2.3 5.3 5.3 2.2-5.3 2.2 2.3 5.3-5.3-2.3-2.2 5.3-2.2-5.3-5.3 2.3 2.3-5.3-5.3-2.2 5.3-2.2-2.3-5.3 5.3 2.3z"/>',
  skull: '<path d="M12 3a8 8 0 0 0-5 14.2V20h10v-2.8A8 8 0 0 0 12 3z"/><circle cx="9" cy="11" r="1.6"/><circle cx="15" cy="11" r="1.6"/><path d="M10 20v-2.5M14 20v-2.5"/>',
  gun: '<circle cx="12" cy="12" r="7"/><path d="M12 2v6M12 16v6M2 12h6M16 12h6"/>',
  walk: '<path d="M8 20c-1.5 0-2-1.5-1.6-3.5S8 11 9.5 11 11 14 10.5 17 9.5 20 8 20zM16 14c-1.5 0-2-1.5-1.6-3.5S16 5 17.5 5 19 8 18.5 11 17.5 14 16 14z"/>',
  dots: '<circle cx="5.5" cy="12" r="1.6"/><circle cx="12" cy="12" r="1.6"/><circle cx="18.5" cy="12" r="1.6"/>',
  finger: '<path d="M12 4a6 6 0 0 0-6 6v3M12 8a2.5 2.5 0 0 0-2.5 2.5V16M12 8a2.5 2.5 0 0 1 2.5 2.5V19M18 11a6 6 0 0 0-6-6M12 12v6"/>',
  siren: '<path d="M7 19v-6a5 5 0 0 1 10 0v6M5 19h14M12 3v2M4.5 7l1.5 1.2M19.5 7L18 8.2"/>',
};
export const icon = (n, size = 20) =>
  `<svg viewBox="0 0 24 24" width="${size}" height="${size}" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${P[n] || ''}</svg>`;
