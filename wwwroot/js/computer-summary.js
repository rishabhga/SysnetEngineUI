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
        console.error("Domain ID not found â€” skipping data load");
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
            loadProcessorDetails(false);  
            loadNetworkAdapters();
            loadKeyboardDetails();
            loadMotherboardDetails();
            loadMotherboardHealthHistory();
            loadMemoryDetails();
            loadHardDiskDetails();
            break;
        case '#Battery':
            checkBatteryReportExists();
            loadBatteryDetails();
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

        var firstSubTabMap = {
            '#Hardware': '#hardwareSubTabs .hardware-tab:first-child a',
            '#System': '#systemSubTabs .system-tab:first-child a',
            '#Software': '#softwareSubTabs .software-tab:first-child a',
            '#Security': '#securitySubTabs .security-tab:first-child a',
            '#History': '#historySubTabs .history-tab:first-child a'
        };
        if (firstSubTabMap[target]) {
            var $firstSubTab = $(firstSubTabMap[target]);
            if ($firstSubTab.length) {
                var $activeSubPane = $(target).find('.tab-pane.active');
                if ($activeSubPane.length === 0) {
                    $firstSubTab.trigger('click');
                }
            }
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
        '.updatelog-tab a',
        '.restriction-tab a',
        '.usbaudit-tab a'
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
        { data: null, render: (row) => flexRender(row, 'DisplayName') },
        {
            data: null, render: (row) => {
                const st = (row.startupType || row.StartupType || '').toString();
                let color = '#64748b', bg = '#f1f5f9';
                const stl = st.toLowerCase();
                if (stl === 'automatic' || stl === 'auto') { color = '#166534'; bg = '#dcfce7'; }
                else if (stl === 'manual') { color = '#92400e'; bg = '#fef3c7'; }
                else if (stl === 'disabled') { color = '#991b1b'; bg = '#fee2e2'; }
                return `<span style="display:inline-block;font-size:.74rem;font-weight:700;padding:2px 8px;border-radius:999px;background:${bg};color:${color};white-space:nowrap;">${st || 'Unknown'}</span>`;
            }
        },
        {
            data: null, render: (row) => {
                const state = (row.state || row.State || row.status || row.Status || '').toString();
                const running = state.toLowerCase() === 'running';
                const dot = `<span style="display:inline-block;width:7px;height:7px;border-radius:50%;background:${running ? '#22c55e' : '#94a3b8'};margin-right:5px;flex-shrink:0;"></span>`;
                return `<span style="display:inline-flex;align-items:center;font-size:.78rem;font-weight:600;color:${running ? '#166534' : '#475569'};white-space:nowrap;">${dot}${state || 'Unknown'}</span>`;
            }
        },
        { data: null, render: (row) => flexRender(row, 'LogonName') },
        { data: null, render: (row) => flexRender(row, 'DateTime') },
        {
            data: null, render: (row) => {
                const sName = (row.displayName || row.DisplayName || '').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
                const state = (row.state || row.State || row.status || row.Status || '').toLowerCase();
                const stype = (row.startupType || row.StartupType || '').toLowerCase();
                const disabled = stype === 'disabled';

                let btns = '';
                if (disabled) {
                    btns = `<span style="font-size:.72rem;color:#94a3b8;font-weight:600;padding:3px 8px;background:#f1f5f9;border-radius:6px;">Disabled</span>`;
                } else if (state === 'running') {
                    btns = `
                        <button data-svc="${sName}" data-action="stop" class="svc-btn svc-stop" title="Stop ${sName}"><i class="fas fa-stop"></i></button>
                        <button data-svc="${sName}" data-action="restart" class="svc-btn svc-restart" title="Restart ${sName}"><i class="fas fa-redo"></i></button>`;
                } else {
                    btns = `
                        <button data-svc="${sName}" data-action="start" class="svc-btn svc-start" title="Start ${sName}"><i class="fas fa-play"></i></button>`;
                }

                return `<div style="display:flex;align-items:center;gap:5px;">${btns}</div>`;
            }
        }
    ]);

    if (!document.getElementById('svcBtnStyle')) {
        const s = document.createElement('style');
        s.id = 'svcBtnStyle';
        s.textContent = `
            .svc-btn { display:inline-flex;align-items:center;justify-content:center;width:30px;height:30px;border:none;border-radius:7px;cursor:pointer;font-size:.72rem;transition:filter .15s,transform .1s;flex-shrink:0; }
            .svc-btn:hover { filter:brightness(.88);transform:scale(1.08); }
            .svc-btn:active { transform:scale(.96); }
            .svc-stop    { background:#fee2e2;color:#991b1b; }
            .svc-restart { background:#dbeafe;color:#1d4ed8; }
            .svc-start   { background:#dcfce7;color:#166534; }
            .svc-btn.loading { opacity:.35;pointer-events:none; }
            .svc-processing { display:inline-flex;align-items:center;gap:5px;font-size:.72rem;font-weight:600;color:var(--cyan,#06b6d4);white-space:nowrap; }
        `;
        document.head.appendChild(s);
    }

    $(document).off('click.svc').on('click.svc', '.svc-btn', function () {
        const $btn = $(this);
        const $row = $btn.closest('tr');
        const serviceName = $btn.data('svc');
        const action = $btn.data('action');

        if (!serviceName) {
            sysAlert('Service name could not be read. Try refreshing the page.', 'error');
            return;
        }

        const actionLabel = action.charAt(0).toUpperCase() + action.slice(1);
        if (!confirm(`${actionLabel} the service "${serviceName}"?`)) return;

        const $actionCell = $btn.closest('div');
        $actionCell.find('.svc-btn').addClass('loading');

        const $spinner = $(`<span class="svc-processing"><i class="fas fa-circle-notch fa-spin"></i> Processing...</span>`);
        $actionCell.append($spinner);

        sysAlert(`Sending ${actionLabel} command to "${serviceName}"...`, 'info');

        $.ajax({
            url: `/ComputerSummary/ControlService`,
            type: 'POST',
            timeout: 0, 
            data: {
                domain: actualDomainName,
                serviceName: serviceName,
                action: action
            },
            success: function (res) {
                if (res && res.success) {
                    sysAlert(res.message || `"${serviceName}" — ${actionLabel} completed.`, 'success');
                    if ($.fn.DataTable.isDataTable('#servicesTable')) {
                        $('#servicesTable').DataTable().ajax.reload(null, false);
                    }
                } else {
                    sysAlert(res && res.message ? res.message : `Command sent but state not confirmed yet.`, 'warning');
                    $spinner.remove();
                    $actionCell.find('.svc-btn').removeClass('loading');
                }
            },
            error: function () {
                sysAlert(`Could not reach the device. It may be offline.`, 'error');
                $spinner.remove();
                $actionCell.find('.svc-btn').removeClass('loading');
            }
        });
    });

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

    window.expandedDriversGroups = window.expandedDriversGroups || {};

    function getDeviceIcon(category) {
        let icon = 'fa-microchip';
        if (!category) return icon;
        const g = category.toLowerCase();

        if (g.includes('computer')) icon = 'fa-desktop';
        else if (g.includes('display') || g.includes('monitor')) icon = 'fa-tv';
        else if (g.includes('audio') || g.includes('sound') || g.includes('media')) icon = 'fa-volume-up';
        else if (g.includes('disk') || g.includes('volume') || g.includes('drive')) icon = 'fa-hdd';
        else if (g.includes('mouse') || g.includes('pointing')) icon = 'fa-mouse';
        else if (g.includes('keyboard')) icon = 'fa-keyboard';
        else if (g.includes('print') || g.includes('fax')) icon = 'fa-print';
        else if (g.includes('net') || g.includes('wan') || g.includes('wi-fi') || g.includes('bluetooth')) icon = 'fa-network-wired';
        else if (g.includes('usb')) icon = 'fa-usb';
        else if (g.includes('processor') || g.includes('cpu')) icon = 'fa-microchip';
        else if (g.includes('hid') || g.includes('human interface')) icon = 'fa-gamepad';
        else if (g.includes('storage') || g.includes('scsi')) icon = 'fa-database';
        else if (g.includes('system') || g.includes('board') || g.includes('ide') || g.includes('hdc')) icon = 'fa-microchip';

        return icon;
    }

    initTable('#driversTable', `/ComputerSummary/drivers?domain=${domaindata}`, [
        { data: function (row) { return row.Category || row.category || 'Other Devices'; }, visible: false },
        {
            data: null,
            render: function (data, type, row) {
                let icon = getDeviceIcon(row.Category || row.category);
                return `<div class="flex items-center relative pl-6">
                            <div class="absolute left-2 top-0 bottom-0 w-px bg-slate-300"></div>
                            <div class="absolute left-2 top-1/2 w-3 h-px bg-slate-300"></div>
                            <i class="fas ${icon} mr-2 text-slate-500 w-4 text-center z-10 bg-white"></i>
                            <span class="z-10 truncate" title="${flexRender(row, 'DeviceName', 'Name')}">${flexRender(row, 'DeviceName', 'Name')}</span>
                        </div>`;
            }
        },
        { data: null, render: (row) => flexRender(row, 'Manufacturer', 'Description') },
        { data: null, render: (row) => flexRender(row, 'Status') },
        { data: null, render: (row) => flexRender(row, 'DateTime') }
    ], {
        order: [[0, 'asc']],
        paging: false,
        rowGroup: {
            dataSrc: function (row) {
                return row.Category || row.category || 'Other Devices';
            },
            startRender: function (rows, group) {
                let icon = getDeviceIcon(group);
                var expanded = !!window.expandedDriversGroups[group];

                rows.nodes().each(function (r) {
                    r.style.display = expanded ? '' : 'none';
                });

                return $('<tr class="cursor-pointer group-header hover:bg-slate-50 transition-colors" data-name="' + group + '"/>')
                    .append('<td colspan="5" class="bg-white font-bold text-slate-800 border-y border-slate-200 py-2 px-2 shadow-sm">' +
                        '<i class="fas fa-chevron-' + (expanded ? 'down' : 'right') + ' mr-2 w-4 text-center text-slate-400 text-xs"></i>' +
                        '<i class="fas ' + icon + ' mr-2 text-cyan-600"></i>' +
                        group +
                        ' <span class="text-xs text-slate-500 font-normal ml-2">(' + rows.count() + ')</span></td>');
            }
        }
    });

    $('#driversTable tbody').off('click', 'tr.group-header').on('click', 'tr.group-header', function () {
        var name = $(this).data('name');
        if (name) {
            window.expandedDriversGroups[name] = !window.expandedDriversGroups[name];
            $('#driversTable').DataTable().draw(false);
        }
    });


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
let _fullMemHistory = [];
let _fullCpuHistory = [];

function formatChartLabel(dt) {
    if (isNaN(dt.getTime())) return '';
    var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return months[dt.getMonth()] + ' ' + dt.getDate() + ' ' + String(dt.getHours()).padStart(2, '0') + ':' + String(dt.getMinutes()).padStart(2, '0');
}

function chartScaleXOptions() {
    return {
        x: {
            ticks: { maxRotation: 45, minRotation: 25, font: { size: 9 }, autoSkip: true, maxTicksLimit: 12 }
        }
    };
}

function filterByRange(history, range, dateKey, startDate, endDate) {
    if (!history || !history.length || range === 'all') return history;
    var now = new Date();
    var cutoffStart, cutoffEnd;
    if (range === 'today') {
        cutoffStart = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        cutoffEnd = now;
    } else if (range === '7d') {
        cutoffStart = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        cutoffEnd = now;
    } else if (range === '30d') {
        cutoffStart = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
        cutoffEnd = now;
    } else if (range === '90d') {
        cutoffStart = new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000);
        cutoffEnd = now;
    } else if (range === 'custom' && startDate && endDate) {
        cutoffStart = new Date(startDate);
        cutoffEnd = new Date(endDate);
        cutoffEnd.setHours(23, 59, 59, 999);
    } else {
        return history;
    }
    return history.filter(function (h) {
        var val = h[dateKey] || h['DateTime'] || h['dateTime'];
        var dt = new Date(val);
        return !isNaN(dt.getTime()) && dt >= cutoffStart && dt <= cutoffEnd;
    });
}

function setActiveFilter(container, btn) {
    $(container).find('.chart-filter-btn').removeClass('active');
    if (btn) $(btn).addClass('active');
}

function filterMemChart(range, btn) {
    setActiveFilter('#memChartFilters', btn);
    if (range === 'custom') {
        var s = $('#memDateStart').val(), e = $('#memDateEnd').val();
        if (!s || !e) return;
        var filtered = filterByRange(_fullMemHistory, 'custom', 'dateTime', s, e);
        renderMemoryTrendChart(filtered, true);
    } else {
        $('#memDateStart').val(''); $('#memDateEnd').val('');
        var filtered = filterByRange(_fullMemHistory, range, 'dateTime');
        renderMemoryTrendChart(filtered, true);
    }
}

function filterCpuChart(range, btn) {
    setActiveFilter('#cpuChartFilters', btn);
    if (range === 'custom') {
        var s = $('#cpuDateStart').val(), e = $('#cpuDateEnd').val();
        if (!s || !e) return;
        var filtered = filterByRange(_fullCpuHistory, 'custom', 'dateTime', s, e);
        renderProcessorTrendCharts(filtered, true);
    } else {
        $('#cpuDateStart').val(''); $('#cpuDateEnd').val('');
        var filtered = filterByRange(_fullCpuHistory, range, 'dateTime');
        renderProcessorTrendCharts(filtered, true);
    }
}

