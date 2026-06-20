var domaindata = "";
var actualDomainName = "";
var dataTables = {};
var tableRegistry = {};

const flexRender = (row, ...fields) => {
    let val = undefined;
    for (const field of fields) {
        if (row[field] !== undefined && row[field] !== null) { val = row[field]; break; }
        const camel = field.charAt(0).toLowerCase() + field.slice(1);
        if (row[camel] !== undefined && row[camel] !== null) { val = row[camel]; break; }
    }
    if (val === undefined || val === null || val === '') return "N/A";
    if (typeof val === 'string' && val.match(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/)) {
        try {
            const date = new Date(val);
            if (!isNaN(date.getTime())) {
                return date.toLocaleString(undefined, {
                    year: 'numeric', month: 'short', day: 'numeric',
                    hour: '2-digit', minute: '2-digit'
                });
            }
        } catch (e) { }
    }
    return val;
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
            timer: 3500,
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
    actualDomainName = $('#domainName').val() || domaindata;

    if (!domaindata) {
        console.error("Domain ID not found — skipping data load");
        return;
    }

    loadSummaryData();
    loadOSDetails();
    loadDeviceDetails();
    loadLogicalDrivesDashboard();

    setTimeout(function () {
        initializeAllTables();
    }, 500);
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
            loadMemoryDetails();
            break;
        case '#Restriction':
            loadRestrictionData();
            break;
    }
}
function initTabStyles() {
    var loadedTabs = { '#Summary': true };

    $(document).on('click', '#mainTabList .main-tab a', function (e) {
        e.preventDefault();
        var $li = $(this).closest('li');
        var target = $(this).attr('href');
        if (!target) return;

        $('#mainTabList .main-tab').removeClass('active');
        $li.addClass('active');

        if ($li.length && $li[0].scrollIntoView) {
            $li[0].scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
        }

        $('#mainTabContent').children('.tab-pane').removeClass('active');
        $(target).addClass('active');

        if (!loadedTabs[target]) {
            loadedTabs[target] = true;
            lazyLoadTabData(target);
        }

        setTimeout(function () {
            $(window).trigger('resize');
            if ($.fn.DataTable) {
                $.each($.fn.dataTable.tables({ visible: true, api: true }), function () {
                    this.columns.adjust();
                });
            }
        }, 150);
    });

    var subTabSelector = [
        '.system-tab a',
        '.hardware-tab a',
        '.software-tab a',
        '.security-tab a',
        '.patch-sub-tab a',
        '.usb-tab a',
        '.history-tab a',
        '.updatelog-tab a'
    ].join(', ');

    $(document).on('click', subTabSelector, function (e) {
        e.preventDefault();
        var $li = $(this).closest('li');
        var target = $(this).attr('href');
        if (!target) return;
        $li.siblings().removeClass('active');
        $li.addClass('active');

        if ($li.length && $li[0].scrollIntoView) {
            $li[0].scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
        }

        var $paneParent = $(target).parent();
        $paneParent.children('.tab-pane').removeClass('active');
        $(target).addClass('active');

        setTimeout(function () {
            $(window).trigger('resize');
            if ($.fn.DataTable) {
                $(target).find('table.dataTable').each(function () {
                    if ($.fn.DataTable.isDataTable(this)) {
                        $(this).DataTable().columns.adjust();
                    }
                });
            }
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
        { data: null, render: (row) => flexRender(row, 'Status') },
        { data: null, render: (row) => flexRender(row, 'LastLogin', 'LastLogin', 'Last Login') }
    ]);

    initTable('#groupsTable', `/ComputerSummary/groups?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'Description') },
        { data: null, render: (row) => flexRender(row, 'SID', 'Sid') },
        { data: null, render: (row) => flexRender(row, 'Status') }
    ]);

    initTable('#driversTable', `/ComputerSummary/drivers?domain=${domaindata}`, [
        { data: function(row) { return row.Category || row.category || 'Other Devices'; }, visible: false },
        { data: null, render: (row) => flexRender(row, 'DeviceName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer', 'Description') },
        { data: null, render: (row) => flexRender(row, 'Status') },
        { data: null, render: (row) => flexRender(row, 'DateTime') }
    ], {
        order: [[0, 'asc']],
        rowGroup: {
            dataSrc: function (row) {
                return row.Category || row.category || 'Other Devices';
            },
            startRender: function (rows, group) {
                let icon = 'fa-microchip';
                const g = group.toLowerCase();
                if (g.includes('system') || g.includes('board')) icon = 'fa-cogs';
                else if (g.includes('volume') || g.includes('disk') || g.includes('storage') || g.includes('hdc')) icon = 'fa-hdd';
                else if (g.includes('mouse') || g.includes('pointing')) icon = 'fa-mouse';
                else if (g.includes('keyboard')) icon = 'fa-keyboard';
                else if (g.includes('print') || g.includes('fax')) icon = 'fa-print';
                else if (g.includes('usb')) icon = 'fa-usb';
                else if (g.includes('net') || g.includes('wan') || g.includes('wi-fi')) icon = 'fa-network-wired';
                else if (g.includes('audio') || g.includes('sound') || g.includes('media')) icon = 'fa-volume-up';
                else if (g.includes('video') || g.includes('display') || g.includes('monitor')) icon = 'fa-desktop';
                else if (g.includes('processor') || g.includes('cpu')) icon = 'fa-microchip';
                else if (g.includes('bluetooth')) icon = 'fa-bluetooth';
                else if (g.includes('hidclass')) icon = 'fa-gamepad';
                
                return $('<tr/>').append('<td colspan="5" class="bg-slate-100 font-bold text-slate-700 border-y border-slate-300 py-2 px-4 shadow-sm"><i class="fas ' + icon + ' mr-2 text-cyan-600"></i>' + group + ' <span class="text-xs text-slate-500 font-normal ml-2">(' + rows.count() + ')</span></td>');
            }
        }
    });

    initTable('#hardDiskTable', `/ComputerSummary/HardDisk?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Model') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'SerialNumber') },
        { data: null, render: (row) => flexRender(row, 'TotalCapacity', 'Capacity') }
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
                return '<button data-software-name="' + escapeHtml(name) + '" class="btn-uninstall px-2 py-1 bg-red-500 text-white text-xs rounded-lg hover:bg-red-600 transition">' +
                    '<i class="fas fa-trash"></i> Uninstall</button>';
            }
        }
    ]);

    initTable('#storeAppsTable', `/ComputerSummary/MicrosoftstoreApps?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name', 'DisplayName') },
        { data: null, render: (row) => flexRender(row, 'PackageFullName') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') }
    ]);

    initTable('#installersTable', `/ComputerSummary/InstallationSoft?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'SoftwareName', 'FileName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        {
            data: null,
            render: function (data, type, row) {
                const fileName = flexRender(row, 'FileName', 'Name');
                return '<button data-file-name="' + escapeHtml(fileName) + '" class="btn-install px-2 py-1 bg-green-500 text-white text-xs rounded-lg hover:bg-green-600 transition">' +
                    '<i class="fas fa-download"></i> Install</button>';
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
                if (desc && desc !== 'N/A' && desc.length > 50) {
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
                const name = flexRender(row, 'PatchName', 'Name');
                return '<button data-patch-id="' + escapeHtml(id) + '" data-patch-name="' + escapeHtml(name) + '" class="btn-patch px-2 py-1 bg-blue-500 text-white text-xs rounded-lg hover:bg-blue-600 transition">' +
                    '<i class="fas fa-download"></i> Update</button>';
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

    $(document).on('click', '.btn-uninstall', function () {
        var name = $(this).data('software-name');
        uninstallSoftware(name);
    });

    $(document).on('click', '.btn-install', function () {
        var fileName = $(this).data('file-name');
        installSoftware(fileName);
    });

    $(document).on('click', '.btn-patch', function () {
        var patchId = $(this).data('patch-id');
        var patchName = $(this).data('patch-name');
        installPatch(patchId, patchName);
    });
}

function initTable(selector, url, columns, options) {
    options = options || {};
    dataTables[selector] = { url: url, columns: columns, options: options };
    tableRegistry[selector] = { url: url, columns: columns, options: options };

    if ($.fn.DataTable.isDataTable(selector)) {
        $(selector).DataTable().destroy();
    }

    var dtConfig = {
        ajax: {
            url: url,
            type: "GET",
            dataSrc: function (json) {
                if (Array.isArray(json)) return json;
                if (json && typeof json === 'object' && Array.isArray(json.data)) return json.data;
                return [];
            },
            error: function (xhr) {
                console.error("DataTable error for " + selector + ":", xhr.status);
                $(selector + ' tbody').html('<tr><td colspan="100" class="text-center py-8 text-slate-400"><i class="fas fa-exclamation-circle text-2xl mb-2 block"></i>Failed to load data</td></tr>');
            }
        },
        columns: columns,
        responsive: true,
        pageLength: 10,
        processing: true,
        language: {
            processing: '<div class="cs-dt-processing"><i class="fas fa-circle-notch fa-spin"></i> Loading data...</div>',
            search: "",
            searchPlaceholder: "Search records...",
            lengthMenu: "_MENU_ per page",
            info: "Showing _START_ to _END_ of _TOTAL_",
            emptyTable: '<div class="text-center py-8 text-slate-400"><i class="fas fa-inbox text-2xl mb-2 block"></i>No records found</div>',
            zeroRecords: '<div class="text-center py-8 text-slate-400"><i class="fas fa-search text-2xl mb-2 block"></i>No matching records found</div>',
            paginate: {
                previous: '<i class="fas fa-chevron-left"></i>',
                next: '<i class="fas fa-chevron-right"></i>'
            }
        },
        dom: '<"cs-dt-top"lf>rt<"cs-dt-bottom"ip>',
        drawCallback: function () {
        },
        initComplete: function () {
            var $wrapper = $(selector).closest('.dataTables_wrapper');
            $wrapper.find('.dataTables_filter').addClass('cs-dt-search-wrapper');
            $wrapper.find('.dataTables_filter input').addClass('cs-dt-search-input');
            if ($wrapper.find('.dataTables_filter i.fa-search').length === 0) {
                $wrapper.find('.dataTables_filter').prepend('<i class="fas fa-search cs-dt-search-icon"></i>');
            }
            $wrapper.find('.dataTables_length select').addClass('cs-dt-length-select');
        }
    };

    if (options) {
        Object.assign(dtConfig, options);
    }

    $(selector).DataTable(dtConfig);
}

function initTablesInPane(paneId) {
    $(paneId).find('table').each(function () {
        var tableId = '#' + $(this).attr('id');
        if (!$.fn.DataTable.isDataTable(tableId) && tableRegistry[tableId]) {
            initTable(tableId, tableRegistry[tableId].url, tableRegistry[tableId].columns, tableRegistry[tableId].options);
        }
    });
}

let memUsageChartInstance = null;
let memCapacityChartInstance = null;

function renderMemoryTrendChart(history) {
    if (!history || !history.length) return;
    
    var labels = history.map(function (h) {
        var dt = new Date(h.dateTime || h.DateTime);
        return isNaN(dt.getTime()) ? '' : dt.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
    });

    var usageCanvas = document.getElementById('memUsageTrendChart');
    if (usageCanvas) {
        if (memUsageChartInstance) memUsageChartInstance.destroy();
        memUsageChartInstance = new Chart(usageCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Usage %', data: history.map(h => parseFloat(h.usagePercent || h.UsagePercent) || 0), borderColor: '#0ea5e9', backgroundColor: 'rgba(14, 165, 233, .1)', borderWidth: 2, fill: true, tension: 0, pointRadius: 0 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { title: { display: true, text: '%' }, min: 0, max: 100 } }
            }
        });
    }

    var capacityCanvas = document.getElementById('memCapacityTrendChart');
    if (capacityCanvas) {
        if (memCapacityChartInstance) memCapacityChartInstance.destroy();
        memCapacityChartInstance = new Chart(capacityCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Used (GB)', data: history.map(h => parseFloat(h.usedMemoryGB || h.UsedMemoryGB) || 0), borderColor: '#0ea5e9', borderWidth: 2, fill: false, tension: 0, pointRadius: 0 },
                    { label: 'Free (GB)', data: history.map(h => parseFloat(h.freeMemoryGB || h.FreeMemoryGB) || 0), borderColor: '#22c55e', borderWidth: 2, fill: false, tension: 0, pointRadius: 0 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } } },
                scales: { y: { title: { display: true, text: 'GB' }, min: 0 } }
            }
        });
    }
}

