function openAddGroupModal() {
    document.getElementById('addGroupModal').classList.add('active');
    document.getElementById('addGroupName').value = '';
    document.getElementById('addGroupNameError').classList.add('hidden');
    document.getElementById('addGroupMessage').classList.add('hidden');
}

function closeAddGroupModal() {
    const modal = document.getElementById('addGroupModal');
    modal.classList.remove('active');
    modal.classList.add('hidden');
}

window.onclick = function (event) {
    if (event.target.classList.contains('modal')) {
        event.target.classList.remove('active');
        event.target.classList.add('hidden');
    }
};

function validateGroupName(name) {
    const errors = [];
    if (!name || name.trim().length === 0) {
        errors.push('Group name is required');
    } else if (name.trim().length < 3) {
        errors.push('Must be at least 3 characters');
    } else if (name.trim().length > 100) {
        errors.push('Must not exceed 100 characters');
    } else if (!/^[a-zA-Z0-9\s\-&.()_]+$/.test(name)) {
        errors.push('Invalid characters');
    }
    return errors;
}

function checkAndAddGroup() {
    const nameInput = document.getElementById('addGroupName');
    const name = nameInput.value.trim();
    const companyId = document.getElementById('addGroupCompanyId').value;
    const errorElement = document.getElementById('addGroupNameError');
    const messageElement = document.getElementById('addGroupMessage');
    const btn = document.getElementById('addGroupBtn');

    if (btn.classList.contains('processing')) return;

    errorElement.classList.add('hidden');
    messageElement.classList.add('hidden');

    const errors = validateGroupName(name);
    if (errors.length > 0) {
        errorElement.textContent = errors[0];
        errorElement.classList.remove('hidden');
        return;
    }

    btn.disabled = true;
    btn.classList.add('processing');
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Saving...';

    $.ajax({
        url: '/Companies/GroupAdd',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ GroupName: name, CompanyID: parseInt(companyId) }),
        success: function (response) {
            console.log("GroupAdd Success:", response);
            
            closeAddGroupModal();
            
            if (typeof showSuccessMessage === 'function') {
                showSuccessMessage('Group added successfully');
            }
            
            setTimeout(() => location.reload(), 500);
        },
        error: function (xhr) {
            console.error("GroupAdd Error:", xhr.status, xhr.responseText);
            let errorMessage = 'Error adding group. Please try again.';
            
            try {
                const resp = JSON.parse(xhr.responseText);
                if (resp && resp.message) errorMessage = resp.message;
            } catch(e) {}

            messageElement.textContent = errorMessage;
            messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-red-50 text-red-700';
            messageElement.classList.remove('hidden');
            btn.disabled = false;
            btn.classList.remove('processing');
            btn.innerHTML = 'Add';
        }
    });
}

function showSuccessMessage(message) {
    const notification = document.createElement('div');
    notification.className = 'fixed top-4 right-4 bg-green-500 text-white px-4 py-3 rounded-lg shadow-lg z-50 flex items-center space-x-2 animate-in fade-in duration-300';
    notification.style.zIndex = '9999';
    notification.innerHTML = `
        <i class="fas fa-check-circle"></i>
        <span>${message}</span>
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.style.opacity = '0';
        notification.style.transition = 'opacity 0.3s';
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}
