document.addEventListener('DOMContentLoaded', () => {
    const userInfo = document.getElementById('userInfo');
    const logoutBtn = document.getElementById('logoutBtn');
    const userSearch = document.getElementById('userSearch');
    const searchResults = document.getElementById('searchResults');
    const contactsList = document.getElementById('contactsList');
    const contactListSearch = document.getElementById('contactListSearch');
    const addError = document.getElementById('addError');

    // Basic auth check
    const token = localStorage.getItem('jwt');
    if (!token) { location.href = '/login.html'; return; }

    function authHeaders() {
        return { 'Authorization': 'Bearer ' + localStorage.getItem('jwt') };
    }

    // Show username from token
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        userInfo.textContent = payload.unique_name || payload.name || payload.sub || payload.nameidentifier || 'You';
    } catch { userInfo.textContent = 'You'; }

    logoutBtn?.addEventListener('click', () => {
        localStorage.removeItem('jwt');
        location.href = '/login.html';
    });

    // Load and display contacts
    async function loadContacts() {
        try {
            const res = await fetch('/api/contacts', { headers: authHeaders() });
            if (!res.ok) throw new Error('Failed to load contacts');

            const contacts = await res.json();
            renderContactsList(contacts);
        } catch (err) {
            console.error('Load contacts error:', err);
            contactsList.innerHTML = '<li class="error">Failed to load contacts</li>';
        }
    }

    function renderContactsList(contacts) {
        contactsList.innerHTML = '';

        if (contacts.length === 0) {
            const li = document.createElement('li');
            li.className = 'empty-state';
            li.textContent = 'No contacts yet. Add one from the form below!';
            contactsList.appendChild(li);
            return;
        }

        contacts.forEach(contact => {
            const li = document.createElement('li');
            li.className = 'contact-item';
            li.dataset.contactId = contact.contactUserId;

            const nameDiv = document.createElement('div');
            nameDiv.className = 'contact-name';
            nameDiv.textContent = contact.username;

            const dateDiv = document.createElement('div');
            dateDiv.className = 'contact-date';
            dateDiv.textContent = 'Added ' + new Date(contact.createdAt).toLocaleDateString();

            const removeBtn = document.createElement('button');
            removeBtn.className = 'btn-remove';
            removeBtn.textContent = 'Remove';
            removeBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                removeContact(contact.contactUserId);
            });

            li.appendChild(nameDiv);
            li.appendChild(dateDiv);
            li.appendChild(removeBtn);
            contactsList.appendChild(li);
        });
    }

    // Search contacts in list
    contactListSearch?.addEventListener('input', (e) => {
        const q = e.target.value.toLowerCase();
        document.querySelectorAll('#contactsList .contact-item').forEach(li => {
            const name = li.querySelector('.contact-name')?.textContent?.toLowerCase() || '';
            li.style.display = name.includes(q) ? '' : 'none';
        });
    });

    // Search users to add as contacts
    let searchTimeout = null;
    userSearch?.addEventListener('input', async (e) => {
        const query = e.target.value.trim();

        clearTimeout(searchTimeout);
        if (query.length < 1) {
            searchResults.classList.remove('show');
            return;
        }

        searchTimeout = setTimeout(async () => {
            try {
                const res = await fetch(`/api/contacts/search?query=${encodeURIComponent(query)}`, {
                    headers: authHeaders()
                });
                if (!res.ok) throw new Error('Search failed');

                const results = await res.json();
                searchResults.innerHTML = '';
                results.forEach(user => {
                    const li = document.createElement('li');
                    li.dataset.userId = user.id;
                    li.textContent = user.username;
                    li.addEventListener('click', () => addContact(user.id, user.username, li));
                    searchResults.appendChild(li);
                });
                searchResults.classList.toggle('show', results.length > 0);
            } catch (err) {
                console.error('Search error:', err);
            }
        }, 300);
    });

    // Add contact
    async function addContact(userId, username, resultElement) {
        try {
            addError.textContent = '';
            const res = await fetch('/api/contacts', {
                method: 'POST',
                headers: { ...authHeaders(), 'Content-Type': 'application/json' },
                body: JSON.stringify({ contactUserId: userId })
            });

            const data = await res.json();
            if (!res.ok) {
                addError.textContent = data.message || 'Failed to add contact';
                return;
            }

            userSearch.value = '';
            searchResults.classList.remove('show');
            searchResults.innerHTML = '';
            await loadContacts();
        } catch (err) {
            addError.textContent = 'Error adding contact: ' + err.message;
        }
    }

    // Remove contact
    async function removeContact(contactUserId) {
        if (!confirm('Remove this contact?')) return;

        try {
            const res = await fetch(`/api/contacts/${contactUserId}`, {
                method: 'DELETE',
                headers: authHeaders()
            });

            if (!res.ok) throw new Error('Failed to remove contact');

            await loadContacts();
        } catch (err) {
            console.error('Remove contact error:', err);
            alert('Failed to remove contact: ' + err.message);
        }
    }

    // Initial load
    loadContacts();
});
