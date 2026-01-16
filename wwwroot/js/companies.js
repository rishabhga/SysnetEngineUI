function openAddCompanyModal() {
    const modal = document.getElementById('addCompanyModal');
    modal.classList.add('active');
    document.getElementById('addCompanyName').value = '';
    document.getElementById('addCompanyNameError').classList.add('hidden');
    document.getElementById('addCompanyNameError').textContent = '';
    document.getElementById('addCompanyMessage').classList.add('hidden');
    document.getElementById('addCompanyMessage').textContent = '';
    document.getElementById('addCompanyMessage').className = 'hidden';
}

function closeAddCompanyModal() {
    const modal = document.getElementById('addCompanyModal');
    modal.classList.remove('active');
    // Clean everything
    document.getElementById('addCompanyName').value = '';
    document.getElementById('addCompanyNameError').classList.add('hidden');
    document.getElementById('addCompanyNameError').textContent = '';
    document.getElementById('addCompanyMessage').classList.add('hidden');
    document.getElementById('addCompanyMessage').textContent = '';
    document.getElementById('addCompanyMessage').className = 'hidden';
    const btn = document.getElementById('addCompanyBtn');
    btn.disabled = false;
    btn.innerHTML = 'Add';
}

function openEditCompanyModal(id, name) {
    const modal = document.getElementById('editCompanyModal');
    modal.classList.add('active');
    document.getElementById('editCompanyId').value = id;
    document.getElementById('editCompanyName').value = name;
    document.getElementById('editCompanyNameError').classList.add('hidden');
    document.getElementById('editCompanyNameError').textContent = '';
    document.getElementById('editCompanyMessage').classList.add('hidden');
    document.getElementById('editCompanyMessage').textContent = '';
    document.getElementById('editCompanyMessage').className = 'hidden';
}

function closeEditCompanyModal() {
    const modal = document.getElementById('editCompanyModal');
    modal.classList.remove('active');
    // Clean everything
    document.getElementById('editCompanyId').value = '';
    document.getElementById('editCompanyName').value = '';
    document.getElementById('editCompanyNameError').classList.add('hidden');
    document.getElementById('editCompanyNameError').textContent = '';
    document.getElementById('editCompanyMessage').classList.add('hidden');
    document.getElementById('editCompanyMessage').textContent = '';
    document.getElementById('editCompanyMessage').className = 'hidden';
    const btn = document.getElementById('updateCompanyBtn');
    btn.disabled = false;
    btn.innerHTML = 'Update';
}

// Close modal when clicking outside
window.onclick = function (event) {
    if (event.target.classList.contains('modal')) {
        if (event.target.id === 'addCompanyModal') {
            closeAddCompanyModal();
        } else if (event.target.id === 'editCompanyModal') {
            closeEditCompanyModal();
        }
    }
};

function validateCompanyName(name) {
    const errors = [];
    if (!name || name.trim().length === 0) {
        errors.push('Company name is required');
    } else if (name.trim().length < 2) {
        errors.push('Must be at least 2 characters');
    } else if (name.trim().length > 100) {
        errors.push('Must not exceed 100 characters');
    } else if (!/^[a-zA-Z0-9\s\-&.()]+$/.test(name)) {
        errors.push('Invalid characters');
    }
    return errors;
}

function checkAndAddCompany() {
    const name = document.getElementById('addCompanyName').value.trim();
    const errorElement = document.getElementById('addCompanyNameError');
    const messageElement = document.getElementById('addCompanyMessage');
    const btn = document.getElementById('addCompanyBtn');

    errorElement.classList.add('hidden');
    errorElement.textContent = '';
    messageElement.classList.add('hidden');
    messageElement.textContent = '';
    messageElement.className = 'hidden';

    const errors = validateCompanyName(name);
    if (errors.length > 0) {
        errorElement.textContent = errors[0];
        errorElement.classList.remove('hidden');
        return;
    }

    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Checking...';

    $.ajax({
        url: '/Companies/CheckCompanyName?name=' + encodeURIComponent(name),
        type: 'GET',
        success: function (response) {
            if (response.exists) {
                messageElement.textContent = 'Company already exists';
                messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-red-50 text-red-700';
                messageElement.classList.remove('hidden');
                btn.disabled = false;
                btn.innerHTML = 'Add';
            } else {
                addCompany(name, btn, messageElement);
            }
        },
        error: function (xhr, status, error) {
            console.error('Check company error:', error);
            messageElement.textContent = 'Error checking company name';
            messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-yellow-50 text-yellow-700';
            messageElement.classList.remove('hidden');
            btn.disabled = false;
            btn.innerHTML = 'Add';
        }
    });
}

