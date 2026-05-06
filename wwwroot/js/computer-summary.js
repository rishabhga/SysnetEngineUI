var domaindata = "";
var dataTables = {};

const flexRender = (row, ...fields) => {
    for (const field of fields) {
        if (row[field] !== undefined && row[field] !== null) return row[field];
        const camel = field.charAt(0).toLowerCase() + field.slice(1);
        if (row[camel] !== undefined && row[camel] !== null) return row[camel];
    }
    return "N/A";
};

const commonLogColumns = [
    { data: null, render: (row) => flexRender(row, 'FieldName') },
    { data: null, render: (row) => flexRender(row, 'PreviousValue') },
    { data: null, render: (row) => flexRender(row, 'ChangedValue') },
    { data: null, render: (row) => flexRender(row, 'ChangeDate', 'ChangeDateTime') }
];

window.sysAlert = function (msg, type) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            text: msg,
            icon: type || 'info',
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            background: '#1e293b',
            color: '#ffffff'
        });
    } else {
        alert(msg);
    }
};

$(document).ready(function () {
    initTabStyles();
    domaindata = $('#domainid').val();

    if (!domaindata) {
        console.error("Domain ID not found - skipping data load");
        return;
    }

    loadSummaryData();
    loadDiskChart();
    loadOSDetails();
    loadDeviceDetails();

    setTimeout(function () {
        initializeAllTables();
    }, 500);

    var loadedTabs = { '#Summary': true };

    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var targetId = $(e.target).attr('href');
        if (!loadedTabs[targetId]) {
            loadedTabs[targetId] = true;
            lazyLoadTabData(targetId);
        }
        initTablesInPane(targetId);
    });

    $('.tab-item a').on('click', function () {
        var targetId = $(this).attr('href');
        if (targetId && !loadedTabs[targetId]) {
            loadedTabs[targetId] = true;
            lazyLoadTabData(targetId);
        }
    });
});

function lazyLoadTabData(tabId) {
    switch (tabId) {
        case '#Hardware':
            loadBiosDetails();
            loadBatteryDetails();
            loadMonitorDetails();
            loadProcessorDetails();
            loadNetworkAdapters();
            loadKeyboardDetails();
            loadMotherboardDetails();
            loadPhysicalMemoryDetails();
            break;
        case '#Restriction':
            loadRestrictionData();
            break;
    }
}

function initTabStyles() {
    // 1. Top-level category tabs
    $('.tab-item a').off('click').on('click', function (e) {
        e.preventDefault();
        var $li = $(this).closest('.tab-item');
        $('.tab-item').removeClass('active');
        $li.addClass('active');
        
        var target = $(this).attr('href');
        $('.tab-content').first().children('.tab-pane').removeClass('active');
        $(target).addClass('active');
        
        // Trigger resize for DataTables in the newly shown tab
        setTimeout(function() {
            $(window).trigger('resize');
        }, 150);
    });

    // 2. Sub-tabs (sidebar tabs inside main categories)
    // Consolidate all sub-tabs into one delegated handler to prevent recursion and double-binding
    const subTabSelector = '.system-tab a, .hardware-tab a, .software-tab a, .security-tab a, .patch-sub-tab a, .usb-tab a, .history-tab a, .updatelog-tab a';
    $(document).off('click', subTabSelector).on('click', subTabSelector, function (e) {
        e.preventDefault();
        var $li = $(this).closest('li');
        $li.siblings().removeClass('active');
        $li.addClass('active');
        
        var target = $(this).attr('href');
        // Handle case where target might not be a sibling (e.g. nested deeply)
        $(target).parent().children('.tab-pane').removeClass('active');
        $(target).addClass('active');
        
        // Trigger resize for DataTables
        setTimeout(function() {
            $(window).trigger('resize');
        }, 150);
    });
}