function renderMemoryTrendChart(history, skipStore) {
    if (!history || !history.length) return;
    if (!skipStore) _fullMemHistory = history;

    var labels = history.map(function (h) {
        return formatChartLabel(new Date(h.dateTime || h.DateTime));
    });

    var usageCanvas = document.getElementById('memUsageTrendChart');
    if (usageCanvas) {
        if (memUsageChartInstance) memUsageChartInstance.destroy();
        memUsageChartInstance = new Chart(usageCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Usage %', data: history.map(h => parseFloat(h.usagePercent || h.UsagePercent) || 0), borderColor: '#0ea5e9', backgroundColor: 'rgba(14, 165, 233, .1)', borderWidth: 2, fill: true, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { mode: 'index', intersect: false } },
                scales: Object.assign({ y: { title: { display: true, text: '%' }, min: 0, max: 100 } }, chartScaleXOptions())
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
                    { label: 'Used (GB)', data: history.map(h => parseFloat(h.usedMemoryGB || h.UsedMemoryGB) || 0), borderColor: '#0ea5e9', borderWidth: 2, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Free (GB)', data: history.map(h => parseFloat(h.freeMemoryGB || h.FreeMemoryGB) || 0), borderColor: '#22c55e', borderWidth: 2, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } }, tooltip: { mode: 'index', intersect: false } },
                scales: Object.assign({ y: { title: { display: true, text: 'GB' }, min: 0 } }, chartScaleXOptions())
            }
        });
    }
}
function computeMemoryHealth(data) {
    var modules = data.memoryModules || data.MemoryModules || [];
    var totalSlots = data.totalSlots || data.TotalSlots || 0;
    var usedSlots = data.usedSlots || data.UsedSlots || modules.length || 0;
    var usagePct = parseFloat(data.usagePercent || data.UsagePercent) || 0;

    var issues = [];
    var configScore = 100;

    var emptySlots = Math.max(0, totalSlots - usedSlots);
    if (totalSlots > 0 && emptySlots > 0) {
        var emptyRatio = emptySlots / totalSlots;
        configScore -= Math.round(emptyRatio * 15);
        issues.push(emptySlots + ' of ' + totalSlots + ' slot(s) empty — upgrade headroom available.');
    }

    var speeds = modules.map(function (m) {
        return m.speedMHz || m.SpeedMHz || m.configuredClockSpeedMHz || m.ConfiguredClockSpeedMHz || 0;
    }).filter(function (s) { return s > 0; });
    var uniqueSpeeds = speeds.filter(function (v, i, a) { return a.indexOf(v) === i; });
    var channelMode = 'Single Channel';
    if (usedSlots >= 2) {
        if (uniqueSpeeds.length > 1) {
            configScore -= 20;
            issues.push('Installed modules run at mismatched speeds (' + uniqueSpeeds.join(', ') + ' MHz) — channel performance is limited to the slowest module.');
            channelMode = 'Mismatched Speed';
        } else {
            channelMode = usedSlots >= 4 ? 'Quad Channel' : (usedSlots === 3 ? 'Triple Channel' : 'Dual Channel');
        }
    }

    var capacities = modules.map(function (m) { return m.capacityGB || m.CapacityGB || 0; }).filter(function (c) { return c > 0; });
    var uniqueCapacities = capacities.filter(function (v, i, a) { return a.indexOf(v) === i; });
    if (usedSlots >= 2 && uniqueCapacities.length > 1) {
        configScore -= 15;
        issues.push('Module capacities are mismatched (' + uniqueCapacities.map(function (c) { return c.toFixed(0); }).join(' GB, ') + ' GB) — consider matching pairs for best performance.');
    }

    var badModules = modules.filter(function (m) {
        var st = (m.status || m.Status || '').toLowerCase();
        return st && st !== 'ok' && st !== 'enabled' && st !== 'healthy' && st !== '';
    });
    if (badModules.length > 0) {
        configScore -= 25;
        issues.push(badModules.length + ' module(s) reporting a non-OK status.');
    }

    configScore = Math.max(0, Math.min(100, configScore));

    var usageScore;
    if (usagePct <= 60) usageScore = 100;
    else if (usagePct <= 75) usageScore = 90 - (usagePct - 60) * 1.0;
    else if (usagePct <= 90) usageScore = 75 - (usagePct - 75) * 2.0;
    else usageScore = Math.max(0, 45 - (usagePct - 90) * 3.0);
    usageScore = Math.round(Math.max(0, Math.min(100, usageScore)));

    if (usagePct > 90) issues.push('RAM usage at last audit was critically high (' + usagePct.toFixed(1) + '%) — system may be swapping to disk.');
    else if (usagePct > 75) issues.push('RAM usage at last audit was elevated (' + usagePct.toFixed(1) + '%) — monitor for slowdowns under load.');

    if (issues.length === 0) issues.push('No issues detected — memory configuration and usage are within healthy ranges.');

    var overall = Math.round(configScore * 0.6 + usageScore * 0.4);

    var status, color;
    if (overall >= 85) { status = 'Healthy'; color = '#22c55e'; }
    else if (overall >= 65) { status = 'Fair'; color = '#f59e0b'; }
    else if (overall >= 40) { status = 'Needs Attention'; color = '#f97316'; }
    else { status = 'Critical'; color = '#ef4444'; }

    return {
        overall: overall, configScore: Math.round(configScore), usageScore: usageScore,
        status: status, color: color, channelMode: channelMode, emptySlots: emptySlots, issues: issues
    };
}

function renderMemoryHealth(data) {
    var health = computeMemoryHealth(data);

    $('#memHealthScoreText').text(health.overall + '%');

    var circumference = 283; // 2 * PI * r45, matches the SVG circle radius
    var healthDash = Math.max(0, Math.min(100, health.overall)) / 100 * circumference;
    $('#memHealthCircle').css({
        'stroke-dasharray': healthDash + ', ' + circumference,
        'stroke': health.color,
        'transition': 'stroke-dasharray 1.2s ease-in-out, stroke 1s ease'
    });

    $('#memHealthBadge').text(health.status).css({
        'background': health.color + '22',
        'color': health.color
    });

    $('#memStatusBadge').html('<span class="cpu-live-dot"></span> ' + health.status).css({
        'background': health.color + '22',
        'color': health.color
    });

    $('#memConfigScore').text(health.configScore + '%');
    $('#memUsageScore').text(health.usageScore + '%');
    $('#memChannelMode').text(health.channelMode);
    $('#memEmptySlots').text(health.emptySlots);

    var list = $('#memHealthIssuesList').empty();
    health.issues.forEach(function (issue) {
        list.append('<li>' + escapeHtml(issue) + '</li>');
    });
}

// Returns true if the payload represents a real audit result (not an empty/default object)
function hasRealMemoryData(data) {
    if (!data) return false;
    var installed = data.installedMemoryGB || data.InstalledMemoryGB || 0;
    var modules = data.memoryModules || data.MemoryModules || [];
    var dt = data.dateTime || data.DateTime;
    return installed > 0 || modules.length > 0 || !!dt;
}

function renderMemoryPanel(data) {
    renderMemoryHeaderOnly(data);

    let usagePct = (data.usagePercent || data.UsagePercent || 0);
    $('#memUtilizationText').text(usagePct.toFixed(1) + '%');

    var usedGB = parseFloat(data.usedMemoryGB || data.UsedMemoryGB || 0);
    var freeGB = parseFloat(data.freeMemoryGB || data.FreeMemoryGB || 0);
    var totalGB = parseFloat(data.installedMemoryGB || data.InstalledMemoryGB || 0);
    $('#memUsedGBText').text(usedGB.toFixed(1));
    $('#memFreeGBText').text(freeGB.toFixed(1));
    $('#memTotalGBText').text(totalGB.toFixed(1));

    let utilColor = '#3b82f6';
    if (usagePct > 90) utilColor = '#ef4444';
    else if (usagePct > 75) utilColor = '#f59e0b';
    else if (usagePct > 60) utilColor = '#0ea5e9';

    var circumference = 283; 
    var utilDash = Math.max(0, Math.min(100, usagePct)) / 100 * circumference;
    $('#memUtilizationCircle').css({
        'stroke-dasharray': utilDash + ', ' + circumference,
        'stroke': utilColor,
        'transition': 'stroke-dasharray 1.2s ease-in-out, stroke 1s ease'
    });

    renderMemoryHealth(data);

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

    // Reveal the gated audit section, hide the placeholder/loading states
    $('#memAuditPlaceholder').hide();
    $('#memAuditLoading').hide();
    $('#memAuditGate').show();

    // Pull trend history alongside the fresh audit result
    $.get(`/ComputerSummary/MemoryHistory?domain=${domaindata}`, function (history) {
        if (history && history.length > 0) {
            $('#memTrendNoData').hide();
            $('#memTrendChartWrap').show();
            renderMemoryTrendChart(history);
        } else {
            $('#memTrendChartWrap').hide();
            $('#memTrendNoData').show();
        }
    });
}

// Populates only the always-visible header stats (hero + key specs) from the last saved
// record, WITHOUT opening the gated audit section. The gated section (utilization gauge,
// health score, modules table, charts) only opens when the user actually clicks Audit Memory
// in this session — see the #btnAuditMemory handler.
function loadMemoryDetails() {
    $.get(`/ComputerSummary/MemorySummary?domain=${domaindata}`, function (data) {
        if (hasRealMemoryData(data)) {
            renderMemoryHeaderOnly(data);
        }
        // else: no data has ever been recorded for this device — leave placeholder visible
    });
}