function addCompany(name, btn, messageElement) {
    btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Adding...';

    $.ajax({
        url: '/Companies/CompanyAdd',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ CompanyName: name }),
        success: function (response) {
            console.log('Add company response:', response);
            if (response.status === "success") {
                messageElement.textContent = 'Success! Reloading...';
                messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-green-50 text-green-700';
                messageElement.classList.remove('hidden');
                setTimeout(() => {
                    closeAddCompanyModal();
                    location.reload();
                }, 600);
            } else {
                messageElement.textContent = response.message || 'Failed to add company';
                messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-red-50 text-red-700';
                messageElement.classList.remove('hidden');
                btn.disabled = false;
                btn.innerHTML = 'Add';
            }
        },
        error: function (xhr, status, error) {
            console.error('Add company error:', error, xhr.responseText);
            messageElement.textContent = 'Error adding company';
            messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-red-50 text-red-700';
            messageElement.classList.remove('hidden');
            btn.disabled = false;
            btn.innerHTML = 'Add';
        }
    });
}

function updateCompany() {
    const id = document.getElementById('editCompanyId').value;
    const name = document.getElementById('editCompanyName').value.trim();
    const errorElement = document.getElementById('editCompanyNameError');
    const messageElement = document.getElementById('editCompanyMessage');
    const btn = document.getElementById('updateCompanyBtn');

    errorElement.classList.add('hidden');
    errorElement.textContent = '';
    messageElement.classList.add('hidden');
    messageElement.textContent = '';
    messageElement.className = 'hidden';

    const errors = validateCompanyName(name);
    if (errors.length > 0) {
        errorElement.textContent = errors[0];
        errorElement.classList.remove('hidden');
        return;
    }

    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Updating...';

    $.ajax({
        url: '/Companies/UpdateCompany',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ Id: parseInt(id), CompanyName: name }),
        success: function (response) {
            console.log('Update company response:', response);
            if (response.status === "success") {
                messageElement.textContent = 'Success! Reloading...';
                messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-green-50 text-green-700';
                messageElement.classList.remove('hidden');
                setTimeout(() => {
                    closeEditCompanyModal();
                    location.reload();
                }, 600);
            } else {
                messageElement.textContent = response.message || 'Failed to update';
                messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-red-50 text-red-700';
                messageElement.classList.remove('hidden');
                btn.disabled = false;
                btn.innerHTML = 'Update';
            }
        },
        error: function (xhr, status, error) {
            console.error('Update company error:', error, xhr.responseText);
            messageElement.textContent = 'Error updating company';
            messageElement.className = 'mt-4 p-3 rounded-lg text-sm font-medium bg-red-50 text-red-700';
            messageElement.classList.remove('hidden');
            btn.disabled = false;
            btn.innerHTML = 'Update';
        }
    });
}

function toggleDebug() {
    const debugSection = document.getElementById('debugSection');
    if (debugSection) {
        debugSection.classList.toggle('hidden');
    }
}

// Search functionality
$(document).ready(function () {
    $('#searchCompanies').on('keyup', function () {
        var searchTerm = $(this).val().toLowerCase().trim();

        if (!searchTerm) {
            $('.grid > div').show();
            return;
        }

        $('.grid > div').each(function () {
            var $card = $(this);
            var cardText = $card.text().toLowerCase();
            if (cardText.includes(searchTerm)) {
                $card.show();
            } else {
                $card.hide();
            }
        });
    });
});

// Modal click outside to close
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('click', function (e) {
            if (e.target === this) {
                if (this.id === 'addCompanyModal') {
                    closeAddCompanyModal();
                } else if (this.id === 'editCompanyModal') {
                    closeEditCompanyModal();
                }
            }
        });
    });
});