function initializeAllTables() {
    initTable('#servicesTable', `/ComputerSummary/services?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'DisplayName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'StartupType') },
        { data: null, render: (row) => flexRender(row, 'State', 'Status') },
        { data: null, render: (row) => flexRender(row, 'LogonName') },
        { data: null, render: (row) => flexRender(row, 'DateTime') }
    ]);

    initTable('#usersTable', `/ComputerSummary/users?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'UserName') },
        { data: null, render: (row) => flexRender(row, 'DomainName') },
        { data: null, render: (row) => flexRender(row, 'FullName') },
        { data: null, render: (row) => flexRender(row, 'AccountType') },
        { data: null, render: (row) => flexRender(row, 'Status') }
    ]);

    initTable('#groupsTable', `/ComputerSummary/groups?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'Description') },
        { data: null, render: (row) => flexRender(row, 'SID', 'Sid') },
        { data: null, render: (row) => flexRender(row, 'Status') }
    ]);

    initTable('#driversTable', `/ComputerSummary/drivers?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'Description') },
        { data: null, render: (row) => flexRender(row, 'Status') },
        { data: null, render: (row) => flexRender(row, 'DateTime') }
    ]);

    initTable('#hardDiskTable', `/ComputerSummary/HardDisk?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Model') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'SerialNumber') },
        { data: null, render: (row) => flexRender(row, 'TotalCapacity', 'Capacity') }
    ]);

    initTable('#logicalDiskTable', `/ComputerSummary/LocalDisk?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'DriveLetter', 'Name') },
        { data: null, render: (row) => flexRender(row, 'FileSystem') },
        { data: null, render: (row) => flexRender(row, 'TotalCapacity', 'Size') },
        { data: null, render: (row) => flexRender(row, 'FreeSpace') },
        { data: null, render: (row) => flexRender(row, 'UsedSpace') },
        { data: null, render: (row) => flexRender(row, 'UsagePercentage', 'Usage') }
    ]);

    initTable('#pointingTable', `/ComputerSummary/PointingDevices?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'Description', 'Name') },
        { data: null, render: (row) => flexRender(row, 'NumberOfButtons') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#printersTable', `/ComputerSummary/Printers?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'PrinterName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'PortName') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#soundTable', `/ComputerSummary/Sound?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#videoTable', `/ComputerSummary/VideoControllers?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'AdapterName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'AdapterRAM', 'Ram') },
        { data: null, render: (row) => flexRender(row, 'HorizontalResolution', 'Resolution') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#usbTable', `/ComputerSummary/USBControllers?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'HardwareName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#desktopAppsTable', `/ComputerSummary/DesktopApps?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name', 'SoftwareName') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer', 'Publisher') },
        { data: null, render: (row) => flexRender(row, 'InstalledDate', 'DateTime') },
        {
            data: null,
            render: function (data, type, row) {
                const name = flexRender(row, 'Name', 'SoftwareName');
                return `<button onclick="uninstallSoftware('${escapeHtml(name)}')" class="px-2 py-1 bg-red-500 text-white text-xs rounded-lg hover:bg-red-600 transition">
                            <i class="fas fa-trash"></i> Uninstall
                        </button>`;
            }
        }
    ]);

    initTable('#storeAppsTable', `/ComputerSummary/MicrosoftstoreApps?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name', 'DisplayName') },
        { data: null, render: (row) => flexRender(row, 'PackageFullName') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') }
    ]);

    initTable('#meteredTable', `/ComputerSummary/MeteredSoftware?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'SoftwareName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'Usages', 'UsageCount') }
    ]);

    initTable('#installersTable', `/ComputerSummary/InstallationSoft?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'FileName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'FileSize', 'Size') },
        {
            data: null,
            render: function (data, type, row) {
                const fileName = flexRender(row, 'FileName', 'Name');
                return `<button onclick="installSoftware('${escapeHtml(fileName)}')" class="px-2 py-1 bg-green-500 text-white text-xs rounded-lg hover:bg-green-600 transition">
                            <i class="fas fa-download"></i> Install
                        </button>`;
            }
        }
    ]);

    initTable('#antivirusTable', `/ComputerSummary/Antivirus?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'ProtectionStatus', 'Status') },
        { data: null, render: (row) => flexRender(row, 'LicenseStatus') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') }
    ]);

    initTable('#firewallTable', `/ComputerSummary/Firewall?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'ProtectionStatus', 'Status') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') }
    ]);

    initTable('#windowUpdateTable', `/ComputerSummary/Missingpatchwindow?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'PatchId', 'Id') },
        { data: null, render: (row) => flexRender(row, 'Bulletin') },
        { data: null, render: (row) => flexRender(row, 'PatchName', 'Name') },
        {
            data: null,
            render: function (data, type, row) {
                const desc = flexRender(row, 'PatchDescription', 'Description');
                if (desc && desc.length > 50) {
                    return '<span title="' + escapeHtml(desc) + '">' + escapeHtml(desc.substring(0, 50)) + '...</span>';
                }
                return escapeHtml(desc) || 'N/A';
            }
        },
        { data: null, render: (row) => flexRender(row, 'Severity') },
        { data: null, render: (row) => flexRender(row, 'DateTime') }
    ]);

    initTable('#thirdPartyTable', `/ComputerSummary/Missingpatch?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'PatchName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Vendor', 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'CurrentVersion') },
        { data: null, render: (row) => flexRender(row, 'AvailableVersion') },
        { data: null, render: (row) => flexRender(row, 'Severity') },
        {
            data: null,
            render: function (data, type, row) {
                const id = flexRender(row, 'PatchId', 'Id');
                return `<button onclick="installPatch('${escapeHtml(id)}')" class="px-2 py-1 bg-blue-500 text-white text-xs rounded-lg hover:bg-blue-600 transition">
                            <i class="fas fa-download"></i> Update
                        </button>`;
            }
        }
    ]);

    initTable('#hotfixTable', `/ComputerSummary/Hotfix?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'HotFixID', 'HotfixId') },
        { data: null, render: (row) => flexRender(row, 'Caption', 'Title') },
        { data: null, render: (row) => flexRender(row, 'InstalledOn') },
        { data: null, render: (row) => flexRender(row, 'InstalledBy') }
    ]);

    initTable('#usbAuditTable', `/ComputerSummary/UsbDeviceAudit?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'DeviceName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'UserName') },
        { data: null, render: (row) => flexRender(row, 'DeviceType') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'ConnectedTime', 'DateTime') },
        { data: null, render: (row) => flexRender(row, 'UsageDuration', 'Duration') }
    ]);

    initTable('#auditHistoryTable', `/ComputerSummary/AuditHistory?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'DeviceName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'UserName', 'Username') },
        { data: null, render: (row) => flexRender(row, 'DeviceType') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'ConnectedTime', 'DateTime') },
        { data: null, render: (row) => flexRender(row, 'UsageDuration', 'Duration') }
    ]);

    initTable('#loginHistoryTable', `/ComputerSummary/LoginHistory?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'UserName', 'Username') },
        { data: null, render: (row) => flexRender(row, 'LogonTime') },
        { data: null, render: (row) => flexRender(row, 'LogoffTime') },
        { data: null, render: (row) => flexRender(row, 'DateTime') }
    ]);

    initTable('#summaryLogTable', `/ComputerSummary/SummaryUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#osSummaryLogTable', `/ComputerSummary/OSSummaryUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#userLogTable', `/ComputerSummary/WindowsUserChangeAuditUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#groupLogTable', `/ComputerSummary/WindowsGroupChangeAuditUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#batteryLogTable', `/ComputerSummary/BatteryLog?domain=${domaindata}`, commonLogColumns);
    initTable('#biosLogTable', `/ComputerSummary/BiosSummaryChageUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#desktopAppsLogTable', `/ComputerSummary/DesktopAppsChangeAuditUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#antivirusLogTable', `/ComputerSummary/AntivirusChangeAuditUpdateLog?domain=${domaindata}`, commonLogColumns);
}

function initTable(selector, url, columns) {
    dataTables[selector] = { url: url, columns: columns };

    if ($.fn.DataTable.isDataTable(selector)) {
        $(selector).DataTable().destroy();
    }

    $(selector).DataTable({
        ajax: {
            url: url,
            type: "GET",
            dataSrc: function (json) {
                if (Array.isArray(json)) return json;
                if (json && typeof json === 'object' && json.data && Array.isArray(json.data)) return json.data;
                return [];
            },
            error: function (xhr, error, thrown) {
                console.error("DataTable error for " + selector + ":", error);
                $(selector + ' tbody').html('<tr><td colspan="100" class="text-center py-8 text-slate-400"><i class="fas fa-exclamation-circle text-2xl mb-2 block"></i>Failed to load data</td></tr>');
            }
        },
        columns: columns,
        responsive: true,
        pageLength: 10,
        language: {
            search: "",
            searchPlaceholder: "Search records...",
            lengthMenu: "_MENU_ per page",
            info: "Showing _START_ to _END_ of _TOTAL_",
            emptyTable: `<div class="text-center py-10">
                            <i class="fas fa-inbox text-4xl text-slate-200 mb-3 block"></i>
                            <span class="text-slate-400 font-medium">No records found</span>
                         </div>`,
            paginate: {
                previous: '<i class="fas fa-chevron-left"></i>',
                next: '<i class="fas fa-chevron-right"></i>'
            }
        },
        dom: '<"flex flex-col sm:flex-row justify-between items-center mb-4 gap-4"l f>rt<"flex flex-col sm:flex-row justify-between items-center mt-4 gap-4"i p>',
        drawCallback: function () {
            $('.dataTables_paginate .paginate_button').addClass('px-3 py-1 border border-slate-200 rounded-lg text-sm mx-0.5 hover:bg-slate-50 transition-colors');
            $('.dataTables_paginate .paginate_button.current').addClass('bg-blue-600 text-white border-blue-600 hover:bg-blue-700');
            $('.dataTables_paginate .paginate_button.disabled').addClass('opacity-50 cursor-not-allowed');
        },
        initComplete: function () {
            $(selector).parent().find('.dataTables_filter input').addClass('pl-10 pr-4 py-2 border border-slate-200 rounded-xl text-sm focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 w-64 transition-all');
            $(selector).parent().find('.dataTables_filter').addClass('relative').prepend('<i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm z-10"></i>');
            $(selector).parent().find('.dataTables_length select').addClass('px-3 py-2 border border-slate-200 rounded-xl text-sm focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500');
        }
    });
}

function loadPhysicalMemoryDetails() {
    $('#tdMaximumSupportedRAM, #tdLocation, #tdSlotsAvailable, #tdSlotsUsed').html('<i class="fas fa-spinner fa-spin text-gray-400"></i>');
    $.get(`/ComputerSummary/PhysicalMemory?domain=${domaindata}`, function (data) {
        if (data) {
            $('#tdMaximumSupportedRAM').text(data.maximumSupportedRAM || data.MaximumSupportedRAM || 'N/A');
            $('#tdLocation').text(data.location || data.Location || 'N/A');
            $('#tdSlotsAvailable').text(data.slotsAvailable !== undefined ? data.slotsAvailable : (data.SlotsAvailable !== undefined ? data.SlotsAvailable : '0'));
            $('#tdSlotsUsed').text(data.slotsUsed !== undefined ? data.slotsUsed : (data.SlotsUsed !== undefined ? data.SlotsUsed : '0'));
        } else {
            $('#tdMaximumSupportedRAM, #tdLocation, #tdSlotsAvailable, #tdSlotsUsed').text('N/A');
        }
    }).fail(function () {
        $('#tdMaximumSupportedRAM, #tdLocation, #tdSlotsAvailable, #tdSlotsUsed').html('<span class="text-red-400">Error</span>');
    });
}

var tableRegistry = {};
function initTablesInPane(paneId) {
    $(paneId).find('table').each(function () {
        var tableId = '#' + $(this).attr('id');
        if (!$.fn.DataTable.isDataTable(tableId) && tableRegistry[tableId]) {
            initTable(tableId, tableRegistry[tableId].url, tableRegistry[tableId].columns);
        }
    });
}

function escapeHtml(str) {
    if (!str) return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function loadSummaryData() {
    $.get(`/ComputerSummary/Summary?domain=${domaindata}`, function (data) {
        $("#tdTotalHardware").text(data.totalHardware || data.TotalHardware || 0);
        $("#tdTotalSoftware").text(data.totalSoftware || data.TotalSoftware || 0);
        $("#tdCommercialSoftware").text(data.commercialSoftware || data.CommercialSoftware || 0);
        $("#tdNonCommercialSoftware").text(data.nonCommercialSoftware || data.NonCommercialSoftware || 0);
        $("#tdProhibitedSoftware").text(data.prohibitedSoftware || data.ProhibitedSoftware || 0);
        $("#tdMissingPatches").text(data.missingPatches || data.MissingPatches || 0);
    }).fail(function () {
        console.error("Failed to load summary data");
    });
}

function loadDiskChart() {
    $.get(`/ComputerSummary/UsegeDisk?domain=${domaindata}`, function (data) {
        var ctx = document.getElementById('assetChart');
        if (!ctx) return;

        var chartCtx = ctx.getContext('2d');
        new Chart(chartCtx, {
            type: 'doughnut',
            data: {
                labels: ['Used Space', 'Free Space'],
                datasets: [{
                    data: [data.usedSpaceGB || 0, data.freeSpaceGB || 0],
                    backgroundColor: ['#3b82f6', '#10b981'],
                    borderWidth: 0,
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                cutout: '65%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            font: { size: 11, family: 'Inter' },
                            padding: 15
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return context.label + ': ' + context.raw + ' GB';
                            }
                        }
                    }
                }
            }
        });
    }).fail(function () {
        console.error("Failed to load disk chart data");
    });
}