function loadMemoryDetails() {
    $.get(`/ComputerSummary/MemorySummary?domain=${domaindata}`, function (data) {
        if (data) {
            $('#memTotalCapacity').text((data.installedMemoryGB || data.InstalledMemoryGB || 0).toFixed(2) + ' GB Installed');
            $('#memUsageBadge').html(`<i class="fas fa-chart-pie"></i> ${(data.usagePercent || data.UsagePercent || 0).toFixed(1)}% Used`);
            
            let totalSlots = data.totalSlots || data.TotalSlots || 0;
            let usedSlots = data.usedSlots || data.UsedSlots || 0;
            $('#memSlotsBadge').html(`<i class="fas fa-grip-horizontal"></i> ${usedSlots}/${totalSlots} Slots`);
            
            let dt = data.dateTime || data.DateTime;
            if (dt) {
                var d = new Date(dt);
                if (!isNaN(d.getTime())) $('#memLastUpdated').text('Updated: ' + d.toLocaleTimeString());
            }

            $('#memInstalled').text((data.installedMemoryGB || data.InstalledMemoryGB || 0).toFixed(2) + ' GB');
            $('#memMaxSupported').text((data.maximumSupportedMemoryGB || data.MaximumSupportedMemoryGB || 0).toFixed(2) + ' GB');
            $('#memUsed').text((data.usedMemoryGB || data.UsedMemoryGB || 0).toFixed(2) + ' GB');
            $('#memFree').text((data.freeMemoryGB || data.FreeMemoryGB || 0).toFixed(2) + ' GB');
            $('#memTotalSlots').text(totalSlots);
            $('#memUsedSlots').text(usedSlots);
            
            var tbody = $('#memoryModulesTable tbody').empty();
            var modules = data.memoryModules || data.MemoryModules || [];
            if (modules.length === 0) {
                tbody.append(`<tr><td colspan="6" style="padding:15px;text-align:center;color:#94a3b8;">No modules found</td></tr>`);
            } else {
                modules.forEach(m => {
                    tbody.append(`
                        <tr style="border-bottom:1px solid var(--slate-100);">
                            <td style="padding:10px 12px;font-family:var(--font-mono);font-size:0.8rem;color:var(--slate-700);">${m.deviceLocator || m.DeviceLocator || 'N/A'}</td>
                            <td style="padding:10px 12px;color:var(--slate-800);">${m.manufacturer || m.Manufacturer || 'N/A'}</td>
                            <td style="padding:10px 12px;color:var(--slate-800);font-weight:600;">${(m.capacityGB || m.CapacityGB || 0).toFixed(2)} GB</td>
                            <td style="padding:10px 12px;color:var(--slate-600);">${m.speedMHz || m.SpeedMHz || m.configuredClockSpeedMHz || m.ConfiguredClockSpeedMHz || 0} MHz</td>
                            <td style="padding:10px 12px;color:var(--slate-600);"><span style="background:var(--slate-100);padding:2px 6px;border-radius:4px;font-size:0.75rem;">${decodeMap(MEM_TYPE_MAP, m.memoryType || m.MemoryType)}</span></td>
                            <td style="padding:10px 12px;color:var(--slate-600);">${decodeMap(MEM_FORM_FACTOR_MAP, m.formFactor || m.FormFactor)}</td>
                        </tr>
                    `);
                });
            }
        }
    });

    $.get(`/ComputerSummary/MemoryHistory?domain=${domaindata}`, function (history) {
        if (history && history.length > 0) {
            renderMemoryTrendChart(history);
        }
    });
}

