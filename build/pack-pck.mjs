#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { pathToFileURL } from 'node:url';

const PCK_MAGIC = Buffer.from('GDPC', 'ascii');
const PCK_VERSION = 3;
const GODOT_MAJOR = 4;
const GODOT_MINOR = 5;
const GODOT_PATCH = 1;
const PACK_REL_FILEBASE = 2;
const HEADER_SIZE = 112;

function walkFiles(dir, base = dir) {
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      out.push(...walkFiles(full, base));
    } else if (entry.isFile()) {
      const rel = path.relative(base, full).replace(/\\/g, '/');
      out.push({ full, rel });
    }
  }
  out.sort((a, b) => a.rel.localeCompare(b.rel));
  return out;
}

export function packDirectory(inputDir, outputPck, options = {}) {
  const prefix = (options.prefix || '').replace(/^\/+|\/+$/g, '');
  const files = walkFiles(inputDir);
  const entries = [];
  const dataChunks = [];
  let payloadCursor = 0;

  for (const f of files) {
    const data = fs.readFileSync(f.full);
    const md5 = crypto.createHash('md5').update(data).digest();
    let pckPath = f.rel;
    if (prefix && !pckPath.startsWith(`${prefix}/`)) {
      pckPath = `${prefix}/${pckPath}`;
    }
    const rawPathBuf = Buffer.from(pckPath, 'utf8');
    const paddedPathLen = Math.ceil((rawPathBuf.length + 1) / 4) * 4;
    const pathBuf = Buffer.alloc(paddedPathLen, 0);
    rawPathBuf.copy(pathBuf, 0);

    const alignedSize = Math.ceil(data.length / 16) * 16;
    const padLen = alignedSize - data.length;

    entries.push({
      pckPath,
      pathBuf,
      relOfs: payloadCursor,
      size: data.length,
      md5,
    });
    dataChunks.push(data);
    if (padLen > 0) dataChunks.push(Buffer.alloc(padLen, 0));
    payloadCursor += alignedSize;
  }

  const dirOffset = HEADER_SIZE + payloadCursor;
  const header = Buffer.alloc(HEADER_SIZE, 0);
  PCK_MAGIC.copy(header, 0);
  header.writeUInt32LE(PCK_VERSION, 4);
  header.writeUInt32LE(GODOT_MAJOR, 8);
  header.writeUInt32LE(GODOT_MINOR, 12);
  header.writeUInt32LE(GODOT_PATCH, 16);
  header.writeUInt32LE(PACK_REL_FILEBASE, 20);
  header.writeBigUInt64LE(BigInt(HEADER_SIZE), 24);
  header.writeBigUInt64LE(BigInt(dirOffset), 32);

  const dirParts = [];
  const countBuf = Buffer.alloc(4);
  countBuf.writeUInt32LE(entries.length, 0);
  dirParts.push(countBuf);

  for (const e of entries) {
    const lenBuf = Buffer.alloc(4);
    lenBuf.writeUInt32LE(e.pathBuf.length, 0);
    const metaBuf = Buffer.alloc(8 + 8 + 16 + 4, 0);
    metaBuf.writeBigUInt64LE(BigInt(e.relOfs), 0);
    metaBuf.writeBigUInt64LE(BigInt(e.size), 8);
    e.md5.copy(metaBuf, 16);
    metaBuf.writeUInt32LE(0, 32);
    dirParts.push(lenBuf, e.pathBuf, metaBuf);
  }

  fs.mkdirSync(path.dirname(outputPck), { recursive: true });
  const finalBuf = Buffer.concat([header, ...dataChunks, ...dirParts]);
  fs.writeFileSync(outputPck, finalBuf);
  return { count: entries.length, totalBytes: finalBuf.length, entries };
}

export function listPck(pckFile) {
  const fd = fs.openSync(pckFile, 'r');
  try {
    const hdr = Buffer.alloc(HEADER_SIZE);
    fs.readSync(fd, hdr, 0, HEADER_SIZE, 0);
    const magic = hdr.toString('ascii', 0, 4);
    if (magic !== 'GDPC') throw new Error(`Invalid PCK magic: ${magic}`);
    const packVersion = hdr.readUInt32LE(4);
    const godot = `${hdr.readUInt32LE(8)}.${hdr.readUInt32LE(12)}.${hdr.readUInt32LE(16)}`;
    const flags = hdr.readUInt32LE(20);
    const fileBase = Number(hdr.readBigUInt64LE(24));
    const dirOffset = Number(hdr.readBigUInt64LE(32));
    const stat = fs.fstatSync(fd);
    const dirBuf = Buffer.alloc(stat.size - dirOffset);
    fs.readSync(fd, dirBuf, 0, dirBuf.length, dirOffset);
    const count = dirBuf.readUInt32LE(0);
    let p = 4;
    const entries = [];
    for (let i = 0; i < count; i++) {
      const slen = dirBuf.readUInt32LE(p);
      p += 4;
      const entryPath = dirBuf.toString('utf8', p, p + slen).replace(/\0+$/, '');
      p += slen;
      const ofs = Number(dirBuf.readBigUInt64LE(p));
      const size = Number(dirBuf.readBigUInt64LE(p + 8));
      p += 16 + 16 + 4;
      entries.push({ path: entryPath, ofs, size });
    }
    return { packVersion, godot, flags, fileBase, dirOffset, entries };
  } finally {
    fs.closeSync(fd);
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  const [, , cmd, arg1, arg2, ...rest] = process.argv;
  if (cmd === 'pack' && arg1 && arg2) {
    const pIdx = rest.indexOf('--prefix');
    const prefix = pIdx >= 0 ? rest[pIdx + 1] : '';
    const res = packDirectory(arg1, arg2, { prefix });
    console.log(`Packed ${res.count} file(s) -> ${arg2} (${res.totalBytes} bytes)`);
  } else if (cmd === 'list' && arg1) {
    const info = listPck(arg1);
    console.log(`GDPC v${info.packVersion} (Godot ${info.godot}), files: ${info.entries.length}`);
    for (const e of info.entries) {
      console.log(`  ${String(e.size).padStart(8)}  ${e.path}`);
    }
  } else {
    console.log('Usage:\n  node pack-pck.mjs pack <inputDir> <outputPck> [--prefix <modId>]\n  node pack-pck.mjs list <pckFile>');
    process.exit(cmd ? 1 : 0);
  }
}
