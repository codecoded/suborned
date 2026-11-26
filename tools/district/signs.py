"""Bake the enamel destination plates used on the Harbour shelters and pier."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / 'unity/Assets/Suborned/UI/Resources/Kit/fonts'
TARGET = ROOT / 'unity/Assets/Suborned/Materials/Play/HarbourSigns.png'
DESTINATIONS = ['Terraces', 'Lamplight', 'Rocket', 'Orbit', 'Circus', 'Market Pier', 'Skyport Landing', 'Harbour Market']
S = 2
atlas = Image.new('RGB', (1024 * S, 512 * S), '#163e42')
for index, title in enumerate(DESTINATIONS):
    field = '#19565a' if index < 5 else '#215261'
    cream, metal, accent = '#fff3d5', '#ccb77e', '#ee976f'
    plate = Image.new('RGB', (512 * S, 128 * S), field)
    pen = ImageDraw.Draw(plate)
    def line(points, ink, width=1):
        pen.line([(x * S, y * S) for x, y in points], fill=ink, width=width * S)
    for y in [5, 122]:
        line([(8, y), (504, y)], metal, 2)
    for y, length in [(51, 32), (59, 40), (67, 30)]:
        line([(12, y), (12 + length, y)], cream, 3)
    pen.ellipse((35*S, 35*S, 87*S, 87*S), outline=cream, width=3*S)
    pen.ellipse((43*S, 43*S, 79*S, 79*S), outline=metal, width=S)
    line([(46, 70), (61, 43), (65, 62), (77, 70)], accent, 4)
    line([(94, 28), (94, 100)], metal)
    label = 'HARBOUR TRAMWAYS' if index < 5 else 'HARBOUR DISTRICT'
    pen.text((113*S, 18*S), label, fill=metal, font=ImageFont.truetype(str(FONTS/'Jost-Medium.ttf'), 18*S))
    size=58
    while True:
        font=ImageFont.truetype(str(FONTS/'Jost-SemiBold.ttf'), size*S)
        if pen.textlength(title, font=font) < 377*S: break
        size -= 1
    pen.text((110*S, 45*S), title, font=font, fill=cream, anchor='lt')
    atlas.paste(plate, ((index % 2)*512*S, (index // 2)*128*S))
atlas.resize((1024, 512), Image.Resampling.LANCZOS).save(TARGET, optimize=True)
