"""Encode the app's explicit documentation-capture frames. Requires Pillow."""
from pathlib import Path
import sys
from PIL import Image

root = Path(__file__).resolve().parents[1]
source = Path(sys.argv[1]) if len(sys.argv) > 1 else root / "dist/demo-isolated/demo-frames"
output = root / "docs/images"
output.mkdir(parents=True, exist_ok=True)
for name in ("solo", "battle"):
    files = sorted((source / name).glob("*.png"))
    if len(files) < 10:
        raise RuntimeError(f"Too few frames for {name}: {len(files)}")
    frames = [Image.open(file).convert("RGB") for file in files]
    sheet = Image.new("RGB", (160 * 8, 130 * ((len(frames) + 7) // 8)))
    for i, frame in enumerate(frames):
        thumb = frame.copy()
        thumb.thumbnail((160, 130))
        sheet.paste(thumb, ((i % 8) * 160, (i // 8) * 130))
    palette = sheet.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
    indexed = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
    duration = max(20, round(6000 / len(frames) / 10) * 10)
    target = output / f"{name}.gif"
    indexed[0].save(target, save_all=True, append_images=indexed[1:], duration=duration,
                    loop=0, optimize=True, disposal=1)
    with Image.open(target) as check:
        assert check.n_frames >= 10
        print(f"{target.name}: {check.n_frames} frames, {check.size}, {target.stat().st_size} bytes")
