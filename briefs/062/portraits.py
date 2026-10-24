# Glyph, brief 062: the three faces of the briefing as file photographs, from renders of the game's own figures (councillor, waitress, driver).
# The councillor is a stand-in (a young commuter model, brief 022): its hair is turned silver here; Forma owes the real Vale.
import sys; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/062")
from common import *
B = SC + "blender/"
CROPS = {"councillor": (120, 230, 780, 960), "waitress": (130, 200, 770, 900), "driver": (130, 200, 770, 900)}
def photo(name, size, silver=False):
    return file_photo(B + name + ".png", size, CROPS[name], silver_hair=silver, seed={"councillor": 3, "waitress": 5, "driver": 8}[name])
if __name__ == "__main__":
    W = Image.new("RGB", (1500, 520), (23, 48, 47))
    for i, n in enumerate(("councillor", "waitress", "driver")):
        W.paste(photo(n, (480, 520), silver=(n == "councillor")).convert("RGB"), (i * 500, 0))
    W.save(SC + "photos.png")
