//#!/usr/bin/env node
///* eslint-disable no-undef */
//// Scan project for non-ASCII/control characters and attempt to compile .js and .cshtml <script> contents.
//// Usage: node tools/find-bad-chars.js [dir1 dir2 ...]    (defaults: UI/wwwroot UI/Views)
//const fs = require('fs');
//const path = require('path');
//const vm = require('vm');
//const roots = process.argv.slice(2);
//if (roots.length === 0) {
//    roots.push('UI/wwwroot', 'UI/Views');
//}
//function walk(dir, cb) {
//    if (!fs.existsSync(dir)) return;
//    for (const name of fs.readdirSync(dir)) {
//        const full = path.join(dir, name);
//        const stat = fs.statSync(full);
//        if (stat.isDirectory()) walk(full, cb);
//        else cb(full);
//    }
//}
//function reportNonAscii(file, content) {
//    const lines = content.split(/\r?\n/);
//    let reported = false;
//    for (let i = 0; i < lines.length; i++) {
//        const line = lines[i];
//        for (let j = 0; j < line.length; j++) {
//            const code = line.charCodeAt(j);
//            // allow common whitespace and printable ascii (9,10,13,32..126)
//            if (code !== 9 && code !== 10 && code !== 13 && (code < 32 || code > 126)) {
//                if (!reported) {
//                    console.log(`\n[NON-ASCII] ${file}`);
//                    reported = true;
//                }
//                console.log(`  line ${i + 1}, col ${j + 1}, code=${code} char=${JSON.stringify(line[j])}`);
//            }
//        }
//    }
//    return reported;
//}
//function tryCompileJS(file, js) {
//    try {
//        // compile-only, does not execute
//        new vm.Script(js, { filename: file });
//        return null;
//    } catch (err) {
//        return err;
//    }
//}
//for (const root of roots) {
//    walk(root, (file) => {
//        const ext = path.extname(file).toLowerCase();
//        if (ext === '.js' || ext === '.html' || ext === '.cshtml') {
//            const raw = fs.readFileSync(file, 'utf8');
//            // If cshtml: extract <script> blocks for validation
//            if (ext === '.cshtml' || ext === '.html') {
//                const regex = /<script\b[^>]*>([\s\S]*?)<\/script>/gi;
//                let m;
//                let idx = 0;
//                while ((m = regex.exec(raw)) !== null) {
//                    idx++;
//                    const script = m[1];
//                    const label = `${file} (script#${idx})`;
//                    reportNonAscii(label, script);
//                    const err = tryCompileJS(label, script);
//                    if (err) {
//                        console.error(`\n[SYNTAX ERROR] ${label} => ${err.name}: ${err.message}`);
//                        console.error(err.stack && err.stack.split('\n')[0]);
//                    }
//                }
//            } else {
//                reportNonAscii(file, raw);
//                if (ext === '.js') {
//                    const err = tryCompileJS(file, raw);
//                    if (err) {
//                        console.error(`\n[SYNTAX ERROR] ${file} => ${err.name}: ${err.message}`);
//                        console.error(err.stack && err.stack.split('\n')[0]);
//                    }
//                }
//            }
//        }
//    });
//}
//console.log('\nScan complete.');
//# sourceMappingURL=find-bad-chars.js.map