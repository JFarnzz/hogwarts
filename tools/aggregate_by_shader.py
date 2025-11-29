#!/usr/bin/env python3
"""
aggregate_by_shader.py

Aggregate materials by their referenced shader asset (from shader_report.csv)
and produce `shader_aggregate.csv` with counts and material lists. Helps prioritize
which shaders to port first.

Usage:
  python tools/aggregate_by_shader.py
"""
import csv
import os
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
INPUT = os.path.join(ROOT, 'shader_report.csv')
OUT = os.path.join(ROOT, 'shader_aggregate.csv')

if not os.path.exists(INPUT):
    print('shader_report.csv not found. Run tools/scan_material_shaders.py first.')
    raise SystemExit(1)

agg = {}
with open(INPUT, newline='', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for r in reader:
        mat = r['material']
        shader_asset = r.get('shader_asset','').strip()
        shader_name = r.get('shader_name_text','').strip()
        key = shader_asset or shader_name or 'UNKNOWN'
        entry = agg.setdefault(key, {'count':0, 'materials':[]})
        entry['count'] += 1
        entry['materials'].append(mat)

with open(OUT, 'w', newline='', encoding='utf-8') as f:
    writer = csv.writer(f)
    writer.writerow(['shader_asset_or_name','count','materials_sample'])
    for k, v in sorted(agg.items(), key=lambda x: -x[1]['count']):
        sample = ';'.join(v['materials'][:10])
        writer.writerow([k, v['count'], sample])

print('Wrote', OUT)
