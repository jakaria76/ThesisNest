(function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .withAutomaticReconnect()
        .build();

    // --- Elements ---
    const messagesEl = document.getElementById('messages');
    const typingEl = document.getElementById('typingIndicator');
    const nameInput = document.getElementById('userName');
    const msgInput = document.getElementById('messageInput');
    const sendBtn = document.getElementById('sendBtn');
    const emojiBtn = document.getElementById('emojiBtn');
    const emojiPicker = document.getElementById('emojiPicker');

    const nowIso = () => new Date().toISOString();
    const fmtTime = iso => new Date(iso).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });

    // --- Safe linkifier ---
    const urlRegex = /\b(https?:\/\/[^\s<>()]+[^\s<>().,!?)]|\bwww\.[^\s<>()]+)\b/g;
    function buildSafeNodes(text) {
        const out = []; let last = 0, m;
        while ((m = urlRegex.exec(text)) !== null) {
            const url = m[0];
            if (m.index > last) out.push(document.createTextNode(text.slice(last, m.index)));
            const a = document.createElement('a');
            a.rel = 'noopener noreferrer nofollow';
            a.target = '_blank';
            a.href = url.startsWith('http') ? url : `https://${url}`;
            a.textContent = url;
            out.push(a);
            last = m.index + url.length;
        }
        if (last < text.length) out.push(document.createTextNode(text.slice(last)));
        return out;
    }

    // --- UI helpers ---
    function scrollBottom() {
        requestAnimationFrame(() => { messagesEl.scrollTop = messagesEl.scrollHeight; });
    }
    function setUiEnabled(v) {
        if (msgInput) msgInput.disabled = !v;
        if (sendBtn) sendBtn.disabled = !v;
        if (v && msgInput) msgInput.focus();
    }

    function addMessage({ user, message, time, isBot = false, optimisticId = null }) {
        const wrap = document.createElement('div');
        wrap.className = `message ${isBot ? 'bot' : 'user'} mb-3`;
        if (optimisticId) wrap.dataset.optimisticId = optimisticId;

        const header = document.createElement('div');
        header.className = 'message-header small text-muted';
        header.textContent = `${user} • ${fmtTime(time)}`;

        const body = document.createElement('div');
        body.className = 'message-body mt-1';

        String(message).split(/\r?\n/).forEach((line, i) => {
            buildSafeNodes(line).forEach(n => body.appendChild(n));
            if (i < message.split(/\r?\n/).length - 1) body.appendChild(document.createElement('br'));
        });

        wrap.appendChild(header);
        wrap.appendChild(body);
        messagesEl.appendChild(wrap);
        scrollBottom();
        return wrap;
    }

    function markFailed(id) {
        const el = messagesEl.querySelector(`[data-optimistic-id="${CSS.escape(id)}"]`);
        if (!el) return;
        el.classList.add('message-failed');
        const h = el.querySelector('.message-header');
        if (h) h.textContent += ' • failed to send';
    }

    // --- SignalR ---
    connection.on('ReceiveMessage', (user, message, time) => {
        const isBot = (user === 'Bot' || user === '🤖 Bot');
        addMessage({ user, message, time: time || nowIso(), isBot });
    });
    connection.on('BotTyping', (isTyping) => {
        if (!typingEl) return;
        typingEl.style.display = isTyping ? 'block' : 'none';
    });

    connection.onreconnecting(() => { setUiEnabled(false); messagesEl?.setAttribute('aria-busy', 'true'); });
    connection.onreconnected(() => { setUiEnabled(true); messagesEl?.removeAttribute('aria-busy'); typingEl.style.display = 'none'; });
    connection.onclose(() => { setUiEnabled(false); messagesEl?.removeAttribute('aria-busy'); typingEl.style.display = 'none'; setTimeout(start, 4000); });

    async function start() {
        try {
            await connection.start();
            setUiEnabled(true);
            scrollBottom();
        } catch (e) {
            console.error('SignalR start error', e);
            setUiEnabled(false);
            setTimeout(start, 3000);
        }
    }

    async function sendCurrent() {
        if (!msgInput) return;
        const text = msgInput.value.trim();
        if (!text) return;
        const name = (nameInput?.value?.trim()) || 'You';
        const optimisticId = `opt-${Date.now()}-${Math.random().toString(36).slice(2)}`;
        addMessage({ user: name, message: text, time: nowIso(), isBot: false, optimisticId });

        try {
            setUiEnabled(false);
            await connection.invoke('SendMessage', name, text);
            msgInput.value = '';
        } catch (e) {
            console.error(e);
            markFailed(optimisticId);
            alert('Send failed');
        } finally {
            setUiEnabled(true);
            msgInput.focus();
        }
    }

    sendBtn?.addEventListener('click', sendCurrent);
    msgInput?.addEventListener('keydown', (e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendCurrent(); } });

    // --- Emoji handling ---
    emojiBtn?.addEventListener('click', (e) => {
        if (!emojiPicker) return;
        const visible = emojiPicker.style.display === 'block';
        emojiPicker.style.display = visible ? 'none' : 'block';
        if (!visible) {
            const r = emojiBtn.getBoundingClientRect();
            emojiPicker.style.position = 'absolute';
            emojiPicker.style.top = (r.bottom + window.scrollY + 6) + 'px';
            emojiPicker.style.left = (r.left + window.scrollX) + 'px';
        }
        e.stopPropagation();
    });
    emojiPicker?.addEventListener('emoji-click', (e) => {
        const ch = e?.detail?.unicode || e?.detail?.emoji?.unicode || '';
        if (!ch) return;
        const s = msgInput.selectionStart ?? msgInput.value.length;
        const t = msgInput.selectionEnd ?? msgInput.value.length;
        msgInput.setRangeText(ch, s, t, 'end');
        msgInput.focus();
    });

    // --- Click outside emoji closes it ---
    document.addEventListener('click', (e) => {
        if (!emojiPicker?.contains(e.target) && e.target !== emojiBtn) {
            emojiPicker.style.display = 'none';
        }
    });

    setUiEnabled(false);
    start();
})();
