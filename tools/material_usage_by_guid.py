#!/usr/bin/env python3
"""
material_usage_by_guid.py

More reliable material usage scanner: resolves each material's GUID (from its .meta)
and searches scenes/prefabs/assets for references to that GUID. Produces
`material_usage_by_guid.csv` with usage counts and file lists.

Usage:
  python tools/material_usage_by_guid.py
"""

import os
import csv
import sys
from collections import defaultdict

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ASSETS = os.path.join(ROOT, 'Assets')
INPUT = os.path.join(ROOT, 'shader_report.csv')
OUT = os.path.join(ROOT, 'material_usage_by_guid.csv')

if not os.path.exists(INPUT):
    print('shader_report.csv not found. Run tools/scan_material_shaders.py first.')
    sys.exit(1)

def read_meta_guid(asset_path):
    meta = asset_path + '.meta'
    if not os.path.exists(meta):
        return None
    try:
        with open(meta, 'r', encoding='utf-8', errors='ignore') as f:
            for line in f:
                if line.strip().startswith('guid:'):
                    return line.split(':',1)[1].strip()
    except Exception:
        return None
    return None

# load materials that have a non-empty shader_asset or name (we'll prioritize these)
materials = []
with open(INPUT, newline='', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for r in reader:
        mat = r['material']
        shader_asset = r.get('shader_asset','').strip()
        shader_name = r.get('shader_name_text','').strip()
        if shader_asset or shader_name:
            abs_mat = os.path.join(ROOT, mat.replace('/','\\'))
            guid = read_meta_guid(abs_mat)
            materials.append({
                'material': mat.replace('\\','/'),
                'abs_path': abs_mat,
                'guid': guid,
                'shader_asset': shader_asset.replace('\\','/'),
                'shader_name_text': shader_name
            })

print('Resolved GUIDs for {} prioritized materials.'.format(len(materials)))

occ = defaultdict(lambda: {'count':0, 'files':set(), 'shader_asset':'', 'shader_name_text':''})

for dirpath, dirs, files in os.walk(ASSETS):
    for fname in files:
        if fname.endswith(('.unity', '.prefab', '.asset', '.controller', '.anim', '.bytes')):
            full = os.path.join(dirpath, fname)
            try:
                with open(full, 'r', encoding='utf-8', errors='ignore') as fh:
                    data = fh.read()
            except Exception:
                continue
            for m in materials:
                guid = m['guid']
                if not guid:
                    continue
                if guid in data:
                    key = m['material']
                    occ[key]['count'] += data.count(guid)
                    occ[key]['files'].add(os.path.relpath(full, ROOT).replace('\\','/'))
                    occ[key]['shader_asset'] = m['shader_asset']
                    occ[key]['shader_name_text'] = m['shader_name_text']

with open(OUT, 'w', newline='', encoding='utf-8') as csvf:
    writer = csv.DictWriter(csvf, fieldnames=['material','guid','shader_asset','shader_name_text','usage_count','referenced_in_files'])
    writer.writeheader()
    for mat, info in sorted(occ.items(), key=lambda x: -x[1]['count']):
        # find guid from materials list
        guid = next((m['guid'] for m in materials if m['material']==mat), '')
        writer.writerow({
            'material': mat,
            'guid': guid or '',
            'shader_asset': info.get('shader_asset',''),
            'shader_name_text': info.get('shader_name_text',''),
            'usage_count': info['count'],
            'referenced_in_files': ';'.join(sorted(info['files']))
        })

print('Wrote', OUT)
top = sorted(occ.items(), key=lambda x: -x[1]['count'])[:20]
print('Top referenced materials (sample):')
for mat, info in top:
    print(info['count'], mat, '->', info.get('shader_asset') or info.get('shader_name_text'))
