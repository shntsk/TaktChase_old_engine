"""Rasterises the std::lite hit circle, approach circle and hit-circle numerals (SVG) to transparent 2x PNGs.

Usage: python tools/render_lite_skin.py <lite>/src <output dir>
Needs Python with playwright + Chromium. The artwork is rendered unchanged; the only adjustment is that the approach circle's
clip path (which Inkscape writes as <use> of a <g>, something Chromium ignores) is inlined with identical geometry.
"""
import sys, re, pathlib
from playwright.sync_api import sync_playwright
L=pathlib.Path(sys.argv[1])
OUT=pathlib.Path(sys.argv[2]); OUT.mkdir(parents=True,exist_ok=True)
jobs={'hitcircle':L/'hitcircle/normal/hitcircle.svg','approachcircle':L/'hitcircle/approachcircle.svg'}
for d in range(10): jobs[f'default-{d}']=L/f'fonts/hitcircle/numeral-{d}.svg'
SCALE=2
with sync_playwright() as p:
    b=p.chromium.launch()
    for name,path in jobs.items():
        svg=path.read_text(encoding='utf-8')
        if name=='approachcircle':
            # Chromium ignores <use> of a <g> inside <clipPath> (Inkscape allows it). Inline the two clip shapes; geometry is unchanged.
            paths=re.findall(r'<path\s+id="path3228-6".*?/>|<path\s+style="[^"]*filter:url[^>]*?/>',svg,re.S)
            clip_geo=''.join(re.sub(r'style="[^"]*"','',p).replace('id="path3228-6"','').replace('id="path3228"','') for p in paths)
            svg=re.sub(r'<use\s+x="0"\s+y="0"\s+xlink:href="#g3314"\s+id="use3320"\s*/>',clip_geo,svg,flags=re.S)
        m=re.search(r'<svg[^>]*?\swidth="([\d.]+)"[^>]*?\sheight="([\d.]+)"',svg,re.S)
        w,h=round(float(m.group(1))),round(float(m.group(2)))
        pg=b.new_page(viewport={'width':w,'height':h},device_scale_factor=SCALE)
        pg.set_content(f'<html><body style="margin:0;background:transparent"><img src="data:image/svg+xml;base64,{__import__("base64").b64encode(svg.encode()).decode()}" width="{w}" height="{h}" style="display:block"></body></html>')
        pg.wait_for_timeout(200)
        pg.screenshot(path=str(OUT/f'{name}.png'),omit_background=True,clip={'x':0,'y':0,'width':w,'height':h})
        pg.close(); print(name,w,h)
    b.close()
