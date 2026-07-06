// Logo: abgerundeter Kraftstoff-Tropfen mit ansteigender Balkenlinie im Negativraum.
export function Logo({ size = 34, drop = 'var(--accent)', inner = 'var(--card)' }: { size?: number; drop?: string; inner?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 48 48" fill="none" aria-label="Tankbuch Logo">
      <path d="M24 3.5C24 3.5 9.5 19.5 9.5 29.5C9.5 37.5 16 44 24 44C32 44 38.5 37.5 38.5 29.5C38.5 19.5 24 3.5 24 3.5Z" fill={drop} />
      <rect x="15.5" y="28.5" width="4.4" height="8.5" rx="1.8" fill={inner} />
      <rect x="21.8" y="24.5" width="4.4" height="12.5" rx="1.8" fill={inner} />
      <rect x="28.1" y="20.5" width="4.4" height="16.5" rx="1.8" fill={inner} />
    </svg>
  );
}

// Umriss-Variante für Empty-States
export function LogoOutline({ size = 44 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 48 48" fill="none" style={{ opacity: 0.9 }}>
      <path d="M24 3.5C24 3.5 9.5 19.5 9.5 29.5C9.5 37.5 16 44 24 44C32 44 38.5 37.5 38.5 29.5C38.5 19.5 24 3.5 24 3.5Z"
        fill="var(--card2)" stroke="var(--text3)" strokeWidth="1.5" />
    </svg>
  );
}
