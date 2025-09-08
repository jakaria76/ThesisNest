// wwwroot/js/comm.js
// ---------------------------------------------------------
// SignalR + WebRTC (Audio/Video) + Rich Chat (emoji, file, audio/video)
// Uses special token for attachments: [[file|contentType|url|name|size|durationMs=..|w=..|h=..]]
// ---------------------------------------------------------

window.Comm = (function () {
    let hub = null, pc = null, localStream = null, currentCallId = null;
    let threadId = null, cachedIce = null, startingCall = false;

    // ---------- utils ----------
    function getCsrf() {
        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta?.getAttribute('content') || '';
    }
    function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

    // ---------- ICE (TURN/STUN) ----------
    async function getIceServers() {
        if (cachedIce) return cachedIce;
        try {
            const res = await fetch('/rtc/ice', { credentials: 'same-origin' });
            if (res.ok) {
                const data = await res.json();
                const arr = Array.isArray(data.iceServers) ? data.iceServers : [];
                cachedIce = arr.map(s => ({ urls: s.urls, username: s.username, credential: s.credential }))
                    .filter(s => s.urls && s.urls.length);
            }
        } catch (_) { }
        if (!cachedIce || cachedIce.length === 0) {
            cachedIce = [{ urls: ['stun:stun.l.google.com:19302'] }];
        }
        return cachedIce;
    }

    // ---------- SignalR ----------
    async function ensureHubStarted(hubUrl = '/hubs/comm') {
        if (!window.signalR) throw new Error('SignalR client not loaded. Include signalr.min.js before comm.js.');
        if (!hub) {
            hub = new signalR.HubConnectionBuilder().withUrl(hubUrl).withAutomaticReconnect().build();

            // WebRTC signaling
            hub.on('receiveOffer', async (offer) => {
                try { pc?.close(); } catch { }
                pc = await createPc();
                await attachLocalTracks(true);
                await pc.setRemoteDescription(offer);
                const answer = await pc.createAnswer();
                await pc.setLocalDescription(answer);
                await hub.invoke('SendAnswer', threadId, answer);
            });
            hub.on('receiveAnswer', async (answer) => { try { if (pc) await pc.setRemoteDescription(answer); } catch (e) { console.error(e); } });
            hub.on('receiveIceCandidate', async (cand) => { try { await pc?.addIceCandidate(cand); } catch (e) { console.warn('Bad ICE', e); } });

            hub.on('callStarted', (c) => { currentCallId = c.callId; });
            hub.on('callEnded', () => { currentCallId = null; cleanup(); });

            // Chat
            hub.on('receiveMessage', (msg) => addMsgToUi(msg));
        }
        if (hub.state !== 'Connected') await hub.start();
    }

    // ---------- RTCPeerConnection ----------
    async function createPc() {
        const iceServers = await getIceServers();
        const peer = new RTCPeerConnection({ iceServers });

        peer.onicecandidate = (e) => { if (e.candidate && hub) hub.invoke('SendIceCandidate', threadId, e.candidate).catch(() => { }); };
        peer.ontrack = (e) => { const remote = document.getElementById('remote'); if (remote) remote.srcObject = e.streams[0]; };
        peer.onconnectionstatechange = () => {
            const st = peer.connectionState;
            if (st === 'failed' || st === 'disconnected' || st === 'closed') cleanup();
        };
        return peer;
    }

    async function attachLocalTracks(video) {
        if (!navigator.mediaDevices?.getUserMedia) { alert('This browser does not support media devices.'); throw new Error('getUserMedia not supported'); }
        const constraints = { audio: true, video: !!video };
        try {
            localStream = await navigator.mediaDevices.getUserMedia(constraints);
        } catch (err) {
            console.error('[getUserMedia]', err);
            const notFound = err && (err.name === 'NotFoundError' || err.name === 'NotReadableError');
            if (video && notFound) { alert('Camera not found. Starting audio-only.'); return attachLocalTracks(false); }
            alert('Mic/Camera not found or permission blocked.');
            throw err;
        }
        const local = document.getElementById('local');
        if (local) local.srcObject = localStream;
        localStream.getTracks().forEach(t => pc.addTrack(t, localStream));
    }

    // ---------- Public: Thread/Call ----------
    async function joinThread(id) {
        threadId = id;
        await getIceServers();
        await ensureHubStarted('/hubs/comm');
        await hub.invoke('JoinThread', id);
    }

    async function startCall(video) {
        if (startingCall) return;
        startingCall = true;
        try {
            await ensureHubStarted('/hubs/comm');
            try { pc?.close(); } catch { }
            pc = await createPc();
            await attachLocalTracks(!!video);
            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);
            await hub.invoke('StartCall', threadId, video ? 2 : 1);
            await hub.invoke('SendOffer', threadId, offer);
        } finally {
            startingCall = false;
        }
    }

    async function endCall() {
        try { if (currentCallId && hub) await hub.invoke('EndCall', currentCallId); } catch { }
        cleanup();
    }

    function cleanup() {
        try { localStream?.getTracks().forEach(t => t.stop()); } catch { }
        localStream = null;
        if (pc) { try { pc.onicecandidate = null; pc.ontrack = null; pc.close(); } catch { } pc = null; }
    }

    // ---------- Chat: load / plain text ----------
    async function loadMessages() {
        if (!threadId) return;
        try {
            const res = await fetch(`/Video/Messages?threadId=${encodeURIComponent(threadId)}&take=100`, { credentials: 'same-origin' });
            if (!res.ok) return;
            const data = await res.json();
            const box = document.getElementById('chat-messages');
            box.innerHTML = '';
            (data.items || []).forEach(addMsgToUi);
            box.scrollTop = box.scrollHeight;
        } catch (e) { console.error(e); }
    }

    async function sendMessage(text) {
        const t = (text || '').trim();
        if (!t || !hub) return;
        await hub.invoke('SendMessage', threadId, t);
    }

    // ---------- Chat: attachments ----------
    function buildFileToken(payload) {
        // payload: {contentType,url,name,size, durationMs?, w?, h?}
        const parts = [
            '[[file',
            payload.contentType || '',
            payload.url || '',
            payload.name || '',
            String(payload.size || 0),
            payload.durationMs != null ? `durationMs=${payload.durationMs}` : '',
            payload.w != null ? `w=${payload.w}` : '',
            payload.h != null ? `h=${payload.h}` : ''
        ];
        return parts.join('|') + ']]';
    }

    function parseFileToken(text) {
        // returns null or {contentType,url,name,size,durationMs,w,h}
        if (!text?.startsWith('[[')) return null;
        const m = text.match(/^\[\[file\|([^|]*)\|([^|]*)\|([^|]*)\|([^|]*)(?:\|([^|\]]*))?(?:\|([^|\]]*))?(?:\|([^|\]]*))?\]\]$/);
        if (!m) return null;
        const extras = {};
        [m[5], m[6], m[7]].filter(Boolean).forEach(kv => {
            const [k, v] = kv.split('=');
            if (!k) return;
            extras[k] = v ? decodeURIComponent(v) : '';
        });
        return {
            contentType: m[1],
            url: m[2],
            name: m[3],
            size: Number(m[4] || 0),
            durationMs: extras.durationMs ? Number(extras.durationMs) : undefined,
            w: extras.w ? Number(extras.w) : undefined,
            h: extras.h ? Number(extras.h) : undefined
        };
    }

    async function sendAttachment(file) {
        if (!file) return;
        // optional: quick size/type guard
        if (file.size > 30_000_000) { alert('Max 30 MB'); return; }

        const fd = new FormData();
        fd.append('threadId', String(threadId));
        fd.append('file', file, file.name);

        const res = await fetch('/Video/Upload', {
            method: 'POST',
            headers: { 'RequestVerificationToken': getCsrf() },
            body: fd,
            credentials: 'same-origin'
        });
        if (!res.ok) { alert('Upload failed'); return; }
        const data = await res.json();
        if (!data?.ok) { alert('Upload failed'); return; }

        const token = buildFileToken({
            contentType: data.contentType,
            url: data.url,
            name: data.originalName,
            size: data.size,
            durationMs: data.durationMs,
            w: data.width,
            h: data.height
        });
        await sendMessage(token);
    }

    // ---------- Recording ----------
    async function startAudioRecord() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            const rec = new MediaRecorder(stream, { mimeType: 'audio/webm' });
            const chunks = [];
            rec.ondataavailable = e => { if (e.data?.size) chunks.push(e.data); };
            rec.onstop = async () => {
                const blob = new Blob(chunks, { type: 'audio/webm' });
                const file = new File([blob], `voice-${Date.now()}.webm`, { type: 'audio/webm' });
                // rough duration (not exact, but ok)
                const payload = { file, durationMs: undefined };
                // try reading duration via <audio>
                try {
                    const url = URL.createObjectURL(blob);
                    const a = new Audio();
                    a.src = url;
                    await a.play().catch(() => { });
                    await sleep(50);
                    payload.durationMs = Math.round((a.duration || 0) * 1000);
                    a.pause(); URL.revokeObjectURL(url);
                } catch { }
                await sendAttachmentWithMeta(file, { durationMs: payload.durationMs });
                stream.getTracks().forEach(t => t.stop());
            };

            rec.start();
            alert('Recording... click OK to stop.');
            rec.stop();
        } catch (e) {
            console.error(e);
            alert('Microphone not available or blocked.');
        }
    }

    async function startVideoRecord() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true, video: true });
            const rec = new MediaRecorder(stream, { mimeType: 'video/webm' });
            const chunks = [];
            rec.ondataavailable = e => { if (e.data?.size) chunks.push(e.data); };
            rec.onstop = async () => {
                const blob = new Blob(chunks, { type: 'video/webm' });
                const file = new File([blob], `clip-${Date.now()}.webm`, { type: 'video/webm' });

                // try to extract dimensions & duration
                let w, h, durationMs;
                try {
                    const url = URL.createObjectURL(blob);
                    const v = document.createElement('video');
                    v.src = url; await v.play().catch(() => { });
                    await sleep(80);
                    w = v.videoWidth; h = v.videoHeight;
                    durationMs = Math.round((v.duration || 0) * 1000);
                    v.pause(); URL.revokeObjectURL(url);
                } catch { }

                await sendAttachmentWithMeta(file, { width: w, height: h, durationMs });
                stream.getTracks().forEach(t => t.stop());
            };

            rec.start();
            alert('Recording video... click OK to stop (max ~1 min suggested).');
            rec.stop();
        } catch (e) {
            console.error(e);
            alert('Camera/Microphone not available or blocked.');
        }
    }

    async function sendAttachmentWithMeta(file, meta) {
        const fd = new FormData();
        fd.append('threadId', String(threadId));
        fd.append('file', file, file.name);
        if (meta?.durationMs != null) fd.append('durationMs', String(meta.durationMs));
        if (meta?.width != null) fd.append('width', String(meta.width));
        if (meta?.height != null) fd.append('height', String(meta.height));

        const res = await fetch('/Video/Upload', {
            method: 'POST',
            headers: { 'RequestVerificationToken': getCsrf() },
            body: fd,
            credentials: 'same-origin'
        });
        if (!res.ok) { alert('Upload failed'); return; }
        const data = await res.json();
        if (!data?.ok) { alert('Upload failed'); return; }

        const token = buildFileToken({
            contentType: data.contentType,
            url: data.url,
            name: data.originalName,
            size: data.size,
            durationMs: data.durationMs,
            w: data.width,
            h: data.height
        });
        await sendMessage(token);
    }

    // ---------- Chat: render ----------
    function addMsgToUi(msg) {
        const box = document.getElementById('chat-messages');
        if (!box) return;

        const me = document.body.getAttribute('data-userid');
        const meta = box.dataset;

        const mine = (msg.senderUserId === me);
        const senderName =
            msg.senderName
            || (msg.senderUserId === meta.teacheruserid ? meta.teachername
                : msg.senderUserId === meta.studentuserid ? meta.studentname
                    : 'Someone');

        const time = new Date(msg.sentAt).toLocaleString();
        const row = document.createElement('div');
        row.className = 'd-flex mb-2 ' + (mine ? 'justify-content-end' : 'justify-content-start');

        const wrap = document.createElement('div');
        wrap.className = 'chat-bubble';
        const token = parseFileToken(msg.text);

        let bodyHtml = '';
        if (token) {
            // attachment
            const ct = token.contentType || '';
            if (ct.startsWith('image/')) {
                bodyHtml = `<a href="${token.url}" target="_blank" rel="noopener">
                      <img src="${token.url}" alt="${escapeHtml(token.name)}"
                           style="max-width:260px; max-height:200px" class="rounded border">
                    </a>`;
            } else if (ct.startsWith('audio/')) {
                bodyHtml = `<audio controls preload="metadata" src="${token.url}" style="max-width:260px"></audio>
                    <div class="small mt-1">${escapeHtml(token.name || 'audio')}</div>`;
            } else if (ct.startsWith('video/')) {
                bodyHtml = `<video controls preload="metadata" src="${token.url}"
                           style="max-width:260px; max-height:200px" class="rounded border"></video>
                    <div class="small mt-1">${escapeHtml(token.name || 'video')}</div>`;
            } else {
                // generic file
                bodyHtml = `<a class="d-inline-flex align-items-center gap-2"
                       href="${token.url}" target="_blank" download>
                      <i class="bi bi-paperclip"></i>
                      <span>${escapeHtml(token.name || 'file')}</span>
                    </a>`;
            }
        } else {
            // plain text (supports emoji)
            bodyHtml = `<div>${escapeHtml(msg.text).replace(/\n/g, '<br>')}</div>`;
        }

        wrap.innerHTML = `
      <div class="small text-muted ${mine ? 'text-end' : ''}">
        <span class="fw-semibold">${escapeHtml(senderName)}</span>
        <span class="text-muted"> • ${time}</span>
      </div>
      <div class="p-2 rounded-3 ${mine ? 'bg-primary text-white' : 'bg-light border'}">
        ${bodyHtml}
      </div>`;

        row.appendChild(wrap);
        box.appendChild(row);
        box.scrollTop = box.scrollHeight;
    }

    function escapeHtml(s) {
        return (s || '').replace(/[&<>"']/g, m => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[m]));
    }

    // ---------- unload cleanup ----------
    window.addEventListener('beforeunload', () => {
        try { localStream?.getTracks().forEach(t => t.stop()); } catch { }
        try { pc?.close(); } catch { }
    });

    // Public API
    return {
        joinThread,
        startCall,
        endCall,
        cleanup,
        loadMessages,
        sendMessage,
        sendAttachment,
        startAudioRecord,
        startVideoRecord
    };
})();
