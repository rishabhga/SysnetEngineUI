window.DashboardHierarchy = (function () {
    let hierarchyData = [];
    let state = {
        company: null,
        group: null,
        location: null,
        searchTerm: ''
    };

    const init = async () => {
        setupEventListeners();
        await fetchData();
        render();
    };

    const setupEventListeners = () => {
        $(document).on('searchEvent', (e, term) => {
            state.searchTerm = term.toLowerCase();
            render();
        });

        window.onpopstate = (event) => {
            if (event.state) {
                state = event.state;
                if (window.SearchMediator && window.SearchMediator.updateNavLogo) {
                    if (state.company) {
                        const comp = hierarchyData.find(c => c.companyId === state.company.id);
                        if (comp) window.SearchMediator.updateNavLogo(comp.logoUrl, comp.companyName);
                    } else {
                        window.SearchMediator.resetNavLogo();
                    }
                }
                render();
            }
        };
    };

    const fetchData = async () => {
        try {
            const data = await $.get('/Home/GetDashboardHierarchy');
            hierarchyData = data;
        } catch (error) {
            console.error("Failed to load hierarchy data", error);
            showError("Could not load organization structure.");
        }
    };

    const navigate = (level, id, name) => {
        const newState = { ...state };

        if (level === 'all') {
            newState.company = null;
            newState.group = null;
            newState.location = null;
        } else if (level === 'company') {
            newState.company = { id, name };
            newState.group = null;
            newState.location = null;

            const comp = hierarchyData.find(c => c.companyId === id);
            if (comp && comp.groups.length === 1) {
                const g = comp.groups[0];
                newState.group = { id: g.groupId, name: g.groupName };
                if (g.locations.length === 1) {
                    const l = g.locations[0];
                    newState.location = { id: l.locationId, name: l.locationName };
                }
            }
        } else if (level === 'group') {
            newState.group = { id, name };
            newState.location = null;

            const comp = hierarchyData.find(c => c.companyId === state.company.id);
            const grp = comp?.groups.find(g => g.groupId === id);
            if (grp && grp.locations.length === 1) {
                const l = grp.locations[0];
                newState.location = { id: l.locationId, name: l.locationName };
            }
        } else if (level === 'location') {
            newState.location = { id, name };
        }

        state = newState;
        render();
        if (typeof window.onHierarchyChange === 'function') {
            window.onHierarchyChange(state);
        }
    };

    const render = () => {
        renderBreadcrumbs();
        renderCompanyStrip();

        const $container = $('#hierarchyContainer');
        $container.empty();

        if (state.searchTerm) {
            renderSearchResults($container);
            return;
        }

        if (!state.company) {
            renderCompanies($container);
        } else if (!state.group) {
            renderGroups($container);
        } else if (!state.location) {
            renderLocations($container);
        } else {
            renderUsers($container);
        }
    };

    const renderBreadcrumbs = () => {
        const $bc = $('#hierarchyBreadcrumbs').empty();

        const addCrumb = (name, level, id) => {
            const $item = $(`<li class="flex items-center gap-2">
                <i class="fas fa-chevron-right text-[10px] text-slate-400"></i>
                <button class="hover:text-blue-500 transition">${name}</button>
            </li>`);
            $item.find('button').click(() => navigate(level, id, name));
            $bc.append($item);
        };

        const $home = $(`<li class="flex items-center gap-2">
            <button class="hover:text-blue-500 transition font-bold">ALL COMPANIES</button>
        </li>`);
        $home.find('button').click(() => navigate('all'));
        $bc.append($home);

        if (state.company) addCrumb(state.company.name, 'company', state.company.id);
        if (state.group) addCrumb(state.group.name, 'group', state.group.id);
        if (state.location) addCrumb(state.location.name, 'location', state.location.id);
    };

    const renderCompanyStrip = () => {
        const $strip = $('#companyStrip').empty();
        hierarchyData.forEach(comp => {
            const isActive = state.company && state.company.id === comp.companyId;
            const $item = $(`
                <button class="flex flex-col items-center gap-2 p-3 rounded-xl transition-all ${isActive ? 'bg-blue-50 border-blue-200 border-2 scale-110 shadow-lg' : 'hover:bg-slate-50 border-transparent border-2'}" style="min-width: 100px;">
                    <div class="w-12 h-12 rounded-lg bg-white shadow-sm flex items-center justify-center overflow-hidden border border-slate-100">
                        <img src="${comp.logoUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(comp.companyName)}&background=random&color=fff`}" 
                             class="max-h-full object-contain"
                             onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(comp.companyName)}&background=00BCEB&color=fff'">
                    </div>
                    <span class="text-[10px] font-bold uppercase tracking-tight text-slate-600">${comp.companyName}</span>
                </button>
            `);
            $item.click(() => navigate('company', comp.companyId, comp.companyName));
            $strip.append($item);
        });
    };

    const renderCompanies = ($c) => {
        const $grid = $('<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6"></div>');
        hierarchyData.forEach(comp => {
            const groupCount = comp.groups.length;
            const locCount = comp.groups.reduce((acc, g) => acc + g.locations.length, 0);
            const userCount = comp.groups.reduce((acc, g) => acc + g.locations.reduce((acc2, l) => acc2 + l.users.length, 0), 0);

            const $card = $(`
                <div class="monitor-card p-6 cursor-pointer hover:border-blue-300 hover:shadow-xl transition-all group">
                    <div class="flex justify-between items-start mb-4">
                        <div class="w-16 h-16 rounded-2xl bg-slate-50 flex items-center justify-center border border-slate-100 group-hover:bg-blue-50 transition-colors">
                            <img src="${comp.logoUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(comp.companyName)}&background=random&color=fff`}" 
                                 class="max-h-10 object-contain"
                                 onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(comp.companyName)}&background=00BCEB&color=fff'">
                        </div>
                        <i class="fas fa-arrow-right text-slate-200 group-hover:text-blue-400 transition-colors"></i>
                    </div>
                    <h3 class="text-lg font-extrabold text-slate-800 mb-4">${comp.companyName}</h3>
                    <div class="grid grid-cols-3 gap-2">
                        <div class="bg-slate-50 rounded-lg p-2 text-center">
                            <div class="text-[10px] font-black text-slate-400 uppercase">Groups</div>
                            <div class="text-sm font-bold text-slate-700">${groupCount}</div>
                        </div>
                        <div class="bg-slate-50 rounded-lg p-2 text-center">
                            <div class="text-[10px] font-black text-slate-400 uppercase">Sites</div>
                            <div class="text-sm font-bold text-slate-700">${locCount}</div>
                        </div>
                        <div class="bg-slate-50 rounded-lg p-2 text-center">
                            <div class="text-[10px] font-black text-slate-400 uppercase">Users</div>
                            <div class="text-sm font-bold text-slate-700">${userCount}</div>
                        </div>
                    </div>
                </div>
            `);
            $card.click(() => navigate('company', comp.companyId, comp.companyName));
            $grid.append($card);
        });
        $c.append($grid);
    };

    const renderGroups = ($c) => {
        const comp = hierarchyData.find(c => c.companyId === state.company.id);
        if (!comp) return;

        const $grid = $('<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4"></div>');
        comp.groups.forEach(grp => {
            const locCount = grp.locations.length;
            const userCount = grp.locations.reduce((acc, l) => acc + l.users.length, 0);

            const $card = $(`
                <div class="monitor-card p-4 cursor-pointer hover:border-cyan-300 hover:shadow-lg transition-all group">
                    <div class="flex items-center gap-3 mb-3">
                        <div class="w-10 h-10 rounded-lg bg-cyan-50 flex items-center justify-center text-cyan-500">
                            <i class="fas fa-users-rectangle"></i>
                        </div>
                        <h4 class="font-bold text-slate-700 group-hover:text-cyan-600 transition-colors truncate">${grp.groupName}</h4>
                    </div>
                    <div class="flex gap-4">
                        <div class="flex items-baseline gap-1">
                            <span class="text-xs font-bold text-slate-800">${locCount}</span>
                            <span class="text-[9px] font-black text-slate-400 uppercase">Sites</span>
                        </div>
                        <div class="flex items-baseline gap-1">
                            <span class="text-xs font-bold text-slate-800">${userCount}</span>
                            <span class="text-[9px] font-black text-slate-400 uppercase">Users</span>
                        </div>
                    </div>
                </div>
            `);
            $card.click(() => navigate('group', grp.groupId, grp.groupName));
            $grid.append($card);
        });
        $c.append($grid);
    };

    const renderLocations = ($c) => {
        const comp = hierarchyData.find(c => c.companyId === state.company.id);
        const grp = comp?.groups.find(g => g.groupId === state.group.id);
        if (!grp) return;

        const $grid = $('<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4"></div>');
        grp.locations.forEach(loc => {
            const userCount = loc.users.length;
            const onlineCount = loc.users.filter(u => u.isOnline).length;
            const isCrit = loc.isCritical || false;

            const $card = $(`
                <div class="monitor-card p-4 cursor-pointer hover:border-emerald-300 hover:shadow-lg transition-all group ${isCrit ? 'border-l-4 border-l-red-500' : ''}">
                    <div class="flex items-center gap-3 mb-3">
                        <div class="w-10 h-10 rounded-lg ${isCrit ? 'bg-red-50 text-red-500' : 'bg-emerald-50 text-emerald-500'} flex items-center justify-center">
                            <i class="fas fa-location-dot"></i>
                        </div>
                        <h4 class="font-bold text-slate-700 group-hover:text-emerald-600 transition-colors truncate">${loc.locationName}</h4>
                    </div>
                    <div class="flex items-baseline gap-1">
                        <span class="text-xs font-bold text-slate-800">${onlineCount}/${userCount}</span>
                        <span class="text-[9px] font-black text-slate-400 uppercase">Online Users</span>
                    </div>
                </div>
            `);
            $card.click(() => navigate('location', loc.locationId, loc.locationName));
            $grid.append($card);
        });
        $c.append($grid);
    };

    const renderUsers = ($c) => {
        const comp = hierarchyData.find(c => c.companyId === state.company.id);
        const grp = comp?.groups.find(g => g.groupId === state.group.id);
        const loc = grp?.locations.find(l => l.locationId === state.location.id);
        if (!loc) return;

        const $table = $(`
            <div class="monitor-card overflow-hidden">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-100 flex justify-between items-center">
                    <span class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Active Users in ${loc.locationName}</span>
                    <span class="bg-blue-500 text-white text-[9px] font-bold px-2 py-0.5 rounded-full">${loc.users.filter(u => u.isOnline).length} ONLINE</span>
                </div>
                <table class="w-full text-left table-compact">
                    <thead>
                        <tr>
                            <th>User Details</th>
                            <th>OS Status</th>
                            <th>Identity</th>
                            <th class="text-right">Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${loc.users.map(u => `
                            <tr class="hover:bg-slate-50 transition-colors">
                                <td>
                                    <div class="flex items-center gap-3">
                                        <div class="relative w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-400 font-bold text-xs">
                                            ${u.userName ? u.userName[0].toUpperCase() : 'U'}
                                            ${u.isOnline ? '<div class="absolute bottom-0 right-0 w-2.5 h-2.5 bg-emerald-500 border-2 border-white rounded-full"></div>' : '<div class="absolute bottom-0 right-0 w-2.5 h-2.5 bg-slate-300 border-2 border-white rounded-full"></div>'}
                                        </div>
                                        <div>
                                            <div class="font-bold text-slate-700">${u.userName || 'Unknown'}</div>
                                            <div class="text-[9px] text-slate-400 font-mono">${u.ipAddress || '0.0.0.0'}</div>
                                        </div>
                                    </div>
                                </td>
                                <td>
                                    <span class="px-2 py-1 rounded text-[9px] font-bold ${u.osLicenseStatus === 'Licensed' ? 'bg-emerald-50 text-emerald-600' : 'bg-slate-100 text-slate-500'}">
                                        ${(u.osLicenseStatus || 'PENDING').toUpperCase()}
                                    </span>
                                </td>
                                <td class="text-slate-500 font-medium text-xs">${u.domainName || '--'}</td>
                                <td class="text-right">
                                    <a href="/ComputerSummary/Deshboad?comId=${state.company.id}&companyName=${encodeURIComponent(state.company.name)}&groupid=${state.group.id}&groupName=${encodeURIComponent(state.group.name)}&locationId=${state.location.id}&locationName=${encodeURIComponent(state.location.name)}" 
                                       class="text-blue-500 hover:text-blue-700 text-xs font-bold uppercase transition">
                                        Monitor <i class="fas fa-chevron-right ml-1"></i>
                                    </a>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `);
        $c.append($table);
    };

    const renderSearchResults = ($c) => {
        const results = { users: [], locations: [], groups: [], companies: [] };
        const term = state.searchTerm;

        hierarchyData.forEach(comp => {
            if (comp.companyName.toLowerCase().includes(term)) results.companies.push(comp);

            comp.groups.forEach(grp => {
                if (grp.groupName.toLowerCase().includes(term)) results.groups.push({ comp, grp });

                grp.locations.forEach(loc => {
                    if (loc.locationName.toLowerCase().includes(term)) results.locations.push({ comp, grp, loc });

                    loc.users.forEach(user => {
                        if (
                            (user.userName?.toLowerCase().includes(term)) ||
                            (user.domainName?.toLowerCase().includes(term)) ||
                            (user.ipAddress?.toLowerCase().includes(term))
                        ) {
                            results.users.push({ comp, grp, loc, user });
                        }
                    });
                });
            });
        });

        const totalResults = results.users.length + results.locations.length + results.groups.length + results.companies.length;

        if (totalResults === 0) {
            $c.append(`
                <div class="monitor-card py-20 text-center animate-fade-in">
                    <div class="w-20 h-20 bg-slate-50 rounded-full flex items-center justify-center mx-auto mb-6">
                        <i class="fas fa-search text-slate-200 text-4xl"></i>
                    </div>
                    <h3 class="text-slate-900 font-bold text-xl mb-2">No matches found</h3>
                    <p class="text-slate-500 max-w-xs mx-auto">We couldn't find anything matching "${term}". Try searching for a user name, IP address, or site name.</p>
                </div>
            `);
            return;
        }

        $c.prepend(`
            <div class="mb-6 flex justify-between items-center animate-fade-in">
                <div>
                    <h3 class="text-lg font-bold text-slate-800">Search Results</h3>
                    <p class="text-xs text-slate-500">Found ${totalResults} matching entries for "${term}"</p>
                </div>
                <button onclick="$('#globalSearchInput').val('').trigger('input')" class="text-xs font-bold text-blue-600 hover:text-blue-800 uppercase tracking-wider">Clear Search</button>
            </div>
        `);

        const $list = $('<div class="flex flex-col gap-4 animate-fade-in"></div>');

        // Render User Results
        results.users.forEach(res => {
            const $item = $(`
                <div class="monitor-card p-4 hover:border-blue-400 cursor-pointer transition-all flex items-center gap-4 group">
                    <div class="w-12 h-12 rounded-xl bg-blue-50 text-blue-500 flex items-center justify-center group-hover:scale-110 transition-transform">
                        <i class="fas fa-user"></i>
                    </div>
                    <div class="flex-1">
                        <div class="flex items-center gap-2 mb-1">
                            <span class="font-bold text-slate-800 text-sm group-hover:text-blue-700 transition-colors">${res.user.userName}</span>
                            <span class="px-2 py-0.5 bg-slate-100 rounded text-[9px] font-mono text-slate-500 uppercase">${res.user.domainName}</span>
                        </div>
                        <div class="text-[10px] text-slate-400 flex items-center gap-2">
                             <span class="font-medium text-slate-500">${res.comp.companyName}</span>
                             <i class="fas fa-chevron-right text-[8px] opacity-30"></i>
                             <span>${res.grp.groupName}</span>
                             <i class="fas fa-chevron-right text-[8px] opacity-30"></i>
                             <span class="text-blue-500/70 font-semibold">${res.loc.locationName}</span>
                        </div>
                    </div>
                </div>
            `);
            $item.click(() => {
                $('#globalSearchInput').val('');
                state.searchTerm = '';
                state.company = { id: res.comp.companyId, name: res.comp.companyName };
                state.group = { id: res.grp.groupId, name: res.grp.groupName };
                state.location = { id: res.loc.locationId, name: res.loc.locationName };
                render();
            });
            $list.append($item);
        });

        // Render Location Results
        results.locations.forEach(res => {
            const $item = $(`
                <div class="monitor-card p-4 hover:border-emerald-400 cursor-pointer transition-all flex items-center gap-4 group bg-emerald-50/10">
                    <div class="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-500 flex items-center justify-center group-hover:scale-110 transition-transform">
                        <i class="fas fa-location-dot"></i>
                    </div>
                    <div class="flex-1">
                        <div class="flex items-center gap-2 mb-1">
                            <span class="font-bold text-slate-800 text-sm group-hover:text-emerald-700 transition-colors">${res.loc.locationName} (SITE)</span>
                        </div>
                        <div class="text-[10px] text-slate-400 flex items-center gap-2">
                             <span class="font-medium text-slate-500">${res.comp.companyName}</span>
                             <i class="fas fa-chevron-right text-[8px] opacity-30"></i>
                             <span>${res.grp.groupName}</span>
                        </div>
                    </div>
                </div>
            `);
            $item.click(() => {
                $('#globalSearchInput').val('');
                state.searchTerm = '';
                state.company = { id: res.comp.companyId, name: res.comp.companyName };
                state.group = { id: res.grp.groupId, name: res.grp.groupName };
                state.location = { id: res.loc.locationId, name: res.loc.locationName };
                render();
            });
            $list.append($item);
        });

        $c.append($list);
    };

    const showError = (msg) => {
        $('#hierarchyContainer').html(`<div class="bg-red-50 text-red-600 p-4 rounded-xl border border-red-100 flex items-center gap-3">
            <i class="fas fa-exclamation-circle"></i>
            <span class="text-sm font-medium">${msg}</span>
        </div>`);
    };

    return { init };
})();

$(document).ready(() => DashboardHierarchy.init());
