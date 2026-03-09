#!/usr/bin/env node
/**
 * twee-to-ink.js
 * Converts NGames Twee files (.twee, SugarCube 2 / Twee 3 format) to Ink.
 *
 * Supported Twee conventions:
 *   :: PassageName          — knot header
 *   scene: key              — emits  # scene: key
 *   mood: value             — emits  # mood: value
 *   music: cue              — emits  # music: cue
 *   audio: cue              — emits  # audio: cue
 *   Name: dialogue          — emits  # speaker: Name  then  dialogue
 *   (stage direction)       — preserved inline at start of a line
 *   [[Text->Target]]        — choice or divert (Twee 3 arrow)
 *   [[Text|Target]]         — choice or divert (SugarCube pipe)
 *   [[Target]]              — plain divert
 *   <<set $var to value>>   — ~ var = value
 *   <<set $var += n>>       — ~ var = var + n
 *   <<if $cond>> … <<endif>>— {cond: … }
 *   * or + prefix           — explicit Ink choice line (passed through)
 */

'use strict';

const fs   = require('fs');
const path = require('path');

// ── Helpers ────────────────────────────────────────────────────────────────

function slugify(name) {
    return name
        .toLowerCase()
        .replace(/['']/g, '')
        .replace(/[^a-z0-9]+/g, '_')
        .replace(/^_+|_+$/g, '');
}

// Splits .twee source into passage objects: { name, rawTags, body }
function parsePassages(src) {
    const passages = [];
    const headerRe = /^::\s*(.+?)(?:\s*\[([^\]]*)\]|\s*\{[^}]*\})?\s*$/;
    let current = null;

    for (const line of src.split('\n')) {
        const m = line.match(headerRe);
        if (m) {
            if (current) passages.push(current);
            current = { name: m[1].trim(), rawTags: m[2] || '', body: [] };
        } else if (current) {
            current.body.push(line);
        }
    }
    if (current) passages.push(current);
    return passages;
}

// Parse inline tag directives at the top of a passage body.
// Lines like "scene: key" or "mood: tense" before blank/dialogue lines.
function extractDirectives(bodyLines) {
    const directives = {};
    const directiveRe = /^(scene|mood|music|audio|flag|achievement)\s*:\s*(.+)$/i;
    let i = 0;
    for (; i < bodyLines.length; i++) {
        const l = bodyLines[i].trim();
        if (l === '') { i++; break; }
        const m = l.match(directiveRe);
        if (m) {
            directives[m[1].toLowerCase()] = m[2].trim();
        } else {
            break;  // first non-directive non-blank → stop
        }
    }
    return { directives, rest: bodyLines.slice(i) };
}

// Convert SugarCube macro to Ink equivalent (best-effort)
function convertMacros(line) {
    // <<set $var to value>>
    line = line.replace(/<<set\s+\$(\w+)\s+to\s+([^>]+)>>/g, (_, v, val) => {
        val = val.trim().replace(/\$(\w+)/g, '$1');
        return `~ ${v} = ${val}`;
    });
    // <<set $var += n>>
    line = line.replace(/<<set\s+\$(\w+)\s*\+=\s*([^>]+)>>/g, (_, v, n) =>
        `~ ${v} = ${v} + ${n.trim()}`);
    line = line.replace(/<<set\s+\$(\w+)\s*-=\s*([^>]+)>>/g, (_, v, n) =>
        `~ ${v} = ${v} - ${n.trim()}`);
    // <<if $cond>> … <<endif>>  →  {cond: ...}  (single-line only — multi-line left for manual)
    line = line.replace(/<<if\s+([^>]+)>>(.*?)<<\/if>>/g, (_, cond, body) => {
        cond = cond.trim().replace(/\$(\w+)/g, '$1');
        return `{${cond}: ${body.trim()}}`;
    });
    // Remove any remaining unrecognised macros with a TODO comment
    line = line.replace(/<<[^>]+>>/g, m => `/* TODO: ${m} */`);
    return line;
}

// Resolve link target to a slug
function linkSlug(target) {
    return slugify(target.trim());
}