function escapeHtml(str) {
    if (str === null || str === undefined) return '';
    return String(str)
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
    }).fail(function () { console.error("Failed to load summary data"); });
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
    }).fail(function () { console.error("Failed to load OS details"); });
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
    }).fail(function () { console.error("Failed to load device details"); });
}

function loadLogicalDrivesDashboard() {
    $.get(`/ComputerSummary/LocalDisk?domain=${domaindata}`, function (res) {
        var data = res;
        if (res && res.data) data = res.data;

        var container = $('#summaryLogicalDrivesContainer').empty();
        if (data && data.length) {
            data.forEach(function (d) {
                var driveLetter = d.driveLetter || d.DriveLetter || d.Name || 'Unknown';
                var fileSystem = d.fileSystem || d.FileSystem || 'Unknown';
                var total = parseFloat(d.totalCapacity || d.Size || 0).toFixed(2);
                var free = parseFloat(d.freeSpace || d.FreeSpace || 0).toFixed(2);
                var used = parseFloat(d.usedSpace || d.UsedSpace || 0).toFixed(2);
                var usagePct = parseFloat(d.usagePercentage || d.Usage || 0);

                var barColor = 'var(--cyan)';
                if (usagePct > 90) barColor = 'var(--red)';
                else if (usagePct > 80) barColor = 'var(--amber)';

                var icon = driveLetter.includes('C:') ? '<i class="fab fa-windows" style="color:var(--cyan); font-size: 1.15rem;"></i>' : '<i class="fas fa-hdd" style="color:var(--slate-400); font-size: 1.15rem;"></i>';

                var dashLen = (usagePct / 100) * 100;
                var circleColor = barColor;

                container.append(
                    '<div class="cs-drive-card" style="background: var(--white); border: 1px solid var(--slate-200); border-radius: var(--radius-md); padding: 10px 14px; display: flex; gap: 14px; align-items: center; box-shadow: var(--shadow-sm); cursor: default;">' +
                    '<div style="position:relative; width: 54px; height: 54px; flex-shrink: 0;">' +
                    '<svg viewBox="0 0 36 36" style="width:100%; height:100%; transform: rotate(-90deg);">' +
                    '<path stroke="#a7f3d0" stroke-width="3" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />' +
                    '<path stroke="' + circleColor + '" stroke-width="3" stroke-dasharray="' + dashLen + ', 100" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" style="stroke-linecap: round; transition: stroke-dasharray 1s ease-in-out;" />' +
                    '</svg>' +
                    '<div style="position:absolute; inset:0; display:flex; align-items:center; justify-content:center; font-size:0.75rem; font-weight:800; color:var(--slate-800);">' + usagePct.toFixed(0) + '%</div>' +
                    '</div>' +
                    '<div style="flex: 1; min-width: 0; display: flex; flex-direction: column; justify-content: center;">' +
                    '<div style="display:flex; justify-content:space-between; align-items:center; margin-bottom: 2px;">' +
                    '<div style="font-weight: 800; color: var(--slate-800); font-size: 0.95rem; display: flex; align-items: center; gap: 8px;">' + icon + ' ' + escapeHtml(driveLetter) + '</div>' +
                    '<div style="font-size: 0.65rem; color: var(--slate-500); font-weight: 700; background: var(--slate-100); padding: 2px 5px; border-radius: 4px; letter-spacing: 0.5px;">' + escapeHtml(fileSystem) + '</div>' +
                    '</div>' +
                    '<div style="font-size: 0.8rem; color: var(--slate-500); font-weight: 500;">' +
                    '<span style="color:#10b981; font-weight:700;">' + free + ' GB</span> free of ' + total + ' GB' +
                    '</div>' +
                    '</div>' +
                    '</div>'
                );
            });
        } else {
            container.html('<div style="text-align:center; padding: 20px; color: var(--slate-400); font-weight: 500; font-size: 0.9rem; grid-column: 1 / -1;"><i class="fas fa-inbox text-2xl mb-2 block"></i>No logical drives found</div>');
        }
    }).fail(function () {
        $('#summaryLogicalDrivesContainer').html('<div style="text-align:center; padding: 20px; color: var(--red); font-weight: 500; font-size: 0.9rem; grid-column: 1 / -1;"><i class="fas fa-exclamation-triangle text-2xl mb-2 block"></i>Failed to load drive details</div>');
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
    }).fail(function () { console.error("Failed to load BIOS details"); });
}