function loadOSDetails() {
    $.get(`/ComputerSummary/OSSummary?domain=${domaindata}`, function (data) {
        $('#tdOperatingSystem').text(data.operatingSystem || data.OperatingSystem || 'N/A');
        $('#tdOSVersion').text(data.osVersion || data.OSVersion || 'N/A');
        $('#tdRegisteredTo').text(data.registeredTo || data.RegisteredTo || 'N/A');
        $('#tdProductID').text(data.productID || data.ProductID || 'N/A');
        $('#tdLicenseType').text(data.licenseType || data.LicenseType || 'N/A');
        $('#tdSystemDrive').text(data.systemDrive || data.SystemDrive || 'N/A');
        $('#tdOSCDKey').text(data.oscdKey || data.OSCDKey || 'N/A');
        $('#tdOSBuildNumber').text(data.osBuildNumber || data.OSBuildNumber || 'N/A');
    }).fail(function () {
        console.error("Failed to load OS details");
    });
}

function loadDeviceDetails() {
    $.get(`/ComputerSummary/DeviceSummary?domain=${domaindata}`, function (data) {
        $('#tdDeviceManufacturer').text(data.deviceManufacturer || data.DeviceManufacturer || 'N/A');
        $('#tdDeviceModel').text(data.deviceModel || data.DeviceModel || 'N/A');
        $('#tdDeviceType').text(data.deviceType || data.DeviceType || 'N/A');
        $('#tdProcessor').text(data.processor || data.Processor || 'N/A');
        $('#tdMemory').text(data.memory || data.Memory || 'N/A');
        $('#tdSerialNumber').text(data.serialNumber || data.SerialNumber || 'N/A');
        $('#tdProcessorArchitecture').text(data.processorArchitecture || data.ProcessorArchitecture || 'N/A');
        $('#tdUDID').text(data.udid || data.UDID || 'N/A');
        $('#tdBatteryLevel').text(data.batteryLevel || data.BatteryLevel || 'N/A');
    }).fail(function () {
        console.error("Failed to load device details");
    });
}

