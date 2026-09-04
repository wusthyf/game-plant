from pathlib import Path
import sys
import math
from PIL import Image

render_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(r'E:\26翌光游戏开发\_gdd_render')
pages = sorted(render_dir.glob('page-*.png'))

for sheet_index in range(math.ceil(len(pages) / 4)):
    batch = pages[sheet_index * 4:(sheet_index + 1) * 4]
    first = Image.open(batch[0]).convert('RGB')
    canvas = Image.new('RGB', (first.width * 2, first.height * 2), 'white')
    for item_index, path in enumerate(batch):
        image = Image.open(path).convert('RGB')
        canvas.paste(image, ((item_index % 2) * first.width, (item_index // 2) * first.height))
    canvas.save(render_dir / f'sheet-{sheet_index + 1}.png')