// Renders just the hero header + key-spec boxes (always safe to show — these are simple
// facts, not an "audit result"). Does NOT touch the gated section.
function renderMemoryHeaderOnly(data) {
    $('#memTotalCapacity').text((data.installedMemoryGB || data.InstalledMemoryGB || 0).toFixed(2) + ' GB Installed');
    $('#memUsageBadge').html(`<i class="fas fa-chart-pie"></i> ${(data.usagePercent || data.UsagePercent || 0).toFixed(1)}% Used`);

    let totalSlots = data.totalSlots || data.TotalSlots || 0;
    let usedSlots = data.usedSlots || data.UsedSlots || 0;
    $('#memSlotsBadge').html(`<i class="fas fa-grip-horizontal"></i> ${usedSlots}/${totalSlots} Slots`);

    let dt = data.dateTime || data.DateTime;
    if (dt) {
        var d = new Date(dt);
        if (!isNaN(d.getTime())) $('#memLastUpdated').text('Last audit: ' + d.toLocaleString());
    }

    $('#memInstalled').text((data.installedMemoryGB || data.InstalledMemoryGB || 0).toFixed(2) + ' GB');
    $('#memMaxSupported').text((data.maximumSupportedMemoryGB || data.MaximumSupportedMemoryGB || 0).toFixed(2) + ' GB');
    $('#memUsed').text((data.usedMemoryGB || data.UsedMemoryGB || 0).toFixed(2) + ' GB');
    $('#memFree').text((data.freeMemoryGB || data.FreeMemoryGB || 0).toFixed(2) + ' GB');
    $('#memTotalSlots').text(totalSlots);
    $('#memUsedSlots').text(usedSlots);
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

var _logicalDrivesCache = null;

function loadLogicalDrivesDashboard() {
    $.get(`/ComputerSummary/LocalDisk?domain=${domaindata}`, function (res) {
        var data = (res && res.data) ? res.data : res;
        _logicalDrivesCache = data;
        _renderSummaryDrives(data);
    }).fail(function () {
        $('#summaryLogicalDrivesContainer').html(
            '<div style="text-align:center; padding: 20px; color: var(--red); font-weight: 500; font-size: 0.9rem; grid-column: 1 / -1;">' +
            '<i class="fas fa-exclamation-triangle text-2xl mb-2 block"></i>Failed to load drive details</div>'
        );
    });
}

function loadHwPartitions() {
    if (_logicalDrivesCache !== null) {
        _renderHwPartitions(_logicalDrivesCache);
        return;
    }
    $.get(`/ComputerSummary/LocalDisk?domain=${domaindata}`, function (res) {
        var data = (res && res.data) ? res.data : res;
        _logicalDrivesCache = data; _renderHwPartitions(data);
    }).fail(function () {
        $('#hwPartitionsContainer').html(
            '<div style="padding:16px;text-align:center;color:var(--red);font-size:.83rem;">' +
            '<i class="fas fa-exclamation-triangle"></i> Failed to load partition data</div>'
        );
    });
}

function _renderSummaryDrives(data) {
    var container = $('#summaryLogicalDrivesContainer').empty();
    if (!data || !data.length) {
        container.html('<div style="text-align:center; padding: 20px; color: var(--slate-400); font-weight: 500; font-size: 0.9rem; grid-column: 1 / -1;"><i class="fas fa-inbox text-2xl mb-2 block"></i>No logical drives found</div>');
        return;
    }
    data.forEach(function (d) {
        var driveLetter = d.driveLetter || d.DriveLetter || d.Name || 'Unknown';
        var fileSystem = d.fileSystem || d.FileSystem || 'Unknown';
        var total = parseFloat(d.totalCapacity || d.Size || 0).toFixed(2);
        var free = parseFloat(d.freeSpace || d.FreeSpace || 0).toFixed(2);
        var usagePct = parseFloat(d.usagePercentage || d.Usage || 0);

        var barColor = 'var(--cyan)';
        if (usagePct > 90) barColor = 'var(--red)';
        else if (usagePct > 80) barColor = 'var(--amber)';

        var icon = driveLetter.includes('C:')
            ? '<i class="fab fa-windows" style="color:var(--cyan); font-size: 1.15rem;"></i>'
            : '<i class="fas fa-hdd" style="color:var(--slate-400); font-size: 1.15rem;"></i>';

        var dashLen = (usagePct / 100) * 100;

        container.append(
            '<div class="cs-drive-card" style="background: var(--white); border: 1px solid var(--slate-200); border-radius: var(--radius-md); padding: 10px 14px; display: flex; gap: 14px; align-items: center; box-shadow: var(--shadow-sm); cursor: default;">' +
            '<div style="position:relative; width: 54px; height: 54px; flex-shrink: 0;">' +
            '<svg viewBox="0 0 36 36" style="width:100%; height:100%; transform: rotate(-90deg);">' +
            '<path stroke="#a7f3d0" stroke-width="3" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />' +
            '<path stroke="' + barColor + '" stroke-width="3" stroke-dasharray="' + dashLen + ', 100" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" style="stroke-linecap: round; transition: stroke-dasharray 1s ease-in-out;" />' +
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
}

function _renderHwPartitions(data) {
    var container = $('#hwPartitionsContainer');
    if (!container.length) return;

    if (!data || !data.length) {
        container.html('<div style="padding:16px;text-align:center;color:var(--slate-400);font-size:.83rem;">No partition data available</div>');
        return;
    }

    function _driveStyle(letter, driveType) {
        var l = (letter || '').toUpperCase();
        var t = (driveType || '').toLowerCase();
        if (l.startsWith('C')) return { bg: '#eff6ff', iconBg: '#3b82f6', icon: 'fab fa-windows', label: 'System', labelBg: '#dbeafe', labelColor: '#1d4ed8' };
        if (t.includes('cd') || t.includes('dvd') || t.includes('optical') || t.includes('removable'))
            return { bg: '#faf5ff', iconBg: '#8b5cf6', icon: 'fas fa-compact-disc', label: 'Optical', labelBg: '#ede9fe', labelColor: '#6d28d9' };
        if (t.includes('network') || t.includes('remote'))
            return { bg: '#ecfdf5', iconBg: '#10b981', icon: 'fas fa-network-wired', label: 'Network', labelBg: '#d1fae5', labelColor: '#065f46' };
        if (t.includes('usb') || t.includes('removable'))
            return { bg: '#fff7ed', iconBg: '#f97316', icon: 'fas fa-usb', label: 'Removable', labelBg: '#fed7aa', labelColor: '#c2410c' };
        return { bg: '#f8fafc', iconBg: '#475569', icon: 'fas fa-hdd', label: 'Data', labelBg: '#e2e8f0', labelColor: '#334155' };
    }

    var cardsHtml = '<div class="partition-grid">';

    data.forEach(function (d) {
        var letter = d.driveLetter || d.DriveLetter || d.Name || '?:';
        var fs = d.fileSystem || d.FileSystem || 'N/A';
        var label = d.volumeName || d.VolumeName || d.VolumeLabel || '';
        var driveType = d.driveType || d.DriveType || '';
        var total = parseFloat(d.totalCapacity || d.Size || 0);
        var free = parseFloat(d.freeSpace || d.FreeSpace || 0);
        var used = parseFloat(d.usedSpace || d.UsedSpace || (total - free) || 0);
        var pct = total > 0 ? Math.min(100, (used / total) * 100) : 0;

        var style = _driveStyle(letter, driveType);

        var barColor = '#22c55e';
        if (pct >= 90) barColor = '#ef4444';
        else if (pct >= 75) barColor = '#f59e0b';

        var displayName = escapeHtml(letter) + (label ? ' â€” ' + escapeHtml(label) : '');

        var isOptical = (driveType || '').toLowerCase().includes('cd') ||
            (driveType || '').toLowerCase().includes('dvd') ||
            (driveType || '').toLowerCase().includes('optical');
        var noMedia = (isOptical && total === 0);

        var barSection = noMedia
            ? '<div style="font-size:.72rem;color:var(--slate-400);padding:4px 0;font-style:italic;">No media inserted</div>'
            : ('<div class="partition-bar-wrap">' +
                '<div class="partition-bar-track">' +
                '<div class="partition-bar-fill" style="width:' + pct.toFixed(1) + '%;background:' + barColor + ';"></div>' +
                '</div>' +
                '<div class="partition-bar-labels">' +
                '<span style="color:' + barColor + ';">' + pct.toFixed(1) + '% used</span>' +
                '<span style="color:#22c55e;">' + free.toFixed(2) + ' GB free</span>' +
                '</div>' +
                '</div>');

        var statsSection = noMedia ? '' :
            '<div class="partition-stats">' +
            '<div class="partition-stat"><div class="partition-stat-val">' + total.toFixed(1) + ' GB</div><div class="partition-stat-lbl">Total</div></div>' +
            '<div class="partition-stat" style="border-left:1px solid var(--slate-100);border-right:1px solid var(--slate-100);">' +
            '<div class="partition-stat-val" style="color:#f59e0b;">' + used.toFixed(1) + ' GB</div><div class="partition-stat-lbl">Used</div></div>' +
            '<div class="partition-stat"><div class="partition-stat-val" style="color:#22c55e;">' + free.toFixed(1) + ' GB</div><div class="partition-stat-lbl">Free</div></div>' +
            '</div>';

        cardsHtml +=
            '<div class="partition-card" style="background:' + style.bg + ';">' +

            '<div class="partition-card-header">' +
            '<div class="partition-drive-badge">' +
            '<div class="partition-drive-icon" style="background:' + style.iconBg + ';color:#fff;">' +
            '<i class="' + style.icon + '"></i>' +
            '</div>' +
            '<div>' +
            '<div class="partition-drive-letter">' + displayName + '</div>' +
            '</div>' +
            '</div>' +
            '<span class="partition-type-badge" style="background:' + style.labelBg + ';color:' + style.labelColor + ';">' + style.label + '</span>' +
            '</div>' +

            barSection +
            statsSection +

            '</div>';
    });

    cardsHtml += '</div>';
    container.html(cardsHtml);
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

function loadProcessorDetails(includeAuditSections) {
    // Static identity info (name, specs, cache) — always show from latest DB row on page load.
    // Thermal health + trend charts only show when includeAuditSections=true (i.e. after audit).
    $.get(`/ComputerSummary/Processors?domain=${domaindata}`, function (data) {
        if (!data) return;
        renderProcessorHero(data);
        renderProcessorSpecs(data);
        renderProcessorCache(data);
        if (includeAuditSections) {
            renderProcessorThermal(data);
        } else {
            // Ensure both gated sections stay hidden on page load
            $('#cpuHealthSection').hide();
            $('#cpuHealthPlaceholder').show();
        }
    }).fail(function () { console.error("Failed to load processor details"); });

    if (includeAuditSections) {
        $.get(`/ComputerSummary/ProcessorHistory?domain=${domaindata}&count=20`, function (history) {
            renderProcessorTrendCharts(history);
        }).fail(function () {
            $('#cpuTrendSection').hide();
            $('#cpuTrendPlaceholder').show();
        });
    } else {
        $('#cpuTrendSection').hide();
        $('#cpuTrendPlaceholder').show();
    }
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
    var maxMHz = parseFloat(cpuVal(d, 'maxClockSpeedMHz', 'MaxClockSpeedMHz', 'maxClockSpeed', 'MaxClockSpeed')) || 0;
    var currentMHz = parseFloat(cpuVal(d, 'currentClockSpeedMHz', 'CurrentClockSpeedMHz', 'currentClockSpeed', 'CurrentClockSpeed')) || 0;
    $('#cpuBaseClock').text(baseGhz > 0 ? baseGhz.toFixed(2) + ' GHz' : (maxMHz > 0 ? (maxMHz / 1000).toFixed(2) + ' GHz' : '--'));
    $('#cpuCurrentClock').text(currentMHz > 0 ? (currentMHz / 1000).toFixed(2) + ' GHz' : '--');
    $('#cpuMaxClock').text(maxMHz > 0 ? (maxMHz / 1000).toFixed(2) + ' GHz' : '--');

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

    if (pkgTemp <= 0) {
        $('#cpuHealthSection').hide();
        $('#cpuHealthPlaceholder').show();
        return;
    }

    $('#cpuHealthPlaceholder').hide();
    $('#cpuHealthSection').show();

    // ── Health Score calculation ──────────────────────────────────
    // Based purely on real temperature readings from the audit.
    // Score degrades as temperature climbs toward unsafe thresholds.
    // These bands match Intel/AMD published safe-operation guidelines:
    //   < 50°C: cool/idle — no degradation
    //   50–70°C: warm/load — mild concern, starts deducting
    //   70–85°C: hot — moderate concern, significant deduction
    //   85–95°C: very hot — high risk, heavy deduction
    //   > 95°C: critical — approaching throttle/shutdown territory
    var healthScore;
    var healthStatus, healthColor;
    if (pkgTemp < 50) { healthScore = 100; healthStatus = 'Cool'; healthColor = '#22c55e'; }
    else if (pkgTemp < 60) { healthScore = 90 - (pkgTemp - 50) * 0.5; healthStatus = 'Healthy'; healthColor = '#22c55e'; }
    else if (pkgTemp < 70) { healthScore = 85 - (pkgTemp - 60) * 1.0; healthStatus = 'Warm'; healthColor = '#84cc16'; }
    else if (pkgTemp < 80) { healthScore = 75 - (pkgTemp - 70) * 2.0; healthStatus = 'Hot'; healthColor = '#f59e0b'; }
    else if (pkgTemp < 90) { healthScore = 55 - (pkgTemp - 80) * 3.0; healthStatus = 'Very Hot'; healthColor = '#f97316'; }
    else { healthScore = 25 - (pkgTemp - 90) * 2.0; healthStatus = 'Critical'; healthColor = '#ef4444'; }
    healthScore = Math.max(0, Math.min(100, Math.round(healthScore)));

    // ── Health circle gauge ───────────────────────────────────────
    var circumference = 2 * Math.PI * 45;
    var healthDash = (healthScore / 100) * circumference;
    $('#cpuHealthCircle').css({ stroke: healthColor, 'stroke-dasharray': healthDash + ', ' + circumference, transition: 'stroke-dasharray 1.5s ease, stroke 1s ease' });
    $('#cpuHealthScoreText').text(healthScore + '%');
    $('#cpuHealthBadge').html('<i class="fas fa-' + (healthScore >= 80 ? 'check-circle' : healthScore >= 55 ? 'exclamation-triangle' : 'fire') + '"></i> ' + healthStatus)
        .css({ background: healthColor + '22', color: healthColor });

    // ── Package temp gauge ────────────────────────────────────────
    var tempDash = Math.min(100, (pkgTemp / 100) * 100) / 100 * circumference;
    var tempColor = pkgTemp < 60 ? '#22c55e' : pkgTemp < 75 ? '#f59e0b' : '#ef4444';
    $('#cpuPackageTempCircle').css({ stroke: tempColor, 'stroke-dasharray': tempDash + ', ' + circumference });
    $('#cpuPackageTempText').text(pkgTemp.toFixed(0) + '\u00B0C');
    $('#cpuPackagePowerText').text('Power draw: ' + (pkgPower > 0 ? pkgPower.toFixed(1) + ' W' : 'N/A'));
    $('#cpuHealthStatus').html('<i class="fas fa-thermometer-half"></i> ' + healthStatus).css('color', healthColor);

    // ── Sub-score info boxes ──────────────────────────────────────
    $('#cpuHealthTemp').text(pkgTemp.toFixed(0) + '\u00B0C').css('color', tempColor);
    var cores = parseInt(cpuVal(d, 'cores', 'Cores', 'numberOfCores', 'NumberOfCores')) || 0;
    var threads = parseInt(cpuVal(d, 'logicalProcessors', 'LogicalProcessors', 'numberOfLogicalProcessors', 'NumberOfLogicalProcessors')) || 0;
    $('#cpuHealthCores').text(cores > 0 ? cores + 'C / ' + threads + 'T' : '--');
    var maxMHz = parseFloat(cpuVal(d, 'maxClockSpeedMHz', 'MaxClockSpeedMHz', 'maxClockSpeed', 'MaxClockSpeed')) || 0;
    $('#cpuHealthClock').text(maxMHz > 0 ? (maxMHz / 1000).toFixed(2) + ' GHz' : '--');
    $('#cpuHealthPower').text(pkgPower > 0 ? pkgPower.toFixed(1) + ' W' : 'N/A');

    // ── Per-core temperature mini-circles ─────────────────────────
    var maxTemp = 100;
    var coreReadings = [
        { label: 'Core 0', value: parseFloat(cpuVal(d, 'core0Temp', 'Core0Temp', 'coreTemp0', 'CoreTemp0')) || 0 },
        { label: 'Core 1', value: parseFloat(cpuVal(d, 'core1Temp', 'Core1Temp', 'coreTemp1', 'CoreTemp1')) || 0 },
        { label: 'Core 2', value: parseFloat(cpuVal(d, 'core2Temp', 'Core2Temp', 'coreTemp2', 'CoreTemp2')) || 0 },
        { label: 'Core 3', value: parseFloat(cpuVal(d, 'core3Temp', 'Core3Temp', 'coreTemp3', 'CoreTemp3')) || 0 }
    ].filter(function (c) { return c.value > 0; });   // only show cores with real data

    var coreHtml = '';
    if (coreReadings.length > 0) {
        coreReadings.forEach(function (c) {
            var cColor = c.value < 60 ? '#22c55e' : c.value < 75 ? '#f59e0b' : '#ef4444';
            var cDash = Math.min(100, (c.value / maxTemp) * 100);
            coreHtml += '<div class="cpu-core-gauge">' +
                '<svg viewBox="0 0 36 36" style="width:56px;height:56px;transform:rotate(-90deg);">' +
                '<path stroke="#e2e8f0" stroke-width="3.2" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />' +
                '<path stroke="' + cColor + '" stroke-width="3.2" stroke-dasharray="' + cDash + ', 100" fill="none" stroke-linecap="round" style="transition:stroke-dasharray 1s ease;" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />' +
                '</svg>' +
                '<div class="cpu-core-gauge-label">' + c.label + '</div>' +
                '<div class="cpu-core-gauge-value" style="color:' + cColor + ';font-weight:700;">' + c.value.toFixed(0) + '\u00B0C</div>' +
                '</div>';
        });
    } else {
        coreHtml = '<div style="color:var(--slate-400);font-size:.8rem;">Per-core temperature not reported by this device.</div>';
    }
    $('#cpuCoreGaugeContainer').html(coreHtml);

    $('#cpuStatusBadge').html('<span class="cpu-live-dot"></span> ' + healthStatus)
        .css({ background: healthColor + '22', color: healthColor })
        .removeClass('is-down')
        .toggleClass('is-down', healthScore < 40);

    var findings = [];
    if (pkgTemp >= 90) findings.push('Package temperature is critically high (' + pkgTemp.toFixed(0) + '°C) — CPU may be thermal-throttling. Check cooling.');
    else if (pkgTemp >= 80) findings.push('Package temperature is very high (' + pkgTemp.toFixed(0) + '°C) — verify fan speed and thermal paste.');
    else if (pkgTemp >= 70) findings.push('Package temperature is elevated (' + pkgTemp.toFixed(0) + '°C) — monitor under sustained load.');
    var hotCores = coreReadings.filter(function (c) { return c.value >= 80; });
    if (hotCores.length > 0) findings.push(hotCores.length + ' core(s) exceeding 80°C (' + hotCores.map(function (c) { return c.label + ': ' + c.value.toFixed(0) + '°C'; }).join(', ') + ').');
    if (pkgPower > 45) findings.push('Power draw is high (' + pkgPower.toFixed(1) + ' W) — may contribute to thermal pressure.');
    if (findings.length === 0) findings.push('No thermal issues detected — processor is operating within healthy temperature ranges.');

    var list = $('#cpuHealthIssuesList').empty();
    findings.forEach(function (f) { list.append('<li>' + escapeHtml(f) + '</li>'); });
}

let cpuTempChartInstance = null;
let cpuClockChartInstance = null;

function renderProcessorTrendCharts(history, skipStore) {
    if (!history || !history.length) {
        $('#cpuTrendSection').hide();
        $('#cpuTrendPlaceholder').show();
        $('#cpuTrendPlaceholderText').html(skipStore ? 'No samples found in the selected date range.' : 'Run <strong>Audit Processor</strong> above to see temperature and clock trend data.');
        return;
    }
    if (!skipStore) _fullCpuHistory = history;

    var hasRealTempData = history.some(function (h) {
        return (parseFloat(cpuVal(h, 'cpuPackageTemperature', 'CpuPackageTemperature', 'packageTemperature', 'PackageTemperature')) || 0) > 0;
    });
    if (!hasRealTempData) {
        $('#cpuTrendSection').hide();
        $('#cpuTrendPlaceholder').show();
        $('#cpuTrendPlaceholderText').html(skipStore ? 'No temperature samples in this date range.' : 'Run <strong>Audit Processor</strong> above to see temperature and clock trend data.');
        return;
    }

    $('#cpuTrendPlaceholder').hide();
    $('#cpuTrendSection').show();

    var labels = history.map(function (h) {
        return formatChartLabel(new Date(cpuVal(h, 'dateTime', 'DateTime')));
    });

    var tempCanvas = document.getElementById('cpuTempTrendChart');
    if (tempCanvas) {
        if (cpuTempChartInstance) cpuTempChartInstance.destroy();
        cpuTempChartInstance = new Chart(tempCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Package', data: history.map(h => parseFloat(cpuVal(h, 'cpuPackageTemperature', 'CpuPackageTemperature', 'packageTemperature', 'PackageTemperature')) || 0), borderColor: '#ef4444', backgroundColor: 'rgba(239,68,68,.08)', borderWidth: 2, fill: true, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Core 0', data: history.map(h => parseFloat(cpuVal(h, 'core0Temp', 'Core0Temp', 'coreTemp0', 'CoreTemp0')) || 0), borderColor: '#0ea5e9', borderWidth: 1.5, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Core 1', data: history.map(h => parseFloat(cpuVal(h, 'core1Temp', 'Core1Temp', 'coreTemp1', 'CoreTemp1')) || 0), borderColor: '#22c55e', borderWidth: 1.5, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Core 2', data: history.map(h => parseFloat(cpuVal(h, 'core2Temp', 'Core2Temp', 'coreTemp2', 'CoreTemp2')) || 0), borderColor: '#f59e0b', borderWidth: 1.5, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Core 3', data: history.map(h => parseFloat(cpuVal(h, 'core3Temp', 'Core3Temp', 'coreTemp3', 'CoreTemp3')) || 0), borderColor: '#a855f7', borderWidth: 1.5, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } }, tooltip: { mode: 'index', intersect: false } },
                scales: Object.assign({ y: { title: { display: true, text: 'Â°C' } } }, chartScaleXOptions())
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
                    { label: 'Current Clock (GHz)', data: history.map(h => { var v = parseFloat(cpuVal(h, 'currentClockSpeedMHz', 'CurrentClockSpeedMHz', 'currentClockSpeed', 'CurrentClockSpeed')) || 0; return v > 0 ? (v / 1000) : 0; }), borderColor: '#0ea5e9', backgroundColor: 'rgba(14,165,233,.08)', borderWidth: 2, fill: true, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Bus Speed (MHz)', data: history.map(h => parseFloat(cpuVal(h, 'busSpeedMHz', 'BusSpeedMHz', 'extClock', 'ExtClock')) || 0), borderColor: '#cbd5e1', borderWidth: 1.5, borderDash: [5, 5], fill: false, pointRadius: 0 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } }, tooltip: { mode: 'index', intersect: false } },
                scales: Object.assign({ y: { title: { display: true, text: 'GHz' } } }, chartScaleXOptions())
            }
        });
    }
}

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


let mbChartInstances = {};

function loadMotherboardHealthLatest() {
    $('#mbAuditLoading').hide();
    $.get(`/ComputerSummary/MotherboardHealthLatest?domain=${domaindata}`, function (data) {
        if (data && (data.health || data.Health)) {
            renderMotherboardAudit(data);
            $('#mbAuditPlaceholder').hide();
            $('#mbAuditGate').show();
        } else {
            $('#mbAuditGate').hide();
            $('#mbAuditPlaceholder').show();
        }
    }).fail(function () {
        $('#mbAuditGate').hide();
        $('#mbAuditPlaceholder').show();
    });
}

$(document).on('click', '#btnAuditMotherboard', function (e) {
    e.preventDefault();
    let btn = $(this);
    let originalText = btn.html();
    btn.html('<i class="fas fa-circle-notch fa-spin"></i> Processing...');
    btn.prop('disabled', true);
    btn.css('opacity', '0.7');

    $('#mbAuditPlaceholder').hide();
    $('#mbAuditGate').hide();
    $('#mbAuditLoading').show();

    sysAlert('Motherboard audit requested. This can take a little while — please wait...', 'info');

    $.ajax({
        url: '/ComputerSummary/AuditMotherboard?domain=' + encodeURIComponent(domaindata) + '&hostName=' + encodeURIComponent(actualDomainName),
        type: 'POST',
        timeout: 90000,
        success: function (res) {
            $('#mbAuditLoading').hide();
            if (res && res.success && res.data) {
                renderMotherboardAudit(res.data);
                loadMotherboardHealthHistory();
                $('#mbAuditGate').show();
                sysAlert(res.message || 'Motherboard audit completed!', 'success');
            } else {
                $('#mbAuditPlaceholder').show();
                sysAlert(res && res.message ? res.message : 'Motherboard audit failed.', 'error');
            }
        },
        error: function (xhr, status) {
            $('#mbAuditLoading').hide();
            $('#mbAuditPlaceholder').show();
            let msg = status === 'timeout' ? 'Motherboard audit timed out. The device may still be processing.' : 'Connection error while requesting motherboard audit.';
            sysAlert(msg, 'error');
        },
        complete: function () {
            btn.html(originalText);
            btn.prop('disabled', false);
            btn.css('opacity', '1');
        }
    });
});

function renderMotherboardAudit(data) {
    const health = data.health || data.Health;
    const cpu = data.cpu || data.Cpu;
    if (!health) return;

    const val = (obj, a, b) => (obj[a] ?? obj[b] ?? 0);
    const score = val(health, 'healthScore', 'HealthScore');
    const status = health.status ?? health.Status ?? 'Unknown';

    const circumference = 283; 
    const dash = (score / 100) * circumference;
    const color = score >= 80 ? '#22c55e' : score >= 50 ? '#f59e0b' : '#ef4444';
    $('#mbHealthCircle').attr('stroke-dasharray', `${dash},${circumference}`).attr('stroke', color);
    $('#mbHealthPercentText').text(score + '%');

    const badgeBg = score >= 80 ? '#dcfce7' : score >= 50 ? '#fef3c7' : '#fee2e2';
    const badgeColor = score >= 80 ? '#15803d' : score >= 50 ? '#b45309' : '#b91c1c';
    $('#mbStatusBadge').css({ background: badgeBg, color: badgeColor })
        .html(`<span class="cpu-live-dot" style="background:${badgeColor};"></span> ${status}`);

    $('#mbBoardTemp').text(fmtMb1(val(health, 'motherboardTemperature', 'MotherboardTemperature')));
    $('#mbCpuTemp').text(fmtMb1(val(health, 'cpuTemperature', 'CpuTemperature')));
    $('#mbVolt12').text(fmtMb2(val(health, 'voltage12V', 'Voltage12V')));
    $('#mbVolt5').text(fmtMb2(val(health, 'voltage5V', 'Voltage5V')));
    $('#mbVolt33').text(fmtMb2(val(health, 'voltage3V3', 'Voltage3V3')));
    $('#mbFanRpm').text(Math.round(val(health, 'fanRPM', 'FanRPM')));
    $('#mbWheaErrors').text(val(health, 'wheaErrors', 'WheaErrors'));

    const auditDate = health.auditDate ?? health.AuditDate;
    $('#mbLastAudit').text(auditDate ? new Date(auditDate).toLocaleString() : '--');

    const issues = health.issues ?? health.Issues ?? [];
    if (issues.length > 0) {
        $('#mbIssuesList').html(issues.map(i => `<li>${escapeHtml(i)}</li>`).join(''));
        $('#mbIssuesWrap').show();
    } else {
        $('#mbIssuesWrap').hide();
    }

    if (cpu) {
        const cval = (a, b) => (cpu[a] ?? cpu[b] ?? 0);
        const cores = [
            { load: cval('cpuCore1Load', 'CpuCore1Load'), temp: cval('cpuCore1Temp', 'CpuCore1Temp'), clock: cval('cpuCore1Clock', 'CpuCore1Clock') },
            { load: cval('cpuCore2Load', 'CpuCore2Load'), temp: cval('cpuCore2Temp', 'CpuCore2Temp'), clock: cval('cpuCore2Clock', 'CpuCore2Clock') },
            { load: cval('cpuCore3Load', 'CpuCore3Load'), temp: cval('cpuCore3Temp', 'CpuCore3Temp'), clock: cval('cpuCore3Clock', 'CpuCore3Clock') },
            { load: cval('cpuCore4Load', 'CpuCore4Load'), temp: cval('cpuCore4Temp', 'CpuCore4Temp'), clock: cval('cpuCore4Clock', 'CpuCore4Clock') },
        ];
        const rows = cores.map((c, idx) =>
            `<tr style="border-bottom:1px solid var(--slate-100);">
                <td style="padding:6px 8px;">Core ${idx + 1}</td>
                <td style="padding:6px 8px;">${fmtMb1(c.load)}%</td>
                <td style="padding:6px 8px;">${fmtMb1(c.temp)}</td>
                <td style="padding:6px 8px;">${Math.round(c.clock)}</td>
            </tr>`
        ).join('');
        $('#mbCpuCoreTableBody').html(rows);
        $('#mbCpuPackagePower').text(fmtMb1(cval('cpuPackagePower', 'CpuPackagePower')));
        $('#mbCpuBusSpeed').text(fmtMb1(cval('busSpeed', 'BusSpeed')));
        $('#mbCpuMaxTemp').text(fmtMb1(cval('coreMaxTemp', 'CoreMaxTemp')));
    }
}

function fmtMb1(n) { return (typeof n === 'number') ? n.toFixed(1) : n; }
function fmtMb2(n) { return (typeof n === 'number') ? n.toFixed(2) : n; }

function loadMotherboardHealthHistory() {
    $.get(`/ComputerSummary/MotherboardHealthHistory?domain=${domaindata}`, function (history) {
        if (!history || history.length === 0) {
            $('#mbHistoryChartWrap').hide();
            $('#mbHistoryEmpty').show();
            $('#mbHistAuditCount').text('0 audits');
            return;
        }
        $('#mbHistoryChartWrap').show();
        $('#mbHistoryEmpty').hide();
        $('#mbHistAuditCount').text(history.length + ' audit' + (history.length === 1 ? '' : 's'));
        renderMotherboardHistoryCharts(history);
    }).fail(function () {
        $('#mbHistoryChartWrap').hide();
        $('#mbHistoryEmpty').show();
    });
}

function renderMotherboardHistoryCharts(history) {
    const val = (obj, a, b) => (obj[a] ?? obj[b] ?? null);
    const labels = history.map(h => {
        const d = val(h, 'auditDate', 'AuditDate');
        return d ? new Date(d).toLocaleDateString() + ' ' + new Date(d).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '';
    });

    drawMbLineChart('mbHistHealthChart', labels, [
        { label: 'Health Score %', data: history.map(h => val(h, 'healthScore', 'HealthScore')), color: '#22c55e' }
    ]);

    drawMbLineChart('mbHistTempChart', labels, [
        { label: 'Board °C', data: history.map(h => val(h, 'motherboardTemperature', 'MotherboardTemperature')), color: '#f59e0b' },
        { label: 'CPU °C', data: history.map(h => val(h, 'cpuTemperature', 'CpuTemperature')), color: '#ef4444' }
    ]);

    drawMbLineChart('mbHistVoltageChart', labels, [
        { label: '12V', data: history.map(h => val(h, 'voltage12V', 'Voltage12V')), color: '#3b82f6' },
        { label: '5V', data: history.map(h => val(h, 'voltage5V', 'Voltage5V')), color: '#8b5cf6' },
        { label: '3.3V', data: history.map(h => val(h, 'voltage3V3', 'Voltage3V3')), color: '#06b6d4' }
    ]);

    drawMbLineChart('mbHistFanChart', labels, [
        { label: 'Fan RPM', data: history.map(h => val(h, 'fanRPM', 'FanRPM')), color: '#0ea5e9' }
    ]);
}

function drawMbLineChart(canvasId, labels, series) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    if (mbChartInstances[canvasId]) {
        mbChartInstances[canvasId].destroy();
    }
    mbChartInstances[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: series.map(s => ({
                label: s.label,
                data: s.data,
                borderColor: s.color,
                backgroundColor: s.color + '22',
                borderWidth: 2,
                tension: 0.3,
                pointRadius: 2,
                fill: series.length === 1
            }))
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: series.length > 1, labels: { boxWidth: 10, font: { size: 10 } } } },
            scales: {
                x: { ticks: { font: { size: 9 } } },
                y: { ticks: { font: { size: 9 } }, beginAtZero: false }
            }
        }
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

        sysAlert('Sending uninstall commandï¿½', 'info');

        $.ajax({
            url: '/ComputerSummary/Uninstallsoftware?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ softwareName: softwareName }),
            success: function (res) {
                if (res && res.status === 'success') {
                    sysAlert('Uninstall command sent. Checking statusï¿½', 'info');
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

        sysAlert('Sending install commandï¿½', 'info');

        $.ajax({
            url: '/ComputerSummary/PatchUpdate?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ softwareName: fileName }),
            success: function (res) {
                if (res && res.status === 'success') {
                    sysAlert('Install command sent. Checking statusï¿½', 'info');
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

        sysAlert('Sending patch commandï¿½', 'info');
        $.ajax({
            url: '/ComputerSummary/PatchUpdate?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ softwareName: displayName }),
            success: function (res) {
                if (res && res.status === 'success') {
                    sysAlert('Patch command sent. Checking statusï¿½', 'info');
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

function loadBatteryDetails() {
    $.get(`/ComputerSummary/Battery?domain=${actualDomainName}`, function (data) {
        if (!data) return;

        var mfr = data.manufacturer || data.Manufacturer || '';
        var status = data.status || data.Status || '';
        var battName = data.batteryName || data.BatteryName || '';
        var serial = data.serialNumber || data.SerialNumber || '';
        var chemistry = data.chemistry || data.Chemistry || '';
        var cycleCount = data.cycleCount || data.CycleCount || 0;
        var desc = data.description || data.Description || '';
        var systemType = data.systemType || data.SystemType || '';
        var noDataValues = ['No Battery', 'Not found', 'Unknow Manufacturer', '', null, undefined];
        var mfrReal = mfr && !noDataValues.includes(mfr);
        var nameReal = battName && !noDataValues.includes(battName);
        var serialReal = serial && !noDataValues.includes(serial) && serial !== 'Not found';
        var chemReal = chemistry && !noDataValues.includes(chemistry);
        var isBatteryDevice = mfrReal || nameReal || serialReal || chemReal;

        var isDesktop = !isBatteryDevice && (
            (systemType && systemType.toLowerCase() === 'desktop') ||
            mfr === 'No Battery'
        );

        if (isDesktop) {
            $('#batteryNoBatteryState').show();
            $('#batteryContentWrap').hide();
            $('#batteryActionButtons').hide();
            return;
        }

        $('#batteryNoBatteryState').hide();
        $('#batteryContentWrap').show();
        $('#batteryActionButtons').show();

        if (isBatteryDevice) {
            $('#batteryManufacturer').text(mfr || 'N/A');
            $('#batteryStatus').text(status || 'N/A');
            $('#batteryNameDb').text(battName || 'N/A');
            $('#batterySerialDb').text(serial || 'N/A');
            $('#batteryChemistryDb').text(chemistry || 'N/A');
            $('#batteryCycleCountDb').text(cycleCount > 0 ? cycleCount : 'N/A');
            $('#batteryDescription').text(desc || 'N/A');
        } else {
            $('#batteryManufacturer').text('No Battery');
            $('#batteryStatus').text('N/A');
            $('#batteryNameDb').text('N/A');
            $('#batterySerialDb').text('N/A');
            $('#batteryChemistryDb').text('N/A');
            $('#batteryCycleCountDb').text('N/A');
            $('#batteryDescription').text('Desktop — no battery installed');
        }

        let scanDate = data.scanDate || data.ScanDate || data.dateTime || data.DateTime;
        if (scanDate) {
            try {
                let d = new Date(scanDate);
                $('#batteryLastAuditDb').text(d.toLocaleString(undefined, {
                    year: 'numeric', month: 'short', day: 'numeric',
                    hour: '2-digit', minute: '2-digit'
                }));
            } catch (e) { $('#batteryLastAuditDb').text(scanDate); }
        }

        let lvl = data.batteryPercentage || data.BatteryPercentage || 0;
        let charging = data.isCharging || data.IsCharging || false;
        if (lvl > 0) {
            var lvlColor = lvl <= 20 ? '#ef4444' : (lvl <= 40 ? '#f59e0b' : '#22c55e');
            $('#batteryLevel').html(`<div style="display:flex;align-items:center;gap:5px;">
                    <span style="font-size:1rem;font-weight:700;color:${lvlColor};">${lvl}%</span>
                    <i class="fas ${charging ? 'fa-bolt' : 'fa-battery-three-quarters'}" style="color:${charging ? '#22c55e' : lvlColor};font-size:.78rem;"></i>
                </div>
                <div style="font-size:.65rem;color:var(--slate-500);margin-top:1px;">${charging ? 'Charging' : 'Discharging'}</div>`);
        } else {
            $('#batteryLevel').text('N/A');
        }
        $('#batteryHistoryChartLoading').hide();
        $('#batteryHistoryChartContainer').hide();
    }).fail(function () { console.error("Failed to load battery details"); });

    checkBatteryReportExists();
}

function renderBatteryAuditPanel(metrics) {
    if (!metrics) return;

    var mfr = metrics.manufacturer || metrics.Manufacturer || '';
    var battName = metrics.batteryName || metrics.BatteryName || '';
    var serial = metrics.serialNumber || metrics.SerialNumber || '';
    var chemistry = metrics.chemistry || metrics.Chemistry || '';
    var cycleCount = parseInt(metrics.cycleCount || metrics.CycleCount) || 0;
    var status = metrics.status || metrics.Status || 'Unknown';
    var designCap = parseInt(metrics.designCapacity || metrics.DesignCapacity) || 0;
    var fullCap = parseInt(metrics.fullChargeCapacity || metrics.FullChargeCapacity) || 0;
    var wearRate = metrics.wearRatePerMonth !== undefined ? metrics.wearRatePerMonth
        : (metrics.WearRatePerMonth !== undefined ? metrics.WearRatePerMonth : '--');
    var remaining = metrics.estimatedRemainingMonths !== undefined ? metrics.estimatedRemainingMonths
        : (metrics.EstimatedRemainingMonths !== undefined ? metrics.EstimatedRemainingMonths : undefined);
    var healthPercent = parseFloat(
        metrics.healthPercentage !== undefined ? metrics.healthPercentage :
            (metrics.HealthPercentage !== undefined ? metrics.HealthPercentage :
                (metrics.batteryHealthPercent !== undefined ? metrics.batteryHealthPercent :
                    (metrics.BatteryHealthPercent !== undefined ? metrics.BatteryHealthPercent : 0)))
    ) || 0;

    var hasValidHealthData = (healthPercent > 0 || designCap > 0);

    if (!hasValidHealthData && !mfr) return;

    $('#batteryAuditPlaceholder').hide();
    $('#batteryAuditGate').show();
    $('#batteryAuditLoading').hide();
    $('#batteryAuditResults').css('display', 'flex');

    if (mfr) $('#batteryManufacturer').text(mfr);
    if (battName) $('#batteryNameDb').text(battName);
    if (serial) $('#batterySerialDb').text(serial);
    if (chemistry) $('#batteryChemistryDb').text(chemistry);
    if (cycleCount > 0) $('#batteryCycleCountDb').text(cycleCount);
    var auditDateRaw = metrics.scanDate || metrics.ScanDate || metrics.dateTime || metrics.DateTime;
    if (auditDateRaw) {
        try {
            var auditD = new Date(auditDateRaw);
            $('#batteryLastAuditDb').text(auditD.toLocaleString(undefined, {
                year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
            }));
        } catch (e) { $('#batteryLastAuditDb').text(String(auditDateRaw)); }
    }

    $('#auditHealthPercentText').text(hasValidHealthData ? healthPercent.toFixed(1) + '%' : 'N/A');
    $('#auditDesignCap').text(designCap > 0 ? designCap.toLocaleString() : '--');
    $('#auditFullCap').text(fullCap > 0 ? fullCap.toLocaleString() : '--');
    $('#auditCycleCount').text(cycleCount > 0 ? cycleCount : '--');
    $('#auditWearRate').text(wearRate !== '--' ? Math.abs(wearRate).toFixed(1) : '--');

    var remainingText = '--';
    if (remaining !== undefined) {
        remainingText = (remaining === 999 || remaining > 120) ? 'Healthy' : remaining;
    }
    $('#auditRemainingLife').text(remainingText);

    const circle = $('#auditHealthCircle');
    var circumference = 2 * Math.PI * 45;
    var dashLen = hasValidHealthData ? (healthPercent / 100) * circumference : 0;
    circle.css('stroke-dasharray', dashLen + ', ' + circumference);

    let color = '#4ade80';
    let icon = '<i class="fas fa-check-circle" style="color:#4ade80;"></i>';

    if (!hasValidHealthData) {
        color = '#94a3b8'; icon = '<i class="fas fa-question-circle" style="color:#94a3b8;"></i>';
        status = 'No Data';
    } else if (healthPercent < 50) {
        color = '#ef4444'; icon = '<i class="fas fa-times-circle" style="color:#ef4444;"></i>';
        status = status || 'Critical';
    } else if (healthPercent < 60) {
        color = '#f97316'; icon = '<i class="fas fa-tools" style="color:#f97316;"></i>';
        status = status || 'Replacement Recommended';
    } else if (healthPercent < 80) {
        color = '#f59e0b'; icon = '<i class="fas fa-exclamation-triangle" style="color:#f59e0b;"></i>';
        status = status || 'Aging';
    }

    circle.css('stroke', color);
    $('#auditStatus').html(icon + ' <span style="color:' + color + ';">' + status + '</span>');
    $('#batteryStatus').text(status);

    var liveLvl = metrics.liveBatteryLevel !== undefined ? metrics.liveBatteryLevel
        : (metrics.LiveBatteryLevel !== undefined ? metrics.LiveBatteryLevel : null);
    var liveDetails = metrics.liveBatteryDetails || metrics.LiveBatteryDetails || '';

    if (liveLvl !== null && liveLvl !== undefined) {
        $('#liveBatteryCard').css('display', 'flex');
        $('#liveBatteryLevelText').text(liveLvl + '%');

        let liveColor = '#10b981';
        if (liveLvl <= 20) liveColor = '#ef4444';
        else if (liveLvl <= 50) liveColor = '#f59e0b';

        setTimeout(() => {
            $('#liveBatteryFill').css({ 'width': liveLvl + '%', 'background': liveColor });
        }, 100);

        if (liveDetails.toLowerCase().includes('ac') || liveDetails.toLowerCase().includes('charge')) {
            $('#liveBatteryLightning').show();
            liveDetails = 'Charging (' + liveDetails + ')';
        } else {
            $('#liveBatteryLightning').hide();
            if (liveDetails.toLowerCase() === 'battery') liveDetails = 'Discharging';
        }
        $('#liveBatteryDetailsText').text(liveDetails || 'Unknown');

        if (liveLvl > 0) {
            $('#batteryLevel').html(`
                <div style="font-size:1.1rem;font-weight:700;">${liveLvl}%</div>
                <div style="font-size:.65rem;color:var(--slate-500);">${liveDetails}</div>`);
        }
    } else {
        $('#liveBatteryCard').hide();
    }

    renderCapacityTrendChart(metrics.capacityHistory || metrics.CapacityHistory);
    renderBatteryUsageTables(
        metrics.batteryUsage || metrics.BatteryUsage,
        metrics.usageHistory || metrics.UsageHistory
    );
    checkBatteryReportExists();
}

let battHistHealthChart = null;
let battHistCapacityChart = null;
let battHistCycleChart = null;
let battHistLiveChart = null;

function loadBatteryHistoryCharts() {
    $('#batteryHistoryChartLoading').show();
    $('#batteryHistoryChartWrap').hide();
    $('#batteryHistoryNoData').hide();
    $('#batteryHistoryChartContainer').show();

    $.get(`/ComputerSummary/GetBatteryHistory?domain=${actualDomainName}`, function (rows) {
        $('#batteryHistoryChartLoading').hide();

        var validRows = (rows || []).filter(function (r) {
            var health = parseFloat(r.batteryHealthPercent || r.BatteryHealthPercent) || 0;
            var cap = parseInt(r.fullChargeCapacity || r.FullChargeCapacity) || 0;
            return health > 0 || cap > 0;
        });

        if (!validRows || validRows.length === 0) {
            $('#batteryHistoryChartContainer').hide();
            return;
        }

        var rows = validRows;
        rows.sort((a, b) => new Date(a.scanDate || a.ScanDate) - new Date(b.scanDate || b.ScanDate));

        const labels = rows.map(r => {
            let d = new Date(r.scanDate || r.ScanDate);
            return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: '2-digit' });
        });

        const healthData = rows.map(r => parseFloat(r.batteryHealthPercent || r.BatteryHealthPercent) || null);
        const capacityData = rows.map(r => parseInt(r.fullChargeCapacity || r.FullChargeCapacity) || null);
        const cycleData = rows.map(r => parseInt(r.cycleCount || r.CycleCount) || null);
        const liveData = rows.map(r => parseInt(r.batteryPercentage || r.BatteryPercentage) || null);

        $('#battHistAuditCount').text(rows.length + ' audit' + (rows.length === 1 ? '' : 's'));
        let firstDate = new Date(rows[0].scanDate || rows[0].ScanDate);
        let lastDate = new Date(rows[rows.length - 1].scanDate || rows[rows.length - 1].ScanDate);
        $('#battHistFirstAudit').text('First: ' + firstDate.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' }));
        $('#battHistLastAudit').text('Latest: ' + lastDate.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' }));

        const sharedXAxis = { ticks: { font: { size: 9 }, maxRotation: 45 } };
        const sharedOptions = {
            responsive: true, maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: { x: sharedXAxis }
        };

        if (battHistHealthChart) battHistHealthChart.destroy();
        battHistHealthChart = new Chart(
            document.getElementById('battHistHealthChart').getContext('2d'), {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Health %',
                    data: healthData,
                    borderColor: '#22c55e',
                    backgroundColor: 'rgba(34,197,94,0.12)',
                    borderWidth: 2.5, fill: true, tension: 0.3,
                    pointRadius: rows.length > 20 ? 2 : 4,
                    pointBackgroundColor: healthData.map(v =>
                        v >= 80 ? '#22c55e' : v >= 60 ? '#f59e0b' : '#ef4444'
                    )
                }]
            },
            options: {
                ...sharedOptions,
                scales: {
                    x: sharedXAxis,
                    y: { min: 0, max: 100, ticks: { callback: v => v + '%', font: { size: 10 } } }
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => `Health: ${ctx.raw}%`,
                            afterLabel: ctx => {
                                let r = rows[ctx.dataIndex];
                                let status = r.status || r.Status || '';
                                return status ? 'Status: ' + status : '';
                            }
                        }
                    }
                }
            }
        });

        if (battHistCapacityChart) battHistCapacityChart.destroy();
        battHistCapacityChart = new Chart(
            document.getElementById('battHistCapacityChart').getContext('2d'), {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Full Charge (mWh)',
                    data: capacityData,
                    backgroundColor: 'rgba(14,165,233,0.7)',
                    borderColor: '#0ea5e9',
                    borderWidth: 1,
                    borderRadius: 3
                }, {
                    label: 'Design (mWh)',
                    data: rows.map(r => parseInt(r.designCapacity || r.DesignCapacity) || null),
                    type: 'line',
                    borderColor: '#94a3b8',
                    borderWidth: 1.5,
                    borderDash: [4, 4],
                    fill: false,
                    pointRadius: 0
                }]
            },
            options: {
                ...sharedOptions,
                plugins: { legend: { display: true, position: 'bottom', labels: { font: { size: 10 } } } },
                scales: {
                    x: sharedXAxis,
                    y: { ticks: { font: { size: 10 } }, beginAtZero: false }
                }
            }
        });

        if (battHistCycleChart) battHistCycleChart.destroy();
        battHistCycleChart = new Chart(
            document.getElementById('battHistCycleChart').getContext('2d'), {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Cycle Count',
                    data: cycleData,
                    borderColor: '#a855f7',
                    backgroundColor: 'rgba(168,85,247,0.1)',
                    borderWidth: 2, fill: true, tension: 0.3,
                    pointRadius: rows.length > 20 ? 2 : 4
                }]
            },
            options: {
                ...sharedOptions,
                scales: {
                    x: sharedXAxis,
                    y: { ticks: { font: { size: 10 } }, beginAtZero: true }
                }
            }
        });

        if (battHistLiveChart) battHistLiveChart.destroy();
        battHistLiveChart = new Chart(
            document.getElementById('battHistLiveChart').getContext('2d'), {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Battery % at Audit',
                    data: liveData,
                    backgroundColor: liveData.map(v =>
                        !v ? '#e2e8f0' :
                            v <= 20 ? 'rgba(239,68,68,0.7)' :
                                v <= 50 ? 'rgba(245,158,11,0.7)' :
                                    'rgba(16,185,129,0.7)'
                    ),
                    borderRadius: 3
                }]
            },
            options: {
                ...sharedOptions,
                scales: {
                    x: sharedXAxis,
                    y: { min: 0, max: 100, ticks: { callback: v => v + '%', font: { size: 10 } } }
                }
            }
        });

        $('#batteryHistoryChartWrap').show();

    }).fail(function () {
        $('#batteryHistoryChartLoading').hide();
        $('#batteryHistoryChartContainer').hide();
    });
}