function loadBatteryDetails() {
    $.get(`/ComputerSummary/Battery?domain=${domaindata}`, function (data) {
        $('#batteryManufacturer').text(data.manufacturer || 'N/A');
        $('#batteryStatus').text(data.status || 'N/A');
        $('#batteryDescription').text(data.description || 'N/A');
        $('#batteryLevel').text(data.batteryLevel || 'N/A');
        $('#batterySystemType').text(data.systemType || 'N/A');
    }).fail(function () { console.error("Failed to load battery details"); });
}

function loadMonitorDetails() {
    $.get(`/ComputerSummary/Monitor?domain=${domaindata}`, function (data) {
        $('#monitorManufacturer').text(data.manufacturer || 'N/A');
        $('#monitorType').text(data.monitorType || 'N/A');
        $('#monitorResolution').text((data.screenWidth || 'N/A') + ' x ' + (data.screenHeight || 'N/A'));
        $('#monitorSerial').text(data.serialNumber || 'N/A');
        $('#monitorSize').text(data.monitorSize || 'N/A');
        $('#monitorStatus').text(data.deviceStatus || 'N/A');
    }).fail(function () { console.error("Failed to load monitor details"); });
}

const CPU_ARCH_MAP = { '0': 'x86', '1': 'MIPS', '2': 'Alpha', '3': 'PowerPC', '5': 'ARM', '6': 'Itanium-based (IA-64)', '9': 'x64', '12': 'ARM64' };
const CPU_TYPE_MAP = { '1': 'Other', '2': 'Unknown', '3': 'Central Processor', '4': 'Math Processor', '5': 'DSP Processor', '6': 'Video Processor' };
const CPU_STATUS_MAP = { '0': 'Unknown', '1': 'CPU Enabled', '2': 'Disabled by User (BIOS)', '3': 'Disabled by BIOS (Error)', '4': 'CPU Idle', '5': 'Other', '6': 'Reserved', '7': 'Other' };
const CPU_SOCKET_MAP = {
    '4': 'ZIF Socket', '20': 'LGA771', '21': 'LGA775', '25': 'LGA1366', '27': 'AM3', '29': 'LGA1156',
    '36': 'LGA1155', '37': 'LGA1356', '38': 'LGA2011', '41': 'FM1', '42': 'FM2', '45': 'LGA1150',
    '49': 'AM4', '50': 'LGA1151', '54': 'LGA3647', '57': 'LGA2066', '62': 'LGA1200', '64': 'LGA1700',
    '73': 'AM5', '84': 'LGA1851'
};

const MEM_TYPE_MAP = {
    '0': 'Unknown', '1': 'Other', '2': 'DRAM', '3': 'SDRAM', '4': 'Cache DRAM', '5': 'EDO', '6': 'EDRAM', '7': 'VRAM', '8': 'SRAM', '9': 'RAM',
    '10': 'ROM', '11': 'Flash', '12': 'EEPROM', '13': 'FEPROM', '14': 'EPROM', '15': 'CDRAM', '16': '3DRAM', '17': 'SDRAM', '18': 'SGRAM',
    '19': 'RDRAM', '20': 'DDR', '21': 'DDR2', '22': 'DDR2 FB-DIMM', '24': 'DDR3', '25': 'FBD2', '26': 'DDR4', '34': 'DDR5', '35': 'LPDDR5'
};

const MEM_FORM_FACTOR_MAP = {
    '0': 'Unknown', '1': 'Other', '2': 'SIP', '3': 'DIP', '4': 'ZIP', '5': 'SOJ', '6': 'Proprietary', '7': 'SIMM', '8': 'DIMM', '9': 'TSOP',
    '10': 'PGA', '11': 'RIMM', '12': 'SODIMM', '13': 'SRIMM', '14': 'SMD', '15': 'SSMP', '16': 'QFP', '17': 'TQFP', '18': 'SOIC',
    '19': 'LCC', '20': 'PLCC', '21': 'BGA', '22': 'FPBGA', '23': 'LGA', '24': 'FB-DIMM'
};

function decodeMap(map, code) {
    if (code === null || code === undefined) return 'N/A';
    return map[String(code)] || code || 'N/A';
}

function cpuDecode(map, code) {
    return decodeMap(map, code);
}

function cpuDecodeVoltage(raw) {
    var v = parseInt(raw, 10);
    if (isNaN(v) || v === 0) return 'N/A';
    if (v & 0x80) return ((v & 0x7f) / 10).toFixed(1) + ' V';
    return 'Index ' + v;
}

function cpuTempColor(t) {
    if (t === undefined || t === null || isNaN(t) || t <= 0) return '#94a3b8';
    if (t < 50) return '#22c55e';
    if (t < 70) return '#f59e0b';
    return '#ef4444';
}

function cpuVal(d, ...keys) {
    if (!d) return undefined;
    var origKeys = Object.keys(d);
    var lower = origKeys.map(function (k) { return k.toLowerCase(); });
    for (var i = 1; i < arguments.length; i++) {
        var key = arguments[i];
        var idx = lower.indexOf(key.toLowerCase());
        if (idx > -1) {
            var actualKey = origKeys[idx];
            if (d[actualKey] !== undefined && d[actualKey] !== null && d[actualKey] !== '') {
                return d[actualKey];
            }
        }
    }
    return undefined;
}

function loadProcessorDetails() {
    $.get(`/ComputerSummary/Processors?domain=${domaindata}`, function (data) {
        if (!data) return;
        // TEMP DEBUG: open DevTools console and check this log to see the exact
        // field names the API actually returns, then compare against the cpuVal(...)
        // candidate keys used below. Remove this line once everything matches.
        console.log('[Processor API raw payload]', data);
        renderProcessorHero(data);
        renderProcessorSpecs(data);
        renderProcessorCache(data);
        renderProcessorThermal(data);
    }).fail(function () { console.error("Failed to load processor details"); });

    $.get(`/ComputerSummary/ProcessorHistory?domain=${domaindata}&count=20`, function (history) {
        renderProcessorTrendCharts(history);
    }).fail(function () { console.error("Failed to load processor trend history"); });
}