function loadBiosDetails() {
    $.get(`/ComputerSummary/BIOS?domain=${domaindata}`, function (data) {
        $('#biosManufacturer').text(data.manufacturer || 'N/A');
        $('#biosVersion').text(data.version || 'N/A');
        $('#biosSmbiosVersion').text(data.smBiosVersion || data.smbiosVersion || 'N/A');
        $('#biosReleaseDate').text(data.releaseDate || 'N/A');
        $('#biosStatus').text(data.status || 'N/A');
        $('#biosDescription').text(data.description || 'N/A');
    }).fail(function () {
        console.error("Failed to load BIOS details");
    });
}

function loadBatteryDetails() {
    $.get(`/ComputerSummary/Battery?domain=${domaindata}`, function (data) {
        $('#batteryManufacturer').text(data.manufacturer || 'N/A');
        $('#batteryStatus').text(data.status || 'N/A');
        $('#batteryDescription').text(data.description || 'N/A');
        $('#batteryLevel').text(data.batteryLevel || 'N/A');
        $('#batterySystemType').text(data.systemType || 'N/A');
    }).fail(function () {
        console.error("Failed to load battery details");
    });
}

function loadMonitorDetails() {
    $.get(`/ComputerSummary/Monitor?domain=${domaindata}`, function (data) {
        $('#monitorManufacturer').text(data.manufacturer || 'N/A');
        $('#monitorType').text(data.monitorType || 'N/A');
        $('#monitorResolution').text((data.screenWidth || 'N/A') + ' x ' + (data.screenHeight || 'N/A'));
        $('#monitorSerial').text(data.serialNumber || 'N/A');
        $('#monitorSize').text(data.monitorSize || 'N/A');
        $('#monitorStatus').text(data.deviceStatus || 'N/A');
    }).fail(function () {
        console.error("Failed to load monitor details");
    });
}

