"""SkillTray 아이콘 생성: 클로드 스타버스트 위에 파란색 대문자 S.

사용법: python tools/make_icon.py
출력:   SkillTray/Assets/skilltray.ico, SkillTray/Assets/skilltray.png
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
OUT_DIR = ROOT / "SkillTray" / "Assets"

CLAUDE_ORANGE = (217, 119, 87, 255)   # #D97757
S_BLUE = (21, 101, 232, 255)          # #1565E8
S_OUTLINE = (255, 255, 255, 255)

SS = 4            # 슈퍼샘플링 배율
BASE = 256


def draw_starburst(draw, cx, cy, radius):
    """클로드 로고 형태의 방사형 광선(끝이 둥근 테이퍼 광선 12개)."""
    rays = 12
    inner = radius * 0.16
    for i in range(rays):
        # 광선 길이를 조금씩 달리해 손으로 그린 느낌을 살린다
        length = radius * (0.96 if i % 2 == 0 else 0.84)
        angle = math.radians(i * 360 / rays - 90 + (4 if i % 3 == 0 else 0))
        half_w_base = radius * 0.13
        half_w_tip = radius * 0.075
        dx, dy = math.cos(angle), math.sin(angle)
        px, py = -dy, dx
        bx, by = cx + dx * inner, cy + dy * inner
        tx, ty = cx + dx * length, cy + dy * length
        poly = [
            (bx + px * half_w_base, by + py * half_w_base),
            (tx + px * half_w_tip, ty + py * half_w_tip),
            (tx - px * half_w_tip, ty - py * half_w_tip),
            (bx - px * half_w_base, by - py * half_w_base),
        ]
        draw.polygon(poly, fill=CLAUDE_ORANGE)
        draw.ellipse(
            (tx - half_w_tip, ty - half_w_tip, tx + half_w_tip, ty + half_w_tip),
            fill=CLAUDE_ORANGE,
        )
    core = radius * 0.40
    draw.ellipse((cx - core, cy - core, cx + core, cy + core), fill=CLAUDE_ORANGE)


def draw_s(draw, cx, cy, size):
    font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", int(size))
    stroke = max(2, int(size * 0.07))
    bbox = draw.textbbox((0, 0), "S", font=font, stroke_width=stroke)
    w, h = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = cx - w / 2 - bbox[0]
    y = cy - h / 2 - bbox[1]
    draw.text((x, y), "S", font=font, fill=S_BLUE,
              stroke_width=stroke, stroke_fill=S_OUTLINE)


def render(size):
    big = size * SS
    img = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = big / 2
    draw_starburst(d, c, c, big * 0.5)
    draw_s(d, c, c, big * 0.60)
    return img.resize((size, size), Image.LANCZOS)


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
    frames = [render(s) for s in sizes]
    frames[-1].save(OUT_DIR / "skilltray.png")
    frames[-1].save(OUT_DIR / "skilltray.ico", format="ICO",
                    sizes=[(s, s) for s in sizes], append_images=frames[:-1])
    print("written", OUT_DIR / "skilltray.ico")


if __name__ == "__main__":
    main()
