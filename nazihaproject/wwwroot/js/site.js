// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

window.appLog = function(message) {
    console.log('[App]', message);
};

window.loadingManager = {
    timeoutId: null,
    show: function() {
        $('.preloader').fadeIn();
        this.setFailSafe();
    },
    hide: function() {
        $('.preloader').fadeOut();
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
            this.timeoutId = null;
        }
    },
    setFailSafe: function(timeout = 8000) {
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
        }
        this.timeoutId = setTimeout(() => {
            console.log("[Loading] Safety timeout triggered - forcing preloader to hide");
            $('.preloader').fadeOut();
            $('#loadingOverlay').fadeOut();
            this.timeoutId = null;
        }, timeout);
    }
};

window.showLoading = function(show = true) {
    if (show) {
        window.loadingManager.show();
    } else {
        window.loadingManager.hide();
    }
};

$(document).on('click', '.view-details', function() {
    const id = $(this).data('id');
    const modalElement = document.getElementById(`details-${id}`);
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
});
window.setTimeout(function() {
    $(".alert").fadeTo(500, 0).slideUp(500, function(){
        $(this).remove();
    });
}, 5000);
$(document).ready(function() {
    console.log("[Site] Document ready event in site.js");
    
    window.onerror = function(message, source, lineno, colno, error) {
        console.error("[Error]", message, "at", source, "line:", lineno, error);
        window.loadingManager.hide();
        return false;
    };

    window.loadingManager.hide();
    $('#loadingOverlay').fadeOut();

    $('.nav-sidebar a.nav-link').on('click', function(e) {
        const url = $(this).attr('href');
        if (url && url !== '#') {
            window.loadingManager.show();
            
            window.location.href = url;
        }
    });
});