function loadProcessorDetails() {
    $.get(`/ComputerSummary/Processors?domain=${domaindata}`, function (data) {
        $('#processorSpeed').text(data.processorSpeed || 'N/A');
        $('#processorManufacturer').text(data.manufacturer || 'N/A');
        $('#processorCores').text(data.numberOfCores || 'N/A');
        $('#processorSocket').text(data.socketDesignation || 'N/A');
        $('#processorStatus').text(data.deviceStatus || 'N/A');
        $('#processorDescription').text(data.description || 'N/A');
    }).fail(function () {
        console.error("Failed to load processor details");
    });
}

function loadNetworkAdapters() {
    $.get(`/ComputerSummary/NetworkAdapters?domain=${domaindata}`, function (data) {
        var container = $('#networkContainer');
        container.empty();
        if (data && data.length) {
            data.forEach(function (adapter) {
                container.append(`
                    <div class="bg-gray-50 rounded-lg border border-gray-100 overflow-hidden hover:shadow-md transition-shadow">
                        <div class="px-4 py-3 bg-white border-b border-gray-100">
                            <h4 class="font-semibold text-gray-800 text-sm flex items-center gap-2">
                                <i class="fas fa-network-wired text-blue-500"></i> ${escapeHtml(adapter.description || 'Network Adapter')}
                            </h4>
                        </div>
                        <div class="p-4 space-y-2 text-sm">
                            <div class="flex justify-between flex-wrap"><span class="text-gray-500">Manufacturer:</span> <span class="text-gray-700">${escapeHtml(adapter.manufacturer || 'N/A')}</span></div>
                            <div class="flex justify-between flex-wrap"><span class="text-gray-500">MAC Address:</span> <span class="font-mono text-gray-700">${escapeHtml(adapter.macAddress || 'N/A')}</span></div>
                            <div class="flex justify-between flex-wrap"><span class="text-gray-500">IP Address:</span> <span class="font-mono text-gray-700">${escapeHtml(adapter.ipAddress || 'N/A')}</span></div>
                            <div class="flex justify-between flex-wrap"><span class="text-gray-500">DHCP Enabled:</span> <span>${adapter.dhcpEnabled ? '<i class="fas fa-check-circle text-green-500"></i> Yes' : '<i class="fas fa-times-circle text-red-400"></i> No'}</span></div>
                            <div class="flex justify-between flex-wrap"><span class="text-gray-500">Status:</span> <span class="${adapter.deviceStatus === 'OK' ? 'text-green-600' : 'text-red-500'}">${escapeHtml(adapter.deviceStatus || 'N/A')}</span></div>
                        </div>
                    </div>
                `);
            });
        } else {
            container.html('<div class="text-center py-8 text-gray-400"><i class="fas fa-network-wired text-4xl mb-3 block"></i>No network adapters found</div>');
        }
    }).fail(function () {
        $('#networkContainer').html('<div class="text-center py-8 text-red-400">Failed to load network adapters</div>');
    });
}

