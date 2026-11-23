// From the repository root: node tasks/bake-wing-poses.mjs <client Data folder>
import fs from 'node:fs';
import { paintWing } from './paint-armour-wings.mjs';
import path from 'node:path';
import { gzipSync, gunzipSync } from 'node:zlib';

const folder = process.argv[2];
if (!folder) throw Error('Supply the client Data directory.');
const size = 1024;
const banks = [], pages = [];
let page = Buffer.alloc(size * size), x = 1, y = 1, shelf = 0;
function finishPage() {
    pages.push(gzipSync(page, {level: 6}));
    page = Buffer.alloc(size * size);
    x = y = 1;
    shelf = 0;
}
function pack(records, i, w, h, ox, oy, bgra) {
    if (w + 2 > size || 3 * (h + 2) > size) throw Error('Wing frame exceeds page size.');
    if (x + w >= size) { x = 1; y += shelf; shelf = 0; }
    if (y + 3 * (h + 2) >= size) finishPage();
    const at = 2 + i * 14;
    records.writeInt16LE(ox, at);
    records.writeInt16LE(oy, at + 2);
    [w,h,pages.length,x,y].forEach((v,j) => records.writeUInt16LE(v, at + 4 + j * 2));
    
    for (let row = 0; row < h; row++) for (let col = 0; col < w; col++) {
        const p = 4 * (row * w + col);
        for (let c = 0; c < 3; c++)
            page[(y + row + c * (h + 2)) * size + x + col] = Math.round(bgra[p+c] * bgra[p+3] / 255);
    }
    x += w + 2;
    shelf = Math.max(shelf, 3 * (h + 2));
}
for (const family of ['CHumEffect', 'AHumEffect', 'ARHumEffect']) {
    for (const style of ['00', '01']) {
        const lib = fs.readFileSync(path.join(folder, family, style + '.Lib'));
        const count = lib.readInt32LE(4), table = lib.readInt32LE(0) === 3 ? 12 : 8;
        const records = Buffer.alloc(2 + count * 14);
        records.writeUInt16LE(count);
        for (let i = 0; i < count; i++) {
            const src = lib.readInt32LE(table + i * 4);
            if (!src || lib.readInt32LE(src + 13) <= 0) continue;
            const w = lib.readInt16LE(src), h = lib.readInt16LE(src + 2);
            const bgra = gunzipSync(lib.subarray(src + 17, src + 17 + lib.readInt32LE(src + 13)));
            pack(records, i, w, h, lib.readInt16LE(src + 4), lib.readInt16LE(src + 6), bgra);
        }
        banks.push(records);
    }
}
// Each style owns its animation bank so a loop cannot sample the next design.
const frameCounts = [1,1,1,8,8,1,16,16];
for (const [style,count] of frameCounts.entries()) {
    const records = Buffer.alloc(2 + count * 14);
    records.writeUInt16LE(count);
    for (let frame = 0; frame < count; frame++) {
        const {w,h,x,y,bgra} = paintWing(style, frame/count);
        pack(records, frame, w, h, x, y, bgra);
    }
    banks.push(records);
}
finishPage();
const lengths = Buffer.alloc(2 + pages.length * 4);
lengths.writeUInt16LE(pages.length);
pages.forEach((p,i) => lengths.writeInt32LE(p.length, 2 + 4 * i));
fs.writeFileSync('Client.Rendering.Core/Assets/WingPoses.bin', Buffer.concat([...banks, lengths, ...pages]));
console.log('Packed ' + pages.length + ' native-resolution wing mask pages.');

