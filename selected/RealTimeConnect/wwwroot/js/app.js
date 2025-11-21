// Shared frontend JS for login/register/dashboard and SignalR integration
document.addEventListener('DOMContentLoaded', () => {
    // Login page
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        const out = document.getElementById('output');
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const username = document.getElementById('username').value.trim();
            const password = document.getElementById('password').value;
            try {
                const res = await apiPost('/api/auth/login', { username, password });
                if (res.token) {
                    localStorage.setItem('jwt', res.token);
                    setTimeout(() => location.href = '/dashboard.html', 600);
                } else {
                    out.textContent = 'Login failed: unexpected response';
                }
            } catch (err) {
                out.textContent = 'Invalid username or password.';
            }
        });
    }

    // Register page
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        const out = document.getElementById('regOutput');
        const usernameRegex = /^[a-zA-Z][a-zA-Z0-9_]{2,19}$/;
        const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_\-+=<>?{}[\]~]).{8,}$/;
        registerForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const username = document.getElementById('regUsername').value.trim();
            const password = document.getElementById('regPassword').value;

            if (!usernameRegex.test(username)) {
                out.textContent = "Invalid username. Must be 3-20 chars, start with a letter, and contain only letters, numbers, or underscores.";
                return;
            }
            if (!passwordRegex.test(password)) {
                out.textContent = "Invalid password. Must be 8+ chars with upper, lower, number, and special character.";
                return;
            }
            try {
                const res = await fetch('/api/auth/register', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username, password })
                });

                if (res.status === 404) {
                    console.log('Register endpoint not available on server.');
                    return;
                }

                const data = await res.json();
                if (res.ok) {
                    setTimeout(() => location.href = '/login.html', 800);
                } else {
                    console.log('Failed to create account: ' + (data?.message || JSON.stringify(data)));
                }
            } catch (err) {
                console.log('Failed to create account: ' + err);
            }
        });
    }


    // Dashboard page
    const userInfo = document.getElementById('userInfo');
    if (!userInfo) return; // nothing to do if not dashboard

    // Debug log helper
    const debugLogEl = document.getElementById('debugLog');
    function debugLog(msg) {
        console.log('[DEBUG]', msg);
        if (debugLogEl) {
            const div = document.createElement('div');
            div.textContent = new Date().toLocaleTimeString() + ' ' + msg;
            debugLogEl.appendChild(div);
            debugLogEl.scrollTop = debugLogEl.scrollHeight;
            debugLogEl.classList.add('show');
        }
    }

    debugLog('Dashboard loaded');

    // Basic auth check
    const token = localStorage.getItem('jwt');
    if (!token) { debugLog('No token found, redirecting to login'); location.href = '/login.html'; return; }
    debugLog('Token found: ' + token.substring(0, 20) + '...');

    function authHeaders() {
        return { 'Authorization': 'Bearer ' + localStorage.getItem('jwt') };
    }

    // Show username from token
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        userInfo.textContent = payload.unique_name || payload.name || payload.sub || payload.nameidentifier || 'You';
    } catch { userInfo.textContent = 'You'; }

    // UI elements
    const convoListEl = document.getElementById('convoList');
    const messagesEl = document.getElementById('messages');
    const conversationTitle = document.getElementById('conversationTitle');
    const conversationMeta = document.getElementById('conversationMeta');
    const typingIndicator = document.getElementById('typingIndicator');
    const messageInput = document.getElementById('messageInput');
    const sendBtn = document.getElementById('sendBtn');
    const logoutBtn = document.getElementById('logoutBtn');
    const newConvoBtn = document.getElementById('newConvoBtn');
    const searchConvos = document.getElementById('searchConvos');

    let activeConversationId = null;
    let currentConversation = null;
    let conversations = [];

    // SignalR connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/chatHub', { accessTokenFactory: () => localStorage.getItem('jwt') || '' })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    connection.on('ReceiveMessage', (msg) => {
        // msg: { conversationId, senderId, message, timestamp }
        if (msg.conversationId === activeConversationId) {
            // Enrich message object to match API response format
            // Find sender username from the current conversation members
            let enrichedMsg = {
                conversationId: msg.conversationId,
                senderId: msg.senderId,
                content: msg.message || msg.content,
                SentAt: msg.timestamp,
                Sender: msg.Sender // Will be set if available
            };

            // Try to find sender name from current conversation
            if (currentConversation && currentConversation.members) {
                const sender = currentConversation.members.find(m => m.userId === msg.senderId);
                if (sender) enrichedMsg.Sender = sender.username;
            }

            appendMessage(enrichedMsg);
        } else {
            // mark unread on convo list
            const node = document.querySelector(`#convoList li[data-id='${msg.conversationId}']`);
            if (node) node.classList.add('has-unread');
        }
    });

    connection.on('Typing', payload => {
        if (payload.conversationId === activeConversationId) showTyping(payload);
    });

    connection.on('UserPresenceChanged', payload => {
        // update presence in convo list if applicable
        updatePresence(payload.userId, payload.isOnline);
    });

    connection.start().catch(e => {
        debugLog('SignalR connection failed: ' + e.message);
        console.error('SignalR failed to start', e);
    });

    // Load conversations and render
    async function loadConversations() {
        try {
            debugLog('Loading conversations...');
            const res = await fetch('/api/conversations', { headers: { ...authHeaders(), 'Content-Type': 'application/json' } });
            if (!res.ok) {
                const errorBody = await res.text();
                debugLog('Load conversations failed: HTTP ' + res.status + ' - ' + errorBody.substring(0, 100));
                throw new Error('Failed to load, HTTP ' + res.status);
            }
            conversations = await res.json();
            debugLog('Loaded ' + conversations.length + ' conversations');
            renderConvoList(conversations);
        } catch (err) {
            debugLog('Load convos error: ' + err.message);
            console.error('Load convos error', err);
        }
    }

    function renderConvoList(list) {
        convoListEl.innerHTML = '';
        for (const c of list) {
            const li = document.createElement('li');
            li.className = 'convo-item';
            li.dataset.id = c.id;
            const title = document.createElement('div');
            title.className = 'convo-title';
            // For one-on-one: find the OTHER user (not current user)
            const otherMember = c.members && c.members.length ? c.members.find(m => m.userId !== getMyUserId()) : null;
            title.textContent = c.isGroup ? (c.name || 'Group') : (otherMember?.username || 'Chat');
            const sub = document.createElement('div');
            sub.className = 'convo-sub';
            sub.textContent = c.lastMessage ? (c.lastMessage.content || '') : '';
            const right = document.createElement('div');
            right.className = 'convo-right';
            const presence = document.createElement('span');
            presence.className = 'presence';
            presence.dataset.userIds = JSON.stringify(c.members.map(m => m.userId));
            right.appendChild(presence);

            li.appendChild(title);
            li.appendChild(sub);
            li.appendChild(right);

            li.addEventListener('click', () => selectConversation(c.id, c));
            convoListEl.appendChild(li);
        }
    }

    function getMyUserId() {
        try { const p = JSON.parse(atob(localStorage.getItem('jwt').split('.')[1])); return parseInt(p.nameid || p.sub || p.nameIdentifier || p.name || p.unique_name || p.id || 0); } catch { return 0; }
    }

    async function selectConversation(id, convoObj) {
        activeConversationId = id;
        currentConversation = convoObj; // Store the full conversation object for enriching messages
        // highlight selection
        document.querySelectorAll('#convoList li').forEach(n => n.classList.toggle('active', n.dataset.id == id));
        conversationTitle.textContent = convoObj?.name || ('Conversation ' + id);
        typingIndicator.textContent = '';
        await loadMessages(id);
    }

    async function loadMessages(id) {
        messagesEl.innerHTML = '';
        try {
            const res = await fetch(`/api/conversations/${id}/messages`, { headers: authHeaders() });
            if (!res.ok) throw new Error('Failed to load messages');
            const msgs = await res.json();
            for (const m of msgs) appendMessage(m);
            messagesEl.scrollTop = messagesEl.scrollHeight;
        } catch (err) {
            console.error('Load messages error', err);
        }
    }

    function appendMessage(m) {
        // message container
        const el = document.createElement('div');
        // Normalize types when comparing sender id to current user id to avoid string/number mismatches
        const isMe = String(m.senderId) === String(getMyUserId());
        el.className = 'message ' + (isMe ? 'me' : 'other');

        // who (sender name) - shown above bubble for others, optional for self
        const who = document.createElement('div');
        who.className = 'who';
        who.textContent = m.Sender || m.sender || m.senderId;
        // hide sender name for our own messages to match typical chat UX
        if (isMe) who.style.display = 'none';

        // bubble wrapper
        const bubble = document.createElement('div');
        bubble.className = 'bubble';

        const text = document.createElement('div');
        text.className = 'text';
        text.textContent = m.content || m.message || '';

        // Parse timestamp - handle SQL Server format as UTC
        let timeStr = '';
        if (m.SentAt) {
            try {
                let dateObj;
                if (typeof m.SentAt === 'string') {
                    const isoStr = m.SentAt.split('.')[0].replace(' ', 'T') + 'Z';
                    dateObj = new Date(isoStr);
                } else {
                    dateObj = new Date(m.SentAt);
                }
                timeStr = dateObj.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            } catch (e) {
                console.warn('Failed to parse timestamp:', m.SentAt, e);
            }
        }

        const time = document.createElement('div');
        time.className = 'time';
        time.textContent = timeStr;

        bubble.appendChild(text);
        bubble.appendChild(time);

        el.appendChild(who);
        el.appendChild(bubble);
        messagesEl.appendChild(el);
    }

    // Update presence indicators for a user across convo list
    function updatePresence(userId, isOnline) {
        document.querySelectorAll('#convoList .presence').forEach(p => {
            try {
                const ids = JSON.parse(p.dataset.userIds || '[]');
                if (ids.includes(userId)) p.textContent = isOnline ? '●' : '○';
            } catch { }
        });
    }

    // Typing indicator UI
    let typingTimeout = null;
    function showTyping(payload) {
        typingIndicator.textContent = (payload.isTyping ? `User ${payload.userId} is typing...` : '');
        if (typingTimeout) clearTimeout(typingTimeout);
        if (payload.isTyping) typingTimeout = setTimeout(() => typingIndicator.textContent = '', 2000);
    }

    // Send message
    sendBtn?.addEventListener('click', async () => {
        const text = messageInput.value.trim();
        if (!text || !activeConversationId) return;
        try {
            await connection.invoke('SendMessage', activeConversationId, text);
            messageInput.value = '';
        } catch (err) { console.error('Send failed', err); }
    });

    // Typing detection
    if (messageInput) {
        let typingTimer = 0; let isTyping = false;
        messageInput.addEventListener('input', () => {
            if (!activeConversationId) return;
            if (!isTyping) { isTyping = true; connection.invoke('SendTyping', activeConversationId, true).catch(() => { }); }
            clearTimeout(typingTimer);
            typingTimer = setTimeout(() => { isTyping = false; connection.invoke('SendTyping', activeConversationId, false).catch(() => { }); }, 1200);
        });
    }

    // New conversation modal flow: select one-or-many contacts and optional name
    const newConvoModal = document.getElementById('newConvoModal');
    const contactListModal = document.getElementById('contactListModal');
    const noContactsMsg = document.getElementById('noContactsMsg');
    const convoNameModal = document.getElementById('convoNameModal');
    const modalError = document.getElementById('modalError');
    const closeModalBtn = document.getElementById('closeModalBtn');
    const cancelModalBtn = document.getElementById('cancelModalBtn');
    const createConvoBtn = document.getElementById('createConvoBtn');

    let modalContacts = [];
    let selectedInModal = new Set();

    function openNewConvoModal() {
        try {
            console.debug('[UI] openNewConvoModal called');
            if (!newConvoModal) { console.warn('newConvoModal element not found'); return; }
            if (modalError) modalError.textContent = '';
            if (convoNameModal) convoNameModal.value = '';
            if (contactListModal) contactListModal.innerHTML = '';
            selectedInModal = new Set();
            awaitLoadContactsIntoModal();
            newConvoModal.classList.add('show');
        } catch (err) {
            console.error('openNewConvoModal error', err);
        }
    }

    // wrapper to call async loadContacts safely from non-async function
    function awaitLoadContactsIntoModal() {
        loadContactsIntoModal().catch(e => { console.error('loadContactsIntoModal failed', e); });
    }

    function closeNewConvoModal() {
        newConvoModal.classList.remove('show');
    }

    async function loadContactsIntoModal() {
        try {
            const res = await fetch('/api/contacts', { headers: authHeaders() });
            if (!res.ok) throw new Error('Failed to load contacts');
            const contacts = await res.json();
            modalContacts = contacts;
            contactListModal.innerHTML = '';
            if (!contacts || contacts.length === 0) {
                noContactsMsg.style.display = 'block';
                contactListModal.classList.remove('show');
                return;
            }
            noContactsMsg.style.display = 'none';
            // render checkboxes
            for (const c of contacts) {
                const li = document.createElement('li');
                li.className = 'search-result-item';
                const label = document.createElement('label');
                // label.style.display = 'flex';
                label.style.alignItems = 'center';
                const cb = document.createElement('input');
                cb.type = 'checkbox';
                cb.dataset.id = c.contactUserId;
                cb.value = c.username; // store username in the input value
                cb.id = `contact-checkbox-${c.contactUserId}`;
                label.appendChild(cb);
                label.appendChild(document.createTextNode(c.username));
                li.appendChild(label);
                contactListModal.appendChild(li);
            }
            // ensure the list is visible
            contactListModal.classList.add('show');
        } catch (err) {
            console.error('Load contacts for modal error', err);
            modalError.textContent = 'Failed to load contacts';
        }
    }

    // Handle create conversation from modal
    createConvoBtn?.addEventListener('click', async () => {
        modalError.textContent = '';
        try {
            const checked = Array.from(contactListModal.querySelectorAll('input[type=checkbox]:checked'));
            if (checked.length === 0) { modalError.textContent = 'Select one or more contacts'; return; }

            const memberIds = checked.map(cb => parseInt(cb.dataset.id));
            // Ensure we don't include duplicates and include current user will be added server-side
            const convoName = (convoNameModal.value || '').trim() || memberIds.map(id => {
                const c = modalContacts.find(x => x.contactUserId === id);
                return c ? c.username : id;
            }).join(', ');

            createConvoBtn.disabled = true;
            const res = await fetch('/api/conversations', {
                method: 'POST',
                headers: { ...authHeaders(), 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: convoName, memberIds })
            });
            if (!res.ok) {
                const body = await res.text();
                throw new Error(body || 'Failed to create conversation');
            }
            const data = await res.json();
            closeNewConvoModal();
            await loadConversations();
            // Try to open the newly created conversation (handle camelCase or PascalCase)
            const createdId = data?.id || data?.Id;
            if (createdId) selectConversation(createdId, conversations.find(c => c.id == createdId));
        } catch (err) {
            console.error('Create convo error', err);
            modalError.textContent = (err && err.message) || 'Failed to create conversation';
        } finally {
            createConvoBtn.disabled = false;
        }
    });

    // wire modal open/close
    newConvoBtn?.addEventListener('click', openNewConvoModal);
    closeModalBtn?.addEventListener('click', closeNewConvoModal);
    cancelModalBtn?.addEventListener('click', closeNewConvoModal);
    newConvoModal?.addEventListener('click', (e) => { if (e.target === newConvoModal) closeNewConvoModal(); });

    searchConvos?.addEventListener('input', (e) => {
        const q = e.target.value.toLowerCase();
        document.querySelectorAll('#convoList li').forEach(li => {
            const title = li.querySelector('.convo-title')?.textContent?.toLowerCase() || '';
            li.style.display = title.includes(q) ? '' : 'none';
        });
    });

    logoutBtn?.addEventListener('click', () => { localStorage.removeItem('jwt'); location.href = '/login.html'; });

    // Initial load
    loadConversations();
});

// Helper: POST and return parsed JSON or throw (without auth header, for login/register)
async function apiPost(url, body) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!res.ok) throw new Error('HTTP ' + res.status + ' - ' + await res.text());
    return res.json();
}