function loadKeyboardDetails() {
    $.get(`/ComputerSummary/Keyboard?domain=${domaindata}`, function (data) {
        var container = $('#keyboardContainer');
        container.empty();
        if (data && data.length) {
            data.forEach(function (keyboard) {
                container.append(`
                    <div class="bg-gray-50 rounded-lg border border-gray-100 p-4 hover:shadow-md transition-shadow">
                        <div class="flex items-center gap-3 mb-3">
                            <i class="fas fa-keyboard text-gray-500 text-xl"></i>
                            <h4 class="font-semibold text-gray-800">${escapeHtml(keyboard.manufacturer || 'Keyboard')}</h4>
                        </div>
                        <div class="space-y-1 text-sm">
                            <div class="flex justify-between flex-wrap"><span class="text-gray-500">Description:</span> <span class="text-gray-700">${escapeHtml(keyboard.description || 'N/A')}</span></div>
                            <div class="flex justify-between flex-wrap"><span class="text-gray-500">Status:</span> <span class="${keyboard.deviceStatus === 'OK' ? 'text-green-600' : 'text-red-500'}">${escapeHtml(keyboard.deviceStatus || 'N/A')}</span></div>
                        </div>
                    </div>
                `);
            });
        } else {
            container.html('<div class="text-center py-8 text-gray-400"><i class="fas fa-keyboard text-4xl mb-3 block"></i>No keyboard devices found</div>');
        }
    }).fail(function () {
        $('#keyboardContainer').html('<div class="text-center py-8 text-red-400">Failed to load keyboard details</div>');
    });
}

