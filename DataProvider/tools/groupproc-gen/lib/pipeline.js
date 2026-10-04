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
    const selectExprs = [
        ...model.hard.map((c) => ident(c.name)),
        ...model.soft.map((c) => softNormalizeExpr(c)),
        'COUNT_BIG(*)',
    ];
    const groupExprs = [
        ...model.hard.map((c) => ident(c.name)),
        ...model.soft.map((c) => softNormalizeExpr(c)),
    ];

    let where = '';
    if (withFilter && model.filters.length) {
        const preds = model.filters.map(
            (c) => `(@${c.name} IS NULL OR ${ident(c.name)} = @${c.name})`
        );
        where = `\nWHERE ${preds.join('\n  AND ')}`;
    }

    return [
        `INSERT INTO #Distinct (${insertCols.join(', ')}, Cnt)`,
        `SELECT ${selectExprs.join(', ')}`,
        `FROM ${qualify(model.sourceTable)}${where}`,
        `GROUP BY ${groupExprs.join(', ')};`,
    ].join('\n');
}

function distinctIndex(model) {
    const hardNames = model.hard.map((c) => ident(c.name)).join(', ');
    return `CREATE NONCLUSTERED INDEX IX_Distinct ON #Distinct (${hardNames});`;
}

function optionsCte(model, col) {
    const hardIdents = model.hard.map((h) => ident(h.name)).join(', ');
    const pHard = model.hard.map((h) => `p.${ident(h.name)}`).join(', ');
    return [
        `${col.name}Options AS (`,
        `${IND}SELECT DISTINCT ${hardIdents}, ${ident(col.name)} AS Value`,
        `${IND}FROM #Distinct`,
        `${IND}WHERE ${ident(col.name)} IS NOT NULL`,
        `${IND}UNION ALL`,
        `${IND}SELECT ${pHard}, CAST(NULL AS ${col.type})`,
        `${IND}FROM Partitions AS p`,
        `${IND}WHERE NOT EXISTS (`,
        `${IND}${IND}SELECT 1 FROM #Distinct AS d`,
        `${IND}${IND}WHERE ${hardPredicates('d', 'p', model.hard)} AND d.${ident(col.name)} IS NOT NULL)`,
        `)`,
    ].join('\n');
}

function candidatesCte(model) {
    const sel = [
        ...model.hard.map((h) => `o0.${ident(h.name)}`),
        ...model.soft.map((s, i) => `o${i}.Value AS ${ident(s.name)}`),
    ].join(', ');

    const from = [`${IND}FROM ${model.soft[0].name}Options AS o0`];
    for (let i = 1; i < model.soft.length; i++) {
        from.push(`${IND}JOIN ${model.soft[i].name}Options AS o${i} ON ${hardPredicates(`o${i}`, 'o0', model.hard)}`);
    }

    return [`Candidates AS (`, `${IND}SELECT ${sel}`, ...from, `)`].join('\n');
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
        `Partitions AS (\n${IND}SELECT DISTINCT ${model.hard.map((h) => ident(h.name)).join(', ')} FROM #Distinct\n)`,
        ...model.soft.map((c) => optionsCte(model, c)),
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
