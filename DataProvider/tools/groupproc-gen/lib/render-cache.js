'use strict';

// Renders the materialized-cache objects: table (sequential RefId clustered key),
// a refresh proc (recomputes the full grouping, assigns RefId in pagination order),
// and a keyset page proc.

const { ident, qualify, shortName } = require('./sql');
const P = require('./pipeline');

function cacheTable(model) {
    const short = shortName(model.cache.table);
    const cols = [
        `RefId INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_${short} PRIMARY KEY CLUSTERED`,
        ...model.output.map((n) => {
            const c = model.byName.get(n);
            return `${ident(c.name)} ${c.type} ${c.nullable ? 'NULL' : 'NOT NULL'}`;
        }),
        'MemberCount BIGINT NOT NULL',
    ];
    const indexes = model.filters.map(
        (c) =>
            `    CREATE NONCLUSTERED INDEX IX_${short}_${c.name} ON ${qualify(model.cache.table)} (${ident(c.name)}, RefId);`
    );

    return [
        `IF OBJECT_ID('${model.cache.table}', 'U') IS NULL`,
        `BEGIN`,
        `    CREATE TABLE ${qualify(model.cache.table)} (`,
        `        ${cols.join(',\n        ')}`,
        `    );`,
        ...(indexes.length ? ['', ...indexes] : []),
        `END`,
        `GO`,
    ].join('\n');
}

function cacheRefresh(model) {
    const insertCols = [...model.output.map(ident), 'MemberCount'].join(', ');
    const selectCols = [...model.output.map(ident), 'MemberCount'].join(', ');
    const orderBy = model.orderBy.map(ident).join(', ');

    const terminal = [
        `INSERT INTO ${qualify(model.cache.table)} (${insertCols})`,
        `SELECT ${selectCols}`,
        `FROM Grouped`,
        `ORDER BY ${orderBy};`,
    ].join('\n');

    return [
        `CREATE OR ALTER PROCEDURE ${qualify(model.cache.refreshProc)}`,
        `AS`,
        `BEGIN`,
        `    SET NOCOUNT ON;`,
        ``,
        P.indent(P.distinctCreate(model)),
        ``,
        P.indent(P.distinctInsert(model, false)),
        ``,
        P.indent(P.distinctIndex(model)),
        ``,
        `    BEGIN TRANSACTION;`,
        ``,
        `    TRUNCATE TABLE ${qualify(model.cache.table)};`,
        ``,
        P.indent(P.withCtes(model)),
        P.indent(terminal),
        ``,
        `    COMMIT TRANSACTION;`,
        ``,
        `    DROP TABLE #Distinct;`,
        `END`,
        `GO`,
    ].join('\n');
}

function cachePage(model) {
    const params = [
        '@AfterRefId INT = 0',
        '@PageSize INT = 50',
        ...model.filters.map((c) => `@${c.name} ${c.type} = NULL`),
    ];
    const selCols = ['RefId', ...model.output.map(ident), 'MemberCount'].join(', ');
    const filterPreds = model.filters
        .map((c) => `\n      AND (@${c.name} IS NULL OR ${ident(c.name)} = @${c.name})`)
        .join('');

    return [
        `-- Keyset pagination: pass the previous page's last RefId as @AfterRefId (0 for page 1).`,
        `CREATE OR ALTER PROCEDURE ${qualify(model.cache.pageProc)}`,
        `    ${params.join(',\n    ')}`,
        `AS`,
        `BEGIN`,
        `    SET NOCOUNT ON;`,
        ``,
        `    SELECT TOP (@PageSize) ${selCols}`,
        `    FROM ${qualify(model.cache.table)}`,
        `    WHERE RefId > @AfterRefId${filterPreds}`,
        `    ORDER BY RefId;`,
        `END`,
        `GO`,
    ].join('\n');
}

function renderCache(model) {
    return [cacheTable(model), cacheRefresh(model), cachePage(model)].join('\n\n');
}

module.exports = { renderCache };
