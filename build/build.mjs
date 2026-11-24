#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import zlib from 'node:zlib';
import { execFileSync } from 'node:child_process';
import { packDirectory } from './pack-pck.mjs';

const MOD_ROOT = path.resolve(import.meta.dirname, '..');

function resolveSts2Dir() {
  if (process.env.STS2_DIR && fs.existsSync(process.env.STS2_DIR)) {
    return process.env.STS2_DIR;
  }
  const candidates = [
    'C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2',
    'D:/SteamLibrary/steamapps/common/Slay the Spire 2',
    'E:/SteamLibrary/steamapps/common/Slay the Spire 2',
    'E:/steam/steamapps/common/Slay the Spire 2',
  ];
  for (const c of candidates) {
    if (fs.existsSync(path.join(c, 'data_sts2_windows_x86_64'))) return c;
  }
  throw new Error('Slay the Spire 2 directory not found. Set STS2_DIR environment variable.');
}

function resolveCscPath() {
  if (process.env.CSC_PATH && fs.existsSync(process.env.CSC_PATH)) {
    return process.env.CSC_PATH;
  }
  const candidates = [
    'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe',
    'C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/Roslyn/csc.exe',
    'C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/Roslyn/csc.exe',
    'C:/Program Files/Microsoft Visual Studio/2022/Enterprise/MSBuild/Current/Bin/Roslyn/csc.exe',
  ];
  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }
  throw new Error('Roslyn csc.exe not found. Install VS 2022 / BuildTools or set CSC_PATH.');
}

function isManagedDll(filePath) {
  const buf = fs.readFileSync(filePath);
  if (buf.length < 256 || buf[0] !== 0x4d || buf[1] !== 0x5a) return false;
  const peOff = buf.readUInt32LE(0x3c);
  if (peOff + 24 >= buf.length || buf.toString('ascii', peOff, peOff + 4) !== 'PE\0\0') return false;
  const optOff = peOff + 24;
  const magic = buf.readUInt16LE(optOff);
  const cliDirOffset = optOff + (magic === 0x20b ? 112 : 96) + 14 * 8;
  if (cliDirOffset + 8 > buf.length) return false;
  return buf.readUInt32LE(cliDirOffset) > 0;
}

function getManagedReferences(dataDir) {
  return fs.readdirSync(dataDir)
    .filter((n) => n.endsWith('.dll'))
    .map((n) => path.join(dataDir, n))
    .filter((f) => isManagedDll(f));
}

function walkCs(dir) {
  if (!fs.existsSync(dir)) return [];
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walkCs(full));
    else if (entry.isFile() && entry.name.endsWith('.cs')) out.push(full);
  }
  return out;
}

