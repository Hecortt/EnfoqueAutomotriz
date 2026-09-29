// obra360 - JS Global

// Helper: obtener JWT del meta tag
function getJwtToken() {
    return document.querySelector('meta[name="jwt-token"]')?.content ?? '';
}

// Helper: fetch con auth
async function apiRequest(url, method = 'GET', body = null) {
    const token = getJwtToken();
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const opts = { method, headers };
    if (body) opts.body = JSON.stringify(body);

    const response = await fetch(url, opts);
    return response;
}

// Toast notifications
function showToast(message, type = 'success') {
    const colors = {
        success: 'bg-success text-white',
        error: 'bg-danger text-white',
        warning: 'bg-warning text-white',
        info: 'bg-info text-white'
    };

    const toast = document.createElement('div');
    toast.className = `fixed bottom-4 right-4 px-4 py-3 rounded-xl shadow-lg z-50 text-sm font-medium ${colors[type]} transition-all duration-300`;
    toast.style.transform = 'translateY(20px)';
    toast.style.opacity = '0';
    toast.textContent = message;
    document.body.appendChild(toast);

    requestAnimationFrame(() => {
        toast.style.transform = 'translateY(0)';
        toast.style.opacity = '1';
    });

    setTimeout(() => {
        toast.style.transform = 'translateY(10px)';
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Confirmar eliminación
function confirmDelete(message, callback) {
    if (confirm(message ?? '¿Estás seguro de eliminar este registro?')) {
        callback();
    }
}

// Inicializar Lucide icons en contenido dinámico
function refreshIcons() {
    if (typeof lucide !== 'undefined') lucide.createIcons();
}
