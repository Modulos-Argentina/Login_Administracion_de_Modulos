// =========================================================================
// IESTP Argentina - Gestor de Temas (Modo Oscuro / Modo Claro)
// Conserva el celeste institucional de la marca en ambos modos
// =========================================================================

(function() {
    // Inicialización inmediata para evitar parpadeos (FOUC)
    const storedTheme = localStorage.getItem('app-theme') || 'dark';
    document.documentElement.setAttribute('data-theme', storedTheme);
})();

function getTheme() {
    return document.documentElement.getAttribute('data-theme') || localStorage.getItem('app-theme') || 'dark';
}

function setTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('app-theme', theme);
    updateThemeToggleUI(theme);
}

function toggleTheme() {
    const current = getTheme();
    const next = current === 'dark' ? 'light' : 'dark';
    setTheme(next);
}

function updateThemeToggleUI(theme) {
    const isLight = theme === 'light';
    const toggleBtns = document.querySelectorAll('.theme-toggle-btn, .btn-theme-toggle');
    
    toggleBtns.forEach(btn => {
        const textEl = btn.querySelector('.theme-toggle-text');
        if (textEl) {
            textEl.textContent = isLight ? 'Modo Oscuro' : 'Modo Claro';
        }
        const actionTitle = isLight ? 'Cambiar a Modo Oscuro' : 'Cambiar a Modo Claro';
        btn.setAttribute('title', actionTitle);
        btn.setAttribute('aria-label', actionTitle);
    });
}

// Escuchar cambios de preferencia del sistema si el usuario no ha forzado un tema manualmente
if (window.matchMedia) {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
        if (!localStorage.getItem('app-theme')) {
            setTheme(e.matches ? 'dark' : 'light');
        }
    });
}

// =========================================================================
// IESTP Argentina - Gestor de Barra Lateral / Paneles Ocultables (PC & Móvil)
// =========================================================================

const SIDEBAR_STORAGE_KEY = 'sidebar-collapsed';
const DESKTOP_BREAKPOINT = 992;

function isDesktopViewport() {
    return window.innerWidth >= DESKTOP_BREAKPOINT;
}

function toggleSidebar() {
    if (isDesktopViewport()) {
        toggleSidebarDesktop();
    } else {
        toggleSidebarMobile();
    }
}

function toggleSidebarDesktop() {
    const isCollapsed = document.documentElement.classList.contains('sidebar-collapsed') ||
                        document.body.classList.contains('sidebar-collapsed');
    setSidebarDesktopState(!isCollapsed);
}

function setSidebarDesktopState(collapsed) {
    if (collapsed) {
        document.documentElement.classList.add('sidebar-collapsed');
        document.body.classList.add('sidebar-collapsed');
        localStorage.setItem(SIDEBAR_STORAGE_KEY, 'true');
    } else {
        document.documentElement.classList.remove('sidebar-collapsed');
        document.body.classList.remove('sidebar-collapsed');
        localStorage.setItem(SIDEBAR_STORAGE_KEY, 'false');
    }
    updateSidebarUI();
}

function toggleSidebarMobile() {
    const isOpen = document.body.classList.contains('sidebar-mobile-open');
    if (isOpen) {
        closeSidebarMobile();
    } else {
        openSidebarMobile();
    }
}

function openSidebarMobile() {
    document.documentElement.classList.add('sidebar-mobile-open');
    document.body.classList.add('sidebar-mobile-open');
    document.body.style.overflow = 'hidden';
    updateSidebarUI();
}

function closeSidebarMobile() {
    document.documentElement.classList.remove('sidebar-mobile-open');
    document.body.classList.remove('sidebar-mobile-open');
    document.body.style.overflow = '';
    updateSidebarUI();
}

function updateSidebarUI() {
    const isDesktop = isDesktopViewport();
    const toggleBtns = document.querySelectorAll('#sidebarToggleBtn, .btn-sidebar-toggle');
    const floatingBtn = document.getElementById('sidebarFloatingExpandBtn');

    if (isDesktop) {
        const isCollapsed = document.documentElement.classList.contains('sidebar-collapsed') ||
                            document.body.classList.contains('sidebar-collapsed');

        toggleBtns.forEach(btn => {
            if (isCollapsed) {
                btn.classList.add('active');
                btn.setAttribute('title', 'Mostrar panel lateral (Ctrl+B)');
                btn.setAttribute('aria-label', 'Mostrar panel lateral (Ctrl+B)');
            } else {
                btn.classList.remove('active');
                btn.setAttribute('title', 'Ocultar panel lateral (Ctrl+B)');
                btn.setAttribute('aria-label', 'Ocultar panel lateral (Ctrl+B)');
            }
        });

        if (floatingBtn) {
            floatingBtn.setAttribute('title', 'Mostrar panel lateral (Ctrl+B)');
        }
    } else {
        const isOpen = document.body.classList.contains('sidebar-mobile-open');
        toggleBtns.forEach(btn => {
            if (isOpen) {
                btn.classList.add('active');
                btn.setAttribute('title', 'Cerrar menú');
                btn.setAttribute('aria-label', 'Cerrar menú');
            } else {
                btn.classList.remove('active');
                btn.setAttribute('title', 'Abrir menú');
                btn.setAttribute('aria-label', 'Abrir menú');
            }
        });
    }
}

// Atajos de teclado: Ctrl+B / Cmd+B para alternar panel, Escape para cerrar en móvil
document.addEventListener('keydown', function(e) {
    if ((e.ctrlKey || e.metaKey) && (e.key === 'b' || e.key === 'B')) {
        const activeTag = document.activeElement ? document.activeElement.tagName.toLowerCase() : '';
        if (activeTag === 'input' || activeTag === 'textarea' || activeTag === 'select') {
            return;
        }
        e.preventDefault();
        toggleSidebar();
    } else if (e.key === 'Escape') {
        if (!isDesktopViewport() && document.body.classList.contains('sidebar-mobile-open')) {
            closeSidebarMobile();
        }
    }
});

// Manejo inteligente de redimensionamiento de pantalla
let resizeTimeout;
window.addEventListener('resize', function() {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(function() {
        if (isDesktopViewport()) {
            if (document.body.classList.contains('sidebar-mobile-open')) {
                closeSidebarMobile();
            }
            const savedCollapsed = localStorage.getItem(SIDEBAR_STORAGE_KEY) === 'true';
            setSidebarDesktopState(savedCollapsed);
        } else {
            document.documentElement.classList.remove('sidebar-collapsed');
            document.body.classList.remove('sidebar-collapsed');
        }
        updateSidebarUI();
    }, 100);
});

// Inicialización general al cargar el documento
document.addEventListener('DOMContentLoaded', function() {
    updateThemeToggleUI(getTheme());

    if (isDesktopViewport()) {
        const savedCollapsed = localStorage.getItem(SIDEBAR_STORAGE_KEY) === 'true';
        setSidebarDesktopState(savedCollapsed);
    } else {
        closeSidebarMobile();
    }
    updateSidebarUI();

    // En dispositivos móviles, auto-cerrar sidebar al pulsar enlaces de navegación
    const navLinks = document.querySelectorAll('.sidebar-nav a, .sidebar-nav .nav-item, .sidebar-nav .nav-item-jefe, .sidebar-nav [class*="nav-item"]');
    navLinks.forEach(link => {
        link.addEventListener('click', function() {
            if (!isDesktopViewport() && document.body.classList.contains('sidebar-mobile-open')) {
                closeSidebarMobile();
            }
        });
    });
});
