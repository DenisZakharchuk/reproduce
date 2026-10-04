'use strict';

// Renders the OFFSET/FETCH grouping proc (CustomersGrouped-style).

const { ident, qualify } = require('./sql');
const P = require('./pipeline');

function headerComment(model) {
    const hard = model.hard.map((c) => c.name).join(', ');
    const soft = model.soft.map((c) => c.name).join(', ');
    const filters = model.filters.map((c) => c.name).join(', ') || '(none)';
    return [
        `-- HARD grouping: ${hard} (exact; SQL NULLs group together).`,
        `-- SOFT grouping: ${soft} (null/empty is a wildcard; distinct non-null values`,
        `--   split into separate groups; a wildcard row fans out and can be counted in`,
        `--   more than one MemberCount).`,
        `-- Optional filter params: ${filters}.`,
    ].join('\n');
}

function renderOffsetProc(model) {
    const params = [
        '@Offset INT',
        '@PageSize INT',
        ...model.filters.map((c) => `@${c.name} ${c.type} = NULL`),
    ];
    const outSel = [...model.output.map(ident), 'MemberCount'].join(', ');
    const orderBy = model.orderBy.map(ident).join(', ');

    const terminal = [
        `SELECT ${outSel}`,
        `FROM Grouped`,
        `ORDER BY ${orderBy}`,
        `OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;`,
    ].join('\n');

    return [
        headerComment(model),
        `CREATE OR ALTER PROCEDURE ${qualify(model.procName)}`,
        `    ${params.join(',\n    ')}`,
        `AS`,
        `BEGIN`,
        `    SET NOCOUNT ON;`,
        ``,
        P.indent(P.distinctCreate(model)),
        ``,
        P.indent(P.distinctInsert(model, true)),
        ``,
        P.indent(P.distinctIndex(model)),
        ``,
        P.indent(P.withCtes(model)),
        P.indent(terminal),
        ``,
        `    DROP TABLE #Distinct;`,
        `END`,
        `GO`,
    ].join('\n');
}

module.exports = { renderOffsetProc };
