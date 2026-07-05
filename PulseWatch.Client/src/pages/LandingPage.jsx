import { useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import {
  RiRadarLine,
  RiPulseLine,
  RiEyeLine,
  RiSettings3Line,
  RiNotification3Line,
  RiBarChart2Line,
  RiGlobalLine,
} from 'react-icons/ri';
import './LandingPage.css';

const FEATURES = [
  {
    icon: <RiPulseLine size={24} style={{ color: 'var(--primary)' }} />,
    title: 'Real-Time Uptime Monitoring',
    desc: 'Automatically check your websites and APIs every minute. Get instant alerts via in-app notifications and email when something goes wrong.',
  },
  {
    icon: <RiEyeLine size={24} style={{ color: 'var(--primary)' }} />,
    title: 'Deep Browser Checks',
    desc: 'Use a real headless Chromium browser to screenshot your pages, detect JavaScript errors, and verify your site actually loads for users.',
  },
  {
    icon: <RiSettings3Line size={24} style={{ color: 'var(--primary)' }} />,
    title: 'Advanced Request Options',
    desc: 'Monitor private APIs with custom HTTP methods (GET/HEAD/POST), custom headers, and keyword assertions to verify response content.',
  },
  {
    icon: <RiNotification3Line size={24} style={{ color: 'var(--primary)' }} />,
    title: 'Instant Alerts',
    desc: 'Stay notified via in-app SignalR real-time notifications and email alerts the moment a site goes down — or comes back up.',
  },
  {
    icon: <RiBarChart2Line size={24} style={{ color: 'var(--primary)' }} />,
    title: 'Detailed Analytics',
    desc: 'Track uptime percentages, response times, downtime events, and full check history with our intuitive dashboard.',
  },
  {
    icon: <RiGlobalLine size={24} style={{ color: 'var(--primary)' }} />,
    title: 'Public Status Pages',
    desc: 'Share a beautiful, public status page with your customers. Embed SVG badges in your README or website to show live uptime status.',
  },
];

const STATS = [
  { value: '99.9%', label: 'Uptime Tracked' },
  { value: '60s', label: 'Minimum Check Interval' },
  { value: '∞', label: 'Websites You Can Monitor' },
  { value: '24/7', label: 'Always Watching' },
];

export default function LandingPage() {
  const heroRef = useRef(null);
  const featuresRef = useRef(null);

  // Scroll-triggered animation using IntersectionObserver
  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('visible');
          }
        });
      },
      { threshold: 0.1 }
    );

    document.querySelectorAll('.animate-on-scroll').forEach((el) => observer.observe(el));
    return () => observer.disconnect();
  }, []);

  return (
    <div className="landing-root">
      {/* ── NAV ── */}
      <nav className="landing-nav">
        <div className="landing-nav-inner">
          <div className="landing-logo">
            <span className="landing-logo-icon" style={{ display: 'flex', alignItems: 'center' }}>
              <RiRadarLine size={22} style={{ color: 'var(--primary)' }} />
            </span>
            <span className="landing-logo-text">PulseWatch</span>
          </div>
          <div className="landing-nav-links">
            <Link to="/login" className="landing-btn-ghost">Sign In</Link>
            <Link to="/register" className="landing-btn-primary">Get Started Free</Link>
          </div>
        </div>
      </nav>

      {/* ── HERO ── */}
      <section className="landing-hero" ref={heroRef}>
        <div className="landing-hero-glow" />
        <div className="landing-hero-inner">
          <div className="landing-badge animate-on-scroll">
            <span className="landing-badge-dot" />
            Now monitoring thousands of endpoints
          </div>

          <h1 className="landing-headline animate-on-scroll">
            Know Before<br />
            <span className="landing-headline-accent">Your Users Do</span>
          </h1>

          <p className="landing-subhead animate-on-scroll">
            PulseWatch is an uptime monitoring platform that checks your websites every minute,
            sends instant alerts, and gives you beautiful analytics — so downtime never surprises you.
          </p>

          <div className="landing-hero-cta animate-on-scroll">
            <Link to="/register" className="landing-cta-primary">
              Start Monitoring Free →
            </Link>
            <Link to="/login" className="landing-cta-secondary">
              Sign In
            </Link>
          </div>

          {/* Animated status mockup */}
          <div className="landing-mockup animate-on-scroll">
            <div className="mockup-bar">
              <div className="mockup-dot red" />
              <div className="mockup-dot yellow" />
              <div className="mockup-dot green" />
              <span className="mockup-url">pulsewatch.app/dashboard</span>
            </div>
            <div className="mockup-body">
              <div className="mockup-row">
                <span className="mockup-name">My E-Commerce Store</span>
                <span className="mockup-status online">● Online</span>
                <span className="mockup-time">98 ms</span>
                <span className="mockup-uptime">99.9%</span>
              </div>
              <div className="mockup-row">
                <span className="mockup-name">Payments API</span>
                <span className="mockup-status online">● Online</span>
                <span className="mockup-time">132 ms</span>
                <span className="mockup-uptime">100%</span>
              </div>
              <div className="mockup-row">
                <span className="mockup-name">Blog</span>
                <span className="mockup-status offline">● Offline</span>
                <span className="mockup-time">—</span>
                <span className="mockup-uptime downtime">97.2%</span>
              </div>
              <div className="mockup-row">
                <span className="mockup-name">Admin Dashboard</span>
                <span className="mockup-status online">● Online</span>
                <span className="mockup-time">54 ms</span>
                <span className="mockup-uptime">100%</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ── STATS ── */}
      <section className="landing-stats">
        <div className="landing-stats-inner">
          {STATS.map((s) => (
            <div key={s.label} className="landing-stat animate-on-scroll">
              <div className="landing-stat-value">{s.value}</div>
              <div className="landing-stat-label">{s.label}</div>
            </div>
          ))}
        </div>
      </section>

      {/* ── FEATURES ── */}
      <section className="landing-features" ref={featuresRef}>
        <div className="landing-section-inner">
          <div className="landing-section-header animate-on-scroll">
            <h2 className="landing-section-title">Everything you need to monitor with confidence</h2>
            <p className="landing-section-sub">
              From simple HTTP checks to complex API monitoring, PulseWatch has you covered.
            </p>
          </div>
          <div className="landing-features-grid">
            {FEATURES.map((f) => (
              <div key={f.title} className="landing-feature-card animate-on-scroll">
                <div className="landing-feature-icon">{f.icon}</div>
                <h3 className="landing-feature-title">{f.title}</h3>
                <p className="landing-feature-desc">{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── PUBLIC STATUS PAGE SHOWCASE ── */}
      <section className="landing-showcase">
        <div className="landing-section-inner landing-showcase-inner">
          <div className="landing-showcase-text animate-on-scroll">
            <h2 className="landing-section-title">Share your uptime publicly</h2>
            <p className="landing-section-sub">
              Enable a public status page for any website with one click. Share the link with your
              customers, or embed a live SVG badge in your GitHub README.
            </p>
            <div className="landing-badge-preview">
              <code className="landing-code-snippet">
                ![Status](https://pulsewatch.app/api/public/websites/1/badge)
              </code>
            </div>
            <Link to="/register" className="landing-cta-primary" style={{ display: 'inline-block', marginTop: '24px' }}>
              Create your status page →
            </Link>
          </div>
          <div className="landing-showcase-card animate-on-scroll">
            <div className="showcase-status-header">
              <div className="showcase-logo">PW</div>
              <div>
                <div className="showcase-site-name">My SaaS App</div>
                <div className="showcase-site-url">mysaasapp.com</div>
              </div>
              <span className="showcase-badge online">● All Systems Operational</span>
            </div>
            <div className="showcase-stats-row">
              <div className="showcase-stat">
                <div className="showcase-stat-val">99.8%</div>
                <div className="showcase-stat-lbl">30-day uptime</div>
              </div>
              <div className="showcase-stat">
                <div className="showcase-stat-val">142ms</div>
                <div className="showcase-stat-lbl">Avg. response</div>
              </div>
              <div className="showcase-stat">
                <div className="showcase-stat-val">0</div>
                <div className="showcase-stat-lbl">Incidents today</div>
              </div>
            </div>
            <div className="showcase-bars">
              {Array.from({ length: 30 }).map((_, i) => (
                <div key={i} className={`showcase-bar ${i === 7 || i === 21 ? 'down' : 'up'}`} />
              ))}
            </div>
            <div className="showcase-bars-label">
              <span>30 days ago</span><span>Today</span>
            </div>
          </div>
        </div>
      </section>

      {/* ── CTA BANNER ── */}
      <section className="landing-cta-banner">
        <div className="landing-cta-banner-inner animate-on-scroll">
          <h2 className="landing-cta-banner-title">Ready to start monitoring?</h2>
          <p className="landing-cta-banner-sub">
            Sign up free and add your first website in under 60 seconds.
          </p>
          <div className="landing-hero-cta">
            <Link to="/register" className="landing-cta-primary">
              Create Free Account →
            </Link>
            <Link to="/login" className="landing-cta-secondary">
              Sign In
            </Link>
          </div>
        </div>
      </section>

      {/* ── FOOTER ── */}
      <footer className="landing-footer">
        <div className="landing-footer-inner">
          <div className="landing-logo">
            <span className="landing-logo-icon" style={{ display: 'flex', alignItems: 'center' }}>
              <RiRadarLine size={20} style={{ color: 'var(--primary)' }} />
            </span>
            <span className="landing-logo-text">PulseWatch</span>
          </div>
          <p className="landing-footer-copy">
            © {new Date().getFullYear()} PulseWatch. Real-time uptime monitoring for developers and teams.
          </p>
          <div className="landing-footer-links">
            <Link to="/login">Sign In</Link>
            <Link to="/register">Register</Link>
          </div>
        </div>
      </footer>
    </div>
  );
}