// Convert a passage body into Ink lines
function convertBody(bodyLines, defaultSpeaker) {
    const out = [];
    let currentSpeaker = defaultSpeaker || null;
    let pendingLinks = [];  // accumulate [[…]] links for choice block

    const flushLinks = () => {
        if (pendingLinks.length === 0) return;
        if (pendingLinks.length === 1 && pendingLinks[0].text === null) {
            // Plain divert
            out.push(`-> ${pendingLinks[0].target}`);
        } else {
            for (const lk of pendingLinks) {
                const label = lk.text || lk.target;
                out.push(`+ [${label}] -> ${lk.target}`);
            }
        }
        pendingLinks = [];
    };

    // Regexes for Twee 3 and SugarCube link syntax
    const linkRe  = /\[\[([^\]]+)\]\]/g;
    // Speaker line:  "Name: dialogue"  (Name must not contain colons itself)
    const speakerRe = /^([A-Z][A-Za-z\s\-']+?)\s*:\s*(.*)$/;

    for (let rawLine of bodyLines) {
        const line = rawLine.trimEnd();
        const trimmed = line.trim();

        if (trimmed === '') {
            flushLinks();
            out.push('');
            continue;
        }

        // Detect if the line is ALL link tokens (choice / divert line)
        const linkOnlyRe = /^\s*(\[\[[^\]]+\]\]\s*)+$/;
        if (linkOnlyRe.test(trimmed)) {
            flushLinks();
            let m;
            linkRe.lastIndex = 0;
            while ((m = linkRe.exec(trimmed)) !== null) {
                const inner = m[1];
                // Twee 3:  Text->Target  or  ->Target
                let arrowM = inner.match(/^(.*?)->\s*(.+)$/);
                if (arrowM) {
                    const text   = arrowM[1].trim() || null;
                    const target = linkSlug(arrowM[2]);
                    pendingLinks.push({ text, target });
                    continue;
                }
                // SugarCube:  Text|Target
                let pipeM = inner.match(/^(.*?)\|(.+)$/);
                if (pipeM) {
                    pendingLinks.push({ text: pipeM[1].trim() || null, target: linkSlug(pipeM[2]) });
                    continue;
                }
                // Plain link
                pendingLinks.push({ text: null, target: linkSlug(inner) });
            }
            continue;
        }

        flushLinks();

        // Explicit Ink choice passthrough (+ / *)
        if (/^\s*[+*]\s/.test(line)) {
            out.push(line);
            continue;
        }

        // Macros
        if (/<</.test(line)) {
            out.push(convertMacros(line));
            continue;
        }

        // Speaker line detection:  "Name: rest of dialogue"
        const spM = trimmed.match(speakerRe);
        if (spM && !trimmed.startsWith('(')) {
            const speaker = spM[1].trim();
            const rest    = spM[2].trim();
            if (speaker !== currentSpeaker) {
                out.push(`# speaker: ${speaker}`);
                currentSpeaker = speaker;
            }
            // Convert any inline links within the rest
            const converted = convertMacros(rest);
            out.push(converted);
            continue;
        }

        // Inline links embedded in narrative text
        if (/\[\[/.test(line)) {
            const converted = line.replace(linkRe, (_, inner) => {
                let arrowM = inner.match(/^(.*?)->\s*(.+)$/);
                if (arrowM) return arrowM[1].trim() || `-> ${linkSlug(arrowM[2])}`;
                let pipeM = inner.match(/^(.*?)\|(.+)$/);
                if (pipeM) return pipeM[1].trim();
                return inner;
            });
            out.push(convertMacros(converted));
            continue;
        }

        // Ordinary narrative / dialogue line
        out.push(convertMacros(line));
    }

    flushLinks();
    return out;
}

// ── Main convert function ──────────────────────────────────────────────────

function convertTweeToInk(src, episodeId) {
    const passages = parsePassages(src);
    const inkParts = [];

    // Header comment
    inkParts.push(
        `// ─────────────────────────────────────────────────────────────────────────`,
        `// ${episodeId} — Auto-generated from ${episodeId}.twee by NGames pipeline`,
        `// DO NOT EDIT DIRECTLY — edit the .twee source in Writer/ instead`,
        `// ─────────────────────────────────────────────────────────────────────────`,
        ``,
        `INCLUDE ../../Shared/globals.ink`,
        `INCLUDE ../../Shared/functions.ink`,
        ``,
        `EXTERNAL get_player_name()`,
        `EXTERNAL has_completed_episode(episode_id)`,
        ``
    );

    // Collect variable declarations from StoryData / StoryVars passages
    for (const p of passages) {
        if (/^Story(Title|Data|Menu|Settings)$/i.test(p.name)) continue;
        if (/^StoryVar/i.test(p.name)) {
            for (const l of p.body) {
                const m = l.match(/<<set\s+\$(\w+)\s+to\s+([^>]+)>>/);
                if (m) {
                    const val = m[2].trim().replace(/\$(\w+)/g, '$1');
                    inkParts.push(`VAR ${m[1]} = ${val}`);
                }
            }
            inkParts.push('');
        }
    }

    // Convert narrative passages
    let isFirst = true;
    for (const p of passages) {
        // Skip metadata passages
        if (/^Story(Title|Data|Menu|Settings|Vars|Author|Copyright)$/i.test(p.name)) continue;
        if (/^StoryVar/i.test(p.name)) continue;

        const knotName = isFirst && /^start$/i.test(p.name)
            ? slugify(p.name)
            : slugify(p.name);

        const { directives, rest } = extractDirectives(p.body);

        inkParts.push(`=== ${knotName} ===`);

        // Emit tag directives
        for (const [key, val] of Object.entries(directives)) {
            inkParts.push(`# ${key}: ${val}`);
        }

        // Emit rawTags from passage header [scene: x, mood: y]
        if (p.rawTags) {
            for (const tag of p.rawTags.split(',')) {
                const tm = tag.trim().match(/^(\w+)\s*:\s*(.+)$/);
                if (tm) inkParts.push(`# ${tm[1].toLowerCase()}: ${tm[2].trim()}`);
            }
        }

        const bodyLines = convertBody(rest, null);
        inkParts.push(...bodyLines);

        // Ensure knot ends with something navigable
        const lastNonEmpty = [...bodyLines].reverse().find(l => l.trim() !== '');
        if (!lastNonEmpty || (!lastNonEmpty.startsWith('->') && !lastNonEmpty.startsWith('+'))) {
            inkParts.push('-> END');
        }

        inkParts.push('');
        isFirst = false;
    }

    return inkParts.join('\n');
}

// ── CLI entrypoint ─────────────────────────────────────────────────────────

if (require.main === module) {
    const [,, inputPath, outputPath] = process.argv;
    if (!inputPath) {
        console.error('Usage: node twee-to-ink.js <input.twee> [output.ink]');
        process.exit(1);
    }
    const src = fs.readFileSync(inputPath, 'utf8');
    const base = path.basename(inputPath, path.extname(inputPath));
    const ink  = convertTweeToInk(src, base);

    const out = outputPath || inputPath.replace(/\.twee$/, '.ink');
    fs.writeFileSync(out, ink, 'utf8');
    console.log(`Converted: ${inputPath} → ${out}`);
}

module.exports = { convertTweeToInk, slugify };
