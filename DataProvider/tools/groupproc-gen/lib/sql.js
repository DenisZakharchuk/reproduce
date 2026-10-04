'use strict';

// SQL string helpers: identifier quoting and the two reusable predicates
// (hard-key equality — null-safe when a hard column is nullable — and the soft
// normalization expression).

function stripBrackets(s) {
    return String(s).replace(/^\[/, '').replace(/\]$/, '');
}

function ident(name) {
    return '[' + stripBrackets(name).replace(/\]/g, ']]') + ']';
}

// "dbo.Customer" -> "[dbo].[Customer]"
function qualify(name) {
    return String(name).split('.').map(ident).join('.');
}

// Last segment, unbracketed: "dbo.CustomerGroupCache" -> "CustomerGroupCache"
function shortName(name) {
    const parts = String(name).split('.');
    return stripBrackets(parts[parts.length - 1]);
}

function softNormalizeExpr(col, tableAlias) {
    const ref = (tableAlias ? tableAlias + '.' : '') + ident(col.name);
    if (col.normalize === false) return ref;
    return `NULLIF(LTRIM(RTRIM(${ref})), N'')`;
}

// Per hard column: null-safe equality when nullable, plain equality otherwise.
function hardPredicates(aAlias, bAlias, hardCols) {
    return hardCols
        .map((c) => {
            const a = `${aAlias}.${ident(c.name)}`;
            const b = `${bAlias}.${ident(c.name)}`;
            return c.nullable ? `${a} IS NOT DISTINCT FROM ${b}` : `${a} = ${b}`;
        })
        .join(' AND ');
}

function colList(aliasPrefix, names) {
    const p = aliasPrefix ? aliasPrefix + '.' : '';
    return names.map((n) => p + ident(n)).join(', ');
}

module.exports = {
    ident,
    qualify,
    shortName,
    stripBrackets,
    softNormalizeExpr,
    hardPredicates,
    colList,
};