let battDrainChart = null;
let battUsageHistChart = null;

function parseDurationToMinutes(durStr) {
    if (!durStr || durStr === '-' || durStr === '--') return null;
    const parts = durStr.trim().split(':').map(Number);
    if (parts.length === 3) return parts[0] * 60 + parts[1] + parts[2] / 60;
    if (parts.length === 2) return parts[0] * 60 + parts[1];
    return null;
}

function renderBatteryUsageTables(batteryUsage, usageHistory) {
    const container = $('#usageHistoryContainer');
    let hasData = false;

    const drainCanvas = document.getElementById('battDrainChartCanvas');
    if (drainCanvas && batteryUsage && batteryUsage.length > 0) {
        hasData = true;
        const drainLabels = batteryUsage.map(r => r.startTime ? r.startTime.split(' ').slice(-1)[0] : '-');
        const drainMins = batteryUsage.map(r => parseDurationToMinutes(r.duration));
        const drainEnergy = batteryUsage.map(r => {
            if (!r.energyDrained || r.energyDrained === '-' || r.energyDrained === '- -') return null;
            const m = r.energyDrained.match(/(\d[\d,]*)\s*mWh/i);
            return m ? parseInt(m[1].replace(/,/g, '')) : null;
        });

        if (battDrainChart) battDrainChart.destroy();
        battDrainChart = new Chart(drainCanvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: drainLabels,
                datasets: [
                    {
                        label: 'Duration (min)',
                        data: drainMins,
                        backgroundColor: 'rgba(14,165,233,0.7)',
                        borderColor: '#0ea5e9',
                        borderWidth: 1,
                        borderRadius: 3,
                        yAxisID: 'yDur'
                    },
                    {
                        label: 'Energy Drained (mWh)',
                        data: drainEnergy,
                        type: 'line',
                        borderColor: '#f59e0b',
                        backgroundColor: 'rgba(245,158,11,0.12)',
                        borderWidth: 2,
                        fill: false,
                        tension: 0.3,
                        pointRadius: 3,
                        yAxisID: 'yEng'
                    }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { font: { size: 10 } } } },
                scales: {
                    x: { ticks: { font: { size: 9 }, maxRotation: 45 } },
                    yDur: { position: 'left', ticks: { font: { size: 9 } }, title: { display: true, text: 'Duration (min)', font: { size: 9 } } },
                    yEng: { position: 'right', ticks: { font: { size: 9 } }, title: { display: true, text: 'mWh', font: { size: 9 } }, grid: { drawOnChartArea: false } }
                }
            }
        });
    }

    const histCanvas = document.getElementById('battUsageHistChartCanvas');
    if (histCanvas && usageHistory && usageHistory.length > 0) {
        hasData = true;
        const histLabels = usageHistory.map(r => r.period ? r.period.split(' - ').pop() : '-');
        const batMins = usageHistory.map(r => parseDurationToMinutes(r.batteryActive));
        const acMins = usageHistory.map(r => parseDurationToMinutes(r.acActive));

        if (battUsageHistChart) battUsageHistChart.destroy();
        battUsageHistChart = new Chart(histCanvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: histLabels,
                datasets: [
                    {
                        label: 'Battery Active (min)',
                        data: batMins,
                        backgroundColor: 'rgba(168,85,247,0.7)',
                        borderColor: '#a855f7',
                        borderWidth: 1,
                        borderRadius: 3
                    },
                    {
                        label: 'AC Active (min)',
                        data: acMins,
                        backgroundColor: 'rgba(34,197,94,0.7)',
                        borderColor: '#22c55e',
                        borderWidth: 1,
                        borderRadius: 3
                    }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { font: { size: 10 } } } },
                scales: {
                    x: { stacked: false, ticks: { font: { size: 9 }, maxRotation: 45 } },
                    y: { ticks: { font: { size: 9 } }, title: { display: true, text: 'Minutes', font: { size: 9 } }, beginAtZero: true }
                }
            }
        });
    }

    container.toggle(hasData);
}
let capacityChartInstance = null;
function renderCapacityTrendChart(history) {
    const container = $('#capacityChartContainer');
    if (!history || history.length < 2) { container.hide(); return; }
    container.show();

    const canvas = document.getElementById('capacityTrendChart');
    if (!canvas) return;
    if (capacityChartInstance) capacityChartInstance.destroy();

    capacityChartInstance = new Chart(canvas.getContext('2d'), {
        type: 'line',
        data: {
            labels: history.map(h => h.period || h.Period || ''),
            datasets: [
                {
                    label: 'Full Charge Capacity (mWh)',
                    data: history.map(h => h.fullChargeCapacity ?? h.FullChargeCapacity ?? null),
                    borderColor: '#0ea5e9',
                    backgroundColor: 'rgba(14,165,233,0.1)',
                    borderWidth: 2, fill: true, tension: 0.3
                },
                {
                    label: 'Design Capacity (mWh)',
                    data: history.map(h => h.designCapacity ?? h.DesignCapacity ?? null),
                    borderColor: '#cbd5e1', borderWidth: 2,
                    borderDash: [5, 5], fill: false, pointRadius: 0
                }
            ]
        },
        options: {
            responsive: true, maintainAspectRatio: false,
            plugins: { legend: { position: 'bottom', labels: { font: { size: 10 } } } },
            scales: {
                x: { ticks: { font: { size: 9 }, maxRotation: 45 } },
                y: { ticks: { font: { size: 10 } }, beginAtZero: false }
            }
        }
    });
}

