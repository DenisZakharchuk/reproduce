'use strict';

// Renders the parts shared by every generated proc: the #Distinct collapse
// (single-scan pre-aggregation) and the WITH ... Grouped CTE chain that does the
// soft wildcard grouping. Consumers append their own terminal SELECT or INSERT.

const { ident, qualify, softNormalizeExpr, hardPredicates } = require('./sql');

const IND = '    ';

function distinctCreate(model) {
    const lines = [
        ...model.hard.map((c) => `${ident(c.name)} ${c.type} ${c.nullable ? 'NULL' : 'NOT NULL'}`),
        ...model.soft.map((c) => `${ident(c.name)} ${c.type} NULL`),
        'Cnt BIGINT NOT NULL',
    ];
    return `CREATE TABLE #Distinct (\n${IND}${lines.join(',\n' + IND)}\n);`;
}

function distinctInsert(model, withFilter) {
    const insertCols = [...model.hard, ...model.soft].map((c) => ident(c.name));
    const splitColumns = model.soft.filter((c) => c.split);
    const splitAliases = new Map(splitColumns.map((c, i) => [c.name, `split${i}`]));
    const sourceExpr = (c) => {
        const splitAlias = splitAliases.get(c.name);
        if (splitAlias) return `${splitAlias}.[Value]`;
        return c.grouping === 'soft' ? softNormalizeExpr(c, 'src') : `src.${ident(c.name)}`;
    };
    const selectExprs = [
        ...model.hard.map(sourceExpr),
        ...model.soft.map(sourceExpr),
        'COUNT_BIG(*)',
    ];
    const groupExprs = [
        ...model.hard.map(sourceExpr),
        ...model.soft.map(sourceExpr),
    ];

    let where = '';
    if (withFilter && model.filters.length) {
        const preds = model.filters.map(
            (c) => `(@${c.name} IS NULL OR src.${ident(c.name)} = @${c.name})`
        );
        where = `\nWHERE ${preds.join('\n  AND ')}`;
    }

    const applies = splitColumns.map((c, i) => {
        const alias = `split${i}`;
        const delimiter = c.split.delimiter.replace(/'/g, "''");
        const token = `NULLIF(LTRIM(RTRIM(tokens.[value])), N'')`;
        return [
            `OUTER APPLY (`,
            `    SELECT DISTINCT ${token} AS [Value]`,
            `    FROM STRING_SPLIT(src.${ident(c.name)}, N'${delimiter}') AS tokens`,
            `    WHERE ${token} IS NOT NULL`,
            `) AS ${alias}`,
        ].join('\n');
    });
    const from = [`FROM ${qualify(model.sourceTable)} AS src`, ...applies].join('\n');

    return [
        `INSERT INTO #Distinct (${insertCols.join(', ')}, Cnt)`,
        `SELECT ${selectExprs.join(', ')}`,
        from + where,
        `GROUP BY ${groupExprs.join(', ')};`,
    ].join('\n');
}

function distinctIndex(model) {
    const hardNames = model.hard.map((c) => ident(c.name)).join(', ');
    return `CREATE NONCLUSTERED INDEX IX_Distinct ON #Distinct (${hardNames});`;
}

function candidatesCte(model) {
    const sel = [...model.hard, ...model.soft].map((c) => `source.${ident(c.name)}`).join(', ');
    const eligible = model.soft.map((c) => [
        `(`,
        `${IND}source.${ident(c.name)} IS NOT NULL`,
        `${IND}OR NOT EXISTS (`,
        `${IND}${IND}SELECT 1 FROM #Distinct AS concrete`,
        `${IND}${IND}WHERE ${hardPredicates('concrete', 'source', model.hard)}`,
        `${IND}${IND}AND concrete.${ident(c.name)} IS NOT NULL)`,
        `)`,
    ].join('\n'));

    return [
        `Candidates AS (`,
        `${IND}SELECT DISTINCT ${sel}`,
        `${IND}FROM #Distinct AS source`,
        `${IND}WHERE ${eligible.join(`\n${IND}AND `)}`,
        `)`,
    ].join('\n');
}

function groupedCte(model) {
    const sel = [
        ...model.hard.map((h) => `c.${ident(h.name)}`),
        ...model.soft.map((s) => `c.${ident(s.name)}`),
        'SUM(d.Cnt) AS MemberCount',
    ].join(', ');

    const compat = model.soft
        .map((s) => `(d.${ident(s.name)} IS NULL OR d.${ident(s.name)} = c.${ident(s.name)})`)
        .join(`\n${IND}${IND}AND `);

    const groupBy = [...model.hard, ...model.soft].map((c) => `c.${ident(c.name)}`).join(', ');

    return [
        `Grouped AS (`,
        `${IND}SELECT ${sel}`,
        `${IND}FROM Candidates AS c`,
        `${IND}JOIN #Distinct AS d`,
        `${IND}${IND}ON ${hardPredicates('d', 'c', model.hard)}`,
        `${IND}${IND}AND ${compat}`,
        `${IND}GROUP BY ${groupBy}`,
        `)`,
    ].join('\n');
}

// Full "WITH Partitions, <col>Options..., Candidates, Grouped" block (no terminal).
function withCtes(model) {
    const parts = [
        candidatesCte(model),
        groupedCte(model),
    ];
    return 'WITH ' + parts.join(',\n');
}

function indent(block, pad = IND) {
    return block
        .split('\n')
        .map((l) => (l.length ? pad + l : l))
        .join('\n');
}

module.exports = {
    IND,
    distinctCreate,
    distinctInsert,
    distinctIndex,
    withCtes,
    indent,
};
