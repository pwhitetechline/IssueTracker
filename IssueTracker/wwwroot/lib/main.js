
        // Optional UX: only enable resolution fields when status is Resolved
        const statusEl = document.getElementById('statusSelect');
        const dateResolvedEl = document.getElementById('dateResolved');
        const resNoticeEl = document.getElementById('resolutionNotice');

        function toggleResolution() {
            const resolved = statusEl?.value === 'Resolved';
            dateResolvedEl.disabled = !resolved;
            resNoticeEl.disabled = !resolved;
        }
        toggleResolution();
        statusEl?.addEventListener('change', toggleResolution);
