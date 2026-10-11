"""make_sheet.py - 미리보기 PNG 여러 장을 하나의 시트로 합친다.
사용: python make_sheet.py out.png [--cols N] [--cell PX] Name1 Name2 ..."""
import os
import sys

import bpy
import numpy as np

d = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "previews")
args = sys.argv[1:]
out_path = args.pop(0)
cols, cell = 4, 0
while args and args[0].startswith("--"):
    key = args.pop(0)
    if key == "--cols": cols = int(args.pop(0))
    elif key == "--cell": cell = int(args.pop(0))
names = args
imgs = []
for n in names:
    im = bpy.data.images.load(os.path.join(d, n + ".png"))
    if cell:
        im.scale(cell, cell)
    w, h = im.size
    imgs.append(np.array(im.pixels[:]).reshape(h, w, 4))
cols = min(cols, len(imgs)); rows = (len(imgs) + cols - 1) // cols
h, w = imgs[0].shape[:2]
sheet = np.ones((rows * h, cols * w, 4))
for i, a in enumerate(imgs):
    r = rows - 1 - i // cols; c = i % cols
    sheet[r * h:(r + 1) * h, c * w:(c + 1) * w] = a
out = bpy.data.images.new("sheet", cols * w, rows * h)
out.pixels = sheet.ravel().tolist()
out.filepath_raw = out_path; out.file_format = "PNG"; out.save()