function renderProcessorHero(d) {
    $('#cpuName').text(cpuVal(d, 'name', 'Name', 'processorName', 'ProcessorName') || 'Unknown Processor');
    $('#cpuManufacturerBadge').html('<i class="fas fa-industry"></i> ' + (cpuVal(d, 'manufacturer', 'Manufacturer') || 'N/A'));
    $('#cpuArchBadge').html('<i class="fas fa-layer-group"></i> ' + cpuDecode(CPU_ARCH_MAP, cpuVal(d, 'architecture', 'Architecture')));
    $('#cpuTypeBadge').html('<i class="fas fa-tag"></i> ' + cpuDecode(CPU_TYPE_MAP, cpuVal(d, 'processorType', 'ProcessorType')));

    var status = cpuVal(d, 'status', 'Status') || 'Unknown';
    var cpuStatusLabel = cpuDecode(CPU_STATUS_MAP, cpuVal(d, 'cpuStatus', 'CpuStatus'));
    var isOk = String(status).toUpperCase() === 'OK' || cpuStatusLabel.indexOf('Enabled') > -1;
    var $badge = $('#cpuStatusBadge');
    $badge.toggleClass('is-down', !isOk);
    $badge.html('<span class="cpu-live-dot"></span> ' + (isOk ? 'Operational' : status));

    var dt = cpuVal(d, 'dateTime', 'DateTime');
    if (dt) {
        var date = new Date(dt);
        if (!isNaN(date.getTime())) {
            $('#cpuLastUpdated').text('Synced ' + date.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }));
        }
    }
}

function renderProcessorSpecs(d) {
    var cores = cpuVal(d, 'cores', 'Cores', 'numberOfCores', 'NumberOfCores') || 0;
    var threads = cpuVal(d, 'logicalProcessors', 'LogicalProcessors', 'numberOfLogicalProcessors', 'NumberOfLogicalProcessors', 'threadCount', 'ThreadCount') || 0;
    $('#cpuCoresThreads').text(cores + ' Cores / ' + threads + ' Threads');

    var baseGhz = parseFloat(cpuVal(d, 'baseSpeedGHz', 'BaseSpeedGHz')) || 0;
    var maxMHz = cpuVal(d, 'maxClockSpeedMHz', 'MaxClockSpeedMHz', 'maxClockSpeed', 'MaxClockSpeed') || 0;
    $('#cpuBaseClock').text(baseGhz > 0 ? baseGhz.toFixed(2) + ' GHz' : (maxMHz + ' MHz'));
    $('#cpuCurrentClock').text((cpuVal(d, 'currentClockSpeedMHz', 'CurrentClockSpeedMHz', 'currentClockSpeed', 'CurrentClockSpeed') || 0) + ' MHz');
    $('#cpuMaxClock').text(maxMHz + ' MHz');

    var bus = parseFloat(cpuVal(d, 'busSpeedMHz', 'BusSpeedMHz', 'extClock', 'ExtClock', 'externalClock', 'ExternalClock'));
    $('#cpuBusSpeed').text(bus > 0 ? bus.toFixed(1) + ' MHz' : 'N/A');

    $('#cpuSocket').text(cpuVal(d, 'socketDesignation', 'SocketDesignation') || 'N/A');
    $('#cpuUpgradeMethod').text(cpuDecode(CPU_SOCKET_MAP, cpuVal(d, 'upgradeMethod', 'UpgradeMethod')));

    var aw = cpuVal(d, 'addressWidth', 'AddressWidth');
    var dw = cpuVal(d, 'dataWidth', 'DataWidth');
    $('#cpuWidth').text((aw || '?') + '-bit / ' + (dw || '?') + '-bit');

    $('#cpuVoltage').text(cpuDecodeVoltage(cpuVal(d, 'voltage', 'Voltage', 'currentVoltage', 'CurrentVoltage')));
    $('#cpuProcessorId').text(cpuVal(d, 'processorId', 'ProcessorId', 'processorID', 'ProcessorID') || 'N/A');
}

function renderProcessorCache(d) {
    var l1 = parseFloat(cpuVal(d, 'l1CacheKB', 'L1CacheKB', 'l1CacheSize', 'L1CacheSize')) || 0;
    var l2 = parseFloat(cpuVal(d, 'l2CacheKB', 'L2CacheKB', 'l2CacheSize', 'L2CacheSize')) || 0;
    var l3 = parseFloat(cpuVal(d, 'l3CacheKB', 'L3CacheKB', 'l3CacheSize', 'L3CacheSize')) || 0;
    var max = Math.max(l1, l2, l3, 1);

    var rows = [
        { label: 'L1', value: l1, color: '#0ea5e9' },
        { label: 'L2', value: l2, color: '#6366f1' },
        { label: 'L3', value: l3, color: '#f59e0b' }
    ];

    var html = '';
    rows.forEach(function (r) {
        var pct = Math.max(4, (r.value / max) * 100);
        var displayVal = r.value >= 1024 ? (r.value / 1024).toFixed(1) + ' MB' : (r.value > 0 ? r.value + ' KB' : 'N/A');
        html += '<div class="cpu-cache-row">' +
            '<div class="cpu-cache-label">' + r.label + '</div>' +
            '<div class="cpu-cache-track"><div class="cpu-cache-fill" style="width:' + pct + '%;background:' + r.color + ';"></div></div>' +
            '<div class="cpu-cache-value">' + displayVal + '</div>' +
            '</div>';
    });
    $('#cpuCacheContainer').html(html);
}

function renderProcessorThermal(d) {
    var pkgTemp = parseFloat(cpuVal(d, 'cpuPackageTemperature', 'CpuPackageTemperature', 'packageTemperature', 'PackageTemperature', 'cpuTemperature', 'CpuTemperature')) || 0;
    var pkgPower = parseFloat(cpuVal(d, 'cpuPackagePower', 'CpuPackagePower', 'packagePower', 'PackagePower', 'powerDraw', 'PowerDraw')) || 0;

    var circumference = 2 * Math.PI * 45;
    var maxTemp = 100;
    var dashLen = Math.min(100, (pkgTemp / maxTemp) * 100) / 100 * circumference;
    var color = cpuTempColor(pkgTemp);

    $('#cpuPackageTempCircle').css({ stroke: color, 'stroke-dasharray': dashLen + ', ' + circumference });
    $('#cpuPackageTempText').text(pkgTemp > 0 ? pkgTemp.toFixed(0) + '\u00B0C' : 'N/A');
    $('#cpuPackagePowerText').text('Power draw: ' + (pkgPower > 0 ? pkgPower.toFixed(1) + ' W' : 'N/A'));

    var cores = [
        { label: 'Core 0', value: parseFloat(cpuVal(d, 'core0Temp', 'Core0Temp', 'coreTemp0', 'CoreTemp0')) || 0 },
        { label: 'Core 1', value: parseFloat(cpuVal(d, 'core1Temp', 'Core1Temp', 'coreTemp1', 'CoreTemp1')) || 0 },
        { label: 'Core 2', value: parseFloat(cpuVal(d, 'core2Temp', 'Core2Temp', 'coreTemp2', 'CoreTemp2')) || 0 },
        { label: 'Core 3', value: parseFloat(cpuVal(d, 'core3Temp', 'Core3Temp', 'coreTemp3', 'CoreTemp3')) || 0 }
    ];

    var html = '';
    cores.forEach(function (c) {
        var cColor = cpuTempColor(c.value);
        var cDash = Math.min(100, (c.value / maxTemp) * 100);
        html += '<div class="cpu-core-gauge">' +
            '<svg viewBox="0 0 36 36" style="width:48px;height:48px;transform:rotate(-90deg);">' +
            '<path stroke="#e2e8f0" stroke-width="3.2" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />' +
            '<path stroke="' + cColor + '" stroke-width="3.2" stroke-dasharray="' + cDash + ', 100" fill="none" stroke-linecap="round" style="transition:stroke-dasharray 1s ease;" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />' +
            '</svg>' +
            '<div class="cpu-core-gauge-label">' + c.label + '</div>' +
            '<div class="cpu-core-gauge-value" style="color:' + cColor + ';">' + (c.value > 0 ? c.value.toFixed(0) + '\u00B0C' : 'N/A') + '</div>' +
            '</div>';
    });
    $('#cpuCoreGaugeContainer').html(html);
}