function loadMotherboardDetails() {
    $.get(`/ComputerSummary/Motherboard?domain=${domaindata}`, function (data) {
        var container = $('#motherboardContainer');
        container.empty();
        if (data && data.length) {
            data.forEach(function (mb) {
                container.append(`
                    <div class="bg-gray-50 rounded-lg border border-gray-100 p-4 hover:shadow-md transition-shadow">
                        <div class="flex items-center gap-3 mb-3">
                            <i class="fas fa-microchip text-blue-500 text-xl"></i>
                            <h4 class="font-semibold text-gray-800">${escapeHtml(mb.manufacturer || 'Motherboard')}</h4>
                        </div>
                        <div class="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
                            <div><span class="text-gray-500">Model:</span> <span class="text-gray-700">${escapeHtml(mb.model || 'N/A')}</span></div>
                            <div><span class="text-gray-500">Version:</span> <span class="text-gray-700">${escapeHtml(mb.version || 'N/A')}</span></div>
                            <div><span class="text-gray-500">Serial Number:</span> <span class="font-mono text-gray-700">${escapeHtml(mb.serialNumber || 'N/A')}</span></div>
                            <div><span class="text-gray-500">Status:</span> <span class="${mb.deviceStatus === 'OK' ? 'text-green-600' : 'text-red-500'}">${escapeHtml(mb.deviceStatus || 'N/A')}</span></div>
                        </div>
                    </div>
                `);
            });
        } else {
            container.html('<div class="text-center py-8 text-gray-400"><i class="fas fa-microchip text-4xl mb-3 block"></i>No motherboard information found</div>');
        }
    }).fail(function () {
        $('#motherboardContainer').html('<div class="text-center py-8 text-red-400">Failed to load motherboard details</div>');
    });
}

