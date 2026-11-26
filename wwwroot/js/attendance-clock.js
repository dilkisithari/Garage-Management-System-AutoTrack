// Option 8: Live clock and elapsed shift timer on /Attendance/Clock
(function () {
    'use strict';

    var box = document.querySelector('.att-clock-box');
    var timeEl = document.getElementById('live-clock-time');
    var dateEl = document.getElementById('live-clock-date');
    var elapsedEl = document.getElementById('live-elapsed');

    if (box && timeEl && dateEl) {
        var serverAttr = box.getAttribute('data-server-now');
        var clockInAttr = box.getAttribute('data-clockin-at');
        var offsetMs = 0;

        if (serverAttr) {
            var serverMs = new Date(serverAttr).getTime();
            if (!isNaN(serverMs)) {
                offsetMs = serverMs - Date.now();
            }
        }

        var clockInMs = clockInAttr ? new Date(clockInAttr).getTime() : NaN;

        function pad(n) { return n < 10 ? '0' + n : String(n); }

        function renderClock() {
            var now = new Date(Date.now() + offsetMs);

            var hours = now.getHours();
            var ampm = hours >= 12 ? 'PM' : 'AM';
            var h12 = hours % 12 || 12;
            timeEl.textContent = pad(h12) + ':' + pad(now.getMinutes()) + ':' + pad(now.getSeconds()) + ' ' + ampm;

            dateEl.textContent = now.toLocaleDateString('en-US', {
                weekday: 'long',
                day: '2-digit',
                month: 'long',
                year: 'numeric'
            });

            if (elapsedEl && !isNaN(clockInMs)) {
                var diffSec = Math.max(0, Math.floor((now.getTime() - clockInMs) / 1000));
                var eh = Math.floor(diffSec / 3600);
                var em = Math.floor((diffSec % 3600) / 60);
                var es = diffSec % 60;
                elapsedEl.textContent = pad(eh) + ':' + pad(em) + ':' + pad(es);
            }
        }

        renderClock();
        setInterval(renderClock, 1000);
    }

    // Copy optional shift note into whichever button (Clock In / Clock Out) was submitted
    var noteInput = document.getElementById('att-notes');
    document.querySelectorAll('.att-clock-form').forEach(function (form) {
        form.addEventListener('submit', function () {
            var hidden = form.querySelector('.att-note-hidden');
            if (hidden && noteInput) {
                hidden.value = noteInput.value || '';
            }
        });
    });
})();
