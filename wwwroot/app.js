const BASE = '';

let columns = [];

async function loadSchema() {
    const resp = await fetch(`${BASE}/api/schema`);
    if (!resp.ok) throw new Error('Não foi possível carregar o schema.');
    const data = await resp.json();
    columns = data.columns || [];
    renderSelectColumns(columns);
    renderWhereColumns(columns);
    renderOrderByColumns(columns);
}

function renderSelectColumns(cols) {
    const container = document.getElementById('select-columns');
    const allCb = document.getElementById('select-all');
    container.innerHTML = '';
    allCb.addEventListener('change', () => {
        if (allCb.checked) {
            for (const c of container.querySelectorAll('input')) c.checked = false;
        }
        updateAllState();
    });
    for (const col of cols) {
        const id = `select-${col.name}`;
        const label = document.createElement('label');
        const cb = document.createElement('input');
        cb.type = 'checkbox';
        cb.id = id;
        cb.value = col.name;
        cb.addEventListener('change', () => {
            if (cb.checked) allCb.checked = false;
            updateAllState();
        });
        label.appendChild(cb);
        label.appendChild(document.createTextNode(` ${col.name} (${col.type})`));
        container.appendChild(label);
        container.appendChild(document.createElement('br'));
    }
}

function updateAllState() {
    const allCb = document.getElementById('select-all');
    const cbs = document.getElementById('select-columns').querySelectorAll('input');
    allCb.checked = cbs.length > 0 && Array.from(cbs).every(c => c.checked);
}

function renderWhereColumns(cols) {
    // populated per row
}

function renderOrderByColumns(cols) {
    // populated per row
}

function columnOptions(selectedValue, includeBlank = true) {
    const select = document.createElement('select');
    select.className = 'column-select';
    if (includeBlank) {
        const opt = document.createElement('option');
        opt.value = '';
        opt.textContent = '–';
        select.appendChild(opt);
    }
    for (const col of columns) {
        const opt = document.createElement('option');
        opt.value = col.name;
        opt.textContent = `${col.name} (${col.type})`;
        if (opt.value === selectedValue) opt.selected = true;
        select.appendChild(opt);
    }
    return select;
}

function operatorOptions(selectedValue) {
    const operators = ['=', '!=', '<', '<=', '>', '>=', 'LIKE', 'NOT LIKE', 'IS NULL', 'IS NOT NULL'];
    const select = document.createElement('select');
    select.className = 'operator-select';
    for (const op of operators) {
        const opt = document.createElement('option');
        opt.value = op;
        opt.textContent = op;
        if (op === selectedValue) opt.selected = true;
        select.appendChild(opt);
    }
    select.addEventListener('change', () => {
        const valueInput = select.closest('.where-row').querySelector('.value-input');
        if (valueInput) {
            const isNull = select.value === 'IS NULL' || select.value === 'IS NOT NULL';
            valueInput.style.display = isNull ? 'none' : 'inline-block';
            if (isNull) valueInput.value = '';
        }
    });
    return select;
}

function conjunctionOptions(selectedValue = 'AND') {
    const select = document.createElement('select');
    select.className = 'conjunction-select';
    for (const conj of ['AND', 'OR']) {
        const opt = document.createElement('option');
        opt.value = conj;
        opt.textContent = conj;
        if (conj === selectedValue) opt.selected = true;
        select.appendChild(opt);
    }
    return select;
}

function directionOptions(selectedValue = 'ASC') {
    const select = document.createElement('select');
    select.className = 'direction-select';
    for (const dir of ['ASC', 'DESC']) {
        const opt = document.createElement('option');
        opt.value = dir;
        opt.textContent = dir;
        if (dir === selectedValue) opt.selected = true;
        select.appendChild(opt);
    }
    return select;
}

function addWhereRow(column = '', operator = '=', value = '', conjunction = 'AND') {
    const container = document.getElementById('where-rows');
    const row = document.createElement('div');
    row.className = 'where-row';

    const colSel = columnOptions(column);
    const opSel = operatorOptions(operator);
    const valInput = document.createElement('input');
    valInput.type = 'text';
    valInput.className = 'value-input';
    valInput.value = value;
    valInput.placeholder = 'valor';
    const isNull = operator === 'IS NULL' || operator === 'IS NOT NULL';
    if (isNull) valInput.style.display = 'none';

    const conjSel = conjunctionOptions(conjunction);
    const rmBtn = document.createElement('button');
    rmBtn.type = 'button';
    rmBtn.textContent = 'Remover';
    rmBtn.className = 'remove';
    rmBtn.addEventListener('click', () => row.remove());

    row.appendChild(colSel);
    row.appendChild(opSel);
    row.appendChild(valInput);
    row.appendChild(conjSel);
    row.appendChild(rmBtn);
    container.appendChild(row);

    opSel.dispatchEvent(new Event('change'));
    updateConjunctionVisibility();
}