let cpuTempChartInstance = null;
let cpuClockChartInstance = null;

function renderProcessorTrendCharts(history) {
    if (!history || !history.length) return;

    var labels = history.map(function (h) {
        var dt = new Date(cpuVal(h, 'dateTime', 'DateTime'));
        return isNaN(dt.getTime()) ? '' : dt.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
    });

    var tempCanvas = document.getElementById('cpuTempTrendChart');
    if (tempCanvas) {
        if (cpuTempChartInstance) cpuTempChartInstance.destroy();
        cpuTempChartInstance = new Chart(tempCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Package', data: history.map(h => parseFloat(cpuVal(h, 'cpuPackageTemperature', 'CpuPackageTemperature', 'packageTemperature', 'PackageTemperature')) || 0), borderColor: '#ef4444', backgroundColor: 'rgba(239,68,68,.08)', borderWidth: 2, fill: true, tension: 0, pointRadius: 0 },
                    { label: 'Core 0', data: history.map(h => parseFloat(cpuVal(h, 'core0Temp', 'Core0Temp', 'coreTemp0', 'CoreTemp0')) || 0), borderColor: '#0ea5e9', borderWidth: 1.5, fill: false, tension: 0, pointRadius: 0 },
                    { label: 'Core 1', data: history.map(h => parseFloat(cpuVal(h, 'core1Temp', 'Core1Temp', 'coreTemp1', 'CoreTemp1')) || 0), borderColor: '#22c55e', borderWidth: 1.5, fill: false, tension: 0, pointRadius: 0 },
                    { label: 'Core 2', data: history.map(h => parseFloat(cpuVal(h, 'core2Temp', 'Core2Temp', 'coreTemp2', 'CoreTemp2')) || 0), borderColor: '#f59e0b', borderWidth: 1.5, fill: false, tension: 0, pointRadius: 0 },
                    { label: 'Core 3', data: history.map(h => parseFloat(cpuVal(h, 'core3Temp', 'Core3Temp', 'coreTemp3', 'CoreTemp3')) || 0), borderColor: '#a855f7', borderWidth: 1.5, fill: false, tension: 0, pointRadius: 0 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } } },
                scales: { y: { title: { display: true, text: '°C' } } }
            }
        });
    }

    var clockCanvas = document.getElementById('cpuClockTrendChart');
    if (clockCanvas) {
        if (cpuClockChartInstance) cpuClockChartInstance.destroy();
        cpuClockChartInstance = new Chart(clockCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Current Clock (MHz)', data: history.map(h => cpuVal(h, 'currentClockSpeedMHz', 'CurrentClockSpeedMHz', 'currentClockSpeed', 'CurrentClockSpeed') || 0), borderColor: '#0ea5e9', backgroundColor: 'rgba(14,165,233,.08)', borderWidth: 2, fill: true, tension: 0, pointRadius: 0 },
                    { label: 'Bus Speed (MHz)', data: history.map(h => parseFloat(cpuVal(h, 'busSpeedMHz', 'BusSpeedMHz', 'extClock', 'ExtClock')) || 0), borderColor: '#cbd5e1', borderWidth: 1.5, borderDash: [5, 5], fill: false, pointRadius: 0 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } } },
                scales: { y: { title: { display: true, text: 'MHz' } } }
            }
        });
    }
}

$(document).on('click', '#btnRefreshProcessor', function (e) {
    e.preventDefault();
    var $btn = $(this).addClass('spinning');
    loadProcessorDetails();
    setTimeout(function () { $btn.removeClass('spinning'); }, 600);
});

$(document).on('click', '#btnRefreshMemory', function (e) {
    e.preventDefault();
    var $btn = $(this).addClass('spinning');
    loadMemoryDetails();
    setTimeout(function () { $btn.removeClass('spinning'); }, 600);
});