function loadRestrictionData() {
    $.get(`/ComputerSummary/RestrictionOnDevice?domain=${domaindata}`, function (data) {
        $('#tdIsCameraEnabled').html(boolToIcon(data.isCameraEnabled));
        $('#tdIsTelemetryEnabled').html(boolToIcon(data.isTelemetryEnabled));
        $('#tdCanModifyDateTime').html(boolToIcon(data.canModifyDateTime));
        $('#tdIsBluetoothEnabled').html(boolToIcon(data.isBluetoothEnabled));
    });

    $.get(`/ComputerSummary/RestrictionOnNetwork?domain=${domaindata}`, function (data) {
        $('#tdInternetSharing').html(boolToIcon(data.internetSharing));
        $('#tdVPN').html(boolToIcon(data.vpn));
        $('#tdWiFi').html(boolToIcon(data.wiFi));
        $('#tdAllowWiFiConfiguration').html(boolToIcon(data.allowWiFiConfiguration));
    });

    $.get(`/ComputerSummary/bluetootdetailsdata?domain=${domaindata}`, function (data) {
        $('#tdBluetooth').html(boolToIcon(data.bluetooth));
        $('#tdBluetoothdiscovery').html(boolToIcon(data.bluetoothdiscovery));
        $('#tdBluetoothprepairing').html(boolToIcon(data.bluetoothprepairing));
        $('#tdBluetoothservicesadvertising').html(boolToIcon(data.bluetoothservicesadvertising));
    });

    $.get(`/ComputerSummary/SecurityPrivacyDetails?domain=${domaindata}`, function (data) {
        $('#tdLocationServices').html(boolToIcon(data.locationServices));
        $('#tdIsMicrosoftAccountConnected').html(boolToIcon(data.isMicrosoftAccountConnected));
        $('#tdCanAddNonMicrosoftAccounts').html(boolToIcon(data.canAddNonMicrosoftAccounts));
        $('#tdCanResetDevice').html(boolToIcon(data.canResetDevice));
    });

    $.get(`/ComputerSummary/ApplicationSettings?domain=${domaindata}`, function (data) {
        $('#tdInstallNonStoreApps').html(boolToIcon(data.installNonStoreApps));
        $('#tdInstallAppsOnlyInDeviceMemory').html(boolToIcon(data.installAppsOnlyInDeviceMemory));
        $('#tdStoreAppDataOnlyInDeviceMemory').html(boolToIcon(data.storeAppDataOnlyInDeviceMemory));
        $('#tdAutoUpdateStoreApps').html(boolToIcon(data.autoUpdateStoreApps));
    });

    $.get(`/ComputerSummary/SocialSearchSettings?domain=${domaindata}`, function (data) {
        $('#tdCortanaEnabled').html(boolToIcon(data.cortanaEnabled));
        $('#tdSyncSettingsEnabled').html(boolToIcon(data.syncSettingsEnabled));
        $('#tdSearchLocationEnabled').html(boolToIcon(data.searchLocationEnabled));
    });
}

function boolToIcon(value) {
    if (value === true || value === 'true' || value === 'True' || value === 'Enabled') {
        return '<i class="fas fa-check-circle text-green-500 text-xl"></i>';
    }
    return '<i class="fas fa-times-circle text-red-400 text-xl"></i>';
}

function uninstallSoftware(softwareName) {
    Swal.fire({
        title: 'Confirm Uninstall',
        text: `Are you sure you want to uninstall ${softwareName}?`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        confirmButtonText: 'Yes, Uninstall',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: `/ComputerSummary/Uninstallsoftware?domain=${domaindata}`,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ softwareName: softwareName }),
                success: function () {
                    sysAlert('Uninstall command sent successfully', 'success');
                    setTimeout(() => {
                        if ($.fn.DataTable.isDataTable('#desktopAppsTable')) {
                            $('#desktopAppsTable').DataTable().ajax.reload();
                        }
                    }, 3000);
                },
                error: function () {
                    sysAlert('Failed to send uninstall command', 'error');
                }
            });
        }
    });
}

function installSoftware(fileName) {
    Swal.fire({
        title: 'Confirm Installation',
        text: `Are you sure you want to install ${fileName}?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#10b981',
        confirmButtonText: 'Yes, Install',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: `/ComputerSummary/PatchUpdate?domain=${domaindata}`,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ softwareName: fileName }),
                success: function () {
                    sysAlert('Installation command sent successfully', 'success');
                },
                error: function () {
                    sysAlert('Failed to send installation command', 'error');
                }
            });
        }
    });
}

function installPatch(patchId) {
    Swal.fire({
        title: 'Confirm Update',
        text: `Are you sure you want to install this patch?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#3b82f6',
        confirmButtonText: 'Yes, Install',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: `/ComputerSummary/UpdatePatch?domain=${domaindata}`,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ patchId: patchId }),
                success: function () {
                    sysAlert('Patch installation started', 'success');
                    setTimeout(() => {
                        if ($.fn.DataTable.isDataTable('#thirdPartyTable')) {
                            $('#thirdPartyTable').DataTable().ajax.reload();
                        }
                        if ($.fn.DataTable.isDataTable('#windowUpdateTable')) {
                            $('#windowUpdateTable').DataTable().ajax.reload();
                        }
                    }, 5000);
                },
                error: function () {
                    sysAlert('Failed to start patch installation', 'error');
                }
            });
        }
    });
}

function refreshSoftwareTable() {
    if ($.fn.DataTable.isDataTable('#desktopAppsTable')) {
        $('#desktopAppsTable').DataTable().ajax.reload();
    }
}