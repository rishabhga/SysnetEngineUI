class InteractiveHierarchyExplorer {
    constructor(canvasId, data) {
        this.canvas = document.getElementById(canvasId);
        this.ctx = this.canvas.getContext('2d');
        this.data = data;
        this.selectedCompany = null;
        this.selectedGroup = null;
        this.selectedLocation = null;
        this.nodes = [];
        this.connections = [];
        this.scale = 0.7;
        this.offsetX = 0;
        this.offsetY = 0;
        this.isDragging = false;
        this.dragStartX = 0;
        this.dragStartY = 0;
        this.isAnimating = false;
        this.animationFrame = null;
        this.init();
    }

    init() {
        this.resizeCanvas();
        this.setupEventListeners();
        window.addEventListener('resize', () => {
            this.resizeCanvas();
            if (this.selectedCompany) {
                this.generateGraph();
            }
        });
    }

    resizeCanvas() {
        const rect = this.canvas.parentElement.getBoundingClientRect();
        this.canvas.width = rect.width;
        this.canvas.height = rect.height;
    }

    getProp(obj, propName) {
        if (!obj) return null;
        if (obj[propName] !== undefined) return obj[propName];
        const pascalCase = propName.charAt(0).toUpperCase() + propName.slice(1);
        if (obj[pascalCase] !== undefined) return obj[pascalCase];
        const camelCase = propName.charAt(0).toLowerCase() + propName.slice(1);
        return obj[camelCase];
    }

    selectCompany(companyId) {
        this.selectedCompany = this.data.find(c => this.getProp(c, 'companyId') == companyId);
        if (!this.selectedCompany) return;
        this.selectedGroup = null;
        this.selectedLocation = null;
        document.getElementById('instructions')?.classList.add('hidden');
        document.getElementById('breadcrumb')?.classList.remove('hidden');
        this.generateGraph();
        this.updateBreadcrumb();
    }

    generateGraph() {
        this.nodes = [];
        this.connections = [];
        if (!this.selectedCompany) return;

        const companyName = this.getProp(this.selectedCompany, 'companyName') || 'Company';
        const groups = this.getProp(this.selectedCompany, 'groups') || [];

        const companyNode = {
            id: `company-${this.getProp(this.selectedCompany, 'companyId')}`,
            type: 'company',
            label: companyName,
            data: this.selectedCompany,
            x: 200,
            y: 400,
            targetX: 200,
            targetY: 400,
            width: 280,
            height: 140,
            color: '#556270'
        };
        this.nodes.push(companyNode);

        if (!this.selectedGroup) {
            const groupStartY = 100;
            const groupSpacing = 200;
            groups.forEach((group, gIndex) => {
                const groupNode = {
                    id: `group-${this.getProp(group, 'groupId')}`,
                    type: 'group',
                    label: this.getProp(group, 'groupName') || 'Group',
                    data: group,
                    x: 650,
                    y: groupStartY + (gIndex * groupSpacing),
                    targetX: 650,
                    targetY: groupStartY + (gIndex * groupSpacing),
                    width: 240,
                    height: 120,
                    color: '#4ECDC4',
                    clickable: true,
                    expanded: false
                };
                this.nodes.push(groupNode);
                this.connections.push({
                    from: companyNode.id,
                    to: groupNode.id,
                    fromNode: companyNode,
                    toNode: groupNode
                });
            });
        } else {
            const groupNode = {
                id: `group-${this.getProp(this.selectedGroup, 'groupId')}`,
                type: 'group',
                label: this.getProp(this.selectedGroup, 'groupName') || 'Group',
                data: this.selectedGroup,
                x: 650,
                y: 400,
                targetX: 650,
                targetY: 400,
                width: 240,
                height: 120,
                color: '#4ECDC4',
                clickable: true,
                expanded: true
            };
            this.nodes.push(groupNode);
            this.connections.push({
                from: companyNode.id,
                to: groupNode.id,
                fromNode: companyNode,
                toNode: groupNode
            });

            const locations = this.getProp(this.selectedGroup, 'locations') || [];
            if (!this.selectedLocation) {
                const locStartY = 100;
                const locSpacing = 180;
                locations.slice(0, 10).forEach((location, lIndex) => {
                    const locationNode = {
                        id: `location-${this.getProp(location, 'locationId')}`,
                        type: 'location',
                        label: this.getProp(location, 'locationName') || 'Location',
                        data: location,
                        x: 1100,
                        y: locStartY + (lIndex * locSpacing),
                        targetX: 1100,
                        targetY: locStartY + (lIndex * locSpacing),
                        width: 220,
                        height: 110,
                        color: '#FF6B6B',
                        clickable: true,
                        expanded: false
                    };
                    this.nodes.push(locationNode);
                    this.connections.push({
                        from: groupNode.id,
                        to: locationNode.id,
                        fromNode: groupNode,
                        toNode: locationNode
                    });
                });
            } else {
                const locationNode = {
                    id: `location-${this.getProp(this.selectedLocation, 'locationId')}`,
                    type: 'location',
                    label: this.getProp(this.selectedLocation, 'locationName') || 'Location',
                    data: this.selectedLocation,
                    x: 1100,
                    y: 400,
                    targetX: 1100,
                    targetY: 400,
                    width: 220,
                    height: 110,
                    color: '#FF6B6B',
                    clickable: true,
                    expanded: true
                };
                this.nodes.push(locationNode);
                this.connections.push({
                    from: groupNode.id,
                    to: locationNode.id,
                    fromNode: groupNode,
                    toNode: locationNode
                });
                this.createUserLists(this.selectedLocation, locationNode);
            }
        }
        this.startAnimation();
    }

    createUserLists(location, locationNode) {
        const users = this.getProp(location, 'users') || [];
        const activeUsers = users.filter(u => {
            const status = this.getProp(u, 'osLicenseStatus') || '';
            return status.toLowerCase() === 'active';
        });
        const inactiveUsers = users.filter(u => {
            const status = this.getProp(u, 'osLicenseStatus') || '';
            return status.toLowerCase() !== 'active';
        });

        const listWidth = 300;
        const listX = 1550;

        if (activeUsers.length > 0) {
            const activeListNode = {
                id: `userlist-active-${locationNode.id}`,
                type: 'userList',
                label: `✓ Active Users (${activeUsers.length})`,
                users: activeUsers,
                x: listX,
                y: 250,
                targetX: listX,
                targetY: 250,
                width: listWidth,
                height: Math.min(550, 60 + Math.min(activeUsers.length, 18) * 26),
                color: '#27AE60',
                status: 'active'
            };
            this.nodes.push(activeListNode);
            this.connections.push({
                from: locationNode.id,
                to: activeListNode.id,
                fromNode: locationNode,
                toNode: activeListNode
            });
        }

        if (inactiveUsers.length > 0) {
            const inactiveListNode = {
                id: `userlist-inactive-${locationNode.id}`,
                type: 'userList',
                label: `✗ Inactive Users (${inactiveUsers.length})`,
                users: inactiveUsers,
                x: listX,
                y: activeUsers.length > 0 ? 250 + Math.min(550, 60 + Math.min(activeUsers.length, 18) * 26) + 50 : 250,
                targetX: listX,
                targetY: activeUsers.length > 0 ? 250 + Math.min(550, 60 + Math.min(activeUsers.length, 18) * 26) + 50 : 250,
                width: listWidth,
                height: Math.min(550, 60 + Math.min(inactiveUsers.length, 18) * 26),
                color: '#E74C3C',
                status: 'inactive'
            };
            this.nodes.push(inactiveListNode);
            this.connections.push({
                from: locationNode.id,
                to: inactiveListNode.id,
                fromNode: locationNode,
                toNode: inactiveListNode
            });
        }
    }

    startAnimation() {
        this.isAnimating = true;
        this.animate();
    }

    animate() {
        let hasMovement = false;
        const speed = 0.18;
        this.nodes.forEach(node => {
            const dx = node.targetX - node.x;
            const dy = node.targetY - node.y;
            const distance = Math.sqrt(dx * dx + dy * dy);
            if (distance > 1) {
                node.x += dx * speed;
                node.y += dy * speed;
                hasMovement = true;
            } else {
                node.x = node.targetX;
                node.y = node.targetY;
            }
        });

        this.connections.forEach(conn => {
            const fromNode = this.nodes.find(n => n.id === conn.from);
            const toNode = this.nodes.find(n => n.id === conn.to);
            if (fromNode && toNode) {
                conn.fromNode = fromNode;
                conn.toNode = toNode;
            }
        });

        this.render();
        if (hasMovement) {
            this.animationFrame = requestAnimationFrame(() => this.animate());
        } else {
            this.isAnimating = false;
            this.centerView();
        }
    }

    render() {
        this.ctx.save();
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        this.ctx.translate(this.offsetX, this.offsetY);
        this.ctx.scale(this.scale, this.scale);
        this.ctx.globalAlpha = 0.8;
        this.connections.forEach(conn => {
            if (conn.fromNode && conn.toNode) {
                this.drawConnection(conn.fromNode, conn.toNode);
            }
        });
        this.ctx.globalAlpha = 1.0;
        this.nodes.forEach(node => {
            if (node.type === 'userList') {
                this.drawUserList(node);
            } else {
                this.drawNode(node);
            }
        });
        this.ctx.restore();
    }

    drawConnection(from, to) {
        const fromX = from.x;
        const fromY = from.y;
        const toX = to.x;
        const toY = to.y;
        this.ctx.beginPath();
        this.ctx.moveTo(fromX, fromY);
        const controlX = (fromX + toX) / 2;
        const controlY = (fromY + toY) / 2;
        this.ctx.quadraticCurveTo(controlX, controlY, toX, toY);
        this.ctx.strokeStyle = '#7A8896';
        this.ctx.lineWidth = 3;
        this.ctx.lineCap = 'round';
        this.ctx.stroke();
        const angle = Math.atan2(toY - controlY, toX - controlX);
        const arrowSize = 12;
        this.ctx.save();
        this.ctx.translate(toX, toY);
        this.ctx.rotate(angle);
        this.ctx.beginPath();
        this.ctx.moveTo(0, 0);
        this.ctx.lineTo(-arrowSize, -arrowSize / 2);
        this.ctx.lineTo(-arrowSize, arrowSize / 2);
        this.ctx.closePath();
        this.ctx.fillStyle = '#7A8896';
        this.ctx.fill();
        this.ctx.restore();
    }

    drawNode(node) {
        const x = node.x - node.width / 2;
        const y = node.y - node.height / 2;
        this.ctx.shadowColor = 'rgba(0, 0, 0, 0.2)';
        this.ctx.shadowBlur = node.expanded ? 20 : 12;
        this.ctx.shadowOffsetY = node.expanded ? 6 : 3;
        this.ctx.fillStyle = '#F5F7FA';
        this.roundRect(x, y, node.width, node.height, 8);
        this.ctx.fill();
        this.ctx.strokeStyle = node.color;
        this.ctx.lineWidth = node.expanded ? 4 : 3;
        this.ctx.stroke();
        this.ctx.shadowColor = 'transparent';
        const headerHeight = 40;
        this.ctx.fillStyle = node.color;
        this.roundRect(x, y, node.width, headerHeight, 8, true, true, false, false);
        this.ctx.fill();
        this.ctx.fillStyle = 'rgba(255, 255, 255, 0.95)';
        this.ctx.font = 'bold 11px Arial';
        this.ctx.textAlign = 'left';
        this.ctx.textBaseline = 'middle';
        this.ctx.fillText(node.type.toUpperCase(), x + 12, y + headerHeight / 2);
        if (node.clickable) {
            const indicator = node.expanded ? '←' : '→';
            this.ctx.fillStyle = 'rgba(255, 255, 255, 0.95)';
            this.ctx.font = 'bold 18px Arial';
            this.ctx.textAlign = 'right';
            this.ctx.fillText(indicator, x + node.width - 12, y + headerHeight / 2);
        }
        this.ctx.fillStyle = '#2C3E50';
        this.ctx.font = 'bold 15px Arial';
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        let displayLabel = node.label;
        const maxWidth = node.width - 24;
        if (this.ctx.measureText(displayLabel).width > maxWidth) {
            while (this.ctx.measureText(displayLabel + '...').width > maxWidth && displayLabel.length > 0) {
                displayLabel = displayLabel.slice(0, -1);
            }
            displayLabel += '...';
        }
        this.ctx.fillText(displayLabel, node.x, y + headerHeight + (node.height - headerHeight) / 2);
    }

    drawUserList(node) {
        const x = node.x - node.width / 2;
        const y = node.y - node.height / 2;
        this.ctx.shadowColor = 'rgba(0, 0, 0, 0.25)';
        this.ctx.shadowBlur = 15;
        this.ctx.shadowOffsetY = 5;
        this.ctx.fillStyle = '#FFFFFF';
        this.roundRect(x, y, node.width, node.height, 8);
        this.ctx.fill();
        this.ctx.strokeStyle = node.color;
        this.ctx.lineWidth = 3;
        this.ctx.stroke();
        this.ctx.shadowColor = 'transparent';
        const headerHeight = 45;
        this.ctx.fillStyle = node.color;
        this.roundRect(x, y, node.width, headerHeight, 8, true, true, false, false);
        this.ctx.fill();
        this.ctx.fillStyle = '#FFFFFF';
        this.ctx.font = 'bold 14px Arial';
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        this.ctx.fillText(node.label, node.x, y + headerHeight / 2);
        const startY = y + headerHeight + 12;
        const lineHeight = 24;
        const maxVisibleUsers = Math.floor((node.height - headerHeight - 24) / lineHeight);
        this.ctx.font = '12px Arial';
        this.ctx.fillStyle = '#34495E';
        this.ctx.textAlign = 'left';
        node.users.slice(0, maxVisibleUsers).forEach((user, index) => {
            const userName = this.getProp(user, 'userName') || this.getProp(user, 'UserName') || 'Unknown User';
            const itemY = startY + (index * lineHeight);
            this.ctx.fillStyle = node.color;
            this.ctx.beginPath();
            this.ctx.arc(x + 15, itemY + 6, 4, 0, Math.PI * 2);
            this.ctx.fill();
            this.ctx.fillStyle = '#34495E';
            let displayName = userName;
            const maxNameWidth = node.width - 40;
            if (this.ctx.measureText(displayName).width > maxNameWidth) {
                while (this.ctx.measureText(displayName + '...').width > maxNameWidth && displayName.length > 0) {
                    displayName = displayName.slice(0, -1);
                }
                displayName += '...';
            }
            this.ctx.fillText(displayName, x + 28, itemY + 6);
        });
        if (node.users.length > maxVisibleUsers) {
            const remainingCount = node.users.length - maxVisibleUsers;
            this.ctx.fillStyle = node.color;
            this.ctx.font = 'bold 12px Arial';
            this.ctx.textAlign = 'center';
            this.ctx.fillText(`+${remainingCount} more`, node.x, y + node.height - 15);
        }
    }

    roundRect(x, y, w, h, r, tl = true, tr = true, br = true, bl = true) {
        this.ctx.beginPath();
        this.ctx.moveTo(x + (tl ? r : 0), y);
        this.ctx.lineTo(x + w - (tr ? r : 0), y);
        if (tr) this.ctx.arcTo(x + w, y, x + w, y + r, r);
        this.ctx.lineTo(x + w, y + h - (br ? r : 0));
        if (br) this.ctx.arcTo(x + w, y + h, x + w - r, y + h, r);
        this.ctx.lineTo(x + (bl ? r : 0), y + h);
        if (bl) this.ctx.arcTo(x, y + h, x, y + h - r, r);
        this.ctx.lineTo(x, y + (tl ? r : 0));
        if (tl) this.ctx.arcTo(x, y, x + r, y, r);
        this.ctx.closePath();
    }

    centerView() {
        if (this.nodes.length === 0) return;
        let minX = Infinity, maxX = -Infinity;
        let minY = Infinity, maxY = -Infinity;
        this.nodes.forEach(node => {
            minX = Math.min(minX, node.x - node.width / 2);
            maxX = Math.max(maxX, node.x + node.width / 2);
            minY = Math.min(minY, node.y - node.height / 2);
            maxY = Math.max(maxY, node.y + node.height / 2);
        });
        const contentWidth = maxX - minX + 200;
        const contentHeight = maxY - minY + 200;
        const scaleX = this.canvas.width / contentWidth;
        const scaleY = this.canvas.height / contentHeight;
        this.scale = Math.min(scaleX, scaleY, 0.9);
        this.offsetX = (this.canvas.width - (minX + maxX) * this.scale) / 2;
        this.offsetY = (this.canvas.height - (minY + maxY) * this.scale) / 2;
        this.render();
    }

    handleClick(e) {
        const rect = this.canvas.getBoundingClientRect();
        const x = (e.clientX - rect.left - this.offsetX) / this.scale;
        const y = (e.clientY - rect.top - this.offsetY) / this.scale;
        const clickedNode = this.nodes.find(node => (
            x >= node.x - node.width / 2 &&
            x <= node.x + node.width / 2 &&
            y >= node.y - node.height / 2 &&
            y <= node.y + node.height / 2 &&
            node.clickable
        ));
        if (!clickedNode) return;
        if (clickedNode.type === 'group') {
            if (this.selectedGroup && this.getProp(this.selectedGroup, 'groupId') === this.getProp(clickedNode.data, 'groupId')) {
                this.selectedGroup = null;
                this.selectedLocation = null;
            } else {
                this.selectedGroup = clickedNode.data;
                this.selectedLocation = null;
            }
            this.generateGraph();
            this.updateBreadcrumb();
        } else if (clickedNode.type === 'location') {
            if (this.selectedLocation && this.getProp(this.selectedLocation, 'locationId') === this.getProp(clickedNode.data, 'locationId')) {
                this.selectedLocation = null;
            } else {
                this.selectedLocation = clickedNode.data;
            }
            this.generateGraph();
            this.updateBreadcrumb();
        }
    }

    updateBreadcrumb() {
        const path = document.getElementById('breadcrumbPath');
        if (!path) return;
        let html = '';
        if (this.selectedCompany) {
            const companyName = this.getProp(this.selectedCompany, 'companyName');
            html += `<span class="text-blue-600 font-semibold">${companyName}</span>`;
        }
        if (this.selectedGroup) {
            const groupName = this.getProp(this.selectedGroup, 'groupName');
            html += `<span class="text-gray-400">→</span>`;
            html += `<span class="text-blue-600 font-semibold">${groupName}</span>`;
        }
        if (this.selectedLocation) {
            const locationName = this.getProp(this.selectedLocation, 'locationName');
            html += `<span class="text-gray-400">→</span>`;
            html += `<span class="text-red-600 font-semibold">${locationName}</span>`;
        }
        path.innerHTML = html;
    }

    reset() {
        this.selectedCompany = null;
        this.selectedGroup = null;
        this.selectedLocation = null;
        this.nodes = [];
        this.connections = [];
        this.scale = 0.7;
        this.offsetX = 0;
        this.offsetY = 0;
        if (this.animationFrame) {
            cancelAnimationFrame(this.animationFrame);
        }
        document.getElementById('companySelector').value = '';
        document.getElementById('instructions')?.classList.remove('hidden');
        document.getElementById('breadcrumb')?.classList.add('hidden');
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    }

    setupEventListeners() {
        this.canvas.addEventListener('click', (e) => {
            if (!this.isDragging) {
                this.handleClick(e);
            }
        });
        this.canvas.addEventListener('mousedown', (e) => {
            this.isDragging = true;
            this.dragStartX = e.clientX - this.offsetX;
            this.dragStartY = e.clientY - this.offsetY;
            this.canvas.style.cursor = 'grabbing';
        });
        this.canvas.addEventListener('mousemove', (e) => {
            if (this.isDragging) {
                this.offsetX = e.clientX - this.dragStartX;
                this.offsetY = e.clientY - this.dragStartY;
                this.render();
            } else {
                const rect = this.canvas.getBoundingClientRect();
                const x = (e.clientX - rect.left - this.offsetX) / this.scale;
                const y = (e.clientY - rect.top - this.offsetY) / this.scale;
                const hoverNode = this.nodes.find(node => (
                    x >= node.x - node.width / 2 &&
                    x <= node.x + node.width / 2 &&
                    y >= node.y - node.height / 2 &&
                    y <= node.y + node.height / 2 &&
                    node.clickable
                ));
                this.canvas.style.cursor = hoverNode ? 'pointer' : 'grab';
            }
        });
        this.canvas.addEventListener('mouseup', () => {
            this.isDragging = false;
            this.canvas.style.cursor = 'grab';
        });
        this.canvas.addEventListener('mouseleave', () => {
            this.isDragging = false;
            this.canvas.style.cursor = 'grab';
        });
        this.canvas.addEventListener('wheel', (e) => {
            e.preventDefault();
            const delta = e.deltaY > 0 ? 0.9 : 1.1;
            const newScale = this.scale * delta;
            if (newScale >= 0.2 && newScale <= 2) {
                const rect = this.canvas.getBoundingClientRect();
                const mouseX = e.clientX - rect.left;
                const mouseY = e.clientY - rect.top;
                this.offsetX = mouseX - (mouseX - this.offsetX) * (newScale / this.scale);
                this.offsetY = mouseY - (mouseY - this.offsetY) * (newScale / this.scale);
                this.scale = newScale;
                this.render();
            }
        });
    }
}

let explorer;
document.addEventListener('DOMContentLoaded', () => {
    if (typeof companiesData !== 'undefined') {
        explorer = new InteractiveHierarchyExplorer('explorerCanvas', companiesData);
        document.getElementById('companySelector').addEventListener('change', (e) => {
            if (e.target.value) {
                explorer.selectCompany(e.target.value);
            }
        });
        document.getElementById('resetBtn')?.addEventListener('click', () => {
            explorer.reset();
        });
    }
});
