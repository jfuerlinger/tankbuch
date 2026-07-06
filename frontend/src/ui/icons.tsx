import type { ReactNode, SVGProps } from 'react';

type Base = { size?: number; sw?: number } & SVGProps<SVGSVGElement>;

function S({ size = 19, sw = 1.8, children, ...rest }: Base & { children: ReactNode }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth={sw} strokeLinecap="round" strokeLinejoin="round" {...rest}>
      {children}
    </svg>
  );
}

export const IconPlus = (p: Base) => <S sw={2.4} {...p}><path d="M12 5v14M5 12h14" /></S>;
export const IconHome = (p: Base) => <S {...p}><path d="M4 11.2 12 4.5l8 6.7M6 9.8V19h12V9.8" /></S>;
export const IconClock = (p: Base) => <S {...p}><circle cx="12" cy="12" r="8.2" /><path d="M12 7.5V12l3 2" /></S>;
export const IconBars = (p: Base) => <S {...p}><path d="M4 19h16M7 16v-5M12 16V7M17 16v-8" /></S>;
export const IconCar = (p: Base) => <S {...p}><path d="M4.5 16.5V11.2L6.4 6.8h11.2l1.9 4.4v5.3M4.5 11.2h15M7.2 14h.01M16.8 14h.01" /></S>;
export const IconSettings = (p: Base) => (
  <S {...p}>
    <path d="M4 7h7M17.5 7H20M4 12h2.5M11.5 12H20M4 17h10.5" />
    <circle cx="14.5" cy="7" r="2" /><circle cx="9" cy="12" r="2" /><circle cx="17" cy="17" r="2" />
  </S>
);
export const IconLogout = (p: Base) => <S {...p}><path d="M15 4h4v16h-4M10 8l-4 4 4 4M6 12h9" /></S>;
export const IconChevronLeft = (p: Base) => <S sw={2} {...p}><path d="M14 6l-6 6 6 6" /></S>;
export const IconChevronRight = (p: Base) => <S sw={2} {...p}><path d="M10 6l6 6-6 6" /></S>;
export const IconPump = (p: Base) => <S {...p}><path d="M4.5 8.5h3.2l1.6-2.2h5.4l1.6 2.2h3.2V18h-15z" /><circle cx="12" cy="13" r="3" /></S>;
export const IconGauge = (p: Base) => <S {...p}><path d="M5 17.5a8 8 0 1 1 14 0" /><path d="M12 14l3.6-3.6" /></S>;
export const IconCheck = (p: Base) => <S sw={2.6} {...p}><path d="M5 13l4 4L19 7" /></S>;
export const IconEdit = (p: Base) => <S {...p}><path d="M4 20l1-4L16.5 4.5a1.9 1.9 0 0 1 2.7 0l.3.3a1.9 1.9 0 0 1 0 2.7L8 19l-4 1z" /></S>;
export const IconTrash = (p: Base) => <S {...p}><path d="M5 7h14M10 7V5h4v2M7 7l1 13h8l1-13M10 11v5M14 11v5" /></S>;
export const IconClose = (p: Base) => <S sw={2} {...p}><path d="M6 6l12 12M18 6L6 18" /></S>;
export const IconDownload = (p: Base) => <S sw={2} {...p}><path d="M12 4v10M8 10.5l4 4 4-4M5 19h14" /></S>;
export const IconUpload = (p: Base) => <S sw={2} {...p}><path d="M12 14V4M8 7.5l4-4 4 4M5 19h14" /></S>;
export const IconDots = ({ size = 21, ...rest }: Base) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="currentColor" {...rest}>
    <circle cx="5" cy="12" r="1.7" /><circle cx="12" cy="12" r="1.7" /><circle cx="19" cy="12" r="1.7" />
  </svg>
);