function crc32(buf) {
  let crc = 0xffffffff;
  for (let i = 0; i < buf.length; i++) {
    crc ^= buf[i];
    for (let j = 0; j < 8; j++) crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function writeZip(entries, outZipPath) {
  const localParts = [];
  const centralParts = [];
  let offset = 0;
  for (const { name, data } of entries) {
    const nameBuf = Buffer.from(name, 'utf8');
    const crc = crc32(data);
    const compressed = zlib.deflateRawSync(data);
    const useDeflate = compressed.length < data.length;
    const payload = useDeflate ? compressed : data;
    const method = useDeflate ? 8 : 0;

    const lfh = Buffer.alloc(30 + nameBuf.length);
    lfh.writeUInt32LE(0x04034b50, 0);
    lfh.writeUInt16LE(20, 4);
    lfh.writeUInt16LE(0x0800, 6);
    lfh.writeUInt16LE(method, 8);
    lfh.writeUInt32LE(crc, 14);
    lfh.writeUInt32LE(payload.length, 18);
    lfh.writeUInt32LE(data.length, 22);
    lfh.writeUInt16LE(nameBuf.length, 26);
    nameBuf.copy(lfh, 30);
    localParts.push(lfh, payload);

    const cdh = Buffer.alloc(46 + nameBuf.length);
    cdh.writeUInt32LE(0x02014b50, 0);
    cdh.writeUInt16LE(20, 4);
    cdh.writeUInt16LE(20, 6);
    cdh.writeUInt16LE(0x0800, 8);
    cdh.writeUInt16LE(method, 10);
    cdh.writeUInt32LE(crc, 16);
    cdh.writeUInt32LE(payload.length, 20);
    cdh.writeUInt32LE(data.length, 24);
    cdh.writeUInt16LE(nameBuf.length, 28);
    cdh.writeUInt32LE(offset, 42);
    nameBuf.copy(cdh, 46);
    centralParts.push(cdh);
    offset += lfh.length + payload.length;
  }
  const centralSize = centralParts.reduce((s, b) => s + b.length, 0);
  const eocd = Buffer.alloc(22);
  eocd.writeUInt32LE(0x06054b50, 0);
  eocd.writeUInt16LE(entries.length, 8);
  eocd.writeUInt16LE(entries.length, 10);
  eocd.writeUInt32LE(centralSize, 12);
  eocd.writeUInt32LE(offset, 16);
  fs.mkdirSync(path.dirname(outZipPath), { recursive: true });
  fs.writeFileSync(outZipPath, Buffer.concat([...localParts, ...centralParts, eocd]));
}

const deploy = process.argv.includes('--deploy');
const sts2Dir = resolveSts2Dir();
const dataDir = path.join(sts2Dir, 'data_sts2_windows_x86_64');
const cscPath = resolveCscPath();
const manifest = JSON.parse(fs.readFileSync(path.join(MOD_ROOT, 'manifest.json'), 'utf8'));
const modId = manifest.id;
const outDir = path.join(MOD_ROOT, 'dist', modId);
fs.rmSync(outDir, { recursive: true, force: true });
fs.mkdirSync(outDir, { recursive: true });

const sharedCs = walkCs(path.join(MOD_ROOT, 'build', 'Shared'));
const modCs = walkCs(path.join(MOD_ROOT, 'src'));
const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'sts2build-'));
try {
  if (manifest.has_dll !== false && manifest.hasDll !== false) {
    const dllOut = path.join(outDir, `${modId}.dll`);
    const tfmFile = path.join(tmpDir, 'AssemblyTargetFramework.g.cs');
    fs.writeFileSync(tfmFile, '[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]\n');
    const refs = getManagedReferences(dataDir);
    const rspPath = path.join(tmpDir, 'build.rsp');
    const rspLines = [
      '-nologo', '-nostdlib+', '-noconfig', '-target:library', '-langversion:latest',
      '-nullable:enable', '-deterministic', '-optimize+', '-debug-',
      `-pathmap:${MOD_ROOT}=/src`, `-out:${dllOut}`,
      ...refs.map((r) => `-r:${r}`), ...sharedCs, ...modCs, tfmFile,
    ];
    fs.writeFileSync(rspPath, rspLines.map((l) => (l.includes(' ') ? `"${l}"` : l)).join('\n'), 'utf8');
    execFileSync(cscPath, [`@${rspPath}`], { stdio: 'inherit' });
    console.log(`Built DLL: ${dllOut}`);
  }
  fs.copyFileSync(path.join(MOD_ROOT, 'manifest.json'), path.join(outDir, `${modId}.json`));
  const pckInput = path.join(MOD_ROOT, 'pck');
  if ((manifest.has_pck || manifest.hasPck) && fs.existsSync(pckInput)) {
    const pckOut = path.join(outDir, `${modId}.pck`);
    const res = packDirectory(pckInput, pckOut, { prefix: modId });
    console.log(`Packed PCK (${res.count} files): ${pckOut}`);
  }
  const zipOut = path.join(MOD_ROOT, 'releases', `${modId}-v${manifest.version || '1.0.0'}.zip`);
  const zipEntries = fs.readdirSync(outDir).sort().map((name) => ({ name: `${modId}/${name}`, data: fs.readFileSync(path.join(outDir, name)) }));
  writeZip(zipEntries, zipOut);
  console.log(`Packaged ZIP: ${zipOut}`);
  if (deploy) {
    const deployDir = path.join(sts2Dir, 'mods', modId);
    fs.rmSync(deployDir, { recursive: true, force: true });
    fs.mkdirSync(deployDir, { recursive: true });
    for (const f of fs.readdirSync(outDir)) fs.copyFileSync(path.join(outDir, f), path.join(deployDir, f));
    console.log(`Deployed -> ${deployDir}`);
  }
} finally {
  fs.rmSync(tmpDir, { recursive: true, force: true });
}