function addOrderByRow(column = '', direction = 'ASC') {
    const container = document.getElementById('orderby-rows');
    const row = document.createElement('div');
    row.className = 'orderby-row';

    const colSel = columnOptions(column);
    const dirSel = directionOptions(direction);
    const rmBtn = document.createElement('button');
    rmBtn.type = 'button';
    rmBtn.textContent = 'Remover';
    rmBtn.className = 'remove';
    rmBtn.addEventListener('click', () => row.remove());

    row.appendChild(colSel);
    row.appendChild(dirSel);
    row.appendChild(rmBtn);
    container.appendChild(row);
}

function updateConjunctionVisibility() {
    const rows = document.getElementById('where-rows').querySelectorAll('.where-row');
    rows.forEach((row, i) => {
        const conj = row.querySelector('.conjunction-select');
        if (conj) conj.style.display = i === 0 ? 'none' : 'inline-block';
    });
}

function buildRequest() {
    const allCb = document.getElementById('select-all');
    const selectCbs = document.getElementById('select-columns').querySelectorAll('input');
    let select = [];
    if (!allCb.checked) {
        select = Array.from(selectCbs)
            .filter(cb => cb.checked)
            .map(cb => cb.value);
    }

    const where = [];
    const whereRows = document.getElementById('where-rows').querySelectorAll('.where-row');
    whereRows.forEach((row, i) => {
        const column = row.querySelector('.column-select').value;
        if (column === '') return;
        where.push({
            column,
            operator: row.querySelector('.operator-select').value,
            value: row.querySelector('.value-input').value,
            conjunction: row.querySelector('.conjunction-select').value
        });
    });

    const orderBy = [];
    const orderRows = document.getElementById('orderby-rows').querySelectorAll('.orderby-row');
    orderRows.forEach((row) => {
        orderBy.push({
            column: row.querySelector('.column-select').value,
            direction: row.querySelector('.direction-select').value
        });
    });

    const limit = parseInt(document.getElementById('limit').value, 10) || 100;

    return { select, where, orderBy, limit };
}

function showError(msg) {
    const section = document.getElementById('error-section');
    const p = document.getElementById('error-message');
    p.textContent = msg;
    section.classList.remove('hidden');
    document.getElementById('results-section').classList.add('hidden');
    document.getElementById('sql-section').classList.add('hidden');
}

function renderSql(sql) {
    const section = document.getElementById('sql-section');
    const code = document.getElementById('sql-output').querySelector('code');
    code.textContent = sql;
    section.classList.remove('hidden');
}

function renderResults(rows) {
    const section = document.getElementById('results-section');
    const thead = document.getElementById('results-table').querySelector('thead');
    const tbody = document.getElementById('results-table').querySelector('tbody');
    const count = document.getElementById('row-count');
    thead.innerHTML = '';
    tbody.innerHTML = '';

    if (rows.length === 0) {
        count.textContent = '0';
        section.classList.remove('hidden');
        return;
    }

    const keys = Object.keys(rows[0]);
    const trHead = document.createElement('tr');
    for (const key of keys) {
        const th = document.createElement('th');
        th.textContent = key;
        trHead.appendChild(th);
    }
    thead.appendChild(trHead);

    for (const row of rows) {
        const tr = document.createElement('tr');
        for (const key of keys) {
            const td = document.createElement('td');
            td.textContent = row[key] === null ? 'NULL' : String(row[key]);
            tr.appendChild(td);
        }
        tbody.appendChild(tr);
    }

    count.textContent = String(rows.length);
    section.classList.remove('hidden');
}

document.getElementById('add-where').addEventListener('click', () => addWhereRow());
document.getElementById('add-orderby').addEventListener('click', () => addOrderByRow());

document.getElementById('run').addEventListener('click', async () => {
    document.getElementById('error-section').classList.add('hidden');
    document.getElementById('results-section').classList.add('hidden');
    document.getElementById('sql-section').classList.add('hidden');

    const request = buildRequest();

    try {
        const resp = await fetch(`${BASE}/api/query`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(request)
        });
        const data = await resp.json();
        if (!resp.ok) {
            showError(data.error || 'Erro ao executar a query.');
            return;
        }
        renderSql(data.sql);
        renderResults(data.rows || []);
    } catch (err) {
        showError(err.message || 'Erro inesperado.');
    }
});

loadSchema().then(() => {
    addWhereRow('', '=', '', 'AND');
    addOrderByRow();
}).catch(err => showError(err.message));
