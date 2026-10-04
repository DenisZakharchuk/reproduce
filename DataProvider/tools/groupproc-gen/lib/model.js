'use strict';

// Parses and validates the JSON data model into a normalized shape the renderers
// consume. Throws Error (with a clear message) on any invalid model.

const VALID_GROUPING = new Set(['hard', 'soft', 'ignore']);

function fail(msg) {
    return new Error(msg);
}

function parseModel(raw) {
    if (!raw || typeof raw !== 'object' || Array.isArray(raw)) {
        throw fail('Model must be a JSON object.');
    }
    if (!raw.sourceTable) throw fail('Model.sourceTable is required.');
    if (!raw.procName) throw fail('Model.procName is required.');
    if (!Array.isArray(raw.columns) || raw.columns.length === 0) {
        throw fail('Model.columns must be a non-empty array.');
    }

    const seen = new Set();
    const columns = raw.columns.map((c, i) => {
        if (!c || !c.name) throw fail(`columns[${i}].name is required.`);
        if (seen.has(c.name)) throw fail(`Duplicate column name '${c.name}'.`);
        seen.add(c.name);
        if (!VALID_GROUPING.has(c.grouping)) {
            throw fail(`columns[${i}] '${c.name}': grouping must be 'hard', 'soft', or 'ignore'.`);
        }
        if (c.grouping !== 'ignore' && !c.type) {
            throw fail(`columns[${i}] '${c.name}': type is required for ${c.grouping} columns.`);
        }
        return {
            name: c.name,
            type: c.type,
            grouping: c.grouping,
            nullable: c.nullable !== undefined ? !!c.nullable : c.grouping === 'soft',
            filter: !!c.filter,
            normalize: c.normalize !== undefined ? !!c.normalize : true,
        };
    });

    const grouping = columns.filter((c) => c.grouping !== 'ignore');
    const hard = columns.filter((c) => c.grouping === 'hard');
    const soft = columns.filter((c) => c.grouping === 'soft');
    if (hard.length === 0) throw fail('At least one hard grouping column is required.');
    if (soft.length === 0) throw fail('At least one soft grouping column is required.');

    const byName = new Map(grouping.map((c) => [c.name, c]));
    const filters = grouping.filter((c) => c.filter);

    const output = raw.output && raw.output.length ? raw.output : grouping.map((c) => c.name);
    output.forEach((n) => {
        if (!byName.has(n)) throw fail(`output column '${n}' is not a grouping column.`);
    });

    const orderBy = raw.orderBy && raw.orderBy.length ? raw.orderBy : grouping.map((c) => c.name);
    orderBy.forEach((n) => {
        if (!byName.has(n)) throw fail(`orderBy column '${n}' is not a grouping column.`);
    });

    let cache = null;
    if (raw.cache && raw.cache.enabled !== false) {
        const { table, refreshProc, pageProc } = raw.cache;
        if (!table || !refreshProc || !pageProc) {
            throw fail("cache requires 'table', 'refreshProc', and 'pageProc'.");
        }
        cache = { table, refreshProc, pageProc };
    }

    return {
        sourceTable: raw.sourceTable,
        procName: raw.procName,
        columns,
        grouping,
        hard,
        soft,
        filters,
        output,
        orderBy,
        cache,
        byName,
    };
}

module.exports = { parseModel };