function loadHardDiskDetails() {
    $.get(`/ComputerSummary/HardDisk?domain=${domaindata}`, function (disks) {
        if (!disks || !Array.isArray(disks) || disks.length === 0) {
            $('#diskHeroName').text('No disks found');
            return;
        }
        var hasRealData = disks.some(function (d) {
            var model = d.Model || d.model || '';
            var health = d.HealthStatus || d.healthStatus || '';
            var temp = parseFloat(d.Temperature || d.temperature) || 0;
            var cap = parseFloat(d.TotalCapacity || d.totalCapacity) || 0;
            return cap > 0 && model && model !== 'Unknown' && model !== '';
        });

        if (hasRealData) {
            renderHardDiskDashboard(disks);
            loadHwPartitions();
        } else {
            var d = disks[0];
            var model = d.Model || d.model || 'Unknown Disk';
            $('#diskHeroName').text(model);
        }
    }).fail(function () {
        $('#diskHeroName').text('Failed to load disk data');
    });
}

function renderHardDiskDashboard(disks) {
    $('#diskAuditPlaceholder').hide();
    $('#diskAuditGate').show();

    const d = disks[0];

    const totalCap = parseFloat(d.TotalCapacity || d.totalCapacity || 0).toFixed(1);
    $('#diskHeroName').text(d.Model || d.model || 'Unknown Disk');
    $('#diskHeroCapacity').html('<i class="fas fa-database"></i> ' + totalCap + ' GB Total');
    $('#diskHeroInterface').html('<i class="fas fa-plug"></i> ' + (d.InterfaceType || d.interfaceType || 'N/A'));
    const powHrs = d.PowerOnHours || d.powerOnHours || 0;
    $('#diskHeroPowerOn').html('<i class="fas fa-clock"></i> ' + Number(powHrs).toLocaleString() + ' hrs powered');

    const health = (d.HealthStatus || d.healthStatus || '').toString().toUpperCase();
    const predictFail = d.PredictFailure || d.predictFailure || false;
    let healthColor = '#10b981', healthText = health || 'HEALTHY';
    if (predictFail || health === 'CRITICAL' || health === 'FAILING') {
        healthColor = '#ef4444'; healthText = 'FAILING';
        $('#diskHealthBadge').addClass('is-down');
    } else if (health === 'WARNING' || health === 'CAUTION') {
        healthColor = '#f59e0b'; healthText = 'WARNING';
    }
    $('#diskHealthBadge').css('color', healthColor)
        .html('<span class="cpu-live-dot" style="background:' + healthColor + ';"></span> ' + healthText);

    const dt = d.DateTime || d.dateTime;
    if (dt) {
        try {
            const parsed = new Date(dt);
            if (!isNaN(parsed)) $('#diskLastUpdated').text('Updated: ' + parsed.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }));
        } catch (e) { }
    }

    var temp = parseFloat(d.Temperature || d.temperature) || 0;
    var wear = parseFloat(d.Wear || d.wear) || 0;
    var powOnHrs = parseInt(d.PowerOnHours || d.powerOnHours) || 0;
    var tempColor = temp > 55 ? '#ef4444' : temp > 45 ? '#f59e0b' : '#22c55e';
    var wearColor = wear > 80 ? '#ef4444' : wear > 50 ? '#f59e0b' : '#22c55e';

    $('#diskHealthStatusLabel').html(
        predictFail
            ? '<i class="fas fa-times-circle"></i> Failing'
            : health === 'WARNING' || health === 'CAUTION'
                ? '<i class="fas fa-exclamation-triangle"></i> Warning'
                : '<i class="fas fa-check-circle"></i> Healthy'
    ).css('color', predictFail ? '#ef4444' : health === 'WARNING' ? '#f59e0b' : '#22c55e');

    $('#diskHealthTempLabel').text(temp > 0 ? temp.toFixed(0) + '\u00B0C' : 'N/A').css('color', temp > 0 ? tempColor : 'var(--slate-400)');
    $('#diskHealthWearLabel').text(wear > 0 ? wear.toFixed(1) + '%' : 'N/A').css('color', wear > 0 ? wearColor : 'var(--slate-400)');
    $('#diskHealthPowerOn').text(powOnHrs > 0 ? Number(powOnHrs).toLocaleString() + ' hrs' : 'N/A');
    $('#diskHealthPredictFailure').html(
        predictFail
            ? '<i class="fas fa-exclamation-circle" style="color:#ef4444;"></i> Yes'
            : '<i class="fas fa-check-circle" style="color:#22c55e;"></i> No'
    );
    $('#diskHealthSummaryCard').show();

    if (disks.length > 1) {
        $('#diskSelectorWrap').show();
        let tabHtml = '';
        disks.forEach(function (disk, idx) {
            const isActive = idx === 0;
            const cap = parseFloat(disk.TotalCapacity || disk.totalCapacity || 0).toFixed(0);
            tabHtml += `<li style="cursor:pointer;">
                <a class="disk-selector-tab cpu-pill${isActive ? ' active' : ''}"
                   style="${isActive ? 'background:linear-gradient(135deg,var(--primary),#0d9488);color:#fff;border-color:transparent;box-shadow:0 2px 8px rgba(14,165,233,.25);' : 'background:#fff;border-color:var(--slate-200);color:var(--slate-600);'}"
                   data-disk-idx="${idx}">
                   <i class="fas fa-hdd" style="margin-right:4px;"></i>
                   <strong>Disk ${idx + 1}</strong>
                   <span style="opacity:.7;font-size:.7rem;margin-left:4px;">${cap} GB â€” ${(disk.Model || disk.model || 'Unknown').substring(0, 20)}</span>
                </a></li>`;
        });
        $('#diskSelectorTabs').html(tabHtml);
        $(document).off('click', '.disk-selector-tab').on('click', '.disk-selector-tab', function () {
            const idx = parseInt($(this).data('disk-idx'));
            const clickedDisk = disks[idx];
            $('.disk-selector-tab').css({ background: '#fff', color: 'var(--slate-600)', borderColor: 'var(--slate-200)', boxShadow: 'none' }).removeClass('active');
            $(this).css({ background: 'linear-gradient(135deg,var(--primary),#0d9488)', color: '#fff', borderColor: 'transparent', boxShadow: '0 2px 8px rgba(14,165,233,.25)' }).addClass('active');
            const selCap = parseFloat(clickedDisk.TotalCapacity || clickedDisk.totalCapacity || 0).toFixed(1);
            $('#diskHeroName').text(clickedDisk.Model || clickedDisk.model || 'Unknown Disk');
            $('#diskHeroCapacity').html('<i class="fas fa-database"></i> ' + selCap + ' GB Total');
            $('#diskHeroInterface').html('<i class="fas fa-plug"></i> ' + (clickedDisk.InterfaceType || clickedDisk.interfaceType || 'N/A'));
            $('#diskHeroPowerOn').html('<i class="fas fa-clock"></i> ' + Number(clickedDisk.PowerOnHours || clickedDisk.powerOnHours || 0).toLocaleString() + ' hrs powered');
            renderDiskPanels(clickedDisk);
        });
    }

    renderDiskPanels(d);

}

