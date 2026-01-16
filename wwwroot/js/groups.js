function openAddGroupModal() {
    document.getElementById('addGroupModal').classList.add('active');
    document.getElementById('addGroupName').value = '';
    document.getElementById('addGroupNameError').classList.add('hidden');
    document.getElementById('addGroupMessage').classList.add('hidden');
}

function closeAddGroupModal() {
    document.getElementById('addGroupModal').classList.remove('active');
}

window.onclick = function (event) {
    if (event.target.classList.contains('modal')) {
        event.target.classList.remove('active');
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
    } else if (!/^[a-zA-Z0-9\s\-&.()]+$/.test(name)) {
        errors.push('Invalid characters');
    }
    return errors;
}

function checkAndAddGroup() {
    const name = document.getElementById('addGroupName').value.trim();
    const companyId = document.getElementById('addGroupCompanyId').value;
    const errorElement = document.getElementById('addGroupNameError');
    const messageElement = document.getElementById('addGroupMessage');
    const btn = document.getElementById('addGroupBtn');

    errorElement.classList.add('hidden');
    messageElement.classList.add('hidden');

    const errors = validateGroupName(name);
    if (errors.length > 0) {
        errorElement.textContent = errors[0];
        errorElement.classList.remove('hidden');
        return;
    }

    btn.disabled = true;
    btn.innerHTML = 'Adding...';

    $.ajax({
        url: '/Companies/GroupAdd',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ GroupName: name, CompanyID: parseInt(companyId) }),
        success: function (response) {
            if (response.status === "success") {
                messageElement.textContent = 'Success! Reloading...';
                messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-green-50 text-green-700';
                messageElement.classList.remove('hidden');
                setTimeout(() => location.reload(), 800);
            } else {
                messageElement.textContent = response.message || 'Failed to add group';
                messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-red-50 text-red-700';
                messageElement.classList.remove('hidden');
                btn.disabled = false;
                btn.innerHTML = 'Add';
            }
        },
        error: function () {
            messageElement.textContent = 'Error adding group';
            messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-red-50 text-red-700';
            messageElement.classList.remove('hidden');
            btn.disabled = false;
            btn.innerHTML = 'Add';
        }
    });
}
