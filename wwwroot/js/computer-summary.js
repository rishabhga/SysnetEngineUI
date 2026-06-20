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
            loadPhysicalMemoryDetails();
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

function initTable(selector, url, columns) {
    dataTables[selector] = { url: url, columns: columns };
    tableRegistry[selector] = { url: url, columns: columns };

    if ($.fn.DataTable.isDataTable(selector)) {
        $(selector).DataTable().destroy();
    }

    $(selector).DataTable({
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
            emptyTable: '<div class="cs-dt-empty"><i class="fas fa-inbox"></i><span>No records found</span></div>',
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
    });
}

function initTablesInPane(paneId) {
    $(paneId).find('table').each(function () {
        var tableId = '#' + $(this).attr('id');
        if (!$.fn.DataTable.isDataTable(tableId) && tableRegistry[tableId]) {
            initTable(tableId, tableRegistry[tableId].url, tableRegistry[tableId].columns);
        }
    });
}

function loadPhysicalMemoryDetails() {
    $('#tdMaximumSupportedRAM, #tdLocation, #tdSlotsAvailable, #tdSlotsUsed')
        .html('<i class="fas fa-spinner fa-spin text-gray-400"></i>');

    $.get(`/ComputerSummary/PhysicalMemory?domain=${domaindata}`, function (data) {
        if (data) {
            $('#tdMaximumSupportedRAM').text(data.maximumSupportedRAM || data.MaximumSupportedRAM || 'N/A');
            $('#tdLocation').text(data.location || data.Location || 'N/A');
            $('#tdSlotsAvailable').text(data.slotsAvailable !== undefined ? data.slotsAvailable
                : (data.SlotsAvailable !== undefined ? data.SlotsAvailable : '0'));
            $('#tdSlotsUsed').text(data.slotsUsed !== undefined ? data.slotsUsed
                : (data.SlotsUsed !== undefined ? data.SlotsUsed : '0'));
        } else {
            $('#tdMaximumSupportedRAM, #tdLocation, #tdSlotsAvailable, #tdSlotsUsed').text('N/A');
        }
    }).fail(function () {
        $('#tdMaximumSupportedRAM, #tdLocation, #tdSlotsAvailable, #tdSlotsUsed')
            .html('<span class="text-red-400">Error</span>');
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

                var barColor = 'var(--cyan)'; // Cyan Blue for up to 80
                if (usagePct > 90) barColor = 'var(--red)'; // Red for > 90
                else if (usagePct > 80) barColor = 'var(--amber)'; // Yellow for > 80

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

function loadProcessorDetails() {
    $.get(`/ComputerSummary/Processors?domain=${domaindata}`, function (data) {
        $('#processorSpeed').text(data.processorSpeed || 'N/A');
        $('#processorManufacturer').text(data.manufacturer || 'N/A');
        $('#processorCores').text(data.numberOfCores || 'N/A');
        $('#processorSocket').text(data.socketDesignation || 'N/A');
        $('#processorStatus').text(data.deviceStatus || 'N/A');
        $('#processorDescription').text(data.description || 'N/A');
    }).fail(function () { console.error("Failed to load processor details"); });
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

    $('#btnViewBatteryReport').on('click', function(e) {
        e.preventDefault();
        window.open(`/ComputerSummary/ViewBatteryReport?domain=${encodeURIComponent(actualDomainName)}`, '_blank');
    });

    $('#btnAuditBattery').on('click', function (e) {
        e.preventDefault();
        $('#batteryAuditResults').hide();
        $('#batteryAuditLoading').html('<i class="fas fa-circle-notch fa-spin" style="font-size: 1.5rem; color: var(--cyan); margin-bottom: 8px; display: block;"></i> Fetching live diagnostics from device, please wait...');
        $('#batteryAuditLoading').show();

        if (batteryAuditTimeout) clearTimeout(batteryAuditTimeout);
        if (batteryAuditPollInterval) clearInterval(batteryAuditPollInterval);

        let initialTime = Date.now();
        let maxPollDuration = 30000; // 30 seconds max polling

        function stopPolling() {
            if (batteryAuditPollInterval) clearInterval(batteryAuditPollInterval);
            if (batteryAuditTimeout) clearTimeout(batteryAuditTimeout);
        }

        function triggerBatteryFallback(reasonMsg) {
            if (!$('#batteryAuditLoading').is(':visible')) return;
            stopPolling();

            $.get(`/ComputerSummary/Battery?domain=${domaindata}`, function (data) {
                renderBatteryAuditPanel(data);
                sysAlert(reasonMsg || 'Live fetch unavailable. Showing last known state.', 'warning');
            });
        }

        batteryAuditTimeout = setTimeout(function () {
            triggerBatteryFallback('Live fetch timed out (30s). Showing last known state.');
        }, maxPollDuration);

        // Function to poll for the latest file
        function pollLatestMetrics() {
            $.get(`/ComputerSummary/LatestBatteryMetrics?domain=${encodeURIComponent(actualDomainName)}`, function (res) {
                if (res && res.metrics && res.lastWriteTimeUtc) {
                    let fileTime = new Date(res.lastWriteTimeUtc).getTime();
                    // If the file was written AFTER we started the audit (with a 5 second buffer for time drift)
                    if (fileTime > (initialTime - 5000)) {
                        stopPolling();
                        renderBatteryAuditPanel(res.metrics);
                        sysAlert('Battery health data received!', 'success');
                    }
                }
            }).fail(function() {
                // Ignore 404s, keep polling
            });
        }

        $.ajax({
            url: '/ComputerSummary/AuditBattery?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            success: function (res) {
                if (res && res.success) {
                    sysAlert('Battery audit requested from device. Waiting for data...', 'info');
                    batteryAuditPollInterval = setInterval(pollLatestMetrics, 3000);
                } else {
                    triggerBatteryFallback(res.message || 'Failed to send audit request, falling back...');
                }
            },
            error: function () {
                triggerBatteryFallback('Connection error while requesting audit, falling back...');
            }
        });
    });
    
    // Initial check on load
    checkBatteryReportExists();
});