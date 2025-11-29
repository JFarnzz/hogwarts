#!/usr/bin/env python3
"""
scan_material_shaders.py

Scan Unity `Assets/` for .mat materials, extract shader GUIDs or shader names,
attempt to resolve GUIDs to shader asset paths by searching .meta files, and
emit a CSV report `shader_report.csv` in the repository root.

Usage (from repo root):
  python tools/scan_material_shaders.py

This script is safe to run offline (no Unity required) and is intended to give
an inventory to plan URP/HDRP shader conversions.
"""

import os
import re
import csv
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ASSETS = os.path.join(ROOT, 'Assets')
OUT_CSV = os.path.join(ROOT, 'shader_report.csv')

guid_re = re.compile(r'guid:\s*([0-9a-fA-F]+)')
shader_name_re = re.compile(r'shader:\s*"?([^"\n]+)"?')
mat_ext = '.mat'

def find_asset_for_guid(guid):
    """Search project files for a .meta file containing the guid and return the asset path."""
    for dirpath, dirs, files in os.walk(ROOT):
        for fname in files:
            if fname.endswith('.meta'):
                meta_path = os.path.join(dirpath, fname)
                try:
                    with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
                        data = f.read()
                    if guid in data:
                        # meta corresponds to an asset with same name without .meta
                        asset_path = meta_path[:-5]
                        # ensure asset file exists
                        if os.path.exists(asset_path):
                            return os.path.relpath(asset_path, ROOT)
                        else:
                            # sometimes guid in a meta for folder or binary asset; return meta-relative path
                            return os.path.relpath(asset_path, ROOT)
                except Exception:
                    continue
    return ''

def scan_material(mat_path):
    info = {
        'material': os.path.relpath(mat_path, ROOT).replace('\\', '/'),
        'shader_guid': '',
        'shader_asset': '',
        'shader_name_text': ''
    }
    try:
        with open(mat_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        # try to extract guid from m_Shader line
        m = re.search(r'm_Shader:\s*\{[^}]*guid:\s*([0-9a-fA-F]+)[^}]*\}', content)
        if m:
            guid = m.group(1)
            info['shader_guid'] = guid
            resolved = find_asset_for_guid(guid)
            info['shader_asset'] = resolved
        # fallback: look for shader: "Shader Name"
        m2 = shader_name_re.search(content)
        if m2:
            info['shader_name_text'] = m2.group(1).strip()
    except Exception as e:
        info['error'] = str(e)
    return info

def main():
    if not os.path.isdir(ASSETS):
        print('Assets/ directory not found at expected location:', ASSETS)
        sys.exit(1)

    mats = []
    for dirpath, dirs, files in os.walk(ASSETS):
        for fname in files:
            if fname.lower().endswith(mat_ext):
                mats.append(os.path.join(dirpath, fname))

    print('Found {} material files. Scanning...'.format(len(mats)))

    rows = []
    for i, mpath in enumerate(mats, 1):
        info = scan_material(mpath)
        rows.append(info)
        if i % 100 == 0:
            print('  scanned', i)

    # write CSV
    keys = ['material', 'shader_guid', 'shader_asset', 'shader_name_text']
    with open(OUT_CSV, 'w', newline='', encoding='utf-8') as csvf:
        writer = csv.DictWriter(csvf, fieldnames=keys)
        writer.writeheader()
        for r in rows:
            writer.writerow({k: r.get(k, '') for k in keys})

    print('Report written to', OUT_CSV)
    # print a small summary
    used = {}
    for r in rows:
        key = r.get('shader_asset') or r.get('shader_name_text') or 'UNKNOWN'
        used[key] = used.get(key, 0) + 1
    print('\nTop shader usages (sample):')
    for k, v in sorted(used.items(), key=lambda x: -x[1])[:20]:
        print(' ', v, k)

if __name__ == '__main__':
    main()
