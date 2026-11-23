// Convert additive legacy poses into three independently tinted particle coverage masks.
// Run from the repository root with node tasks/bake-spell-pose-masks.mjs <legacy Data directory>.
import fs from 'node:fs';
import path from 'node:path';
import { gzipSync, gunzipSync } from 'node:zlib';

const folder = process.argv[2];
if (!folder) throw Error('Supply the original spell library directory.');
const libraries = new Map();
const chunks = [];
const groups = [
    // The three tiered swings use the fully trained legacy artwork.
    ['Slaying', 'Magic', 2090, 8, 10, 6],
    ['Thrusting', 'Magic', 2460, 8, 10, 6],
    ['HalfMoon', 'Magic', 2830, 8, 10, 6],
    ['CrossHalfMoon', 'Magic2', 40, 8, 10, 6],
    ['BladeAvalanche', 'Magic2', 740, 8, 20, 15],
    ['CatTongue', 'Magic3', 260, 16, 10, 6],
    ['FireBall', 'Magic', 10, 16, 10, 6],
    ['GreatFireBall', 'Magic', 410, 16, 10, 6],
    ['FrostCrunch', 'Magic2', 410, 16, 10, 4],
    ['SoulFireBall', 'Magic', 1160, 16, 10, 3],
    ['FlamingSword', 'Magic', 3480, 8, 10, 6],
    ['TwinDrakeBlade', 'Magic2', 220, 8, 20, 6],
    ['TwinDrakeFollow', 'Magic2', 226, 8, 20, 6],
    ['DrakePrepare', 'Magic2', 210, 1, 0, 6],
    ['DrakeStun', 'Magic2', 380, 1, 0, 6],
    ['LionRoar', 'Magic2', 710, 1, 0, 20],
    ['Entrapment', 'Magic2', 990, 1, 0, 10],
    ['Rage', 'Magic2', 1510, 1, 0, 10],
    ['ProtectionField', 'Magic2', 1520, 1, 0, 10],
    ['Fury', 'Magic3', 187, 1, 0, 10],
    ['FuryFlash', 'Magic3', 200, 1, 0, 8],
    ['ImmortalSkin', 'Magic3', 550, 1, 0, 17],
    ['SkinFlash', 'Magic3', 570, 1, 0, 5],
    ['SlashingBurst', 'Magic2', 1700, 8, 10, 9],
    ['CounterAttack', 'Magic', 3480, 8, 10, 10],
    ['CounterFlash', 'Magic3', 140, 1, 0, 2],
    ['SoulShieldWard', 'Magic', 1320, 1, 0, 15],
    ['BlessedArmourWard', 'Magic', 1340, 1, 0, 15],
    ['CurseRune', 'Magic2', 950, 1, 0, 24],
];

function appendPose(data, index, step) {
    const header = Buffer.alloc(9);
    chunks.push(header);
    const pos = data.readInt32LE((data.readInt32LE(0) === 3 ? 12 : 8) + index * 4);
    if (!pos || data.readInt32LE(pos + 13) <= 0) return;
    const width = data.readInt16LE(pos), height = data.readInt16LE(pos + 2);
    const pixels = gunzipSync(data.subarray(pos + 17, pos + 17 + data.readInt32LE(pos + 13)));
    // Preserve thin cuts and paper markings at native resolution. Large diffuse fields
    // can average 2x2 samples without reading beyond an odd-sized source frame.
    const w = Math.ceil(width / step), h = Math.ceil(height / step);
    const planes = Array.from({length: 3}, () => Buffer.alloc(w * h));
    for (let y = 0; y < height; y += step) for (let x = 0; x < width; x += step) {
        const target = (y / step) * w + x / step;
        for (let c = 0; c < 3; c++) {
            let sum = 0;
            for (let j = y; j < Math.min(y + step, height); j++)
                for (let i = x; i < Math.min(x + step, width); i++) {
                    const at = 4 * (j * width + i);
                    sum += pixels[at+c] * pixels[at+3] / 255;
                }
            planes[c][target] = Math.round(sum / (step * step));
        }
    }
    header.writeInt16LE(data.readInt16LE(pos + 4) - 24);
    header.writeInt16LE(data.readInt16LE(pos + 6) - 16, 2);
    header.writeUInt16LE(w, 4);
    header.writeUInt16LE(h, 6);
    header[8] = step;
    chunks.push(...planes);
}

for (const [name, lib, first, directions, stride, frames] of groups) {
    if (!libraries.has(lib)) libraries.set(lib, fs.readFileSync(path.join(folder, lib + '.Lib')));
    const nameBytes = Buffer.from(name);
    chunks.push(Buffer.from([nameBytes.length]), nameBytes, Buffer.from([directions, frames]));
    for (let d = 0; d < directions; d++)
        for (let f = 0; f < frames; f++)
            appendPose(libraries.get(lib), first + d * stride + f, ['BladeAvalanche','CatTongue','FlamingSword'].includes(name) ? 2 : 1);
}
fs.writeFileSync('Client.Rendering.Core/Assets/SpellPoseMasks.gz', gzipSync(Buffer.concat(chunks), {level: 9}));