function renderDiskPanels(d) {
    const totalCap = parseFloat(d.TotalCapacity || d.totalCapacity || 0);
    const usedGB = parseFloat(d.UsedSpaceGB || d.usedSpaceGB || 0);
    const freeGB = parseFloat(d.FreeSpaceGB || d.freeSpaceGB || 0);
    const usedPct = totalCap > 0 ? Math.min(100, (usedGB / totalCap) * 100) : 0;
    let barColor = '#22c55e';
    if (usedPct >= 90) barColor = '#ef4444';
    else if (usedPct >= 75) barColor = '#f59e0b';

    const usageHtml = `
        <div style="background:#fff;border:1px solid var(--slate-200);border-radius:var(--radius-md);padding:16px 18px;box-shadow:var(--shadow-sm);">
            <div style="display:flex;justify-content:space-between;align-items:flex-start;flex-wrap:wrap;gap:4px;margin-bottom:10px;">
                <span style="font-size:.82rem;font-weight:700;color:var(--slate-700);flex:1;min-width:0;word-break:break-word;">${d.Model || d.model || 'Disk'}</span>
                <span style="font-size:.76rem;color:var(--slate-500);white-space:nowrap;">${usedGB.toFixed(2)} GB used of ${totalCap.toFixed(2)} GB</span>
            </div>
            <div style="height:12px;border-radius:6px;background:var(--slate-100);overflow:hidden;">
                <div style="height:100%;width:${usedPct.toFixed(1)}%;background:${barColor};border-radius:6px;transition:width 1s ease;"></div>
            </div>
            <div style="display:flex;justify-content:space-between;margin-top:6px;">
                <span style="font-size:.7rem;color:${barColor};font-weight:700;">${usedPct.toFixed(1)}% Used</span>
                <span style="font-size:.7rem;color:#22c55e;font-weight:700;">${freeGB.toFixed(2)} GB Free</span>
            </div>
        </div>`;
    $('#diskUsageContainer').html(usageHtml);

    const wear = d.Wear || d.wear;
    const temp = d.Temperature || d.temperature;
    const specsHtml = `
        <div class="cs-info-grid" style="grid-template-columns:repeat(auto-fill,minmax(130px,1fr));">
            <div class="cs-info-box"><div class="cs-info-box-label">Model</div><div class="cs-info-box-value accent" style="font-size:.8rem;">${d.Model || d.model || 'N/A'}</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Manufacturer</div><div class="cs-info-box-value">${d.Manufacturer || d.manufacturer || 'N/A'}</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Serial Number</div><div class="cs-info-box-value" style="font-family:var(--font-mono);font-size:.72rem;">${d.SerialNumber || d.serialNumber || 'N/A'}</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Firmware</div><div class="cs-info-box-value" style="font-family:var(--font-mono);font-size:.74rem;">${d.FirmwareVersion || d.firmwareVersion || 'N/A'}</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Interface</div><div class="cs-info-box-value">${d.InterfaceType || d.interfaceType || 'N/A'}</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Description</div><div class="cs-info-box-value" style="font-size:.75rem;font-weight:500;">${d.Description || d.description || 'N/A'}</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Total Capacity</div><div class="cs-info-box-value accent">${parseFloat(d.TotalCapacity || d.totalCapacity || 0).toFixed(2)} GB</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Used Space</div><div class="cs-info-box-value amber">${usedGB.toFixed(2)} GB</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Free Space</div><div class="cs-info-box-value green">${freeGB.toFixed(2)} GB</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Power-On Hours</div><div class="cs-info-box-value">${Number(d.PowerOnHours || d.powerOnHours || 0).toLocaleString()} hrs</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Health Status</div><div class="cs-info-box-value" style="color:${(d.HealthStatus || '').toUpperCase() === 'HEALTHY' ? '#22c55e' : '#f59e0b'};">${d.HealthStatus || d.healthStatus || 'N/A'}</div></div>
            <div class="cs-info-box"><div class="cs-info-box-label">Predict Failure</div><div class="cs-info-box-value" style="color:${(d.PredictFailure || d.predictFailure) ? '#ef4444' : '#22c55e'};">${(d.PredictFailure || d.predictFailure) ? 'Yes âš ' : 'No'}</div></div>
        </div>`;
    $('#diskSpecsContainer').html(specsHtml);

    const wearVal = (wear !== undefined && wear !== null) ? wear : null;
    const tempVal = (temp !== undefined && temp !== null) ? temp : null;
    const wearPct = wearVal !== null ? Math.min(100, wearVal) : 0;
    const tempNorm = tempVal !== null ? Math.min(100, (tempVal / 70) * 100) : 0;

    let wearColor = '#22c55e';
    if (wearPct >= 80) wearColor = '#ef4444';
    else if (wearPct >= 50) wearColor = '#f59e0b';

    let tempColor = '#22c55e';
    if (tempVal >= 55) tempColor = '#ef4444';
    else if (tempVal >= 45) tempColor = '#f59e0b';

    const readErr = Number(d.ReadErrorsTotal || d.readErrorsTotal || 0);
    const writeErr = Number(d.WriteErrorsTotal || d.writeErrorsTotal || 0);
    const readCorr = Number(d.ReadErrorsCorrected || d.readErrorsCorrected || 0);

    const smartHtml = `
        <div style="background:#fff;border:1px solid var(--slate-200);border-radius:var(--radius-md);padding:18px;box-shadow:var(--shadow-sm);">
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:16px;">

                ${wearVal !== null ? `
                <div>
                    <div style="font-size:.74rem;font-weight:700;color:var(--slate-600);margin-bottom:8px;"><i class="fas fa-tools" style="color:#f59e0b;margin-right:5px;"></i>Wear Level</div>
                    <div style="display:flex;align-items:center;gap:12px;">
                        <div style="flex:1;height:10px;border-radius:6px;background:var(--slate-100);overflow:hidden;">
                            <div style="height:100%;width:${wearPct}%;background:${wearColor};border-radius:6px;transition:width 1s;"></div>
                        </div>
                        <span style="font-size:.8rem;font-weight:800;color:${wearColor};min-width:36px;text-align:right;">${wearPct}%</span>
                    </div>
                </div>` : ''}

                ${tempVal !== null ? `
                <div>
                    <div style="font-size:.74rem;font-weight:700;color:var(--slate-600);margin-bottom:8px;"><i class="fas fa-thermometer-half" style="color:${tempColor};margin-right:5px;"></i>Temperature</div>
                    <div style="display:flex;align-items:center;gap:12px;">
                        <div style="flex:1;height:10px;border-radius:6px;background:var(--slate-100);overflow:hidden;">
                            <div style="height:100%;width:${tempNorm.toFixed(1)}%;background:${tempColor};border-radius:6px;transition:width 1s;"></div>
                        </div>
                        <span style="font-size:.8rem;font-weight:800;color:${tempColor};min-width:40px;text-align:right;">${tempVal}Â°C</span>
                    </div>
                </div>` : ''}

            </div>

            <div style="margin-top:16px;border-top:1px solid var(--slate-100);padding-top:14px;">
                <div style="font-size:.72rem;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:var(--slate-500);margin-bottom:10px;">Error Counters &amp; Metadata</div>
                <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(130px,1fr));gap:8px;">
                    ${_smartMetric('Read Errors (Total)', readErr, readErr > 0 ? '#ef4444' : '#22c55e', 'fas fa-times-circle')}
                    ${_smartMetric('Write Errors (Total)', writeErr, writeErr > 0 ? '#ef4444' : '#22c55e', 'fas fa-times-circle')}
                    ${_smartMetric('Read Errors (Corrected)', readCorr, readCorr > 0 ? '#f59e0b' : '#22c55e', 'fas fa-check-circle')}
                </div>
            </div>
        </div>`;
    $('#diskSmartContainer').html(smartHtml);
}