function loadNetworkAdapters() {
    $.get(`/ComputerSummary/NetworkAdapters?domain=${domaindata}`, function (data) {
        var container = $('#networkContainer').empty();
        if (data && data.length) {
            data.forEach(function (a) {
                container.append(
                    '<div class="bg-gray-50 rounded-lg border border-gray-100 overflow-hidden hover:shadow-md transition-shadow">' +
                    '<div class="px-4 py-3 bg-white border-b border-gray-100"><h4 class="font-semibold text-gray-800 text-sm flex items-center gap-2">' +
                    '<i class="fas fa-network-wired text-blue-500"></i> ' + escapeHtml(a.description || 'Network Adapter') + '</h4></div>' +
                    '<div class="p-4 space-y-2 text-sm">' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">Manufacturer:</span><span class="text-gray-700">' + escapeHtml(a.manufacturer || 'N/A') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">MAC Address:</span><span class="font-mono text-gray-700">' + escapeHtml(a.macAddress || 'N/A') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">IP Address:</span><span class="font-mono text-gray-700">' + escapeHtml(a.ipAddress || 'N/A') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">DHCP:</span><span>' + (a.dhcpEnabled ? '<i class="fas fa-check-circle text-green-500"></i> Yes' : '<i class="fas fa-times-circle text-red-400"></i> No') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">Status:</span><span class="' + (a.deviceStatus === 'OK' ? 'text-green-600' : 'text-red-500') + '">' + escapeHtml(a.deviceStatus || 'N/A') + '</span></div>' +
                    '</div></div>'
                );
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
        var container = $('#keyboardContainer').empty();
        if (data && data.length) {
            data.forEach(function (k) {
                container.append(
                    '<div class="bg-gray-50 rounded-lg border border-gray-100 p-4 hover:shadow-md transition-shadow">' +
                    '<div class="flex items-center gap-3 mb-3"><i class="fas fa-keyboard text-gray-500 text-xl"></i>' +
                    '<h4 class="font-semibold text-gray-800">' + escapeHtml(k.manufacturer || 'Keyboard') + '</h4></div>' +
                    '<div class="space-y-1 text-sm">' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">Description:</span><span class="text-gray-700">' + escapeHtml(k.description || 'N/A') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">Status:</span><span class="' + (k.deviceStatus === 'OK' ? 'text-green-600' : 'text-red-500') + '">' + escapeHtml(k.deviceStatus || 'N/A') + '</span></div>' +
                    '</div></div>'
                );
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
        var container = $('#motherboardContainer').empty();
        if (data && data.length) {
            data.forEach(function (mb) {
                container.append(
                    '<div class="bg-gray-50 rounded-lg border border-gray-100 p-4 hover:shadow-md transition-shadow">' +
                    '<div class="flex items-center gap-3 mb-3"><i class="fas fa-microchip text-blue-500 text-xl"></i>' +
                    '<h4 class="font-semibold text-gray-800">' + escapeHtml(mb.manufacturer || 'Motherboard') + '</h4></div>' +
                    '<div class="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">' +
                    '<div><span class="text-gray-500">Model:</span> <span class="text-gray-700">' + escapeHtml(mb.model || 'N/A') + '</span></div>' +
                    '<div><span class="text-gray-500">Version:</span> <span class="text-gray-700">' + escapeHtml(mb.version || 'N/A') + '</span></div>' +
                    '<div><span class="text-gray-500">Serial:</span> <span class="font-mono text-gray-700">' + escapeHtml(mb.serialNumber || 'N/A') + '</span></div>' +
                    '<div><span class="text-gray-500">Status:</span> <span class="' + (mb.deviceStatus === 'OK' ? 'text-green-600' : 'text-red-500') + '">' + escapeHtml(mb.deviceStatus || 'N/A') + '</span></div>' +
                    '</div></div>'
                );
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
    if (!softwareName || softwareName === 'N/A') return;

    Swal.fire({
        title: 'Confirm Uninstall',
        text: 'Are you sure you want to uninstall ' + softwareName + '?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        confirmButtonText: 'Yes, Uninstall',
        cancelButtonText: 'Cancel'
    }).then(function (result) {
        if (!result.isConfirmed) return;

        sysAlert('Sending uninstall command�', 'info');

        $.ajax({
            url: '/ComputerSummary/Uninstallsoftware?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ softwareName: softwareName }),
            success: function (res) {
                if (res && res.status === 'success') {
                    sysAlert('Uninstall command sent. Checking status�', 'info');
                    pollUninstallStatus(softwareName, 0);
                } else {
                    sysAlert('Uninstall failed: ' + (res && res.message ? res.message : 'Unknown error'), 'error');
                }
            },
            error: function (xhr) {
                sysAlert('Failed to send uninstall command (HTTP ' + xhr.status + ')', 'error');
            }
        });
    });
}

function pollUninstallStatus(softwareName, attempt) {
    if (attempt > 200) {
        sysAlert('Uninstall status unknown - check the device manually.', 'warning');
        reloadTable('#desktopAppsTable');
        return;
    }
    setTimeout(function () {
        $.get('/ComputerSummary/Uninstallsoftwarestatus', {
            softwareName: softwareName,
            domain: actualDomainName
        }, function (res) {
            if (res && res.status === 'success') {
                sysAlert(softwareName + ' uninstalled successfully.', 'success');
                reloadTable('#desktopAppsTable');
            } else if (res && res.status === 'Failed') {
                sysAlert('Uninstall failed on device: ' + (res.message || ''), 'error');
                reloadTable('#desktopAppsTable');
            } else {
                pollUninstallStatus(softwareName, attempt + 1);
            }
        }).fail(function () {
            pollUninstallStatus(softwareName, attempt + 1);
        });
    }, 3000);
}

function installSoftware(fileName) {
    if (!fileName || fileName === 'N/A') return;

    Swal.fire({
        title: 'Confirm Installation',
        text: 'Are you sure you want to install ' + fileName + '?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#10b981',
        confirmButtonText: 'Yes, Install',
        cancelButtonText: 'Cancel'
    }).then(function (result) {
        if (!result.isConfirmed) return;

        sysAlert('Sending install command�', 'info');

        $.ajax({
            url: '/ComputerSummary/PatchUpdate?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ softwareName: fileName }),
            success: function (res) {
                if (res && res.status === 'success') {
                    sysAlert('Install command sent. Checking status�', 'info');
                    pollInstallStatus(fileName, 0);
                } else {
                    sysAlert('Install failed: ' + (res && res.message ? res.message : 'Unknown error'), 'error');
                }
            },
            error: function (xhr) {
                sysAlert('Failed to send install command (HTTP ' + xhr.status + ')', 'error');
            }
        });
    });
}

function pollInstallStatus(fileName, attempt) {
    if (attempt > 150) {
        sysAlert('Install status unknown - check the device manually.', 'warning');
        reloadTable('#installersTable');
        reloadTable('#desktopAppsTable');
        return;
    }
    setTimeout(function () {
        $.get('/ComputerSummary/installsoftwarestatus', {
            softwareName: fileName,
            domain: actualDomainName
        }, function (res) {
            if (res && res.status === 'success') {
                sysAlert(fileName + ' installed successfully.', 'success');
                reloadTable('#installersTable');
                reloadTable('#desktopAppsTable');
            } else if (res && res.status === 'Failed') {
                sysAlert('Install failed on device: ' + (res.message || ''), 'error');
                reloadTable('#installersTable');
            } else {
                pollInstallStatus(fileName, attempt + 1);
            }
        }).fail(function () {
            pollInstallStatus(fileName, attempt + 1);
        });
    }, 4000);
}

function installPatch(patchId, patchName) {
    if (!patchId || patchId === 'N/A') return;

    var displayName = patchName || patchId;

    Swal.fire({
        title: 'Confirm Update',
        text: 'Are you sure you want to install: ' + displayName + '?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#3b82f6',
        confirmButtonText: 'Yes, Install',
        cancelButtonText: 'Cancel'
    }).then(function (result) {
        if (!result.isConfirmed) return;

        sysAlert('Sending patch command�', 'info');
        $.ajax({
            url: '/ComputerSummary/PatchUpdate?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ softwareName: displayName }),
            success: function (res) {
                if (res && res.status === 'success') {
                    sysAlert('Patch command sent. Checking status�', 'info');
                    pollInstallStatus(displayName, 0);
                } else {
                    sysAlert('Patch failed: ' + (res && res.message ? res.message : 'Unknown error'), 'error');
                }
            },
            error: function (xhr) {
                sysAlert('Failed to send patch command (HTTP ' + xhr.status + ')', 'error');
            }
        });
    });
}

function reloadTable(selector) {
    if ($.fn.DataTable.isDataTable(selector)) {
        $(selector).DataTable().ajax.reload(null, false);
    }
}

function refreshSoftwareTable() {
    reloadTable('#desktopAppsTable');
}

function applyUsbBlock() {
    if (!domaindata) return;
    Swal.fire({
        title: 'Block USB?',
        text: 'This will disable USB ports on ' + domaindata,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        confirmButtonText: 'Yes, Block'
    }).then(function (result) {
        if (!result.isConfirmed) return;
        $.ajax({
            url: '/ComputerSummary/BlockUsb?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            success: function (res) {
                if (res && res.success) sysAlert('USB Blocked successfully', 'success');
                else sysAlert(res.message || 'Block failed', 'error');
            },
            error: function () { sysAlert('Connection error', 'error'); }
        });
    });
}

function applyUsbUnblock() {
    if (!domaindata) return;
    Swal.fire({
        title: 'Unblock USB?',
        text: 'This will enable USB ports on ' + domaindata,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#10b981',
        confirmButtonText: 'Yes, Unblock'
    }).then(function (result) {
        if (!result.isConfirmed) return;
        $.ajax({
            url: '/ComputerSummary/UnblockUsb?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            success: function (res) {
                if (res && res.success) sysAlert('USB Unblocked successfully', 'success');
                else sysAlert(res.message || 'Unblock failed', 'error');
            },
        });
    });
}

function checkBatteryReportExists() {
    $.get(`/ComputerSummary/BatteryReportExists?domain=${actualDomainName}`, function (res) {
        if (res && res.exists) {
            $('#btnViewBatteryReport').show();
        } else {
            $('#btnViewBatteryReport').hide();
        }
    });
}

function renderBatteryAuditPanel(metrics) {
    if (!metrics) return;

    $('#batteryAuditLoading').hide();
    $('#batteryAuditResults').css('display', 'flex');

    let healthPercent = metrics.healthPercentage !== undefined ? metrics.healthPercentage : (metrics.batteryHealthPercent || 0);

    $('#auditHealthPercentText').text(healthPercent + '%');
    $('#auditDesignCap').text(metrics.designCapacity ? metrics.designCapacity.toLocaleString() : '--');
    $('#auditFullCap').text(metrics.fullChargeCapacity ? metrics.fullChargeCapacity.toLocaleString() : '--');
    $('#auditCycleCount').text(metrics.cycleCount > 0 ? metrics.cycleCount : '--');

    $('#auditWearRate').text(metrics.wearRatePerMonth !== undefined ? metrics.wearRatePerMonth : '--');

    let remainingLife = '--';
    if (metrics.estimatedRemainingMonths !== undefined) {
        if (metrics.estimatedRemainingMonths === 999) remainingLife = 'Healthy';
        else remainingLife = metrics.estimatedRemainingMonths;
    }
    $('#auditRemainingLife').text(remainingLife);

    const circle = $('#auditHealthCircle');
    var circumference = 2 * Math.PI * 45;
    var dashLen = (healthPercent / 100) * circumference;
    circle.css('stroke-dasharray', dashLen + ', ' + circumference);

    let color = '#4ade80';
    let icon = '<i class="fas fa-check-circle" style="color:#4ade80;"></i>';

    let status = metrics.status || 'Unknown';
    if (status === 'Aging' || (healthPercent < 80 && healthPercent >= 60)) {
        color = '#f59e0b';
        icon = '<i class="fas fa-exclamation-triangle" style="color:#f59e0b;"></i>';
    } else if (status === 'Replacement Recommended' || (healthPercent < 60 && healthPercent >= 50)) {
        color = '#f97316';
        icon = '<i class="fas fa-tools" style="color:#f97316;"></i>';
    } else if (status === 'Critical' || healthPercent < 50) {
        color = '#ef4444';
        icon = '<i class="fas fa-times-circle" style="color:#ef4444;"></i>';
    }

    circle.css('stroke', color);
    $('#auditStatus').html(`${icon} ${status}`);

    renderCapacityTrendChart(metrics.capacityHistory);
    checkBatteryReportExists();
}

let capacityChartInstance = null;
function renderCapacityTrendChart(history) {
    const container = $('#capacityChartContainer');
    if (!history || history.length < 2) {
        container.hide();
        return;
    }
    container.show();

    const canvas = document.getElementById('capacityTrendChart');
    if (!canvas) return;

    if (capacityChartInstance) {
        capacityChartInstance.destroy();
    }

    const labels = history.map(h => h.period);
    const fullChargeData = history.map(h => h.fullChargeCapacity);
    const designCapData = history.map(h => h.designCapacity);

    capacityChartInstance = new Chart(canvas.getContext('2d'), {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Full Charge Capacity (mWh)',
                    data: fullChargeData,
                    borderColor: '#0ea5e9',
                    backgroundColor: 'rgba(14, 165, 233, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.3
                },
                {
                    label: 'Design Capacity (mWh)',
                    data: designCapData,
                    borderColor: '#cbd5e1',
                    borderWidth: 2,
                    borderDash: [5, 5],
                    fill: false,
                    pointRadius: 0
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'bottom' }
            },
            scales: {
                y: {
                    beginAtZero: false,
                    title: { display: true, text: 'Capacity (mWh)' }
                }
            }
        }
    });
}

$(document).ready(function () {
    let batteryAuditTimeout;
    let batteryAuditPollInterval;

    $('#btnViewBatteryReport').on('click', function (e) {
        e.preventDefault();
        window.open(`/ComputerSummary/ViewBatteryReport?domain=${encodeURIComponent(actualDomainName)}`, '_blank');
    });

    $('#btnAuditBattery').on('click', function (e) {
        e.preventDefault();
        $('#batteryAuditResults').hide();
        $('#batteryAuditLoading').html('<i class="fas fa-circle-notch fa-spin" style="font-size: 1.5rem; color: var(--cyan); margin-bottom: 8px; display: block;"></i> Fetching live diagnostics from device, please wait...');
        $('#batteryAuditLoading').show();

        function triggerBatteryFallback(reasonMsg) {
            if (!$('#batteryAuditLoading').is(':visible')) return;
            $.get(`/ComputerSummary/Battery?domain=${domaindata}`, function (data) {
                renderBatteryAuditPanel(data);
                sysAlert(reasonMsg || 'Live fetch unavailable. Showing last known state.', 'warning');
            });
        }

        sysAlert('Battery audit requested from device. Waiting for data...', 'info');

        $.ajax({
            url: '/ComputerSummary/AuditBattery?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            timeout: 30000,
            success: function (res) {
                if (res && res.success && res.data && res.data.metrics) {
                    renderBatteryAuditPanel(res.data.metrics);
                    sysAlert('Battery health data received!', 'success');
                } else {
                    triggerBatteryFallback(res ? res.message : 'Failed to receive audit data, falling back...');
                }
            },
            error: function (xhr, status, error) {
                let msg = status === 'timeout' ? 'Live fetch timed out. Showing last known state.' : 'Connection error while requesting audit, falling back...';
                triggerBatteryFallback(msg);
            }
        });
    });
    checkBatteryReportExists();
});
