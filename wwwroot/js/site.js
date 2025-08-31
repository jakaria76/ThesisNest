// site.js (sidebar behavior + theme toggle)
(function () {
    // Sidebar collapse/expand
    const sidebar = document.getElementById('tnSidebar');
    const collapseBtn = document.getElementById('tnCollapseBtn');
    const mobileBtn = document.getElementById('tnMobileToggle');

    if (collapseBtn) {
        collapseBtn.addEventListener('click', function () {
            sidebar?.classList.toggle('is-collapsed');
            localStorage.setItem('tn-sb-collapsed', sidebar?.classList.contains('is-collapsed') ? '1' : '0');
        });
    }

    // restore saved sidebar state (desktop)
    try {
        const saved = localStorage.getItem('tn-sb-collapsed');
        if (saved === '1' && !window.matchMedia('(max-width: 992px)').matches) {
            sidebar?.classList.add('is-collapsed');
        }
    } catch (e) { /* ignore storage errors */ }

    // mobile open/close
    if (mobileBtn) {
        mobileBtn.addEventListener('click', function () {
            sidebar?.classList.toggle('show');
            document.body.style.overflow = sidebar?.classList.contains('show') ? 'hidden' : '';
        });
    }

    document.addEventListener('click', function (e) {
        if (!window.matchMedia('(max-width: 992px)').matches) return;
        if (!sidebar || !mobileBtn) return;
        const inside = sidebar.contains(e.target) || mobileBtn.contains(e.target);
        if (!inside) {
            sidebar.classList.remove('show');
            document.body.style.overflow = '';
        }
    });
})();

// Theme toggle: set data-theme on <html> and save to localStorage
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const themeToggle = document.getElementById('themeToggleBtn');
        const themeIcon = document.getElementById('themeIcon');
        const root = document.documentElement;

        // read saved theme OR prefered color scheme otherwise
        const saved = localStorage.getItem('tn-theme');
        const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        const initial = saved || (prefersDark ? 'dark' : 'light');
        setTheme(initial);

        function setTheme(theme) {
            root.setAttribute('data-theme', theme);
            document.body.classList.toggle('dark-mode', theme === 'dark');
            if (themeIcon) {
                themeIcon.classList.remove('bi-moon', 'bi-sun');
                themeIcon.classList.add(theme === 'dark' ? 'bi-sun' : 'bi-moon');
            }
            try { localStorage.setItem('tn-theme', theme); } catch (e) { /* ignore */ }
        }

        if (themeToggle) {
            themeToggle.addEventListener('click', function () {
                const current = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
                setTheme(current);
            });
        }
    });
})();