function _smartMetric(label, value, color, icon, isText) {
    const display = isText ? value : Number(value).toLocaleString();
    return `<div style="background:var(--slate-50);border:1px solid var(--slate-200);border-radius:var(--radius-sm);padding:10px 12px;">
                <div style="font-size:.65rem;font-weight:700;text-transform:uppercase;color:var(--slate-500);margin-bottom:4px;">${label}</div>
                <div style="font-size:.88rem;font-weight:800;color:${color};display:flex;align-items:center;gap:6px;">
                    <i class="${icon}" style="font-size:.7rem;"></i>${display}
                </div>
            </div>`;
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
        $('#batteryAuditPlaceholder').hide();
        $('#batteryAuditGate').show();
        $('#batteryAuditResults').hide();
        $('#batteryAuditLoading').html('<i class="fas fa-circle-notch fa-spin" style="font-size: 1.5rem; color: var(--cyan); margin-bottom: 8px; display: block;"></i> Fetching live diagnostics from device, please wait...');
        $('#batteryAuditLoading').show();

        function triggerBatteryFallback(reasonMsg) {
            if (!$('#batteryAuditLoading').is(':visible')) return;
            $.get(`/ComputerSummary/Battery?domain=${actualDomainName}`, function (data) {
                renderBatteryAuditPanel(data);
                loadBatteryHistoryCharts();
                sysAlert(reasonMsg || 'Live fetch unavailable. Showing last known state.', 'warning');
            });
        }

        sysAlert('Battery audit requested from device. Waiting for data...', 'info');

        $.ajax({
            url: '/ComputerSummary/AuditBattery?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            timeout: 85000,
            success: function (res) {
                if (res && res.success && res.data && res.data.metrics) {
                    renderBatteryAuditPanel(res.data.metrics);
                    loadBatteryHistoryCharts();
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

    $('#btnAuditMemory').on('click', function (e) {
        e.preventDefault();
        let btn = $(this);
        let originalText = btn.html();
        btn.html('<i class="fas fa-circle-notch fa-spin"></i> Processing...');
        btn.prop('disabled', true);
        btn.css('opacity', '0.7');

        $('#memAuditPlaceholder').hide();
        $('#memAuditGate').hide();
        $('#memAuditLoading').show();

        function triggerMemoryFallback(reasonMsg) {
            $.get(`/ComputerSummary/MemorySummary?domain=${domaindata}`, function (data) {
                if (hasRealMemoryData(data)) {
                    renderMemoryPanel(data);
                    sysAlert(reasonMsg || 'Live fetch unavailable. Showing last known state.', 'warning');
                } else {
                    $('#memAuditLoading').hide();
                    $('#memAuditPlaceholder').show();
                    sysAlert(reasonMsg || 'No memory data available yet for this device.', 'error');
                }
            }).fail(function () {
                $('#memAuditLoading').hide();
                $('#memAuditPlaceholder').show();
                sysAlert(reasonMsg || 'Unable to reach the server. Please try again.', 'error');
            });
        }

        sysAlert('Memory audit requested. This can take a little while on slower devices — please wait...', 'info');

        $.ajax({
            url: '/ComputerSummary/AuditMemory?domain=' + encodeURIComponent(domaindata) + '&hostName=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            timeout: 0,
            success: function (res) {
                if (res && res.success && res.data && res.data.metrics) {
                    renderMemoryPanel(res.data.metrics);
                    sysAlert(res.message || 'Memory audit completed!', 'success');
                } else if (res && res.success) {
                    $.get(`/ComputerSummary/MemorySummary?domain=${domaindata}`, function (data) {
                        if (hasRealMemoryData(data)) renderMemoryPanel(data);
                    });
                    sysAlert(res.message || 'Memory audit completed!', 'success');
                } else {
                    triggerMemoryFallback(res ? res.message : 'Audit did not complete. Showing last known state.');
                }
            },
            error: function (xhr, status) {
                let msg = 'Connection error while requesting audit. Showing last known state.';
                triggerMemoryFallback(msg);
            },
            complete: function () {
                btn.html(originalText);
                btn.prop('disabled', false);
                btn.css('opacity', '1');
            }
        });
    });

    $('#btnAuditProcessor').on('click', function (e) {
        e.preventDefault();
        let btn = $(this);
        let originalText = btn.html();
        btn.html('<i class="fas fa-circle-notch fa-spin"></i> Processing...');
        btn.prop('disabled', true);
        btn.css('opacity', '0.7');
        sysAlert('Processor audit requested. Waiting for device to respond...', 'info');

        $.ajax({
            url: '/ComputerSummary/AuditProcessor?domain=' + encodeURIComponent(domaindata) + '&hostName=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            timeout: 90000,
            success: function (res) {
                if (res && res.success) {
                    sysAlert(res.message || 'Processor audit completed!', 'success');
                    loadProcessorDetails(true);  
                } else {
                    sysAlert(res.message || 'Processor audit failed.', 'error');
                }
            },
            error: function (xhr, status) {
                let msg = status === 'timeout' ? 'Processor audit timed out. The device may still be processing.' : 'Connection error while requesting processor audit.';
                sysAlert(msg, 'error');
            },
            complete: function () {
                btn.html(originalText);
                btn.prop('disabled', false);
                btn.css('opacity', '1');
            }
        });
    });

    $('#btnAuditHardDisk').on('click', function (e) {
        e.preventDefault();
        let btn = $(this);
        let originalText = btn.html();
        btn.html('<i class="fas fa-circle-notch fa-spin"></i> Processing...');
        btn.prop('disabled', true);
        btn.css('opacity', '0.7');
        sysAlert('Hard Disk audit requested. Waiting for device to respond...', 'info');

        $.ajax({
            url: '/ComputerSummary/AuditHardDisk?domain=' + encodeURIComponent(domaindata) + '&hostName=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            timeout: 90000,
            success: function (res) {
                if (res && res.success) {
                    sysAlert(res.message || 'Hard Disk audit completed!', 'success');
                    loadHardDiskDetails();
                } else {
                    sysAlert(res.message || 'Hard Disk audit failed.', 'error');
                }
            },
            error: function (xhr, status) {
                let msg = status === 'timeout' ? 'Hard Disk audit timed out. The device may still be processing.' : 'Connection error while requesting hard disk audit.';
                sysAlert(msg, 'error');
            },
            complete: function () {
                btn.html(originalText);
                btn.prop('disabled', false);
                btn.css('opacity', '1');
            }
        });
    });

    checkBatteryReportExists();
});
(function () {
    var CS_NAV_IMAGES = {
        '#System': '/images/systeminfo/system.png',            
        '#Hardware': '/images/systeminfo/hardware.png',
        '#Software': '/images/systeminfo/software.png',
        '#security': '/images/systeminfo/security.png',
        '#PatchManagment': '/images/systeminfo/patchManagment.png',
        '#Restriction': '/images/systeminfo/restriction.png',
        '#UsbAudit': '/images/systeminfo/usbAudit.png',
        '#History': '/images/systeminfo/history.png',
        '#Updatelogs': '/images/systeminfo/updatelogs.png',

        '#BatteryHw': '',
        '#BIOSHw': '',
        '#HardDiskHw': '',
        '#KeyboardHw': '',
        '#MonitorHw': '',
        '#MotherboardHw': '',
        '#NetworkHw': '',
        '#MemoryHw': '',
        '#PointingHw': '',
        '#PrintersHw': '',
        '#ProcessorsHw': '',
        '#SoundHw': '',
        '#VideoHw': '',
        '#USBHw': '',

        '#ServicesSys': '/images/systeminfo/ServicesSys.png',
        '#UsersSys': '',
        '#GroupsSys': '',
        '#DriversSys': '',
        '#SharesSys': '',

        '#DesktopAppsSw': '',
        '#StoreAppsSw': '',
        '#InstallersSw': '',

        '#AntivirusSec': '',
        '#FirewallSec': '',
        '#BitLockerSec': '',
        '#ExternalDevSec': '',

        '#WindowsListTab': '',
        '#ThirdPartyTab': '',
        '#HotfixTab': '',

        '#AuditHistory': '',
        '#LoginHistory': '',

        '#SummaryLog': '',
        '#SystemLogs': '',
        '#HardwareLogs': '',
        '#SoftwareLogs': '',
        '#SecurityLogs': '',

        '#RestrictionOverview': '',
        '#UsbAuditOverview': ''
    };

    function cardHtml(target, iconClass, label, size) {
        var sizeClass = size === 'sm' ? ' cs-nav-card-sm' : '';
        var imgUrl = CS_NAV_IMAGES[target];
        var hasImgClass = imgUrl ? ' cs-nav-card-has-img' : '';
        var imgInner = imgUrl
            ? '<img src="' + imgUrl + '" alt="' + label + '" onerror="this.closest(\'.cs-nav-card-img\').classList.remove(\'cs-img-box\');this.parentElement.innerHTML=\'<i class=&quot;' + (iconClass || 'fas fa-square') + '&quot;></i>\';">'
            : '<i class="' + (iconClass || 'fas fa-square') + '"></i>';
        return '<div class="cs-nav-card' + sizeClass + hasImgClass + '" data-target="' + target + '">' +
            '<div class="cs-nav-card-img' + (imgUrl ? ' cs-img-box' : '') + '">' + imgInner + '</div>' +
            '<div class="cs-nav-card-label">' + label + '</div>' +
            '<i class="fas fa-chevron-right cs-nav-card-arrow"></i>' +
            '</div>';
    }

    function linkParts($a) {
        var target = $a.attr('href');
        var iconClass = $a.find('i').first().attr('class');
        var label = $.trim($a.clone().children().remove().end().text());
        return { target: target, iconClass: iconClass, label: label };
    }

    function buildMainCardGrid() {
        var $mainList = $('#mainTabList');
        var $grid = $('#mainTabCardGridInner');
        if (!$mainList.length || !$grid.length) return;

        $mainList.find('li.main-tab').each(function () {
            var $a = $(this).find('a').first();
            var parts = linkParts($a);
            if (!parts.target || parts.target === '#Summary') return;

            var $card = $(cardHtml(parts.target, parts.iconClass, parts.label));
            $card.on('click', function () {
                openMainSection(parts.target);
            });
            $grid.append($card);
        });
    }

    function buildSubCardGrids() {
        $('#mainTabContent > .tab-pane').each(function () {
            var $pane = $(this);
            var $subBar = $pane.find('> .cs-card > .cs-subtab-bar').first();
            var $innerContent = $pane.find('> .cs-card > .tab-content').first();
            if (!$subBar.length || !$innerContent.length) return;

            var $wrap = $('<div class="cs-subtab-card-grid-wrap"></div>');
            var $backBtn = $('<button type="button" class="cs-back-btn"><i class="fas fa-arrow-left"></i> Back to System Info</button>');
            $backBtn.on('click', function () { openMainCardGrid(); });
            var titleText = $.trim($pane.closest('.tab-pane').length ? ($mainLabelFor($pane.attr('id')) || '') : '');
            var $header = $('<div class="cs-subtab-card-grid-header"></div>').append($backBtn);
            if (titleText) {
                $header.append('<h3 class="cs-subtab-card-grid-title">' + titleText + '</h3>');
            }
            var $subGrid = $('<div class="cs-card-grid cs-card-grid-sm"></div>');

            $subBar.find('li').each(function () {
                var $a = $(this).find('a').first();
                var parts = linkParts($a);
                if (!parts.target) return;

                var $card = $(cardHtml(parts.target, parts.iconClass, parts.label || 'Overview', 'sm'));
                $card.on('click', function () {
                    $a.trigger('click');
                    showSubContent($pane);
                });
                $subGrid.append($card);
            });

            $wrap.append($header).append($subGrid);
            $subBar.before($wrap);
            $subBar.css('display', 'none');

            var $backToCards = $('<button type="button" class="cs-back-btn cs-sub-back-btn"><i class="fas fa-th-large"></i> Back to categories</button>');
            $backToCards.on('click', function () { showSubCards($pane); });
            $innerContent.before($backToCards);

            $pane.data('csCardWrap', $wrap);
            $pane.data('csInnerContent', $innerContent);
            $pane.data('csBackToCards', $backToCards);
        });
    }

    function $mainLabelFor(paneId) {
        var $link = $('#mainTabList a[href="#' + paneId + '"]');
        if (!$link.length) return '';
        return $.trim($link.clone().children().remove().end().text());
    }

    function openMainCardGrid() {
        $('#Summary').removeClass('active');
        $('#mainTabContent > .tab-pane').removeClass('active');
        $('#csEntryBanner').hide();
        $('#mainTabCardGrid').show();
        if ($('#mainTabCardGrid')[0] && $('#mainTabCardGrid')[0].scrollIntoView) {
            $('#mainTabCardGrid')[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    function openMainSection(target) {
        var $link = $('#mainTabList a[href="' + target + '"]');
        if (!$link.length) return;
        $link.trigger('click');

        $('#mainTabCardGrid').hide();
        $('#csEntryBanner').hide();

        var $pane = $(target);
        showSubCards($pane);

        if ($pane[0] && $pane[0].scrollIntoView) {
            $pane[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    function showSubCards($pane) {
        var $wrap = $pane.data('csCardWrap');
        var $inner = $pane.data('csInnerContent');
        var $backToCards = $pane.data('csBackToCards');
        if ($wrap && $wrap.length) $wrap.show();
        if ($inner && $inner.length) $inner.hide();
        if ($backToCards && $backToCards.length) $backToCards.hide();
    }

    function showSubContent($pane) {
        var $wrap = $pane.data('csCardWrap');
        var $inner = $pane.data('csInnerContent');
        var $backToCards = $pane.data('csBackToCards');
        if ($wrap && $wrap.length) $wrap.hide();
        if ($inner && $inner.length) $inner.show();
        if ($backToCards && $backToCards.length) $backToCards.show();

        setTimeout(function () {
            $(window).trigger('resize');
            if ($.fn.DataTable) {
                $inner.find('table.dataTable').each(function () {
                    if ($.fn.DataTable.isDataTable(this)) {
                        $(this).DataTable().columns.adjust();
                    }
                });
            }
        }, 150);
    }

    function backToSummary() {
        $('#mainTabCardGrid').hide();
        $('#mainTabContent > .tab-pane').removeClass('active');
        $('#Summary').addClass('active');
        $('#csEntryBanner').show();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    $(document).ready(function () {
        buildMainCardGrid();
        buildSubCardGrids();

        $('#btnOpenSystemInfo').on('click', function () { openMainCardGrid(); });
        $('#btnBackToSummary').on('click', function () { backToSummary(); });
    });

})();